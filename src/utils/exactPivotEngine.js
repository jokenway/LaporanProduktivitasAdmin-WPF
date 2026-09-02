import { compareDateStrings } from './excelParser';

/**
 * Computes exact Pivot Table for NMPG, NOINV, KDPRC, USID with Count JMDOS and Sum NETTO
 * matching the specification from "Contoh tabel.xlsx"
 */
export function buildExactPivot({
  rows = [],
  selectedDate = '',      // Filter TGBON (or 'ALL')
  selectedUser = '',      // Filter USID (or 'ALL')
  selectedJenisB = '',    // Filter JENIS_B (or 'ALL')
  searchQuery = ''
}) {
  if (!rows || rows.length === 0) {
    return {
      availableDates: [],
      availableUsers: [],
      availableJenisB: [],
      filteredGroups: [],
      totalNota: 0,
      totalCountJMDOS: 0,
      totalSumNetto: 0,
      userStats: {},
      dateStats: {},
      grandTotalNotaKeseluruhan: 0,
      grandTotalNettoKeseluruhan: 0,
    };
  }

  // Collect unique filter options from entire dataset
  const dateMap = new Map(); // date -> count distinct NOINV
  const userMap = new Map(); // user -> count distinct NOINV
  const jenisBSet = new Set();
  const allInvoicesSet = new Set();
  let grandTotalNettoAll = 0;

  // Pre-scan for all options
  rows.forEach((r) => {
    const d = String(r.TGBON || r.Tanggal || '').trim();
    const u = String(r.USID || r.User || '').trim();
    const jb = String(r.JENIS_B || r.JENIS || '').trim();
    const noinv = String(r.NOINV || r.noinv || '').trim();
    const netto = Number(String(r.NETTO || 0).replace(/[^0-9.-]+/g, '')) || 0;

    if (d) {
      if (!dateMap.has(d)) dateMap.set(d, new Set());
      if (noinv) dateMap.get(d).add(noinv);
    }
    if (u) {
      if (!userMap.has(u)) userMap.set(u, new Set());
      if (noinv) userMap.get(u).add(noinv);
    }
    if (jb) jenisBSet.add(jb);
    if (noinv) allInvoicesSet.add(noinv);
    grandTotalNettoAll += netto;
  });

  // Sort available dates chronologically (DD-MM-YYYY or YYYY-MM-DD)
  const availableDates = Array.from(dateMap.keys()).sort(compareDateStrings);

  // Sort available users by total invoices
  const availableUsers = Array.from(userMap.keys()).sort((a, b) => {
    return (userMap.get(b)?.size || 0) - (userMap.get(a)?.size || 0);
  });

  const availableJenisB = Array.from(jenisBSet).sort();

  // Apply filters: TGBON, USID, JENIS_B, and searchQuery
  const filteredRows = rows.filter((r) => {
    const d = String(r.TGBON || r.Tanggal || '').trim();
    const u = String(r.USID || r.User || '').trim();
    const jb = String(r.JENIS_B || r.JENIS || '').trim();

    if (selectedDate && selectedDate !== 'ALL' && d !== selectedDate) {
      return false;
    }
    if (selectedUser && selectedUser !== 'ALL' && u !== selectedUser) {
      return false;
    }
    if (selectedJenisB && selectedJenisB !== 'ALL' && jb !== selectedJenisB) {
      return false;
    }

    if (searchQuery) {
      const q = searchQuery.toLowerCase();
      const nmpg = String(r.NMPG || '').toLowerCase();
      const noinv = String(r.NOINV || '').toLowerCase();
      const kdprc = String(r.KDPRC || '').toLowerCase();
      const usid = String(r.USID || '').toLowerCase();
      if (!nmpg.includes(q) && !noinv.includes(q) && !kdprc.includes(q) && !usid.includes(q)) {
        return false;
      }
    }

    return true;
  });

  // Group by (NMPG, NOINV, KDPRC, USID)
  const groupMap = new Map();
  const filteredInvoicesSet = new Set();
  let totalCountJMDOS = 0;
  let totalSumNetto = 0;

  filteredRows.forEach((r) => {
    const nmpg = String(r.NMPG || '(Tanpa Sales)').trim();
    const noinv = String(r.NOINV || '(Tanpa NoInv)').trim();
    const kdprc = String(r.KDPRC || '-').trim();
    const usid = String(r.USID || '-').trim();
    const tgbon = String(r.TGBON || '').trim();
    const key = `${nmpg}|||${noinv}|||${kdprc}|||${usid}`;

    if (!groupMap.has(key)) {
      groupMap.set(key, {
        nmpg,
        noinv,
        kdprc,
        usid,
        tgbon,
        countJMDOS: 0,
        sumNetto: 0,
        rawRows: []
      });
    }

    const g = groupMap.get(key);
    g.countJMDOS += 1;
    const nettoVal = Number(String(r.NETTO || 0).replace(/[^0-9.-]+/g, '')) || 0;
    g.sumNetto += nettoVal;
    g.rawRows.push(r);

    totalCountJMDOS += 1;
    totalSumNetto += nettoVal;
    filteredInvoicesSet.add(noinv);
  });

  // Convert map to array and sort by NMPG, then NOINV
  const filteredGroups = Array.from(groupMap.values()).sort((a, b) => {
    const compNmpg = a.nmpg.localeCompare(b.nmpg);
    if (compNmpg !== 0) return compNmpg;
    return a.noinv.localeCompare(b.noinv);
  });

  // User Stats (Total Nota per User in current filtered context)
  const userStats = {};
  userMap.forEach((invSet, u) => {
    userStats[u] = invSet.size;
  });

  const dateStats = {};
  dateMap.forEach((invSet, d) => {
    dateStats[d] = invSet.size;
  });

  return {
    availableDates,
    availableUsers,
    availableJenisB,
    filteredGroups,
    totalNota: filteredGroups.length, // Each grouped row is 1 distinct NOINV nota
    totalCountJMDOS,
    totalSumNetto,
    userStats,
    dateStats,
    grandTotalNotaKeseluruhan: allInvoicesSet.size,
    grandTotalNettoKeseluruhan: grandTotalNettoAll
  };
}

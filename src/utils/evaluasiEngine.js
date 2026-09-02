/**
 * Engine untuk pengolahan Data Kinerja & Absensi Harian Admin
 * Mengkalkulasi jumlah nota per user per hari, integrasi input nota salah,
 * perhitungan otomatis nota valid, persentase akurasi, dan durasi jam kerja.
 */

import { compareDateStrings } from './excelParser';

export function calculateDuration(jamDatang, jamPulang) {
  if (!jamDatang || !jamPulang) {
    return { minutes: 0, hoursDecimal: 0, text: '-' };
  }

  const parts1 = String(jamDatang).split(':').map((v) => parseInt(v, 10));
  const parts2 = String(jamPulang).split(':').map((v) => parseInt(v, 10));

  if (parts1.length < 2 || parts2.length < 2 || isNaN(parts1[0]) || isNaN(parts1[1]) || isNaN(parts2[0]) || isNaN(parts2[1])) {
    return { minutes: 0, hoursDecimal: 0, text: '-' };
  }

  let startMinutes = parts1[0] * 60 + parts1[1];
  let endMinutes = parts2[0] * 60 + parts2[1];

  if (endMinutes < startMinutes) {
    // Penanganan shift malam (melewati jam 00:00)
    endMinutes += 24 * 60;
  }

  const diffMinutes = endMinutes - startMinutes;
  const hours = Math.floor(diffMinutes / 60);
  const mins = diffMinutes % 60;

  const hoursDecimal = parseFloat((diffMinutes / 60).toFixed(1));
  const text = mins > 0 ? `${hours} jam ${String(mins).padStart(2, '0')} mnt` : `${hours} jam`;

  return {
    minutes: diffMinutes,
    hoursDecimal,
    text
  };
}

export function formatTotalMinutes(totalMinutes) {
  if (!totalMinutes || totalMinutes <= 0) return '0 jam';
  const hours = Math.floor(totalMinutes / 60);
  const mins = totalMinutes % 60;
  if (mins === 0) return `${hours} jam`;
  return `${hours} jam ${mins} mnt`;
}

/**
 * Membangun dataset kinerja & absensi harian dari raw rows Excel dan data inputan manual.
 */
export function buildDailyEvaluationData({
  rows = [],
  selectedDate = 'ALL',
  selectedUser = 'ALL',
  searchQuery = '',
  manualLogs = {}
}) {
  if (!rows || rows.length === 0) {
    return {
      records: [],
      availableDates: [],
      availableUsers: [],
      summary: {
        totalNota: 0,
        totalSalah: 0,
        totalValid: 0,
        overallAkurasi: '100.0',
        totalJamKerjaMinutes: 0,
        totalJamKerjaText: '0 jam',
        userCount: 0,
        dateCount: 0,
        recordCount: 0
      }
    };
  }

  // 1. Grouping invoices per user per date
  const mapData = new Map(); // key: `${user}___${date}` -> Set of NOINV
  const dateSet = new Set();
  const userSet = new Set();

  rows.forEach((r) => {
    const user = String(r.USID || r.User || r['Nama Admin'] || r.NMPG || '').trim();
    const date = String(r.TGBON || r.Tanggal || r.TGL || '').trim();
    const noinv = String(r.NOINV || r.noinv || r['No Invoice'] || '').trim();

    if (!user || !date) return;

    dateSet.add(date);
    userSet.add(user);

    const key = `${user}___${date}`;
    if (!mapData.has(key)) {
      mapData.set(key, {
        user,
        date,
        invoices: new Set()
      });
    }

    if (noinv) {
      mapData.get(key).invoices.add(noinv);
    }
  });

  // Urutkan tanggal
  const availableDates = Array.from(dateSet).sort(compareDateStrings);

  // Urutkan user
  const availableUsers = Array.from(userSet).sort((a, b) => a.localeCompare(b));

  // 2. Build list of records
  const allRecords = [];

  mapData.forEach((item, key) => {
    const { user, date, invoices } = item;
    const totalNota = invoices.size;

    // Ambil data input manual yang tersimpan
    const log = manualLogs[key] || {};
    const notaSalahRaw = log.notaSalah !== undefined && log.notaSalah !== null ? log.notaSalah : 0;
    const notaSalah = Math.max(0, parseInt(notaSalahRaw, 10) || 0);

    const jamDatang = log.jamDatang || '';
    const jamPulang = log.jamPulang || '';
    const keterangan = log.keterangan || '';

    const notaValid = Math.max(0, totalNota - notaSalah);
    const akurasi = totalNota > 0 ? parseFloat(((notaValid / totalNota) * 100).toFixed(1)) : 100.0;
    const duration = calculateDuration(jamDatang, jamPulang);

    allRecords.push({
      key,
      user,
      date,
      totalNota,
      notaSalah,
      notaValid,
      akurasi,
      jamDatang,
      jamPulang,
      durasiMinutes: duration.minutes,
      durasiText: duration.text,
      durasiHoursDecimal: duration.hoursDecimal,
      keterangan
    });
  });

  // 3. Filter records
  const filteredRecords = allRecords.filter((rec) => {
    if (selectedDate && selectedDate !== 'ALL' && rec.date !== selectedDate) {
      return false;
    }
    if (selectedUser && selectedUser !== 'ALL' && rec.user !== selectedUser) {
      return false;
    }
    if (searchQuery) {
      const q = searchQuery.toLowerCase();
      const matchUser = rec.user.toLowerCase().includes(q);
      const matchDate = rec.date.toLowerCase().includes(q);
      const matchKet = rec.keterangan.toLowerCase().includes(q);
      if (!matchUser && !matchDate && !matchKet) return false;
    }
    return true;
  });

  // 4. Sort records: Tanggal (kronologis), lalu User (A-Z)
  filteredRecords.sort((a, b) => {
    const dateDiff = compareDateStrings(a.date, b.date);
    if (dateDiff !== 0) return dateDiff;
    return a.user.localeCompare(b.user);
  });

  // 5. Calculate summary statistics
  let totalNota = 0;
  let totalSalah = 0;
  let totalValid = 0;
  let totalJamKerjaMinutes = 0;
  const filteredUserSet = new Set();
  const filteredDateSet = new Set();

  filteredRecords.forEach((rec) => {
    totalNota += rec.totalNota;
    totalSalah += rec.notaSalah;
    totalValid += rec.notaValid;
    totalJamKerjaMinutes += rec.durasiMinutes;
    filteredUserSet.add(rec.user);
    filteredDateSet.add(rec.date);
  });

  const overallAkurasi = totalNota > 0 ? ((totalValid / totalNota) * 100).toFixed(1) : '100.0';

  return {
    records: filteredRecords,
    availableDates,
    availableUsers,
    summary: {
      totalNota,
      totalSalah,
      totalValid,
      overallAkurasi,
      totalJamKerjaMinutes,
      totalJamKerjaText: formatTotalMinutes(totalJamKerjaMinutes),
      userCount: filteredUserSet.size,
      dateCount: filteredDateSet.size,
      recordCount: filteredRecords.length
    }
  };
}

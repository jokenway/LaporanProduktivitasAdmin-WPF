import React, { useEffect, useMemo, useRef, useState } from 'react';
import { Users, Calendar, Trophy, TrendingUp, CheckCircle, Hash } from 'lucide-react';

export default function RekapProduktivitas({
  availableUsers = [],
  availableDates = [],
  userStats = {},
  dateStats = {},
  grandTotalNotaKeseluruhan = 0,
  rows = [],
  onSelectUser,
  onSelectDate
}) {
  // Compute full matrix [user][date] = count of unique NOINV
  const matrix = {};
  const userTotals = {};
  const dateTotals = {};

  rows.forEach((r) => {
    const u = String(r.USID || r.User || '').trim();
    const d = String(r.TGBON || r.Tanggal || '').trim();
    const noinv = String(r.NOINV || r.noinv || '').trim();

    if (!u || !d || !noinv) return;

    if (!matrix[u]) matrix[u] = {};
    if (!matrix[u][d]) matrix[u][d] = new Set();
    matrix[u][d].add(noinv);

    if (!userTotals[u]) userTotals[u] = new Set();
    userTotals[u].add(noinv);

    if (!dateTotals[d]) dateTotals[d] = new Set();
    dateTotals[d].add(noinv);
  });

  const sortedUsers = [...availableUsers].sort((a, b) => {
    return (userTotals[b]?.size || 0) - (userTotals[a]?.size || 0);
  });

  const tableRef = useRef(null);
  const [scrollTop, setScrollTop] = useState(0);
  const [viewportHeight, setViewportHeight] = useState(520);

  const ROW_HEIGHT = 42;
  const OVERSCAN = 6;

  useEffect(() => {
    if (!tableRef.current) return;

    const resizeObserver = new ResizeObserver((entries) => {
      for (const entry of entries) {
        setViewportHeight(entry.contentRect.height || 520);
      }
    });

    resizeObserver.observe(tableRef.current);
    return () => resizeObserver.disconnect();
  }, []);

  const visibleRows = useMemo(() => {
    const totalRows = sortedUsers.length;
    if (totalRows === 0) return { start: 0, end: 0, topSpacer: 0, bottomSpacer: 0, rows: [] };

    const start = Math.max(0, Math.floor(scrollTop / ROW_HEIGHT) - OVERSCAN);
    const end = Math.min(totalRows, start + Math.ceil(viewportHeight / ROW_HEIGHT) + OVERSCAN * 2);

    return {
      start,
      end,
      topSpacer: start * ROW_HEIGHT,
      bottomSpacer: (totalRows - end) * ROW_HEIGHT,
      rows: sortedUsers.slice(start, end)
    };
  }, [sortedUsers, scrollTop, viewportHeight]);

  return (
    <div className="space-y-6">
      
      {/* Top Cards: Top Performers */}
      <div className="grid grid-cols-1 sm:grid-cols-3 gap-4">
        {sortedUsers.slice(0, 3).map((user, idx) => {
          const tot = userTotals[user]?.size || 0;
          const pct = grandTotalNotaKeseluruhan > 0 ? ((tot / grandTotalNotaKeseluruhan) * 100).toFixed(1) : 0;
          const badgeColors = [
            'from-amber-500 to-yellow-600 shadow-amber-500/20',
            'from-slate-600 to-slate-800 shadow-slate-500/20',
            'from-amber-700 to-amber-900 shadow-amber-700/20'
          ];

          return (
            <div 
              key={user}
              onClick={() => onSelectUser(user)}
              className="p-4 rounded-2xl bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-800 shadow-soft cursor-pointer hover:border-brand-500 transition-all hover:scale-[1.01]"
            >
              <div className="flex items-center justify-between">
                <div className="flex items-center space-x-2">
                  <span className={`w-6 h-6 rounded-full bg-gradient-to-tr ${badgeColors[idx]} text-white text-xs font-bold flex items-center justify-center shadow-md`}>
                    {idx + 1}
                  </span>
                  <h4 className="text-sm font-bold text-slate-900 dark:text-white">
                    {user}
                  </h4>
                </div>
                <Trophy className={`w-4 h-4 ${idx === 0 ? 'text-amber-500' : 'text-slate-400'}`} />
              </div>
              <div className="mt-2">
                <div className="text-2xl font-extrabold text-slate-900 dark:text-white font-mono">
                  {tot.toLocaleString('id-ID')} <span className="text-xs font-normal text-slate-500">Nota</span>
                </div>
                <p className="text-xs text-brand-600 dark:text-brand-400 font-semibold mt-0.5">
                  {pct}% dari total keseluruhan
                </p>
              </div>
            </div>
          );
        })}
      </div>

      {/* Full Cross-tab Matrix Table */}
      <div className="bg-white dark:bg-slate-900 rounded-2xl border border-slate-200 dark:border-slate-800 shadow-soft overflow-hidden">
        <div className="p-4 border-b border-slate-100 dark:border-slate-800 flex items-center justify-between">
          <div>
            <h4 className="text-sm font-bold text-slate-900 dark:text-white">
              Rekap Matriks Jumlah Nota (User × Tanggal)
            </h4>
            <p className="text-xs text-slate-500 dark:text-slate-400">
              Klik pada nama user atau tanggal untuk melihat rincian detail faktur
            </p>
          </div>
          <div className="text-xs font-semibold text-brand-600 bg-brand-50 dark:bg-brand-950 px-2.5 py-1 rounded-lg border border-brand-200 dark:border-brand-800">
            Grand Total: {grandTotalNotaKeseluruhan.toLocaleString('id-ID')} Nota
          </div>
        </div>

        <div
          ref={tableRef}
          className="overflow-x-auto max-h-[600px] overflow-y-auto"
          onScroll={(e) => setScrollTop(e.currentTarget.scrollTop)}
        >
          <table className="w-full text-left text-xs border-collapse">
            <thead className="sticky top-0 z-20">
              <tr className="bg-slate-900 text-white font-semibold uppercase tracking-wider text-[11px]">
                <th className="py-3 px-3 text-center w-12 border-b border-slate-800 bg-slate-900 sticky left-0 z-20">
                  No
                </th>
                <th className="py-3 px-4 min-w-[160px] border-b border-slate-800 bg-slate-900 sticky left-12 z-20">
                  User Fakturis (USID)
                </th>
                {availableDates.map((d) => (
                  <th key={d} className="py-3 px-3 text-center min-w-[100px] border-b border-slate-800 border-l border-slate-800/60 font-mono text-[11px]">
                    {d}
                  </th>
                ))}
                <th className="py-3 px-4 text-center min-w-[140px] border-b border-slate-800 bg-brand-900 text-brand-100 font-bold sticky right-0 z-20">
                  TOTAL KESELURUHAN
                </th>
              </tr>
            </thead>

            <tbody className="divide-y divide-slate-100 dark:divide-slate-800 text-xs">
              {visibleRows.topSpacer > 0 && (
                <tr style={{ height: visibleRows.topSpacer }}>
                  <td colSpan={availableDates.length + 3} className="p-0" aria-hidden="true" />
                </tr>
              )}

              {visibleRows.rows.map((user, idx) => {
                const globalIndex = visibleRows.start + idx;
                const uTotal = userTotals[user]?.size || 0;
                const isEven = globalIndex % 2 === 0;

                return (
                  <tr
                    key={user}
                    className={`hover:bg-brand-50/40 dark:hover:bg-slate-800/50 transition-colors ${
                      isEven ? 'bg-white dark:bg-slate-900' : 'bg-slate-50/60 dark:bg-slate-850/40'
                    }`}
                  >
                    <td className="py-2.5 px-3 text-center font-mono text-slate-400 sticky left-0 z-10 bg-inherit border-r border-slate-100 dark:border-slate-800">
                      {globalIndex + 1}
                    </td>
                    <td 
                      onClick={() => onSelectUser(user)}
                      className="py-2.5 px-4 font-bold text-slate-900 dark:text-slate-100 sticky left-12 z-10 bg-inherit border-r border-slate-100 dark:border-slate-800 cursor-pointer hover:text-brand-600"
                    >
                      {user}
                    </td>

                    {availableDates.map((d) => {
                      const count = matrix[user]?.[d]?.size || 0;
                      return (
                        <td
                          key={d}
                          onClick={() => {
                            onSelectUser(user);
                            onSelectDate(d);
                          }}
                          className={`py-2.5 px-3 text-center font-mono text-[11px] border-l border-slate-100 dark:border-slate-800/60 cursor-pointer hover:bg-brand-100 dark:hover:bg-brand-900/30 ${
                            count > 0 ? 'font-semibold text-slate-800 dark:text-slate-200' : 'text-slate-300 dark:text-slate-600'
                          }`}
                        >
                          {count === 0 ? '-' : count.toLocaleString('id-ID')}
                        </td>
                      );
                    })}

                    <td className="py-2.5 px-4 text-center font-mono font-bold text-brand-600 dark:text-brand-400 bg-brand-50/30 dark:bg-brand-950/20 sticky right-0 z-10 border-l border-slate-200 dark:border-slate-800">
                      {uTotal.toLocaleString('id-ID')}
                    </td>
                  </tr>
                );
              })}

              {visibleRows.bottomSpacer > 0 && (
                <tr style={{ height: visibleRows.bottomSpacer }}>
                  <td colSpan={availableDates.length + 3} className="p-0" aria-hidden="true" />
                </tr>
              )}
            </tbody>

            <tfoot className="sticky bottom-0 z-20">
              <tr className="bg-slate-900 text-white font-bold text-xs border-t-2 border-slate-900">
                <td className="py-3 px-3 text-center bg-slate-900 sticky left-0 z-20">-</td>
                <td className="py-3 px-4 uppercase tracking-wider bg-slate-900 sticky left-12 z-20">
                  GRAND TOTAL (KESELURUHAN)
                </td>
                {availableDates.map((d) => {
                  const dTotal = dateTotals[d]?.size || 0;
                  return (
                    <td key={d} className="py-3 px-3 text-center font-mono text-brand-300 border-l border-slate-800">
                      {dTotal.toLocaleString('id-ID')}
                    </td>
                  );
                })}
                <td className="py-3 px-4 text-center font-mono text-sm text-white bg-brand-600 sticky right-0 z-20 shadow-inner">
                  {grandTotalNotaKeseluruhan.toLocaleString('id-ID')}
                </td>
              </tr>
            </tfoot>
          </table>
        </div>
      </div>

    </div>
  );
}

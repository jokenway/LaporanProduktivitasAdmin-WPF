import React, { useState, useEffect, useMemo, useRef } from 'react';
import { 
  Calendar, 
  User, 
  Filter, 
  Search, 
  FileSpreadsheet, 
  Printer, 
  CheckCircle2, 
  Hash, 
  DollarSign, 
  Package, 
  Layers,
  ArrowRight,
  TrendingUp,
  Sparkles,
  Users
} from 'lucide-react';
import { exportExactTableToExcel } from '../utils/exactExporter';

export default function ExactPivotView({
  exactPivotData,
  selectedDate,
  setSelectedDate,
  selectedUser,
  setSelectedUser,
  selectedJenisB,
  setSelectedJenisB,
  searchQuery,
  setSearchQuery,
  fileName
}) {
  const [isExporting, setIsExporting] = useState(false);
  const [scrollTop, setScrollTop] = useState(0);
  const [viewportHeight, setViewportHeight] = useState(480);
  const tableContainerRef = useRef(null);

  const ROW_HEIGHT = 38;
  const OVERSCAN = 8;

  const { 
    availableDates = [],
    availableUsers = [],
    availableJenisB = [],
    filteredGroups = [],
    totalNota = 0,
    totalCountJMDOS = 0,
    totalSumNetto = 0,
    userStats = {},
    dateStats = {},
    grandTotalNotaKeseluruhan = 0,
    grandTotalNettoKeseluruhan = 0,
  } = exactPivotData;

  const handleExportExcel = async () => {
    try {
      setIsExporting(true);
      const userTag = selectedUser && selectedUser !== 'ALL' ? `_${selectedUser}` : '_SEMUA_USER';
      const dateTag = selectedDate && selectedDate !== 'ALL' ? `_${selectedDate}` : '_SEMUA_TGL';
      await exportExactTableToExcel({
        filteredGroups,
        selectedUser: selectedUser === 'ALL' ? 'SEMUA USER' : selectedUser,
        selectedDate: selectedDate === 'ALL' ? 'SEMUA TANGGAL' : selectedDate,
        totalNota,
        totalCountJMDOS,
        totalSumNetto,
        filename: `Laporan_Pivot${userTag}${dateTag}.xlsx`
      });
    } catch (err) {
      alert('Gagal export Excel: ' + err.message);
    } finally {
      setIsExporting(false);
    }
  };

  const handlePrint = () => {
    window.print();
  };

  useEffect(() => {
    if (!tableContainerRef.current) return;

    const resizeObserver = new ResizeObserver((entries) => {
      for (const entry of entries) {
        setViewportHeight(entry.contentRect.height || 480);
      }
    });

    resizeObserver.observe(tableContainerRef.current);
    return () => resizeObserver.disconnect();
  }, []);

  const visibleRange = useMemo(() => {
    const totalRows = filteredGroups.length;
    if (totalRows === 0) return { start: 0, end: 0, topSpacer: 0, bottomSpacer: 0, visibleRows: [] };

    const start = Math.max(0, Math.floor(scrollTop / ROW_HEIGHT) - OVERSCAN);
    const end = Math.min(totalRows, start + Math.ceil(viewportHeight / ROW_HEIGHT) + OVERSCAN * 2);

    return {
      start,
      end,
      topSpacer: start * ROW_HEIGHT,
      bottomSpacer: (totalRows - end) * ROW_HEIGHT,
      visibleRows: filteredGroups.slice(start, end)
    };
  }, [filteredGroups, scrollTop, viewportHeight]);

  return (
    <div className="space-y-6">
      
      {/* 1. FILTER CONTROLS BAR */}
      <div className="bg-white dark:bg-slate-900 rounded-2xl border border-slate-200 dark:border-slate-800 shadow-soft p-5 transition-colors">
        <div className="flex items-center justify-between pb-4 border-b border-slate-100 dark:border-slate-800 mb-4">
          <div className="flex items-center space-x-2.5">
            <div className="w-8 h-8 rounded-xl bg-brand-500 text-white flex items-center justify-center shadow-md shadow-brand-500/20">
              <Filter className="w-4 h-4" />
            </div>
            <div>
              <h3 className="text-sm font-bold text-slate-900 dark:text-white">
                Filter Pivot (TGBON & USERID)
              </h3>
              <p className="text-xs text-slate-500 dark:text-slate-400">
                Pilih tanggal dan user untuk menghitung jumlah nota yang telah dibuat
              </p>
            </div>
          </div>

          <div className="flex items-center space-x-2">
            <button
              onClick={handleExportExcel}
              disabled={isExporting || filteredGroups.length === 0}
              className="inline-flex items-center space-x-1.5 px-3.5 py-2 rounded-xl text-xs font-semibold text-white bg-emerald-600 hover:bg-emerald-700 active:scale-95 transition-all shadow-sm disabled:opacity-50"
              title="Download Excel dengan format persis seperti Contoh tabel"
            >
              <FileSpreadsheet className="w-4 h-4" />
              <span>Export Excel Format Contoh Tabel</span>
            </button>

            <button
              onClick={handlePrint}
              className="p-2 rounded-xl text-slate-500 hover:text-slate-900 dark:hover:text-white bg-slate-100 dark:bg-slate-800 border border-slate-200 dark:border-slate-700 transition-colors no-print"
              title="Cetak Laporan"
            >
              <Printer className="w-4 h-4" />
            </button>
          </div>
        </div>

        {/* Filter Dropdowns Grid */}
        <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-4">
          
          {/* Filter Tanggal (TGBON) */}
          <div className="space-y-1.5">
            <label className="flex items-center justify-between text-xs font-semibold text-slate-700 dark:text-slate-300">
              <span className="flex items-center space-x-1.5">
                <Calendar className="w-3.5 h-3.5 text-brand-500" />
                <span>Tanggal (TGBON)</span>
              </span>
              <span className="text-[10px] text-slate-400 font-normal">
                {availableDates.length} Hari
              </span>
            </label>
            <select
              value={selectedDate}
              onChange={(e) => setSelectedDate(e.target.value)}
              className="w-full text-xs font-medium bg-slate-50 dark:bg-slate-800 border border-slate-200 dark:border-slate-700 rounded-xl px-3 py-2.5 text-slate-900 dark:text-white focus:ring-2 focus:ring-brand-500 focus:outline-none"
            >
              <option value="ALL">-- SEMUA TANGGAL --</option>
              {availableDates.map((d) => (
                <option key={d} value={d}>
                  {d} ({dateStats[d] || 0} Nota)
                </option>
              ))}
            </select>
          </div>

          {/* Filter User / Fakturis (USID) */}
          <div className="space-y-1.5">
            <label className="flex items-center justify-between text-xs font-semibold text-slate-700 dark:text-slate-300">
              <span className="flex items-center space-x-1.5">
                <User className="w-3.5 h-3.5 text-indigo-500" />
                <span>User / Fakturis (USID)</span>
              </span>
              <span className="text-[10px] text-slate-400 font-normal">
                {availableUsers.length} Admin
              </span>
            </label>
            <select
              value={selectedUser}
              onChange={(e) => setSelectedUser(e.target.value)}
              className="w-full text-xs font-medium bg-slate-50 dark:bg-slate-800 border border-slate-200 dark:border-slate-700 rounded-xl px-3 py-2.5 text-slate-900 dark:text-white focus:ring-2 focus:ring-brand-500 focus:outline-none"
            >
              <option value="ALL">-- SEMUA USER / ADMIN --</option>
              {availableUsers.map((u) => (
                <option key={u} value={u}>
                  {u} ({userStats[u] || 0} Nota)
                </option>
              ))}
            </select>
          </div>

          {/* Filter Jenis Bukti (JENIS_B) */}
          <div className="space-y-1.5">
            <label className="flex items-center space-x-1.5 text-xs font-semibold text-slate-700 dark:text-slate-300">
              <Layers className="w-3.5 h-3.5 text-purple-500" />
              <span>Jenis Bukti (JENIS_B)</span>
            </label>
            <select
              value={selectedJenisB}
              onChange={(e) => setSelectedJenisB(e.target.value)}
              className="w-full text-xs font-medium bg-slate-50 dark:bg-slate-800 border border-slate-200 dark:border-slate-700 rounded-xl px-3 py-2.5 text-slate-900 dark:text-white focus:ring-2 focus:ring-brand-500 focus:outline-none"
            >
              <option value="ALL">-- SEMUA JENIS --</option>
              {availableJenisB.map((jb) => (
                <option key={jb} value={jb}>
                  {jb}
                </option>
              ))}
            </select>
          </div>

          {/* Quick Search */}
          <div className="space-y-1.5">
            <label className="flex items-center space-x-1.5 text-xs font-semibold text-slate-700 dark:text-slate-300">
              <Search className="w-3.5 h-3.5 text-emerald-500" />
              <span>Cari No. Nota / Sales / Principal</span>
            </label>
            <div className="relative">
              <input
                type="text"
                placeholder="Ketik kata kunci..."
                value={searchQuery}
                onChange={(e) => setSearchQuery(e.target.value)}
                className="w-full text-xs bg-slate-50 dark:bg-slate-800 border border-slate-200 dark:border-slate-700 rounded-xl px-3 py-2.5 text-slate-900 dark:text-white placeholder-slate-400 focus:ring-2 focus:ring-brand-500 focus:outline-none"
              />
              {searchQuery && (
                <button
                  onClick={() => setSearchQuery('')}
                  className="absolute right-2.5 top-1/2 -translate-y-1/2 text-xs text-slate-400 hover:text-slate-600"
                >
                  ✕
                </button>
              )}
            </div>
          </div>

        </div>

        {/* Quick Date Shortcuts (Paling Baru / Hari Ini) */}
        {availableDates.length > 0 && (
          <div className="mt-4 pt-3 border-t border-slate-100 dark:border-slate-800 flex flex-wrap items-center gap-1.5 text-xs">
            <span className="text-slate-400 text-[11px] font-medium mr-1">Pilih Cepat Tanggal:</span>
            {availableDates.slice(0, 10).map((d) => (
              <button
                key={d}
                onClick={() => setSelectedDate(d)}
                className={`px-2 py-0.5 rounded-lg text-[11px] font-medium transition-colors ${
                  selectedDate === d
                    ? 'bg-brand-600 text-white font-bold'
                    : 'bg-slate-100 dark:bg-slate-800 text-slate-600 dark:text-slate-300 hover:bg-slate-200'
                }`}
              >
                {d}
              </button>
            ))}
            {availableDates.length > 10 && (
              <span className="text-slate-400 text-[11px]">+{availableDates.length - 10} lainnya</span>
            )}
          </div>
        )}
      </div>

      {/* 2. HEADER INFORMATION BLOCK (SESUAI CONTOH TABEL EXCEL) */}
      <div className="grid grid-cols-1 md:grid-cols-4 gap-4">
        
        {/* Info Fakturis & Tanggal */}
        <div className="md:col-span-2 p-4 rounded-2xl bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-800 shadow-soft">
          <div className="flex items-start justify-between">
            <div>
              <span className="text-[11px] font-bold text-brand-600 dark:text-brand-400 uppercase tracking-wider">
                Informasi Fakturis & Periode
              </span>
              <div className="mt-1.5 space-y-1">
                <div className="flex items-center space-x-2">
                  <span className="text-xs text-slate-500 font-medium w-24">Nama Fakturis:</span>
                  <span className="text-sm font-bold text-slate-900 dark:text-white bg-slate-100 dark:bg-slate-800 px-2 py-0.5 rounded-md">
                    {selectedUser && selectedUser !== 'ALL' ? selectedUser : 'SEMUA USER (KESELURUHAN)'}
                  </span>
                </div>
                <div className="flex items-center space-x-2">
                  <span className="text-xs text-slate-500 font-medium w-24">Tanggal:</span>
                  <span className="text-sm font-bold text-emerald-600 dark:text-emerald-400 bg-emerald-50 dark:bg-emerald-950/60 px-2 py-0.5 rounded-md">
                    {selectedDate && selectedDate !== 'ALL' ? selectedDate : 'SEMUA TANGGAL'}
                  </span>
                </div>
              </div>
            </div>
            <div className="w-10 h-10 rounded-xl bg-brand-50 dark:bg-brand-950 text-brand-600 dark:text-brand-400 flex items-center justify-center">
              <User className="w-5 h-5" />
            </div>
          </div>
        </div>

        {/* Total Nota Dibuat User */}
        <div className="p-4 rounded-2xl bg-gradient-to-br from-brand-600 to-indigo-700 text-white shadow-lg shadow-brand-500/20 relative overflow-hidden">
          <div className="flex items-center justify-between">
            <span className="text-[11px] font-semibold text-brand-100 uppercase tracking-wider">
              Jumlah Nota Dibuat
            </span>
            <div className="w-7 h-7 rounded-lg bg-white/20 flex items-center justify-center">
              <Hash className="w-3.5 h-3.5 text-white" />
            </div>
          </div>
          <div className="mt-2">
            <div className="text-2xl font-extrabold tracking-tight">
              {totalNota.toLocaleString('id-ID')} <span className="text-xs font-normal text-brand-200">Nota</span>
            </div>
            <p className="text-[11px] text-brand-100 mt-1">
              Dari total <b>{grandTotalNotaKeseluruhan.toLocaleString('id-ID')}</b> nota keseluruhan
            </p>
          </div>
        </div>

        {/* Total Netto & Count JMDOS */}
        <div className="p-4 rounded-2xl bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-800 shadow-soft">
          <div className="flex items-center justify-between">
            <span className="text-[11px] font-semibold text-slate-500 uppercase tracking-wider">
              Total Sum of NETTO
            </span>
            <div className="w-7 h-7 rounded-lg bg-emerald-50 dark:bg-emerald-950 text-emerald-600 flex items-center justify-center">
              <DollarSign className="w-3.5 h-3.5" />
            </div>
          </div>
          <div className="mt-2">
            <div className="text-xl font-extrabold text-slate-900 dark:text-white truncate">
              Rp {totalSumNetto.toLocaleString('id-ID', { minimumFractionDigits: 2, maximumFractionDigits: 2 })}
            </div>
            <p className="text-[11px] text-slate-500 dark:text-slate-400 mt-1">
              Total Count JMDOS: <b className="text-slate-800 dark:text-slate-200">{totalCountJMDOS.toLocaleString('id-ID')}</b>
            </p>
          </div>
        </div>

      </div>

      {/* 3. EXACT PIVOT TABLE (NMPG, NOINV, KDPRC, USID, Count JMDOS, Sum NETTO) */}
      <div className="bg-white dark:bg-slate-900 rounded-2xl border border-slate-200 dark:border-slate-800 shadow-soft overflow-hidden">
        
        {/* Table Header Bar */}
        <div className="p-4 border-b border-slate-100 dark:border-slate-800 flex items-center justify-between">
          <div className="flex items-center space-x-2">
            <h4 className="text-sm font-bold text-slate-900 dark:text-white">
              Tabel Pivot: NMPG, NOINV, KDPRC, USID
            </h4>
            <span className="text-xs font-semibold px-2.5 py-0.5 rounded-full bg-brand-50 dark:bg-brand-950 text-brand-700 dark:text-brand-300 border border-brand-200 dark:border-brand-800">
              {filteredGroups.length} Baris Nota
            </span>
          </div>

          <div className="text-xs text-slate-500 dark:text-slate-400">
            Nilai: <b>Count JMDOS</b> & <b>Sum of NETTO</b>
          </div>
        </div>

        {/* The Table */}
        <div
          ref={tableContainerRef}
          className="overflow-x-auto max-h-[600px] overflow-y-auto"
          onScroll={(e) => setScrollTop(e.currentTarget.scrollTop)}
        >
          <table className="w-full text-left text-xs border-collapse">
            
            {/* Table Header */}
            <thead className="sticky top-0 z-20">
              <tr className="bg-slate-900 text-white font-semibold uppercase tracking-wider text-[11px]">
                <th className="py-3 px-3 text-center w-12 border-b border-slate-800 bg-slate-900">
                  NO
                </th>
                <th className="py-3 px-4 min-w-[240px] border-b border-slate-800 bg-slate-900">
                  NMPG (Nama Pegawai / Sales)
                </th>
                <th className="py-3 px-3 min-w-[160px] border-b border-slate-800 bg-slate-900 font-mono">
                  NOINV (Nomor Nota)
                </th>
                <th className="py-3 px-3 text-center w-24 border-b border-slate-800 bg-slate-900 font-mono">
                  KDPRC
                </th>
                <th className="py-3 px-3 text-center w-24 border-b border-slate-800 bg-slate-900 font-mono">
                  USID
                </th>
                <th className="py-3 px-4 text-right min-w-[120px] border-b border-slate-800 bg-brand-900 text-brand-100 font-bold">
                  Count JMDOS
                </th>
                <th className="py-3 px-4 text-right min-w-[150px] border-b border-slate-800 bg-brand-900 text-brand-100 font-bold">
                  Sum of NETTO (Rp)
                </th>
              </tr>
            </thead>

            {/* Table Body */}
            <tbody className="divide-y divide-slate-100 dark:divide-slate-800 text-xs">
              {filteredGroups.length === 0 ? (
                <tr>
                  <td colSpan={7} className="py-12 text-center text-slate-400">
                    Tidak ada nota yang sesuai dengan filter tanggal/user saat ini.
                  </td>
                </tr>
              ) : (
                <>
                  {visibleRange.topSpacer > 0 && (
                    <tr style={{ height: visibleRange.topSpacer }}>
                      <td colSpan={7} className="p-0" aria-hidden="true" />
                    </tr>
                  )}

                  {visibleRange.visibleRows.map((item, idx) => {
                    const globalIndex = visibleRange.start + idx;
                    const isEven = globalIndex % 2 === 0;

                    return (
                      <tr
                        key={`${item.noinv}-${globalIndex}`}
                        className={`hover:bg-brand-50/50 dark:hover:bg-slate-800/50 transition-colors ${
                          isEven ? 'bg-white dark:bg-slate-900' : 'bg-slate-50/60 dark:bg-slate-850/40'
                        }`}
                      >
                        <td className="py-2.5 px-3 text-center font-mono text-[11px] text-slate-400 border-r border-slate-100 dark:border-slate-800">
                          {globalIndex + 1}
                        </td>
                        <td className="py-2.5 px-4 font-medium text-slate-900 dark:text-slate-200 border-r border-slate-100 dark:border-slate-800">
                          {item.nmpg}
                        </td>
                        <td className="py-2.5 px-3 font-mono text-slate-800 dark:text-slate-200 font-semibold border-r border-slate-100 dark:border-slate-800">
                          {item.noinv}
                        </td>
                        <td className="py-2.5 px-3 text-center font-mono text-slate-600 dark:text-slate-400 border-r border-slate-100 dark:border-slate-800">
                          <span className="px-1.5 py-0.5 rounded bg-slate-100 dark:bg-slate-800 font-semibold text-[10px]">
                            {item.kdprc}
                          </span>
                        </td>
                        <td className="py-2.5 px-3 text-center font-mono font-bold text-brand-600 dark:text-brand-400 border-r border-slate-100 dark:border-slate-800">
                          {item.usid}
                        </td>
                        <td className="py-2.5 px-4 text-right font-mono font-medium text-slate-700 dark:text-slate-300 border-r border-slate-100 dark:border-slate-800">
                          {item.countJMDOS.toLocaleString('id-ID')}
                        </td>
                        <td className="py-2.5 px-4 text-right font-mono font-bold text-slate-900 dark:text-white">
                          {item.sumNetto.toLocaleString('id-ID', { minimumFractionDigits: 2, maximumFractionDigits: 2 })}
                        </td>
                      </tr>
                    );
                  })}

                  {visibleRange.bottomSpacer > 0 && (
                    <tr style={{ height: visibleRange.bottomSpacer }}>
                      <td colSpan={7} className="p-0" aria-hidden="true" />
                    </tr>
                  )}
                </>
              )}
            </tbody>

            {/* Table Footer: GRAND TOTAL */}
            <tfoot className="sticky bottom-0 z-20">
              <tr className="bg-slate-900 text-white font-bold text-xs border-t-2 border-slate-900">
                <td className="py-3 px-3 text-center bg-slate-900">
                  -
                </td>
                <td className="py-3 px-4 uppercase tracking-wider bg-slate-900">
                  TOTAL NOTA
                </td>
                <td className="py-3 px-3 font-mono text-brand-300 bg-slate-900">
                  {totalNota.toLocaleString('id-ID')} Nota
                </td>
                <td className="py-3 px-3 text-center bg-slate-900">-</td>
                <td className="py-3 px-3 text-center bg-slate-900">-</td>
                <td className="py-3 px-4 text-right font-mono text-brand-300 bg-brand-950 border-l border-slate-800">
                  {totalCountJMDOS.toLocaleString('id-ID')}
                </td>
                <td className="py-3 px-4 text-right font-mono text-sm text-emerald-400 bg-brand-950 border-l border-slate-800">
                  Rp {totalSumNetto.toLocaleString('id-ID', { minimumFractionDigits: 2, maximumFractionDigits: 2 })}
                </td>
              </tr>
            </tfoot>

          </table>
        </div>

        {/* Footer Notes */}
        <div className="p-3 bg-slate-50 dark:bg-slate-850/70 border-t border-slate-100 dark:border-slate-800 text-[11px] text-slate-500 dark:text-slate-400 flex flex-wrap items-center justify-between gap-2">
          <div className="flex items-center space-x-1.5">
            <CheckCircle2 className="w-3.5 h-3.5 text-emerald-500" />
            <span>Format tabel ter-pivot sesuai kolom dan baris dari <b>Contoh tabel.xlsx</b></span>
          </div>
          <div>
            Total keseluruhan seluruh periode: <b>{grandTotalNotaKeseluruhan.toLocaleString('id-ID')} Nota</b>
          </div>
        </div>

      </div>

    </div>
  );
}

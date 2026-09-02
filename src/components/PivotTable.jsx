import React, { useState } from 'react';
import { 
  Table, 
  Download, 
  FileSpreadsheet, 
  FileText, 
  Printer, 
  Search, 
  Eye, 
  Sparkles,
  ArrowUpDown,
  Filter,
  CheckCircle2,
  TrendingUp,
  LayoutGrid,
  List
} from 'lucide-react';
import { exportPivotToExcel, exportPivotToPDF } from '../utils/exporter';

export default function PivotTable({
  pivotResult,
  config,
  fileName = 'Data_Excel'
}) {
  const [viewMode, setViewMode] = useState('matrix'); // 'matrix' | 'flat'
  const [enableHeatmap, setEnableHeatmap] = useState(true);
  const [localSearch, setLocalSearch] = useState('');
  const [isExporting, setIsExporting] = useState(false);

  const {
    columns = [],
    rowKeys = [],
    matrix = {},
    rowTotals = {},
    colTotals = {},
    grandTotal = 0,
    flatData = [],
    summaryStats = {}
  } = pivotResult || {};

  const {
    rowField = 'User',
    colField = 'Tanggal',
    valField = 'NoInv',
    aggType = 'COUNT'
  } = config;

  // Filter rows locally if search is active
  const filteredRowKeys = rowKeys.filter(r => 
    r.toLowerCase().includes(localSearch.toLowerCase())
  );

  // Find max value in matrix for heatmap intensity calculation
  let maxCellValue = 1;
  rowKeys.forEach(r => {
    columns.forEach(c => {
      const v = matrix[r]?.[c] || 0;
      if (v > maxCellValue) maxCellValue = v;
    });
  });

  const getHeatmapClass = (val) => {
    if (!enableHeatmap || val === 0 || !val) return '';
    const ratio = val / maxCellValue;
    if (ratio > 0.75) return 'bg-emerald-500/20 text-emerald-950 dark:text-emerald-300 font-semibold';
    if (ratio > 0.45) return 'bg-emerald-500/10 text-slate-800 dark:text-emerald-400';
    if (ratio > 0.15) return 'bg-blue-500/5 text-slate-700 dark:text-slate-300';
    return '';
  };

  const handleExportExcel = async () => {
    try {
      setIsExporting(true);
      const cleanName = fileName.replace(/\.[^/.]+$/, '');
      await exportPivotToExcel({
        pivotResult,
        config,
        filename: `Laporan_Produktivitas_${cleanName}.xlsx`
      });
    } catch (err) {
      alert('Gagal export Excel: ' + err.message);
    } finally {
      setIsExporting(false);
    }
  };

  const handleExportPDF = () => {
    try {
      const cleanName = fileName.replace(/\.[^/.]+$/, '');
      exportPivotToPDF({
        pivotResult,
        config,
        filename: `Laporan_Produktivitas_${cleanName}.pdf`
      });
    } catch (err) {
      alert('Gagal export PDF: ' + err.message);
    }
  };

  const handlePrint = () => {
    window.print();
  };

  return (
    <div className="bg-white dark:bg-slate-900 rounded-2xl border border-slate-200 dark:border-slate-800 shadow-soft overflow-hidden mb-8 transition-colors">
      
      {/* Table Action Bar */}
      <div className="p-4 sm:p-5 border-b border-slate-100 dark:border-slate-800 flex flex-col md:flex-row md:items-center justify-between gap-4">
        
        {/* Left Title & Indicator */}
        <div>
          <div className="flex items-center space-x-2">
            <h3 className="text-base font-bold text-slate-900 dark:text-white flex items-center space-x-2">
              <span>Tabel Pivot Produktivitas</span>
            </h3>
            <span className="text-xs font-semibold px-2 py-0.5 rounded-full bg-emerald-50 dark:bg-emerald-950/80 text-emerald-700 dark:text-emerald-400 border border-emerald-200 dark:border-emerald-800">
              {filteredRowKeys.length} Admin
            </span>
          </div>
          <p className="text-xs text-slate-500 dark:text-slate-400 mt-0.5">
            Total <b>{valField || 'No. Invoice'}</b> dikelompokkan berdasarkan <b>{rowField}</b> per <b>{colField || 'Semua Tanggal'}</b>
          </p>
        </div>

        {/* Right Tools: Search, View Mode, Export Buttons */}
        <div className="flex flex-wrap items-center gap-2.5">
          
          {/* Local Search Input */}
          <div className="relative">
            <Search className="w-3.5 h-3.5 text-slate-400 absolute left-3 top-1/2 -translate-y-1/2" />
            <input
              type="text"
              placeholder="Cari admin..."
              value={localSearch}
              onChange={(e) => setLocalSearch(e.target.value)}
              className="text-xs bg-slate-50 dark:bg-slate-800 border border-slate-200 dark:border-slate-700 rounded-xl pl-8 pr-3 py-1.5 text-slate-800 dark:text-slate-200 placeholder-slate-400 focus:ring-2 focus:ring-brand-500 focus:outline-none w-36 sm:w-44 transition-all"
            />
          </div>

          {/* View Toggle (Matrix vs Flat) */}
          <div className="flex items-center bg-slate-100 dark:bg-slate-800 p-0.5 rounded-xl border border-slate-200 dark:border-slate-700">
            <button
              onClick={() => setViewMode('matrix')}
              className={`inline-flex items-center space-x-1 px-2.5 py-1 rounded-lg text-xs font-medium transition-all ${
                viewMode === 'matrix'
                  ? 'bg-white dark:bg-slate-900 text-brand-600 dark:text-brand-400 shadow-sm font-bold'
                  : 'text-slate-600 dark:text-slate-400 hover:text-slate-900 dark:hover:text-white'
              }`}
              title="Tampilan Matriks (Cross-tab Tanggal)"
            >
              <LayoutGrid className="w-3.5 h-3.5" />
              <span className="hidden sm:inline">Matriks</span>
            </button>
            <button
              onClick={() => setViewMode('flat')}
              className={`inline-flex items-center space-x-1 px-2.5 py-1 rounded-lg text-xs font-medium transition-all ${
                viewMode === 'flat'
                  ? 'bg-white dark:bg-slate-900 text-brand-600 dark:text-brand-400 shadow-sm font-bold'
                  : 'text-slate-600 dark:text-slate-400 hover:text-slate-900 dark:hover:text-white'
              }`}
              title="Tampilan List / Rincian"
            >
              <List className="w-3.5 h-3.5" />
              <span className="hidden sm:inline">Rincian</span>
            </button>
          </div>

          {/* Heatmap Toggle */}
          {viewMode === 'matrix' && columns.length > 0 && (
            <button
              onClick={() => setEnableHeatmap(!enableHeatmap)}
              className={`px-2.5 py-1.5 rounded-xl text-xs font-medium border transition-colors ${
                enableHeatmap
                  ? 'bg-emerald-50 dark:bg-emerald-950 text-emerald-700 dark:text-emerald-300 border-emerald-200 dark:border-emerald-800'
                  : 'bg-slate-50 dark:bg-slate-800 text-slate-500 border-slate-200 dark:border-slate-700'
              }`}
              title="Tandai intensitas angka produktivitas"
            >
              Heatmap
            </button>
          )}

          {/* Export Excel Button */}
          <button
            onClick={handleExportExcel}
            disabled={isExporting}
            className="inline-flex items-center space-x-1.5 px-3 py-1.5 rounded-xl text-xs font-semibold text-white bg-emerald-600 hover:bg-emerald-700 shadow-sm transition-all"
            title="Download file Excel .xlsx lengkap dengan format warna & total"
          >
            <FileSpreadsheet className="w-3.5 h-3.5" />
            <span>Excel (.xlsx)</span>
          </button>

          {/* Export PDF Button */}
          <button
            onClick={handleExportPDF}
            className="inline-flex items-center space-x-1.5 px-3 py-1.5 rounded-xl text-xs font-semibold text-slate-700 dark:text-slate-200 bg-slate-100 hover:bg-slate-200 dark:bg-slate-800 dark:hover:bg-slate-700 border border-slate-200 dark:border-slate-700 transition-all"
            title="Download dokumen PDF"
          >
            <FileText className="w-3.5 h-3.5 text-rose-500" />
            <span>PDF</span>
          </button>

          {/* Print Button */}
          <button
            onClick={handlePrint}
            className="p-1.5 rounded-xl text-slate-500 hover:text-slate-900 dark:hover:text-white bg-slate-100 dark:bg-slate-800 border border-slate-200 dark:border-slate-700 transition-colors no-print"
            title="Cetak Laporan"
          >
            <Printer className="w-3.5 h-3.5" />
          </button>

        </div>
      </div>

      {/* MATRIX TABLE VIEW */}
      {viewMode === 'matrix' && (
        <div className="overflow-x-auto">
          <table className="w-full text-left border-collapse">
            
            {/* Table Header */}
            <thead>
              <tr className="bg-slate-900 text-white text-xs font-semibold uppercase tracking-wider">
                <th className="py-3.5 px-3 text-center w-12 border-b border-slate-800 sticky left-0 z-20 bg-slate-900">
                  No
                </th>
                <th className="py-3.5 px-4 min-w-[180px] border-b border-slate-800 sticky left-12 z-20 bg-slate-900">
                  {rowField || 'Nama Admin'}
                </th>
                
                {/* Column Headers (Dates) */}
                {columns.map((colKey) => (
                  <th key={colKey} className="py-3.5 px-3 text-center min-w-[110px] border-b border-slate-800 border-l border-slate-800/60 font-mono text-[11px]">
                    {colKey}
                  </th>
                ))}

                {/* Total Column Header */}
                <th className="py-3.5 px-4 text-center min-w-[140px] border-b border-slate-800 border-l border-slate-800 bg-brand-900/90 text-brand-100 font-bold sticky right-0 z-20">
                  TOTAL KESELURUHAN
                </th>
              </tr>
            </thead>

            {/* Table Body */}
            <tbody className="divide-y divide-slate-200/70 dark:divide-slate-800/70 text-xs">
              {filteredRowKeys.length === 0 ? (
                <tr>
                  <td colSpan={columns.length + 3} className="py-12 text-center text-slate-500 dark:text-slate-400">
                    Tidak ada data yang sesuai dengan pencarian atau filter saat ini.
                  </td>
                </tr>
              ) : (
                filteredRowKeys.map((rKey, idx) => {
                  const rTotal = rowTotals[rKey] ?? 0;
                  const isEven = idx % 2 === 0;

                  return (
                    <tr 
                      key={rKey} 
                      className={`hover:bg-brand-50/40 dark:hover:bg-slate-800/50 transition-colors ${
                        isEven ? 'bg-white dark:bg-slate-900' : 'bg-slate-50/50 dark:bg-slate-850/40'
                      }`}
                    >
                      {/* Index No */}
                      <td className="py-3 px-3 text-center text-slate-400 font-mono text-[11px] sticky left-0 z-10 bg-inherit border-r border-slate-100 dark:border-slate-800/80">
                        {idx + 1}
                      </td>

                      {/* Row Label (Admin Name) */}
                      <td className="py-3 px-4 font-semibold text-slate-900 dark:text-slate-100 sticky left-12 z-10 bg-inherit border-r border-slate-100 dark:border-slate-800/80">
                        <div className="flex items-center space-x-2">
                          <div className="w-2 h-2 rounded-full bg-brand-500" />
                          <span className="truncate">{rKey}</span>
                        </div>
                      </td>

                      {/* Date Cells */}
                      {columns.map((cKey) => {
                        const cellVal = matrix[rKey]?.[cKey] ?? 0;
                        const heatmapClass = getHeatmapClass(cellVal);

                        return (
                          <td 
                            key={cKey} 
                            className={`py-3 px-3 text-center border-l border-slate-100 dark:border-slate-800/60 font-mono text-[11px] transition-colors ${heatmapClass}`}
                          >
                            {cellVal === 0 ? (
                              <span className="text-slate-300 dark:text-slate-600">-</span>
                            ) : (
                              <span className="font-medium text-slate-800 dark:text-slate-200">
                                {cellVal.toLocaleString('id-ID')}
                              </span>
                            )}
                          </td>
                        );
                      })}

                      {/* Row Total (Subtotal User) */}
                      <td className="py-3 px-4 text-center font-bold font-mono text-xs text-brand-600 dark:text-brand-400 bg-brand-50/30 dark:bg-brand-950/20 border-l border-slate-200 dark:border-slate-800 sticky right-0 z-10">
                        {rTotal.toLocaleString('id-ID')}
                      </td>
                    </tr>
                  );
                })
              )}
            </tbody>

            {/* Table Footer: GRAND TOTAL KESELURUHAN */}
            <tfoot>
              <tr className="bg-slate-900 text-white font-bold text-xs border-t-2 border-slate-900 sticky bottom-0 z-20">
                <td className="py-3.5 px-3 text-center sticky left-0 z-20 bg-slate-900">
                  -
                </td>
                <td className="py-3.5 px-4 uppercase tracking-wider sticky left-12 z-20 bg-slate-900">
                  GRAND TOTAL (KESELURUHAN)
                </td>

                {/* Column Totals */}
                {columns.map((cKey) => {
                  const cTotal = colTotals[cKey] ?? 0;
                  return (
                    <td key={cKey} className="py-3.5 px-3 text-center font-mono text-xs text-brand-300 border-l border-slate-800">
                      {cTotal.toLocaleString('id-ID')}
                    </td>
                  );
                })}

                {/* Overall Grand Total */}
                <td className="py-3.5 px-4 text-center font-mono text-sm text-white bg-brand-600 border-l border-slate-800 sticky right-0 z-20 shadow-inner">
                  {grandTotal.toLocaleString('id-ID')}
                </td>
              </tr>
            </tfoot>

          </table>
        </div>
      )}

      {/* FLAT GROUPED VIEW */}
      {viewMode === 'flat' && (
        <div className="overflow-x-auto">
          <table className="w-full text-left border-collapse">
            <thead>
              <tr className="bg-slate-900 text-white text-xs font-semibold uppercase tracking-wider">
                <th className="py-3 px-4 text-center w-14">No</th>
                <th className="py-3 px-4">{rowField}</th>
                {colField && <th className="py-3 px-4">{colField}</th>}
                <th className="py-3 px-4 text-center">{aggType} {valField || 'No. Invoice'}</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-slate-200 dark:divide-slate-800 text-xs">
              {flatData.map((item, idx) => (
                <tr key={idx} className="hover:bg-slate-50 dark:hover:bg-slate-800/50">
                  <td className="py-2.5 px-4 text-center text-slate-400 font-mono">{idx + 1}</td>
                  <td className="py-2.5 px-4 font-semibold text-slate-800 dark:text-slate-200">{item[rowField]}</td>
                  {colField && <td className="py-2.5 px-4 font-mono text-slate-600 dark:text-slate-400">{item[colField]}</td>}
                  <td className="py-2.5 px-4 text-center font-mono font-bold text-brand-600 dark:text-brand-400">
                    {(item[valField || 'Jumlah'] || 0).toLocaleString('id-ID')}
                  </td>
                </tr>
              ))}
            </tbody>
            <tfoot>
              <tr className="bg-slate-900 text-white font-bold text-xs">
                <td colSpan={colField ? 3 : 2} className="py-3 px-4 text-right uppercase">
                  Grand Total Keseluruhan:
                </td>
                <td className="py-3 px-4 text-center font-mono text-sm text-brand-300">
                  {grandTotal.toLocaleString('id-ID')}
                </td>
              </tr>
            </tfoot>
          </table>
        </div>
      )}

      {/* Footer Info & Formula Note */}
      <div className="p-3 bg-slate-50 dark:bg-slate-850/60 border-t border-slate-100 dark:border-slate-800 text-[11px] text-slate-500 dark:text-slate-400 flex flex-wrap items-center justify-between gap-2">
        <div className="flex items-center space-x-2">
          <CheckCircle2 className="w-3.5 h-3.5 text-emerald-500" />
          <span>
            Formula: <b>{aggType}</b> dari <b>{valField || 'No. Invoice'}</b> per User per Tanggal + Grand Total otomatis.
          </span>
        </div>
        <div className="text-slate-400">
          Klik tombol <b>Excel (.xlsx)</b> di atas untuk mengunduh laporan berformat rapi.
        </div>
      </div>

    </div>
  );
}

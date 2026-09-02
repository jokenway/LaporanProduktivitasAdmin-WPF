import React, { useState } from 'react';
import { 
  Sliders, 
  Rows, 
  Columns, 
  Calculator, 
  Calendar, 
  ArrowUpDown, 
  Filter, 
  Sparkles, 
  ChevronDown, 
  ChevronUp,
  Bookmark,
  Check
} from 'lucide-react';

export default function FieldSelector({
  headers = [],
  config,
  onChangeConfig,
  onApplyPreset,
  availableFilters = {},
  activeFilters = {},
  onChangeFilter,
  dateRange,
  onChangeDateRange,
  onResetFilters
}) {
  const [isExpanded, setIsExpanded] = useState(true);
  const [showFilterModal, setShowFilterModal] = useState(false);

  const {
    rowField = '',
    colField = '',
    valField = '',
    aggType = 'COUNT',
    dateFormat = 'daily',
    sortBy = 'total_desc',
  } = config;

  return (
    <div className="bg-white dark:bg-slate-900 rounded-2xl border border-slate-200 dark:border-slate-800 shadow-soft p-5 mb-6 transition-all">
      
      {/* Header & Toggle */}
      <div className="flex flex-wrap items-center justify-between gap-3 pb-4 border-b border-slate-100 dark:border-slate-800">
        <div className="flex items-center space-x-2.5">
          <div className="w-8 h-8 rounded-lg bg-brand-50 dark:bg-brand-950 text-brand-600 dark:text-brand-400 flex items-center justify-center">
            <Sliders className="w-4 h-4" />
          </div>
          <div>
            <h3 className="text-sm font-bold text-slate-900 dark:text-white">
              Konfigurasi Field & Dimensi Pivot
            </h3>
            <p className="text-xs text-slate-500 dark:text-slate-400">
              Tentukan field Excel untuk Baris, Kolom Tanggal, dan Nilai Perhitungan
            </p>
          </div>
        </div>

        {/* Quick 1-Click Presets */}
        <div className="flex items-center space-x-2">
          <button
            onClick={() => onApplyPreset('user_date_noinv')}
            className="inline-flex items-center space-x-1.5 px-3 py-1.5 rounded-lg text-xs font-semibold bg-brand-50 hover:bg-brand-100 dark:bg-brand-950 dark:hover:bg-brand-900 text-brand-700 dark:text-brand-300 border border-brand-200 dark:border-brand-800 transition-colors"
            title="Preset otomatis: Hitung NoInv per User per Tanggal"
          >
            <Sparkles className="w-3.5 h-3.5 text-brand-500" />
            <span>Preset: NoInv per User & Tanggal</span>
          </button>

          <button
            onClick={() => setIsExpanded(!isExpanded)}
            className="p-1.5 text-slate-400 hover:text-slate-600 dark:hover:text-slate-200 rounded-lg hover:bg-slate-100 dark:hover:bg-slate-800 transition-colors"
          >
            {isExpanded ? <ChevronUp className="w-4 h-4" /> : <ChevronDown className="w-4 h-4" />}
          </button>
        </div>
      </div>

      {/* Main Selectors Grid */}
      {isExpanded && (
        <div className="pt-4">
          <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-4">
            
            {/* 1. ROW FIELD (BARIS) */}
            <div className="space-y-1.5">
              <label className="flex items-center space-x-1.5 text-xs font-semibold text-slate-700 dark:text-slate-300">
                <Rows className="w-3.5 h-3.5 text-brand-500" />
                <span>Field Baris (User / Admin)</span>
              </label>
              <select
                value={rowField}
                onChange={(e) => onChangeConfig({ rowField: e.target.value })}
                className="w-full text-xs font-medium bg-slate-50 dark:bg-slate-800 border border-slate-200 dark:border-slate-700 rounded-xl px-3 py-2.5 text-slate-900 dark:text-white focus:ring-2 focus:ring-brand-500 focus:border-brand-500 transition-colors"
              >
                <option value="">-- Pilih Field Baris --</option>
                {headers.map((h) => (
                  <option key={h} value={h}>
                    {h}
                  </option>
                ))}
              </select>
            </div>

            {/* 2. COLUMN FIELD (KOLOM / TANGGAL) */}
            <div className="space-y-1.5">
              <label className="flex items-center space-x-1.5 text-xs font-semibold text-slate-700 dark:text-slate-300">
                <Columns className="w-3.5 h-3.5 text-indigo-500" />
                <span>Field Kolom (Tanggal / Periode)</span>
              </label>
              <select
                value={colField}
                onChange={(e) => onChangeConfig({ colField: e.target.value })}
                className="w-full text-xs font-medium bg-slate-50 dark:bg-slate-800 border border-slate-200 dark:border-slate-700 rounded-xl px-3 py-2.5 text-slate-900 dark:text-white focus:ring-2 focus:ring-brand-500 focus:border-brand-500 transition-colors"
              >
                <option value="">(Tanpa Kolom Matriks - Flat List)</option>
                {headers.map((h) => (
                  <option key={h} value={h}>
                    {h}
                  </option>
                ))}
              </select>
            </div>

            {/* 3. VALUE FIELD (NILAI / NOINV) */}
            <div className="space-y-1.5">
              <label className="flex items-center space-x-1.5 text-xs font-semibold text-slate-700 dark:text-slate-300">
                <Calculator className="w-3.5 h-3.5 text-emerald-500" />
                <span>Field Nilai (No. Invoice / Nominal)</span>
              </label>
              <select
                value={valField}
                onChange={(e) => onChangeConfig({ valField: e.target.value })}
                className="w-full text-xs font-medium bg-slate-50 dark:bg-slate-800 border border-slate-200 dark:border-slate-700 rounded-xl px-3 py-2.5 text-slate-900 dark:text-white focus:ring-2 focus:ring-brand-500 focus:border-brand-500 transition-colors"
              >
                <option value="">(Hitung Semua Baris / Count Rows)</option>
                {headers.map((h) => (
                  <option key={h} value={h}>
                    {h}
                  </option>
                ))}
              </select>
            </div>

            {/* 4. AGGREGATION TYPE */}
            <div className="space-y-1.5">
              <label className="flex items-center space-x-1.5 text-xs font-semibold text-slate-700 dark:text-slate-300">
                <Calculator className="w-3.5 h-3.5 text-amber-500" />
                <span>Rumus Agregasi (Kalkulasi)</span>
              </label>
              <select
                value={aggType}
                onChange={(e) => onChangeConfig({ aggType: e.target.value })}
                className="w-full text-xs font-medium bg-slate-50 dark:bg-slate-800 border border-slate-200 dark:border-slate-700 rounded-xl px-3 py-2.5 text-slate-900 dark:text-white focus:ring-2 focus:ring-brand-500 focus:border-brand-500 transition-colors"
              >
                <option value="COUNT">COUNT (Jumlah Total Transaksi)</option>
                <option value="DISTINCT_COUNT">DISTINCT COUNT (Jumlah No. Invoice Unik)</option>
                <option value="SUM">SUM (Total Nominal / Penjumlahan)</option>
                <option value="AVG">AVG (Rata-rata)</option>
                <option value="MAX">MAX (Nilai Tertinggi)</option>
                <option value="MIN">MIN (Nilai Terendah)</option>
              </select>
            </div>

          </div>

          {/* Secondary Controls: Date Grouping, Sorting, Date Range Filters */}
          <div className="mt-4 pt-4 border-t border-slate-100 dark:border-slate-800/80 flex flex-wrap items-center justify-between gap-3 text-xs">
            
            <div className="flex flex-wrap items-center gap-3">
              {/* Date format selector (if colField has date) */}
              {colField && (
                <div className="flex items-center space-x-1.5 bg-slate-100 dark:bg-slate-800/60 px-2.5 py-1.5 rounded-lg">
                  <Calendar className="w-3.5 h-3.5 text-slate-500" />
                  <span className="text-slate-600 dark:text-slate-400 font-medium">Periode:</span>
                  <select
                    value={dateFormat}
                    onChange={(e) => onChangeConfig({ dateFormat: e.target.value })}
                    className="bg-transparent border-0 text-xs font-semibold text-slate-800 dark:text-slate-200 focus:ring-0 cursor-pointer p-0"
                  >
                    <option value="daily" className="bg-white dark:bg-slate-900">Harian (YYYY-MM-DD)</option>
                    <option value="monthly" className="bg-white dark:bg-slate-900">Bulanan (YYYY-MM)</option>
                    <option value="raw" className="bg-white dark:bg-slate-900">Sesuai Nilai Asli</option>
                  </select>
                </div>
              )}

              {/* Sort Order */}
              <div className="flex items-center space-x-1.5 bg-slate-100 dark:bg-slate-800/60 px-2.5 py-1.5 rounded-lg">
                <ArrowUpDown className="w-3.5 h-3.5 text-slate-500" />
                <span className="text-slate-600 dark:text-slate-400 font-medium">Urutan:</span>
                <select
                  value={sortBy}
                  onChange={(e) => onChangeConfig({ sortBy: e.target.value })}
                  className="bg-transparent border-0 text-xs font-semibold text-slate-800 dark:text-slate-200 focus:ring-0 cursor-pointer p-0"
                >
                  <option value="total_desc" className="bg-white dark:bg-slate-900">Total Tertinggi → Terendah</option>
                  <option value="total_asc" className="bg-white dark:bg-slate-900">Total Terendah → Tertinggi</option>
                  <option value="row_asc" className="bg-white dark:bg-slate-900">Nama Admin (A → Z)</option>
                  <option value="row_desc" className="bg-white dark:bg-slate-900">Nama Admin (Z → A)</option>
                </select>
              </div>
            </div>

            {/* Current Active Selection Summary */}
            <div className="text-slate-500 dark:text-slate-400 text-[11px] font-medium flex items-center space-x-1">
              <span>Matriks:</span>
              <span className="font-semibold text-slate-800 dark:text-slate-200">
                [{rowField || 'Pilih Baris'}] × [{colField || 'Semua Tanggal'}]
              </span>
              <span>→ Hitung:</span>
              <span className="font-semibold text-brand-600 dark:text-brand-400">
                {aggType} [{valField || 'Baris'}]
              </span>
            </div>

          </div>

        </div>
      )}

    </div>
  );
}

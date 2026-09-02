import React from 'react';
import { 
  FileSpreadsheet, 
  Moon, 
  Sun, 
  Table2, 
  Sparkles, 
  RefreshCw, 
  FileDown, 
  Layers
} from 'lucide-react';

export default function Header({
  darkMode,
  setDarkMode,
  hasData,
  fileName,
  sheetNames = [],
  activeSheet,
  onSheetChange,
  rowCount,
  onOpenRawData,
  onLoadSample,
  onReset
}) {
  return (
    <header className="sticky top-0 z-30 border-b border-slate-200 dark:border-slate-800 bg-white/90 dark:bg-slate-900/90 backdrop-blur-md transition-colors">
      <div className="max-w-7xl mx-auto px-3 sm:px-6 lg:px-8">
        <div className="flex flex-col gap-3 py-3 sm:flex-row sm:items-center sm:justify-between sm:h-16">
          
          {/* Logo & App Name */}
          <div className="flex items-center space-x-3 min-w-0">
            <div className="w-10 h-10 rounded-xl bg-gradient-to-tr from-brand-600 to-indigo-500 flex items-center justify-center text-white shadow-lg shadow-brand-500/25 shrink-0">
              <FileSpreadsheet className="w-5 h-5" />
            </div>
            <div className="min-w-0">
              <div className="flex flex-wrap items-center gap-1.5 sm:gap-2">
                <h1 className="text-base sm:text-lg font-bold tracking-tight text-slate-900 dark:text-white truncate">
                  Laporan Produktivitas Admin
                </h1>
                <span className="text-[10px] font-semibold px-2 py-0.5 rounded-full bg-brand-100 dark:bg-brand-950 text-brand-700 dark:text-brand-300 border border-brand-200 dark:border-brand-800 whitespace-nowrap">
                  Pivot Excel Studio
                </span>
              </div>
              <p className="text-[11px] sm:text-xs text-slate-500 dark:text-slate-400 truncate sm:whitespace-normal">
                Hitung otomatis No. Invoice per User, per Tanggal & Grand Total
              </p>
            </div>
          </div>

          {/* Right actions */}
          <div className="flex flex-wrap items-center justify-end gap-2 sm:gap-3">
            {/* If data is loaded: Sheet switcher */}
            {hasData && sheetNames.length > 1 && (
              <div className="flex items-center space-x-1.5 bg-slate-100 dark:bg-slate-800 p-1 rounded-lg text-xs">
                <Layers className="w-3.5 h-3.5 text-slate-500 ml-1.5" />
                <select
                  value={activeSheet}
                  onChange={(e) => onSheetChange(e.target.value)}
                  className="bg-transparent border-0 text-xs font-medium text-slate-700 dark:text-slate-200 focus:ring-0 cursor-pointer pr-6 py-0.5"
                >
                  {sheetNames.map((sheet) => (
                    <option key={sheet} value={sheet} className="bg-white dark:bg-slate-900">
                      Sheet: {sheet}
                    </option>
                  ))}
                </select>
              </div>
            )}

            {/* View Raw Data Button */}
            {hasData && (
              <button
                onClick={onOpenRawData}
                className="inline-flex items-center space-x-1.5 px-2.5 py-1.5 text-[10px] sm:text-xs font-semibold rounded-lg text-slate-700 dark:text-slate-200 bg-slate-100 hover:bg-slate-200 dark:bg-slate-800 dark:hover:bg-slate-700 border border-slate-200 dark:border-slate-700 transition-colors"
                title="Lihat data mentah Excel"
              >
                <Table2 className="w-3.5 h-3.5 text-brand-500 shrink-0" />
                <span className="whitespace-nowrap">Data Mentah ({rowCount.toLocaleString('id-ID')} baris)</span>
              </button>
            )}

            {/* Load Demo Data Button if empty */}
            {!hasData && (
              <button
                onClick={onLoadSample}
                className="inline-flex items-center space-x-1.5 px-2.5 py-1.5 text-[10px] sm:text-xs font-semibold rounded-lg text-brand-700 dark:text-brand-300 bg-brand-50 hover:bg-brand-100 dark:bg-brand-950 dark:hover:bg-brand-900 border border-brand-200 dark:border-brand-800 transition-colors"
              >
                <Sparkles className="w-3.5 h-3.5 text-brand-500 animate-pulse shrink-0" />
                <span className="whitespace-nowrap">Coba Data Contoh</span>
              </button>
            )}

            {/* Reset / Ganti File */}
            {hasData && (
              <button
                onClick={onReset}
                className="inline-flex items-center space-x-1 px-2.5 py-1.5 text-[10px] sm:text-xs font-medium rounded-lg text-slate-500 hover:text-slate-900 dark:hover:text-white hover:bg-slate-100 dark:hover:bg-slate-800 transition-colors"
                title="Unggah file Excel baru"
              >
                <RefreshCw className="w-3.5 h-3.5 shrink-0" />
                <span className="hidden sm:inline whitespace-nowrap">Ganti File</span>
              </button>
            )}

            {/* Dark Mode Toggle */}
            <button
              onClick={() => setDarkMode(!darkMode)}
              className="p-2 rounded-lg text-slate-500 hover:text-slate-900 dark:hover:text-white hover:bg-slate-100 dark:hover:bg-slate-800 transition-colors"
              title={darkMode ? 'Beralih ke mode terang' : 'Beralih ke mode gelap'}
            >
              {darkMode ? <Sun className="w-4 h-4 text-amber-400" /> : <Moon className="w-4 h-4 text-slate-600" />}
            </button>

          </div>

        </div>
      </div>
    </header>
  );
}

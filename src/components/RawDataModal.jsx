import React, { useState } from 'react';
import { X, Search, Table, FileSpreadsheet, ChevronLeft, ChevronRight } from 'lucide-react';

export default function RawDataModal({ isOpen, onClose, rows = [], headers = [], sheetName = '' }) {
  const [searchTerm, setSearchTerm] = useState('');
  const [page, setPage] = useState(1);
  const pageSize = 20;

  if (!isOpen) return null;

  const filtered = rows.filter((r) => {
    if (!searchTerm) return true;
    const term = searchTerm.toLowerCase();
    return Object.values(r).some(val => String(val).toLowerCase().includes(term));
  });

  const totalPages = Math.ceil(filtered.length / pageSize) || 1;
  const paginatedRows = filtered.slice((page - 1) * pageSize, page * pageSize);

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-slate-900/60 backdrop-blur-sm animate-fade-in">
      <div className="bg-white dark:bg-slate-900 rounded-2xl border border-slate-200 dark:border-slate-800 shadow-2xl w-full max-w-6xl max-h-[88vh] flex flex-col overflow-hidden">
        
        {/* Header */}
        <div className="p-4 sm:p-5 border-b border-slate-100 dark:border-slate-800 flex items-center justify-between">
          <div className="flex items-center space-x-3">
            <div className="w-9 h-9 rounded-xl bg-brand-50 dark:bg-brand-950 text-brand-600 dark:text-brand-400 flex items-center justify-center">
              <Table className="w-5 h-5" />
            </div>
            <div>
              <h3 className="text-base font-bold text-slate-900 dark:text-white">
                Data Mentah Excel (Sheet: {sheetName})
              </h3>
              <p className="text-xs text-slate-500 dark:text-slate-400">
                Menampilkan total <b>{filtered.length.toLocaleString('id-ID')}</b> dari {rows.length.toLocaleString('id-ID')} baris
              </p>
            </div>
          </div>

          <div className="flex items-center space-x-3">
            {/* Search */}
            <div className="relative">
              <Search className="w-3.5 h-3.5 text-slate-400 absolute left-3 top-1/2 -translate-y-1/2" />
              <input
                type="text"
                placeholder="Filter data..."
                value={searchTerm}
                onChange={(e) => {
                  setSearchTerm(e.target.value);
                  setPage(1);
                }}
                className="text-xs bg-slate-50 dark:bg-slate-800 border border-slate-200 dark:border-slate-700 rounded-xl pl-8 pr-3 py-1.5 text-slate-800 dark:text-slate-200 focus:ring-2 focus:ring-brand-500 focus:outline-none w-48 transition-all"
              />
            </div>

            <button
              onClick={onClose}
              className="p-1.5 rounded-xl text-slate-400 hover:text-slate-700 dark:hover:text-slate-200 hover:bg-slate-100 dark:hover:bg-slate-800 transition-colors"
            >
              <X className="w-5 h-5" />
            </button>
          </div>
        </div>

        {/* Table Body */}
        <div className="flex-1 overflow-auto p-4">
          <div className="border border-slate-200 dark:border-slate-800 rounded-xl overflow-hidden">
            <table className="w-full text-left text-xs border-collapse">
              <thead>
                <tr className="bg-slate-100 dark:bg-slate-800 text-slate-700 dark:text-slate-200 font-semibold border-b border-slate-200 dark:border-slate-700">
                  <th className="py-2.5 px-3 text-center w-12 border-r border-slate-200 dark:border-slate-700">#</th>
                  {headers.map((h) => (
                    <th key={h} className="py-2.5 px-3 whitespace-nowrap border-r border-slate-200 dark:border-slate-700">
                      {h}
                    </th>
                  ))}
                </tr>
              </thead>
              <tbody className="divide-y divide-slate-100 dark:divide-slate-800">
                {paginatedRows.length === 0 ? (
                  <tr>
                    <td colSpan={headers.length + 1} className="py-8 text-center text-slate-400">
                      Tidak ada baris data yang cocok.
                    </td>
                  </tr>
                ) : (
                  paginatedRows.map((row, idx) => (
                    <tr key={idx} className="hover:bg-slate-50 dark:hover:bg-slate-850 transition-colors">
                      <td className="py-2 px-3 text-center text-slate-400 font-mono text-[11px] border-r border-slate-100 dark:border-slate-800">
                        {(page - 1) * pageSize + idx + 1}
                      </td>
                      {headers.map((h) => (
                        <td key={h} className="py-2 px-3 text-slate-700 dark:text-slate-300 border-r border-slate-100 dark:border-slate-800 truncate max-w-xs">
                          {String(row[h] ?? '')}
                        </td>
                      ))}
                    </tr>
                  ))
                )}
              </tbody>
            </table>
          </div>
        </div>

        {/* Pagination Footer */}
        <div className="p-3.5 border-t border-slate-100 dark:border-slate-800 flex items-center justify-between text-xs text-slate-500">
          <div>
            Halaman <span className="font-semibold text-slate-800 dark:text-slate-200">{page}</span> dari {totalPages}
          </div>

          <div className="flex items-center space-x-2">
            <button
              onClick={() => setPage(p => Math.max(1, p - 1))}
              disabled={page === 1}
              className="p-1.5 rounded-lg border border-slate-200 dark:border-slate-700 hover:bg-slate-100 dark:hover:bg-slate-800 disabled:opacity-40 disabled:cursor-not-allowed"
            >
              <ChevronLeft className="w-4 h-4" />
            </button>
            <button
              onClick={() => setPage(p => Math.min(totalPages, p + 1))}
              disabled={page === totalPages}
              className="p-1.5 rounded-lg border border-slate-200 dark:border-slate-700 hover:bg-slate-100 dark:hover:bg-slate-800 disabled:opacity-40 disabled:cursor-not-allowed"
            >
              <ChevronRight className="w-4 h-4" />
            </button>
          </div>
        </div>

      </div>
    </div>
  );
}

import React from 'react';
import { 
  FileText, 
  Users, 
  CalendarDays, 
  Trophy, 
  TrendingUp, 
  Hash, 
  CheckCircle,
  Percent
} from 'lucide-react';

export default function SummaryCards({ summaryStats, aggType, valField }) {
  if (!summaryStats) return null;

  const {
    grandTotalCalculated = 0,
    uniqueUsers = 0,
    uniqueDates = 0,
    uniqueInvoices = 0,
    topUser = { name: '-', total: 0 },
    avgPerUser = 0,
    avgPerDate = 0,
    filteredRows = 0
  } = summaryStats;

  return (
    <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-4 mb-6">
      
      {/* 1. Grand Total Keseluruhan */}
      <div className="p-4 rounded-2xl bg-gradient-to-br from-brand-600 to-indigo-700 text-white shadow-lg shadow-brand-500/20 relative overflow-hidden">
        <div className="absolute right-0 top-0 translate-x-3 -translate-y-3 w-24 h-24 bg-white/10 rounded-full blur-xl pointer-events-none" />
        
        <div className="flex items-center justify-between">
          <span className="text-xs font-semibold text-brand-100 uppercase tracking-wider">
            Grand Total (Keseluruhan)
          </span>
          <div className="w-8 h-8 rounded-lg bg-white/15 flex items-center justify-center">
            <Hash className="w-4 h-4 text-white" />
          </div>
        </div>

        <div className="mt-2">
          <div className="text-2xl sm:text-3xl font-extrabold tracking-tight">
            {grandTotalCalculated.toLocaleString('id-ID')}
          </div>
          <p className="text-[11px] text-brand-100 mt-1 flex items-center space-x-1">
            <span>{aggType} {valField || 'No. Invoice'}</span>
            <span>•</span>
            <span>{filteredRows.toLocaleString('id-ID')} Baris Data</span>
          </p>
        </div>
      </div>

      {/* 2. Top Performer Admin */}
      <div className="p-4 rounded-2xl bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-800 shadow-soft">
        <div className="flex items-center justify-between">
          <span className="text-xs font-semibold text-slate-500 dark:text-slate-400 uppercase tracking-wider">
            Top Admin Performer
          </span>
          <div className="w-8 h-8 rounded-lg bg-amber-50 dark:bg-amber-950 text-amber-600 dark:text-amber-400 flex items-center justify-center">
            <Trophy className="w-4 h-4" />
          </div>
        </div>

        <div className="mt-2">
          <div className="text-lg sm:text-xl font-extrabold text-slate-900 dark:text-white truncate" title={topUser.name}>
            {topUser.name}
          </div>
          <p className="text-xs text-amber-600 dark:text-amber-400 font-semibold mt-1 flex items-center space-x-1">
            <span>{topUser.total.toLocaleString('id-ID')} Invoice</span>
            <span className="text-slate-400 font-normal">
              ({grandTotalCalculated > 0 ? ((topUser.total / grandTotalCalculated) * 100).toFixed(1) : 0}% dari total)
            </span>
          </p>
        </div>
      </div>

      {/* 3. Total Admin & Rata-rata */}
      <div className="p-4 rounded-2xl bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-800 shadow-soft">
        <div className="flex items-center justify-between">
          <span className="text-xs font-semibold text-slate-500 dark:text-slate-400 uppercase tracking-wider">
            Jumlah Admin Aktif
          </span>
          <div className="w-8 h-8 rounded-lg bg-indigo-50 dark:bg-indigo-950 text-indigo-600 dark:text-indigo-400 flex items-center justify-center">
            <Users className="w-4 h-4" />
          </div>
        </div>

        <div className="mt-2">
          <div className="text-2xl sm:text-3xl font-extrabold text-slate-900 dark:text-white">
            {uniqueUsers} <span className="text-xs font-medium text-slate-500">Admin</span>
          </div>
          <p className="text-xs text-slate-500 dark:text-slate-400 mt-1">
            Rata-rata: <span className="font-semibold text-slate-800 dark:text-slate-200">{avgPerUser.toLocaleString('id-ID')}</span> per admin
          </p>
        </div>
      </div>

      {/* 4. Total Tanggal / Hari Kerja */}
      <div className="p-4 rounded-2xl bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-800 shadow-soft">
        <div className="flex items-center justify-between">
          <span className="text-xs font-semibold text-slate-500 dark:text-slate-400 uppercase tracking-wider">
            Total Hari / Periode
          </span>
          <div className="w-8 h-8 rounded-lg bg-emerald-50 dark:bg-emerald-950 text-emerald-600 dark:text-emerald-400 flex items-center justify-center">
            <CalendarDays className="w-4 h-4" />
          </div>
        </div>

        <div className="mt-2">
          <div className="text-2xl sm:text-3xl font-extrabold text-slate-900 dark:text-white">
            {uniqueDates} <span className="text-xs font-medium text-slate-500">Hari</span>
          </div>
          <p className="text-xs text-slate-500 dark:text-slate-400 mt-1">
            Rata-rata: <span className="font-semibold text-slate-800 dark:text-slate-200">{avgPerDate.toLocaleString('id-ID')}</span> invoice/hari
          </p>
        </div>
      </div>

    </div>
  );
}

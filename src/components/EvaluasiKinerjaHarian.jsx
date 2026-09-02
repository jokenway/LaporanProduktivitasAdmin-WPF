import React, { useState, useEffect, useMemo, useCallback, useRef } from 'react';
import {
  Calendar,
  User,
  Search,
  FileSpreadsheet,
  Printer,
  RotateCcw,
  CheckCircle2,
  AlertCircle,
  Clock,
  CheckSquare,
  FileEdit,
  TrendingUp,
  Percent,
  Sparkles,
  Zap,
  HelpCircle,
  Users
} from 'lucide-react';
import { buildDailyEvaluationData } from '../utils/evaluasiEngine';
import { exportEvaluasiToExcel } from '../utils/evaluasiExporter';

const STORAGE_KEY = 'laporan_admin_kinerja_absensi_v1';

export default function EvaluasiKinerjaHarian({
  rows = [],
  fileName = ''
}) {
  // Filter state
  const [selectedDate, setSelectedDate] = useState('ALL');
  const [selectedUser, setSelectedUser] = useState('ALL');
  const [searchQuery, setSearchQuery] = useState('');
  const [isExporting, setIsExporting] = useState(false);
  const [saveStatus, setSaveStatus] = useState(''); // 'saved' | ''
  const tableContainerRef = useRef(null);
  const [scrollTop, setScrollTop] = useState(0);
  const [viewportHeight, setViewportHeight] = useState(620);

  // Manual logs state (Nota Salah, Jam Datang, Jam Pulang, Keterangan)
  const [manualLogs, setManualLogs] = useState(() => {
    try {
      const saved = localStorage.getItem(STORAGE_KEY);
      return saved ? JSON.parse(saved) : {};
    } catch {
      return {};
    }
  });

  // Save to localStorage when manualLogs change
  const saveLogs = useCallback((newLogs) => {
    setManualLogs(newLogs);
    try {
      localStorage.setItem(STORAGE_KEY, JSON.stringify(newLogs));
      setSaveStatus('Tersimpan otomatis');
      setTimeout(() => setSaveStatus(''), 2500);
    } catch (e) {
      console.error('Failed to save to localStorage', e);
    }
  }, []);

  // Update specific field in manual logs
  const handleUpdateField = (key, field, value) => {
    setManualLogs((prev) => {
      const existing = prev[key] || {};
      const updated = {
        ...prev,
        [key]: {
          ...existing,
          [field]: value
        }
      };
      try {
        localStorage.setItem(STORAGE_KEY, JSON.stringify(updated));
        setSaveStatus('Tersimpan');
        setTimeout(() => setSaveStatus(''), 2000);
      } catch (e) {
        console.error('Error saving', e);
      }
      return updated;
    });
  };

  // Compute evaluation data
  const evaluationData = useMemo(() => {
    return buildDailyEvaluationData({
      rows,
      selectedDate,
      selectedUser,
      searchQuery,
      manualLogs
    });
  }, [rows, selectedDate, selectedUser, searchQuery, manualLogs]);

  const { records, availableDates, availableUsers, summary } = evaluationData;

  const ROW_HEIGHT = 62;
  const OVERSCAN = 8;

  useEffect(() => {
    if (!tableContainerRef.current) return;

    const resizeObserver = new ResizeObserver((entries) => {
      for (const entry of entries) {
        setViewportHeight(entry.contentRect.height || 620);
      }
    });

    resizeObserver.observe(tableContainerRef.current);
    return () => resizeObserver.disconnect();
  }, []);

  const visibleRange = useMemo(() => {
    const totalRows = records.length;
    if (totalRows === 0) return { start: 0, end: 0, topSpacer: 0, bottomSpacer: 0, visibleRows: [] };

    const start = Math.max(0, Math.floor(scrollTop / ROW_HEIGHT) - OVERSCAN);
    const end = Math.min(totalRows, start + Math.ceil(viewportHeight / ROW_HEIGHT) + OVERSCAN * 2);

    return {
      start,
      end,
      topSpacer: start * ROW_HEIGHT,
      bottomSpacer: (totalRows - end) * ROW_HEIGHT,
      visibleRows: records.slice(start, end)
    };
  }, [records, scrollTop, viewportHeight]);

  // Quick fill default working hours (08:00 - 17:00) for currently filtered rows
  const handleApplyDefaultHours = () => {
    const updated = { ...manualLogs };
    let count = 0;
    records.forEach((rec) => {
      const existing = updated[rec.key] || {};
      if (!existing.jamDatang || !existing.jamPulang) {
        updated[rec.key] = {
          ...existing,
          jamDatang: existing.jamDatang || '08:00',
          jamPulang: existing.jamPulang || '17:00'
        };
        count++;
      }
    });

    if (count === 0) {
      alert('Semua baris yang tampil sudah memiliki jam datang dan jam pulang.');
      return;
    }

    saveLogs(updated);
    alert(`Berhasil menerapkan jam standar (08:00 - 17:00) untuk ${count} baris.`);
  };

  // Reset / Clear manual inputs with confirmation
  const handleResetInputs = () => {
    if (window.confirm('Apakah Anda yakin ingin menghapus seluruh data inputan nota salah dan jam kerja?')) {
      saveLogs({});
    }
  };

  // Export to Excel
  const handleExportExcel = async () => {
    try {
      setIsExporting(true);
      const userTag = selectedUser !== 'ALL' ? `_${selectedUser}` : '';
      const dateTag = selectedDate !== 'ALL' ? `_${selectedDate}` : '';
      await exportEvaluasiToExcel({
        records,
        summary,
        selectedDate,
        selectedUser,
        filename: `Laporan_Kinerja_Absensi_Admin${userTag}${dateTag}.xlsx`
      });
    } catch (err) {
      alert('Gagal export Excel: ' + err.message);
    } finally {
      setIsExporting(false);
    }
  };

  // Print
  const handlePrint = () => {
    window.print();
  };

  return (
    <div className="space-y-6">
      
      {/* 1. Header Banner & Quick Actions */}
      <div className="bg-gradient-to-r from-brand-600 via-indigo-600 to-sky-600 rounded-2xl p-6 text-white shadow-lg relative overflow-hidden">
        <div className="absolute right-0 top-0 bottom-0 opacity-10 flex items-center pr-6 pointer-events-none">
          <Clock className="w-64 h-64 -mr-12 -mt-12" />
        </div>

        <div className="relative z-10 flex flex-col md:flex-row md:items-center justify-between gap-4">
          <div>
            <div className="inline-flex items-center space-x-2 px-3 py-1 rounded-full bg-white/20 backdrop-blur-sm text-xs font-semibold uppercase tracking-wider mb-2">
              <CheckSquare className="w-3.5 h-3.5" />
              <span>Monitoring Kinerja & Absensi Harian</span>
            </div>
            <h2 className="text-2xl font-black tracking-tight">
              Evaluasi Nota & Jam Kerja Admin
            </h2>
            <p className="text-brand-100 text-sm mt-1 max-w-2xl">
              Hitung otomatis nota yang dibuat per hari dari Excel, input nota salah/revisi, pantau jam datang dan jam pulang, serta lihat tingkat akurasi secara realtime.
            </p>
          </div>

          {/* Action Buttons */}
          <div className="flex flex-wrap items-center gap-2.5">
            <button
              onClick={handleApplyDefaultHours}
              title="Isi jam 08:00 - 17:00 pada baris yang masih kosong"
              className="inline-flex items-center space-x-2 px-3.5 py-2 rounded-xl bg-white/15 hover:bg-white/25 backdrop-blur-sm text-xs font-bold transition-all border border-white/20"
            >
              <Zap className="w-4 h-4 text-amber-300" />
              <span>Isi Jam Standar (08-17)</span>
            </button>

            <button
              onClick={handleExportExcel}
              disabled={isExporting || records.length === 0}
              className="inline-flex items-center space-x-2 px-4 py-2 rounded-xl bg-emerald-500 hover:bg-emerald-600 text-white text-xs font-bold shadow-md hover:shadow-lg transition-all disabled:opacity-50"
            >
              <FileSpreadsheet className="w-4 h-4" />
              <span>{isExporting ? 'Mengekspor...' : 'Export Excel'}</span>
            </button>

            <button
              onClick={handlePrint}
              disabled={records.length === 0}
              className="inline-flex items-center space-x-2 px-3.5 py-2 rounded-xl bg-slate-900/80 hover:bg-slate-900 text-white text-xs font-bold shadow-md hover:shadow-lg transition-all disabled:opacity-50 border border-slate-700"
            >
              <Printer className="w-4 h-4" />
              <span>Cetak / PDF</span>
            </button>
          </div>
        </div>
      </div>

      {/* 2. Top KPI Summary Cards */}
      <div className="grid grid-cols-2 sm:grid-cols-3 lg:grid-cols-6 gap-3">
        
        {/* Card 1: Total Nota Dibuat */}
        <div className="p-4 rounded-2xl bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-800 shadow-soft">
          <div className="flex items-center justify-between text-slate-400">
            <span className="text-xs font-medium">Nota Dibuat</span>
            <FileSpreadsheet className="w-4 h-4 text-brand-500" />
          </div>
          <div className="mt-2">
            <div className="text-2xl font-black text-slate-900 dark:text-white font-mono">
              {summary.totalNota.toLocaleString('id-ID')}
            </div>
            <p className="text-[11px] text-slate-400 mt-0.5">Otomatis dari Excel</p>
          </div>
        </div>

        {/* Card 2: Total Nota Salah */}
        <div className="p-4 rounded-2xl bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-800 shadow-soft">
          <div className="flex items-center justify-between text-slate-400">
            <span className="text-xs font-medium">Nota Salah</span>
            <AlertCircle className="w-4 h-4 text-rose-500" />
          </div>
          <div className="mt-2">
            <div className={`text-2xl font-black font-mono ${summary.totalSalah > 0 ? 'text-rose-600 dark:text-rose-400' : 'text-slate-900 dark:text-white'}`}>
              {summary.totalSalah.toLocaleString('id-ID')}
            </div>
            <p className="text-[11px] text-slate-400 mt-0.5">Total input kesalahan</p>
          </div>
        </div>

        {/* Card 3: Total Nota Valid */}
        <div className="p-4 rounded-2xl bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-800 shadow-soft">
          <div className="flex items-center justify-between text-slate-400">
            <span className="text-xs font-medium">Nota Valid</span>
            <CheckCircle2 className="w-4 h-4 text-emerald-500" />
          </div>
          <div className="mt-2">
            <div className="text-2xl font-black text-emerald-600 dark:text-emerald-400 font-mono">
              {summary.totalValid.toLocaleString('id-ID')}
            </div>
            <p className="text-[11px] text-slate-400 mt-0.5">Nota bersih / benar</p>
          </div>
        </div>

        {/* Card 4: Tingkat Akurasi */}
        <div className="p-4 rounded-2xl bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-800 shadow-soft">
          <div className="flex items-center justify-between text-slate-400">
            <span className="text-xs font-medium">Tingkat Akurasi</span>
            <Percent className="w-4 h-4 text-purple-500" />
          </div>
          <div className="mt-2">
            <div className="text-2xl font-black text-purple-600 dark:text-purple-400 font-mono">
              {summary.overallAkurasi}%
            </div>
            <p className="text-[11px] text-slate-400 mt-0.5">Persentase keberhasilan</p>
          </div>
        </div>

        {/* Card 5: Total Jam Kerja */}
        <div className="p-4 rounded-2xl bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-800 shadow-soft">
          <div className="flex items-center justify-between text-slate-400">
            <span className="text-xs font-medium">Total Jam Kerja</span>
            <Clock className="w-4 h-4 text-amber-500" />
          </div>
          <div className="mt-2">
            <div className="text-xl font-bold text-slate-900 dark:text-white font-mono truncate">
              {summary.totalJamKerjaText}
            </div>
            <p className="text-[11px] text-slate-400 mt-0.5">Akumulasi durasi</p>
          </div>
        </div>

        {/* Card 6: Admin & Hari Kerja */}
        <div className="p-4 rounded-2xl bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-800 shadow-soft">
          <div className="flex items-center justify-between text-slate-400">
            <span className="text-xs font-medium">Admin & Hari</span>
            <Users className="w-4 h-4 text-sky-500" />
          </div>
          <div className="mt-2">
            <div className="text-2xl font-black text-slate-900 dark:text-white font-mono">
              {summary.userCount} <span className="text-xs font-normal text-slate-400">user</span>
            </div>
            <p className="text-[11px] text-slate-400 mt-0.5">{summary.dateCount} hari kerja</p>
          </div>
        </div>

      </div>

      {/* 3. Filter & Search Controls */}
      <div className="bg-white dark:bg-slate-900 p-4 rounded-2xl border border-slate-200 dark:border-slate-800 shadow-soft flex flex-wrap items-center justify-between gap-4">
        
        <div className="flex flex-wrap items-center gap-3">
          
          {/* Filter Tanggal */}
          <div className="flex items-center space-x-2">
            <Calendar className="w-4 h-4 text-slate-400" />
            <select
              value={selectedDate}
              onChange={(e) => setSelectedDate(e.target.value)}
              className="px-3 py-1.5 rounded-xl border border-slate-300 dark:border-slate-700 bg-slate-50 dark:bg-slate-800 text-xs font-bold text-slate-800 dark:text-slate-200 focus:ring-2 focus:ring-brand-500 focus:outline-none"
            >
              <option value="ALL">Semua Tanggal ({availableDates.length} Hari)</option>
              {availableDates.map((d) => (
                <option key={d} value={d}>Tanggal {d}</option>
              ))}
            </select>
          </div>

          {/* Filter User */}
          <div className="flex items-center space-x-2">
            <User className="w-4 h-4 text-slate-400" />
            <select
              value={selectedUser}
              onChange={(e) => setSelectedUser(e.target.value)}
              className="px-3 py-1.5 rounded-xl border border-slate-300 dark:border-slate-700 bg-slate-50 dark:bg-slate-800 text-xs font-bold text-slate-800 dark:text-slate-200 focus:ring-2 focus:ring-brand-500 focus:outline-none"
            >
              <option value="ALL">Semua Admin ({availableUsers.length} User)</option>
              {availableUsers.map((u) => (
                <option key={u} value={u}>{u}</option>
              ))}
            </select>
          </div>

          {/* Search Box */}
          <div className="relative">
            <Search className="w-3.5 h-3.5 text-slate-400 absolute left-3 top-1/2 transform -translate-y-1/2" />
            <input
              type="text"
              placeholder="Cari user, tgl, catatan..."
              value={searchQuery}
              onChange={(e) => setSearchQuery(e.target.value)}
              className="pl-8 pr-3 py-1.5 rounded-xl border border-slate-300 dark:border-slate-700 bg-slate-50 dark:bg-slate-800 text-xs text-slate-800 dark:text-slate-200 focus:ring-2 focus:ring-brand-500 focus:outline-none w-48 sm:w-60"
            />
          </div>

        </div>

        {/* Status Save & Reset */}
        <div className="flex items-center space-x-3">
          {saveStatus && (
            <span className="text-xs font-medium text-emerald-600 dark:text-emerald-400 flex items-center space-x-1 animate-pulse">
              <CheckCircle2 className="w-3.5 h-3.5" />
              <span>{saveStatus}</span>
            </span>
          )}

          <button
            onClick={handleResetInputs}
            title="Reset data inputan"
            className="inline-flex items-center space-x-1.5 px-2.5 py-1.5 rounded-lg text-xs font-semibold text-rose-600 hover:bg-rose-50 dark:hover:bg-rose-950/30 transition-colors"
          >
            <RotateCcw className="w-3.5 h-3.5" />
            <span>Reset Input</span>
          </button>
        </div>

      </div>

      {/* 4. Interactive Data Table */}
      <div className="bg-white dark:bg-slate-900 rounded-2xl border border-slate-200 dark:border-slate-800 shadow-soft overflow-hidden">
        
        <div className="p-4 border-b border-slate-100 dark:border-slate-800 flex flex-wrap items-center justify-between gap-2">
          <div>
            <h3 className="text-sm font-bold text-slate-900 dark:text-white flex items-center space-x-2">
              <FileEdit className="w-4 h-4 text-brand-500" />
              <span>Tabel Evaluasi Harian Admin & Absensi</span>
            </h3>
            <p className="text-xs text-slate-500 dark:text-slate-400 mt-0.5">
              Ketik langsung jumlah nota salah, jam datang, dan jam pulang pada kolom yang disediakan.
            </p>
          </div>

          <div className="text-xs text-slate-500 dark:text-slate-400 font-medium">
            Menampilkan <b>{records.length}</b> baris data
          </div>
        </div>

        {records.length === 0 ? (
          <div className="py-16 text-center text-slate-400">
            <HelpCircle className="w-12 h-12 mx-auto mb-3 text-slate-300 dark:text-slate-700" />
            <p className="text-sm font-medium">Tidak ada data yang cocok dengan filter yang dipilih</p>
            <p className="text-xs mt-1">Coba ubah filter tanggal atau pencarian Anda.</p>
          </div>
        ) : (
          <div
            ref={tableContainerRef}
            className="overflow-x-auto max-h-[620px] overflow-y-auto"
            onScroll={(e) => setScrollTop(e.currentTarget.scrollTop)}
          >
            <table className="w-full text-left text-xs border-collapse">
              <thead className="sticky top-0 z-20">
                <tr className="bg-slate-900 text-white font-semibold uppercase tracking-wider text-[11px]">
                  <th className="py-3 px-3 text-center w-12 border-b border-slate-800 bg-slate-900 sticky left-0 z-20">
                    No
                  </th>
                  <th className="py-3 px-3 text-center min-w-[100px] border-b border-slate-800 bg-slate-900 sticky left-12 z-20">
                    Tanggal
                  </th>
                  <th className="py-3 px-4 min-w-[150px] border-b border-slate-800 bg-slate-900 sticky left-36 z-20">
                    User Fakturis
                  </th>
                  <th className="py-3 px-3 text-center min-w-[110px] border-b border-slate-800 bg-sky-950 text-sky-200">
                    Nota Dibuat
                  </th>
                  <th className="py-3 px-3 text-center min-w-[120px] border-b border-slate-800 bg-rose-950 text-rose-200">
                    ✏️ Nota Salah
                  </th>
                  <th className="py-3 px-3 text-center min-w-[100px] border-b border-slate-800 bg-emerald-950 text-emerald-200">
                    Nota Valid
                  </th>
                  <th className="py-3 px-3 text-center min-w-[100px] border-b border-slate-800 bg-purple-950 text-purple-200">
                    Akurasi
                  </th>
                  <th className="py-3 px-3 text-center min-w-[120px] border-b border-slate-800 bg-amber-950 text-amber-200">
                    ✏️ Jam Datang
                  </th>
                  <th className="py-3 px-3 text-center min-w-[120px] border-b border-slate-800 bg-amber-950 text-amber-200">
                    ✏️ Jam Pulang
                  </th>
                  <th className="py-3 px-3 text-center min-w-[120px] border-b border-slate-800 bg-teal-950 text-teal-200">
                    Durasi Kerja
                  </th>
                  <th className="py-3 px-4 min-w-[180px] border-b border-slate-800">
                    ✏️ Catatan / Alasan
                  </th>
                </tr>
              </thead>

              <tbody className="divide-y divide-slate-100 dark:divide-slate-800 text-xs">
                {visibleRange.topSpacer > 0 && (
                  <tr style={{ height: visibleRange.topSpacer }}>
                    <td colSpan={11} className="p-0" aria-hidden="true" />
                  </tr>
                )}

                {visibleRange.visibleRows.map((rec, idx) => {
                  const globalIndex = visibleRange.start + idx;
                  const isEven = globalIndex % 2 === 0;

                  let akurasiBadgeClass = 'bg-emerald-50 text-emerald-700 border-emerald-200 dark:bg-emerald-950/40 dark:text-emerald-400 dark:border-emerald-800';
                  if (rec.akurasi < 90) {
                    akurasiBadgeClass = 'bg-rose-50 text-rose-700 border-rose-200 dark:bg-rose-950/40 dark:text-rose-400 dark:border-rose-800';
                  } else if (rec.akurasi < 98) {
                    akurasiBadgeClass = 'bg-amber-50 text-amber-700 border-amber-200 dark:bg-amber-950/40 dark:text-amber-400 dark:border-amber-800';
                  }

                  return (
                    <tr
                      key={rec.key}
                      className={`hover:bg-brand-50/50 dark:hover:bg-slate-800/60 transition-colors ${
                        isEven ? 'bg-white dark:bg-slate-900' : 'bg-slate-50/70 dark:bg-slate-850/40'
                      }`}
                    >
                      <td className="py-2.5 px-3 text-center font-mono text-slate-400 sticky left-0 z-10 bg-inherit border-r border-slate-100 dark:border-slate-800">
                        {globalIndex + 1}
                      </td>

                      <td className="py-2.5 px-3 text-center font-mono font-medium text-slate-700 dark:text-slate-300 sticky left-12 z-10 bg-inherit border-r border-slate-100 dark:border-slate-800">
                        {rec.date}
                      </td>

                      <td className="py-2.5 px-4 font-bold text-slate-900 dark:text-slate-100 sticky left-36 z-10 bg-inherit border-r border-slate-100 dark:border-slate-800">
                        {rec.user}
                      </td>

                      <td className="py-2.5 px-3 text-center font-mono font-bold text-sky-700 dark:text-sky-400 bg-sky-50/30 dark:bg-sky-950/20">
                        <span className="inline-flex items-center px-2 py-0.5 rounded-lg bg-sky-100 dark:bg-sky-900/60 text-sky-800 dark:text-sky-300 font-extrabold">
                          {rec.totalNota}
                        </span>
                      </td>

                      <td className="py-2 px-2 text-center bg-rose-50/20 dark:bg-rose-950/10">
                        <div className="inline-flex items-center justify-center">
                          <input
                            type="number"
                            min="0"
                            max={rec.totalNota}
                            value={rec.notaSalah === 0 ? '' : rec.notaSalah}
                            placeholder="0"
                            onChange={(e) => {
                              const val = e.target.value === '' ? 0 : Math.max(0, parseInt(e.target.value, 10) || 0);
                              handleUpdateField(rec.key, 'notaSalah', val);
                            }}
                            className={`w-16 px-2 py-1 text-center font-mono font-bold text-xs rounded-lg border focus:ring-2 focus:ring-rose-500 focus:outline-none transition-all ${
                              rec.notaSalah > 0
                                ? 'border-rose-400 bg-rose-50 text-rose-700 dark:bg-rose-950 dark:text-rose-300 dark:border-rose-700 font-extrabold'
                                : 'border-slate-200 dark:border-slate-700 bg-white dark:bg-slate-800 text-slate-700 dark:text-slate-300'
                            }`}
                          />
                        </div>
                      </td>

                      <td className="py-2.5 px-3 text-center font-mono font-bold text-emerald-600 dark:text-emerald-400 bg-emerald-50/20 dark:bg-emerald-950/10">
                        {rec.notaValid}
                      </td>

                      <td className="py-2.5 px-3 text-center">
                        <span className={`inline-flex items-center px-2 py-0.5 rounded-md font-mono text-[11px] font-bold border ${akurasiBadgeClass}`}>
                          {rec.akurasi}%
                        </span>
                      </td>

                      <td className="py-2 px-2 text-center bg-amber-50/20 dark:bg-amber-950/10">
                        <input
                          type="time"
                          value={rec.jamDatang}
                          onChange={(e) => handleUpdateField(rec.key, 'jamDatang', e.target.value)}
                          className="px-2 py-1 text-center font-mono text-xs rounded-lg border border-slate-200 dark:border-slate-700 bg-white dark:bg-slate-800 text-slate-800 dark:text-slate-200 focus:ring-2 focus:ring-amber-500 focus:outline-none"
                        />
                      </td>

                      <td className="py-2 px-2 text-center bg-amber-50/20 dark:bg-amber-950/10">
                        <input
                          type="time"
                          value={rec.jamPulang}
                          onChange={(e) => handleUpdateField(rec.key, 'jamPulang', e.target.value)}
                          className="px-2 py-1 text-center font-mono text-xs rounded-lg border border-slate-200 dark:border-slate-700 bg-white dark:bg-slate-800 text-slate-800 dark:text-slate-200 focus:ring-2 focus:ring-amber-500 focus:outline-none"
                        />
                      </td>

                      <td className="py-2.5 px-3 text-center font-mono font-semibold text-teal-700 dark:text-teal-400 bg-teal-50/20 dark:bg-teal-950/10">
                        {rec.durasiText}
                      </td>

                      <td className="py-2 px-3">
                        <input
                          type="text"
                          value={rec.keterangan}
                          placeholder="Catatan / alasan nota salah..."
                          onChange={(e) => handleUpdateField(rec.key, 'keterangan', e.target.value)}
                          className="w-full px-2 py-1 text-xs rounded-lg border border-slate-200 dark:border-slate-700 bg-white dark:bg-slate-800 text-slate-800 dark:text-slate-200 focus:ring-2 focus:ring-brand-500 focus:outline-none"
                        />
                      </td>
                    </tr>
                  );
                })}

                {visibleRange.bottomSpacer > 0 && (
                  <tr style={{ height: visibleRange.bottomSpacer }}>
                    <td colSpan={11} className="p-0" aria-hidden="true" />
                  </tr>
                )}
              </tbody>

              {/* Grand Total Footer */}
              <tfoot className="sticky bottom-0 z-20">
                <tr className="bg-slate-900 text-white font-bold text-xs border-t-2 border-slate-900">
                  <td className="py-3 px-3 text-center bg-slate-900 sticky left-0 z-20">-</td>
                  <td className="py-3 px-3 text-center bg-slate-900 sticky left-12 z-20">-</td>
                  <td className="py-3 px-4 uppercase tracking-wider bg-slate-900 sticky left-36 z-20 text-brand-300">
                    TOTAL KESELURUHAN
                  </td>
                  <td className="py-3 px-3 text-center font-mono text-sky-300 bg-sky-950">
                    {summary.totalNota.toLocaleString('id-ID')}
                  </td>
                  <td className="py-3 px-3 text-center font-mono text-rose-300 bg-rose-950">
                    {summary.totalSalah.toLocaleString('id-ID')}
                  </td>
                  <td className="py-3 px-3 text-center font-mono text-emerald-300 bg-emerald-950">
                    {summary.totalValid.toLocaleString('id-ID')}
                  </td>
                  <td className="py-3 px-3 text-center font-mono text-purple-300 bg-purple-950">
                    {summary.overallAkurasi}%
                  </td>
                  <td className="py-3 px-3 text-center bg-amber-950 text-slate-400">-</td>
                  <td className="py-3 px-3 text-center bg-amber-950 text-slate-400">-</td>
                  <td className="py-3 px-3 text-center font-mono text-teal-300 bg-teal-950">
                    {summary.totalJamKerjaText}
                  </td>
                  <td className="py-3 px-4 text-xs text-slate-400">
                    {summary.recordCount} Baris tercatat
                  </td>
                </tr>
              </tfoot>

            </table>
          </div>
        )}

      </div>

      {/* 5. Helpful Tips & Instructions */}
      <div className="p-4 rounded-2xl bg-brand-50/50 dark:bg-slate-900/50 border border-brand-100 dark:border-slate-800 text-xs text-slate-600 dark:text-slate-400 flex items-start space-x-3">
        <Sparkles className="w-5 h-5 text-brand-500 shrink-0 mt-0.5" />
        <div className="space-y-1">
          <p className="font-bold text-slate-800 dark:text-slate-200">
            Petunjuk Penggunaan Fitur Kinerja & Absensi Harian:
          </p>
          <ul className="list-disc list-inside space-y-0.5 text-[11px]">
            <li><b>Total Nota Dibuat</b> dihitung otomatis dari transaksi unik (nomor invoice) file Excel Anda untuk tiap admin pada tanggal tersebut.</li>
            <li>Ketik <b>Nota Salah</b> jika ada kesalahan input/revisi oleh admin. Nota Valid & Persentase Akurasi akan langsung terhitung otomatis.</li>
            <li>Isi <b>Jam Datang</b> & <b>Jam Pulang</b> untuk menghitung total durasi jam kerja admin harian.</li>
            <li>Gunakan tombol <b>"Isi Jam Standar (08-17)"</b> di kanan atas jika sebagian besar admin bekerja pada jam operasional standar.</li>
            <li>Seluruh data yang Anda ketik <b>otomatis tersimpan</b> di komputer/browser ini, sehingga tidak akan hilang saat berpindah menu atau membuka aplikasi kembali.</li>
          </ul>
        </div>
      </div>

    </div>
  );
}

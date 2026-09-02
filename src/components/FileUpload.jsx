import React, { useState, useRef } from 'react';
import { 
  Upload, 
  FileSpreadsheet, 
  Sparkles, 
  CheckCircle2, 
  FileText, 
  ArrowRight,
  Download,
  AlertCircle
} from 'lucide-react';
import { downloadSampleExcel } from '../sampleData/generateSample';

export default function FileUpload({ onFileLoaded, onLoadSample, isLoading }) {
  const [isDragging, setIsDragging] = useState(false);
  const [errorMsg, setErrorMsg] = useState('');
  const fileInputRef = useRef(null);

  const handleDragOver = (e) => {
    e.preventDefault();
    setIsDragging(true);
  };

  const handleDragLeave = () => {
    setIsDragging(false);
  };

  const handleDrop = (e) => {
    e.preventDefault();
    setIsDragging(false);
    setErrorMsg('');

    const files = e.dataTransfer.files;
    if (files && files.length > 0) {
      processFile(files[0]);
    }
  };

  const handleFileChange = (e) => {
    setErrorMsg('');
    const files = e.target.files;
    if (files && files.length > 0) {
      processFile(files[0]);
    }
  };

  const processFile = (file) => {
    const validExtensions = ['.xlsx', '.xls', '.csv', '.ods'];
    const fileName = file.name.toLowerCase();
    const isValid = validExtensions.some(ext => fileName.endsWith(ext));

    if (!isValid) {
      setErrorMsg('Harap pilih file Excel berformat .xlsx, .xls, atau .csv');
      return;
    }

    onFileLoaded(file);
  };

  return (
    <div className="max-w-4xl mx-auto py-12 px-4 sm:px-6">
      
      {/* Hero Welcome */}
      <div className="text-center mb-10">
        <div className="inline-flex items-center space-x-2 px-3 py-1 rounded-full bg-brand-50 dark:bg-brand-950 text-brand-600 dark:text-brand-400 border border-brand-200 dark:border-brand-800 text-xs font-semibold mb-4">
          <Sparkles className="w-3.5 h-3.5" />
          <span>Solusi Otomatisasi Laporan Produktivitas Admin</span>
        </div>
        <h2 className="text-3xl sm:text-4xl font-extrabold text-slate-900 dark:text-white tracking-tight">
          Olah Data Excel Jadi <span className="text-transparent bg-clip-text bg-gradient-to-r from-brand-600 to-indigo-600">Pivot Produktivitas</span>
        </h2>
        <p className="mt-3 text-sm sm:text-base text-slate-600 dark:text-slate-400 max-w-2xl mx-auto">
          Unggah workbook Excel mentah, pilih field yang Anda inginkan, dan dapatkan perhitungan total <b>No. Invoice per User per Tanggal</b> beserta <b>Grand Total Keseluruhan</b> secara instan.
        </p>
      </div>

      {/* Upload Dropzone */}
      <div
        onDragOver={handleDragOver}
        onDragLeave={handleDragLeave}
        onDrop={handleDrop}
        onClick={() => fileInputRef.current?.click()}
        className={`relative border-2 border-dashed rounded-2xl p-10 text-center cursor-pointer transition-all duration-200 ${
          isDragging
            ? 'border-brand-500 bg-brand-50/50 dark:bg-brand-950/30 scale-[1.01]'
            : 'border-slate-300 dark:border-slate-700 bg-white dark:bg-slate-900 hover:border-brand-400 dark:hover:border-brand-600 hover:bg-slate-50/50 dark:hover:bg-slate-850 shadow-soft'
        }`}
      >
        <input
          ref={fileInputRef}
          type="file"
          accept=".xlsx,.xls,.csv"
          onChange={handleFileChange}
          className="hidden"
        />

        <div className="flex flex-col items-center justify-center space-y-4">
          <div className="w-16 h-16 rounded-2xl bg-brand-100 dark:bg-brand-950/80 text-brand-600 dark:text-brand-400 flex items-center justify-center shadow-inner">
            {isLoading ? (
              <div className="w-8 h-8 border-3 border-brand-500 border-t-transparent rounded-full animate-spin" />
            ) : (
              <FileSpreadsheet className="w-8 h-8" />
            )}
          </div>

          <div>
            <p className="text-base font-semibold text-slate-800 dark:text-slate-200">
              {isLoading ? 'Sedang Memproses File Excel...' : 'Tarik & Letakkan File Excel Di Sini'}
            </p>
            <p className="text-xs text-slate-500 dark:text-slate-400 mt-1">
              Mendukung format <span className="font-mono text-brand-600 font-semibold">.xlsx</span>, <span className="font-mono text-brand-600 font-semibold">.xls</span>, dan <span className="font-mono text-brand-600 font-semibold">.csv</span>
            </p>
          </div>

          <div className="pt-2">
            <button
              type="button"
              disabled={isLoading}
              className="inline-flex items-center space-x-2 px-5 py-2.5 rounded-xl text-sm font-semibold text-white bg-gradient-to-r from-brand-600 to-indigo-600 hover:from-brand-700 hover:to-indigo-700 shadow-md shadow-brand-500/20 active:scale-95 transition-all"
            >
              <Upload className="w-4 h-4" />
              <span>Pilih File Dari Komputer</span>
            </button>
          </div>
        </div>

        {errorMsg && (
          <div className="mt-4 inline-flex items-center space-x-2 px-3 py-1.5 rounded-lg bg-rose-50 dark:bg-rose-950/50 text-rose-600 dark:text-rose-400 text-xs font-medium border border-rose-200 dark:border-rose-800">
            <AlertCircle className="w-4 h-4" />
            <span>{errorMsg}</span>
          </div>
        )}
      </div>

      {/* Alternative actions: Sample Data */}
      <div className="mt-6 flex flex-col sm:flex-row items-center justify-center gap-3">
        <button
          onClick={onLoadSample}
          disabled={isLoading}
          className="w-full sm:w-auto inline-flex items-center justify-center space-x-2 px-4 py-2 text-xs font-semibold rounded-xl text-slate-700 dark:text-slate-200 bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-800 hover:bg-slate-50 dark:hover:bg-slate-800 transition-all shadow-sm"
        >
          <Sparkles className="w-3.5 h-3.5 text-brand-500" />
          <span>Gunakan Data Sampel Siap Pakai (Demo)</span>
        </button>

        <button
          onClick={downloadSampleExcel}
          className="w-full sm:w-auto inline-flex items-center justify-center space-x-2 px-4 py-2 text-xs font-semibold rounded-xl text-slate-600 dark:text-slate-400 bg-transparent hover:text-slate-900 dark:hover:text-white transition-colors"
        >
          <Download className="w-3.5 h-3.5" />
          <span>Download Template Excel Contoh</span>
        </button>
      </div>

      {/* Feature Highlights Grid */}
      <div className="mt-14 grid grid-cols-1 md:grid-cols-3 gap-5">
        <div className="p-5 rounded-2xl bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-800 shadow-soft">
          <div className="w-9 h-9 rounded-xl bg-blue-50 dark:bg-blue-950 text-blue-600 dark:text-blue-400 flex items-center justify-center mb-3">
            <FileSpreadsheet className="w-4.5 h-4.5" />
          </div>
          <h4 className="text-sm font-bold text-slate-900 dark:text-white">
            Hitung NoInv Otomatis
          </h4>
          <p className="text-xs text-slate-500 dark:text-slate-400 mt-1 leading-relaxed">
            Menghitung total transaksi/nomor invoice per user per tanggal lengkap dengan subtotal dan Grand Total.
          </p>
        </div>

        <div className="p-5 rounded-2xl bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-800 shadow-soft">
          <div className="w-9 h-9 rounded-xl bg-purple-50 dark:bg-purple-950 text-purple-600 dark:text-purple-400 flex items-center justify-center mb-3">
            <Sparkles className="w-4.5 h-4.5" />
          </div>
          <h4 className="text-sm font-bold text-slate-900 dark:text-white">
            Pilihan Field Dinamis
          </h4>
          <p className="text-xs text-slate-500 dark:text-slate-400 mt-1 leading-relaxed">
            Tentukan kolom apa saja dari file Excel Anda untuk dijadikan Baris, Kolom matriks, Nilai, dan Filter.
          </p>
        </div>

        <div className="p-5 rounded-2xl bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-800 shadow-soft">
          <div className="w-9 h-9 rounded-xl bg-emerald-50 dark:bg-emerald-950 text-emerald-600 dark:text-emerald-400 flex items-center justify-center mb-3">
            <CheckCircle2 className="w-4.5 h-4.5" />
          </div>
          <h4 className="text-sm font-bold text-slate-900 dark:text-white">
            Export Excel & PDF Rapi
          </h4>
          <p className="text-xs text-slate-500 dark:text-slate-400 mt-1 leading-relaxed">
            Hasil Pivot Table dapat langsung di-export kembali menjadi Excel ber-styling profesional siap cetak.
          </p>
        </div>
      </div>

    </div>
  );
}

import React, { useState, useEffect, useMemo, useDeferredValue, useRef } from 'react';
import Header from './components/Header';
import FileUpload from './components/FileUpload';
import ExactPivotView from './components/ExactPivotView';
import RekapProduktivitas from './components/RekapProduktivitas';
import EvaluasiKinerjaHarian from './components/EvaluasiKinerjaHarian';
import FieldSelector from './components/FieldSelector';
import SummaryCards from './components/SummaryCards';
import PivotTable from './components/PivotTable';
import RawDataModal from './components/RawDataModal';

import { parseExcelFile, autoDetectFields } from './utils/excelParser';
import { buildPivotTable } from './utils/pivotEngine';
import { buildExactPivot } from './utils/exactPivotEngine';
import { generateSampleData } from './sampleData/generateSample';
import { LayoutGrid, Table, ClipboardCheck, Sliders, FileSpreadsheet } from 'lucide-react';

export default function App() {
  // Dark mode state
  const [darkMode, setDarkMode] = useState(() => {
    return localStorage.getItem('theme') === 'dark' || 
      window.matchMedia('(prefers-color-scheme: dark)').matches;
  });

  useEffect(() => {
    if (darkMode) {
      document.documentElement.classList.add('dark');
      localStorage.setItem('theme', 'dark');
    } else {
      document.documentElement.classList.remove('dark');
      localStorage.setItem('theme', 'light');
    }
  }, [darkMode]);

  // Tab View: 'exact' (Format Contoh tabel) | 'rekap' (Matriks User x Tanggal) | 'charts' | 'custom'
  const [activeTab, setActiveTab] = useState('exact');

  // Workbook & Data state
  const [fileName, setFileName] = useState('');
  const [sheetNames, setSheetNames] = useState([]);
  const [activeSheet, setActiveSheet] = useState('');
  const [sheetsData, setSheetsData] = useState({});
  const [isLoading, setIsLoading] = useState(false);
  const [isRawModalOpen, setIsRawModalOpen] = useState(false);

  // Exact Pivot Filters (TGBON, USID, JENIS_B)
  const [selectedDate, setSelectedDate] = useState('ALL');
  const [selectedUser, setSelectedUser] = useState('ALL');
  const [selectedJenisB, setSelectedJenisB] = useState('ALL');
  const [searchQuery, setSearchQuery] = useState('');
  const deferredSearchQuery = useDeferredValue(searchQuery);
  const deferredSelectedDate = useDeferredValue(selectedDate);
  const deferredSelectedUser = useDeferredValue(selectedUser);
  const deferredSelectedJenisB = useDeferredValue(selectedJenisB);

  const exactPivotCacheRef = useRef({});
  const customPivotCacheRef = useRef({});

  // Custom Pivot Configuration state (for custom tab)
  const [customConfig, setCustomConfig] = useState({
    rowField: 'USID',
    colField: 'TGBON',
    valField: 'NOINV',
    aggType: 'DISTINCT_COUNT',
    dateFormat: 'daily',
    sortBy: 'total_desc',
    filters: {},
    dateRange: { start: '', end: '' },
    searchQuery: ''
  });

  // Current active rows & headers
  const currentSheetData = useMemo(() => {
    if (!activeSheet || !sheetsData[activeSheet]) {
      return { headers: [], rows: [], rowCount: 0 };
    }
    return sheetsData[activeSheet];
  }, [activeSheet, sheetsData]);

  // Handle uploaded file
  const handleFileLoaded = async (file) => {
    try {
      setIsLoading(true);
      const parsed = await parseExcelFile(file);
      
      exactPivotCacheRef.current = {};
      customPivotCacheRef.current = {};

      setFileName(file.name);
      setSheetNames(parsed.sheetNames);
      setSheetsData(parsed.sheetsData);
      setActiveSheet(parsed.defaultSheet);

      // Default filters
      setSelectedDate('ALL');
      setSelectedUser('ALL');
      setSelectedJenisB('ALL');
      setSearchQuery('');
      setActiveTab('exact');
    } catch (err) {
      alert('Gagal membaca file Excel: ' + err.message);
    } finally {
      setIsLoading(false);
    }
  };

  // Auto-load JULI - CLOSING.xlsx on first startup if user hasn't uploaded yet
  useEffect(() => {
    async function loadInitialData() {
      // In local dev/electron environment, we can check if file is present
      try {
        const response = await fetch('./JULI - CLOSING.xlsx');
        if (response.ok) {
          const blob = await response.blob();
          await handleFileLoaded(new File([blob], 'JULI - CLOSING.xlsx'));
        }
      } catch {
        // file not accessible via fetch in standard preview, user can upload
      }
    }
    loadInitialData();
  }, []);

  // Handle Loading Sample Demo Data
  const handleLoadSample = () => {
    setIsLoading(true);
    setTimeout(() => {
      const sampleRows = generateSampleData();
      const headers = ['NOINV', 'NMPG', 'USID', 'TGBON', 'KDPRC', 'JMDOS', 'NETTO', 'JENIS_B'];
      
      const mappedSample = sampleRows.map((r, i) => ({
        NOINV: r['No Invoice'],
        NMPG: r['Nama Admin'],
        USID: r['Nama Admin'].split(' ')[0].toUpperCase(),
        TGBON: r['Tanggal'],
        KDPRC: 'UCI',
        JMDOS: Math.floor(1 + Math.random() * 4),
        NETTO: r['Nominal (Rp)'],
        JENIS_B: 'INVOICE'
      }));

      setFileName('Contoh_Data_Produktivitas_Admin.xlsx');
      setSheetNames(['Sheet1']);
      setActiveSheet('Sheet1');
      setSheetsData({
        'Sheet1': {
          headers,
          rows: mappedSample,
          rowCount: mappedSample.length,
          columnStats: {}
        }
      });

      setSelectedDate('ALL');
      setSelectedUser('ALL');
      setSelectedJenisB('ALL');
      setActiveTab('exact');
      setIsLoading(false);
    }, 250);
  };

  // Compute Exact Pivot Data (NMPG, NOINV, KDPRC, USID with Count JMDOS & Sum NETTO)
  const exactPivotData = useMemo(() => {
    const cacheKey = `${activeSheet}|${deferredSelectedDate}|${deferredSelectedUser}|${deferredSelectedJenisB}|${deferredSearchQuery}`;

    if (!exactPivotCacheRef.current[cacheKey]) {
      exactPivotCacheRef.current[cacheKey] = buildExactPivot({
        rows: currentSheetData.rows,
        selectedDate: deferredSelectedDate,
        selectedUser: deferredSelectedUser,
        selectedJenisB: deferredSelectedJenisB,
        searchQuery: deferredSearchQuery
      });
    }

    return exactPivotCacheRef.current[cacheKey];
  }, [activeSheet, currentSheetData.rows, deferredSelectedDate, deferredSelectedUser, deferredSelectedJenisB, deferredSearchQuery]);

  // Compute Custom Pivot Table
  const customPivotResult = useMemo(() => {
    if (!currentSheetData.rows || currentSheetData.rows.length === 0 || !customConfig.rowField) {
      return null;
    }

    const cacheKey = `${activeSheet}|${customConfig.rowField}|${customConfig.colField}|${customConfig.valField}|${customConfig.aggType}|${customConfig.dateFormat}|${customConfig.sortBy}|${customConfig.searchQuery || ''}`;

    if (!customPivotCacheRef.current[cacheKey]) {
      customPivotCacheRef.current[cacheKey] = buildPivotTable(currentSheetData.rows, customConfig);
    }

    return customPivotCacheRef.current[cacheKey];
  }, [activeSheet, currentSheetData.rows, customConfig]);

  const hasData = currentSheetData.rows && currentSheetData.rows.length > 0;

  return (
    <div className="min-h-screen flex flex-col bg-slate-50 dark:bg-slate-950 text-slate-900 dark:text-slate-100 transition-colors overflow-x-hidden">
      
      {/* App Header */}
      <Header
        darkMode={darkMode}
        setDarkMode={setDarkMode}
        hasData={hasData}
        fileName={fileName}
        sheetNames={sheetNames}
        activeSheet={activeSheet}
        onSheetChange={(s) => setActiveSheet(s)}
        rowCount={currentSheetData.rowCount}
        onOpenRawData={() => setIsRawModalOpen(true)}
        onLoadSample={handleLoadSample}
        onReset={() => {
          exactPivotCacheRef.current = {};
          customPivotCacheRef.current = {};
          setFileName('');
          setSheetsData({});
        }}
      />

      {/* Main Content Area */}
      <main className="flex-1 max-w-7xl w-full mx-auto px-4 sm:px-6 lg:px-8 py-6">
        
        {!hasData ? (
          /* Upload / Welcome Screen */
          <FileUpload
            onFileLoaded={handleFileLoaded}
            onLoadSample={handleLoadSample}
            isLoading={isLoading}
          />
        ) : (
          /* Main Workspace View */
          <div>
            
            {/* View Navigation Tabs */}
            <div className="flex flex-col gap-3 pb-4 mb-6 border-b border-slate-200 dark:border-slate-800">
              <div className="flex flex-wrap items-center justify-between gap-3">
                <div className="flex flex-wrap items-center gap-1.5 bg-slate-200/80 dark:bg-slate-850 p-1 rounded-xl w-full lg:w-auto overflow-x-auto scrollbar-thin">
                  
                  {/* TAB 1: Exact Pivot Format Contoh Tabel */}
                  <button
                    onClick={() => setActiveTab('exact')}
                    className={`inline-flex items-center justify-center gap-2 px-3 py-2 rounded-lg text-[10px] sm:text-xs font-bold transition-all min-w-[160px] lg:min-w-0 flex-1 lg:flex-none ${
                      activeTab === 'exact'
                        ? 'bg-white dark:bg-slate-900 text-brand-600 dark:text-brand-400 shadow-sm'
                        : 'text-slate-600 dark:text-slate-400 hover:text-slate-900 dark:hover:text-white'
                    }`}
                  >
                    <Table className="w-4 h-4 shrink-0" />
                    <span className="whitespace-nowrap">Tabel Pivot Detail</span>
                  </button>

                  {/* TAB 2: Rekap Matriks User x Tanggal */}
                  <button
                    onClick={() => setActiveTab('rekap')}
                    className={`inline-flex items-center justify-center gap-2 px-3 py-2 rounded-lg text-[10px] sm:text-xs font-bold transition-all min-w-[160px] lg:min-w-0 flex-1 lg:flex-none ${
                      activeTab === 'rekap'
                        ? 'bg-white dark:bg-slate-900 text-brand-600 dark:text-brand-400 shadow-sm'
                        : 'text-slate-600 dark:text-slate-400 hover:text-slate-900 dark:hover:text-white'
                    }`}
                  >
                    <LayoutGrid className="w-4 h-4 shrink-0" />
                    <span className="whitespace-nowrap">Rekap Matriks</span>
                  </button>

                  {/* TAB 3: Evaluasi Kinerja & Absensi Harian */}
                  <button
                    onClick={() => setActiveTab('evaluasi')}
                    className={`inline-flex items-center justify-center gap-2 px-3 py-2 rounded-lg text-[10px] sm:text-xs font-bold transition-all min-w-[160px] lg:min-w-0 flex-1 lg:flex-none ${
                      activeTab === 'evaluasi'
                        ? 'bg-white dark:bg-slate-900 text-brand-600 dark:text-brand-400 shadow-sm'
                        : 'text-slate-600 dark:text-slate-400 hover:text-slate-900 dark:hover:text-white'
                    }`}
                  >
                    <ClipboardCheck className="w-4 h-4 shrink-0" />
                    <span className="whitespace-nowrap">Kinerja & Absensi</span>
                  </button>

                  {/* TAB 4: Custom Pivot Builder */}
                  <button
                    onClick={() => setActiveTab('custom')}
                    className={`inline-flex items-center justify-center gap-2 px-3 py-2 rounded-lg text-[10px] sm:text-xs font-bold transition-all min-w-[160px] lg:min-w-0 flex-1 lg:flex-none ${
                      activeTab === 'custom'
                        ? 'bg-white dark:bg-slate-900 text-brand-600 dark:text-brand-400 shadow-sm'
                        : 'text-slate-600 dark:text-slate-400 hover:text-slate-900 dark:hover:text-white'
                    }`}
                  >
                    <Sliders className="w-4 h-4 shrink-0" />
                    <span className="whitespace-nowrap">Custom Pivot</span>
                  </button>

                </div>

                {/* Quick Status Tag */}
                <div className="text-[11px] sm:text-xs text-slate-500 font-medium break-words max-w-full">
                  Data aktif: <b className="text-slate-800 dark:text-slate-200">{fileName || 'Excel'}</b> ({currentSheetData.rowCount.toLocaleString('id-ID')} baris)
                </div>
              </div>
            </div>

            {/* TAB 1: EXACT PIVOT VIEW (FORMAT CONTOH TABEL) */}
            {activeTab === 'exact' && (
              <ExactPivotView
                exactPivotData={exactPivotData}
                selectedDate={selectedDate}
                setSelectedDate={setSelectedDate}
                selectedUser={selectedUser}
                setSelectedUser={setSelectedUser}
                selectedJenisB={selectedJenisB}
                setSelectedJenisB={setSelectedJenisB}
                searchQuery={searchQuery}
                setSearchQuery={setSearchQuery}
                fileName={fileName}
              />
            )}

            {/* TAB 2: REKAP PRODUKTIVITAS MATRIKS (USER x TANGGAL) */}
            {activeTab === 'rekap' && (
              <RekapProduktivitas
                availableUsers={exactPivotData.availableUsers}
                availableDates={exactPivotData.availableDates}
                userStats={exactPivotData.userStats}
                dateStats={exactPivotData.dateStats}
                grandTotalNotaKeseluruhan={exactPivotData.grandTotalNotaKeseluruhan}
                rows={currentSheetData.rows}
                onSelectUser={(u) => {
                  setSelectedUser(u);
                  setActiveTab('exact');
                }}
                onSelectDate={(d) => {
                  setSelectedDate(d);
                  setActiveTab('exact');
                }}
              />
            )}

            {/* TAB 3: EVALUASI KINERJA & ABSENSI HARIAN */}
            {activeTab === 'evaluasi' && (
              <EvaluasiKinerjaHarian
                rows={currentSheetData.rows}
                fileName={fileName}
              />
            )}

            {/* TAB 4: CUSTOM PIVOT BUILDER */}
            {activeTab === 'custom' && (
              <div>
                <FieldSelector
                  headers={currentSheetData.headers}
                  config={customConfig}
                  onChangeConfig={(c) => setCustomConfig(prev => ({ ...prev, ...c }))}
                  onApplyPreset={() => {}}
                />
                {customPivotResult && (
                  <>
                    <SummaryCards
                      summaryStats={customPivotResult.summaryStats}
                      aggType={customConfig.aggType}
                      valField={customConfig.valField}
                    />
                    <PivotTable
                      pivotResult={customPivotResult}
                      config={customConfig}
                      fileName={fileName}
                    />
                  </>
                )}
              </div>
            )}

          </div>
        )}

      </main>

      {/* Raw Data Preview Modal */}
      <RawDataModal
        isOpen={isRawModalOpen}
        onClose={() => setIsRawModalOpen(false)}
        rows={currentSheetData.rows}
        headers={currentSheetData.headers}
        sheetName={activeSheet}
      />

      {/* Footer */}
      <footer className="border-t border-slate-200 dark:border-slate-800/80 py-4 text-center text-xs text-slate-400 no-print">
        <p>Laporan Produktivitas Admin & Pivot Studio • Format Contoh Tabel</p>
      </footer>

    </div>
  );
}

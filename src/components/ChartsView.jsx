import React, { useState } from 'react';
import {
  Chart as ChartJS,
  CategoryScale,
  LinearScale,
  BarElement,
  PointElement,
  LineElement,
  ArcElement,
  Title,
  Tooltip,
  Legend,
  Filler
} from 'chart.js';
import { Bar, Line, Doughnut } from 'react-chartjs-2';
import { BarChart3, LineChart, PieChart, Sparkles } from 'lucide-react';

ChartJS.register(
  CategoryScale,
  LinearScale,
  BarElement,
  PointElement,
  LineElement,
  ArcElement,
  Title,
  Tooltip,
  Legend,
  Filler
);

export default function ChartsView({ pivotResult, config }) {
  const [activeChart, setActiveChart] = useState('bar'); // 'bar' | 'line' | 'doughnut'

  const {
    columns = [],
    rowKeys = [],
    rowTotals = {},
    colTotals = {},
    grandTotal = 0
  } = pivotResult || {};

  const { rowField = 'User', colField = 'Tanggal', valField = 'NoInv' } = config;

  if (!rowKeys || rowKeys.length === 0) return null;

  // Modern Color Palette
  const palette = [
    'rgba(14, 140, 233, 0.85)',
    'rgba(99, 102, 241, 0.85)',
    'rgba(168, 85, 247, 0.85)',
    'rgba(236, 72, 153, 0.85)',
    'rgba(16, 185, 129, 0.85)',
    'rgba(245, 158, 11, 0.85)',
    'rgba(239, 68, 68, 0.85)',
    'rgba(20, 184, 166, 0.85)',
    'rgba(139, 92, 246, 0.85)',
    'rgba(6, 182, 212, 0.85)'
  ];

  // 1. Data Bar Chart (User vs Total)
  const barData = {
    labels: rowKeys,
    datasets: [
      {
        label: `Total ${valField || 'No. Invoice'}`,
        data: rowKeys.map(r => rowTotals[r] || 0),
        backgroundColor: rowKeys.map((_, i) => palette[i % palette.length]),
        borderColor: rowKeys.map((_, i) => palette[i % palette.length].replace('0.85', '1')),
        borderWidth: 1,
        borderRadius: 8,
      }
    ]
  };

  // 2. Data Line Chart (Trend per Tanggal)
  const lineData = {
    labels: columns.length > 0 ? columns : rowKeys,
    datasets: [
      {
        label: columns.length > 0 ? 'Total NoInv per Tanggal' : 'Total NoInv',
        data: columns.length > 0 ? columns.map(c => colTotals[c] || 0) : rowKeys.map(r => rowTotals[r] || 0),
        borderColor: '#0e8ce9',
        backgroundColor: 'rgba(14, 140, 233, 0.12)',
        fill: true,
        tension: 0.35,
        pointBackgroundColor: '#0e8ce9',
        pointRadius: 4,
        pointHoverRadius: 6
      }
    ]
  };

  // 3. Data Doughnut Chart (Kontribusi % User)
  const doughnutData = {
    labels: rowKeys,
    datasets: [
      {
        data: rowKeys.map(r => rowTotals[r] || 0),
        backgroundColor: palette,
        borderWidth: 2,
        borderColor: '#ffffff',
      }
    ]
  };

  const chartOptions = {
    responsive: true,
    maintainAspectRatio: false,
    plugins: {
      legend: {
        position: activeChart === 'doughnut' ? 'right' : 'top',
        labels: {
          boxWidth: 12,
          font: { family: 'Plus Jakarta Sans', size: 11 }
        }
      },
      tooltip: {
        padding: 10,
        cornerRadius: 8,
        titleFont: { family: 'Plus Jakarta Sans', size: 12, weight: 'bold' },
        bodyFont: { family: 'JetBrains Mono', size: 12 },
      }
    },
    scales: activeChart === 'doughnut' ? {} : {
      y: {
        beginAtZero: true,
        grid: { color: 'rgba(226, 232, 240, 0.6)' },
        ticks: { font: { family: 'JetBrains Mono', size: 10 } }
      },
      x: {
        grid: { display: false },
        ticks: { font: { family: 'Plus Jakarta Sans', size: 10 } }
      }
    }
  };

  return (
    <div className="bg-white dark:bg-slate-900 rounded-2xl border border-slate-200 dark:border-slate-800 shadow-soft p-5 mb-8 transition-colors">
      
      {/* Header & Chart Switcher */}
      <div className="flex flex-wrap items-center justify-between gap-3 pb-4 border-b border-slate-100 dark:border-slate-800">
        <div>
          <h3 className="text-sm font-bold text-slate-900 dark:text-white flex items-center space-x-2">
            <BarChart3 className="w-4 h-4 text-brand-500" />
            <span>Grafik Visualisasi Produktivitas</span>
          </h3>
          <p className="text-xs text-slate-500 dark:text-slate-400">
            Perbandingan kinerja admin dan tren harian secara grafis
          </p>
        </div>

        {/* Tabs */}
        <div className="flex items-center bg-slate-100 dark:bg-slate-800 p-1 rounded-xl">
          <button
            onClick={() => setActiveChart('bar')}
            className={`inline-flex items-center space-x-1 px-3 py-1.5 rounded-lg text-xs font-semibold transition-all ${
              activeChart === 'bar'
                ? 'bg-white dark:bg-slate-900 text-brand-600 dark:text-brand-400 shadow-sm'
                : 'text-slate-600 dark:text-slate-400 hover:text-slate-900 dark:hover:text-white'
            }`}
          >
            <BarChart3 className="w-3.5 h-3.5" />
            <span>Kinerja Admin</span>
          </button>

          {columns.length > 0 && (
            <button
              onClick={() => setActiveChart('line')}
              className={`inline-flex items-center space-x-1 px-3 py-1.5 rounded-lg text-xs font-semibold transition-all ${
                activeChart === 'line'
                  ? 'bg-white dark:bg-slate-900 text-brand-600 dark:text-brand-400 shadow-sm'
                  : 'text-slate-600 dark:text-slate-400 hover:text-slate-900 dark:hover:text-white'
              }`}
            >
              <LineChart className="w-3.5 h-3.5" />
              <span>Tren Tanggal</span>
            </button>
          )}

          <button
            onClick={() => setActiveChart('doughnut')}
            className={`inline-flex items-center space-x-1 px-3 py-1.5 rounded-lg text-xs font-semibold transition-all ${
              activeChart === 'doughnut'
                ? 'bg-white dark:bg-slate-900 text-brand-600 dark:text-brand-400 shadow-sm'
                : 'text-slate-600 dark:text-slate-400 hover:text-slate-900 dark:hover:text-white'
            }`}
          >
            <PieChart className="w-3.5 h-3.5" />
            <span>Pangsa Kontribusi</span>
          </button>
        </div>
      </div>

      {/* Chart Canvas Container */}
      <div className="pt-4 h-72 sm:h-80 w-full">
        {activeChart === 'bar' && <Bar data={barData} options={chartOptions} />}
        {activeChart === 'line' && <Line data={lineData} options={chartOptions} />}
        {activeChart === 'doughnut' && <Doughnut data={doughnutData} options={chartOptions} />}
      </div>

    </div>
  );
}

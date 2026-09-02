# 📊 Laporan Produktivitas Admin - Pivot Excel Studio (Desktop App)

Aplikasi Desktop modern untuk mengolah data mentah dari file **Excel Workbook (`.xlsx`, `.xls`, `.csv`)** menjadi **Tabel Pivot Produktivitas Interaktif**, menghitung total transaksi/nomor invoice per user per tanggal, serta Grand Total keseluruhan secara otomatis.

---

## 🚀 Cara Menjalankan Aplikasi

Cukup **Double-Click** salah satu file di bawah ini:
1. **`jalankan-aplikasi.bat`** : Menjalankan aplikasi langsung sebagai jendela **Aplikasi Desktop** (Electron).
2. **`buka-di-browser.bat`** : Menjalankan aplikasi di Google Chrome / Microsoft Edge.

---

## ✨ Fitur Utama

### 1. 🧮 Perhitungan Otomatis Sesuai Kebutuhan Anda
- **Perhitungan No. Invoice per User per Tanggal**: Menghitung secara akurat jumlah `noinv` yang dikerjakan oleh masing-masing user/admin di setiap tanggal.
- **Subtotal per Admin**: Total akumulasi yang dikerjakan tiap admin selama seluruh periode.
- **Subtotal per Tanggal**: Total akumulasi seluruh admin pada tanggal tersebut.
- **Grand Total (Keseluruhan)**: Total keseluruhan invoice dari seluruh admin di seluruh tanggal.

### 2. 🎛️ Dynamic Pivot Builder (Bebas Tentukan Field)
Anda dapat memilih field apa saja dari file Excel Anda:
- **Field Baris (Rows)**: Pilih field Admin / Petugas / User / Divisi.
- **Field Kolom (Columns)**: Pilih field Tanggal / Periode / Status.
- **Field Nilai (Values)**: Pilih field `No Invoice`, `Nominal (Rp)`, atau hitung total baris.
- **Rumus Agregasi**:
  - `COUNT` : Menghitung jumlah total transaksi / kemunculan baris.
  - `DISTINCT COUNT` : Menghitung jumlah nomor invoice unik (mencegah duplikasi).
  - `SUM` : Menjumlahkan nominal / angka rupiah.
  - `AVG` : Menghitung rata-rata.
  - `MAX / MIN` : Nilai tertinggi atau terendah.

### 3. 📋 Kinerja & Absensi Harian Admin (Baru)
- **Perhitungan Otomatis Nota Dibuat per Hari**: Menghitung jumlah nomor invoice yang dibuat oleh setiap admin pada setiap tanggal.
- **Input Kolom Nota Salah / Revisi**: Memungkinkan penginputan jumlah nota salah/error yang terjadi per user per hari.
- **Kalkulasi Otomatis Nota Valid & % Akurasi**: Menghitung nota bersih (`Total Nota - Nota Salah`) dan tingkat akurasi kinerja (`Akurasi %`).
- **Input Jam Datang & Jam Pulang**: Kolom penginputan jam kedatangan dan jam kepulangan admin dengan format waktu (HH:MM).
- **Perhitungan Otomatis Durasi Jam Kerja**: Mengkalkulasi selisih jam kerja harian secara otomatis.
- **Penyimpanan Otomatis (Auto-Save)**: Data inputan tidak hilang saat berpindah tab atau menutup browser/aplikasi.
- **Export Laporan Excel & Cetak Dokumen**: Laporan evaluasi harian lengkap dapat langsung diunduh ke Excel (.xlsx) atau dicetak.

### 4. 💾 Export Laporan Lengkap Siap Pakai
- **Export Excel (.xlsx)**: File Excel rapi dengan header bertema, border, format angka, subtotal per user, dan baris Grand Total tebal.
- **Export PDF / Cetak**: Laporan dokumen resmi dengan ringkasan KPI dan tabel pivot / evaluasi harian.
- **Cetak / Print**: Mendukung cetak langsung ke printer fisik atau Save to PDF bawaan Windows.

### 5. 🔍 Fitur Tambahan
- **Sheet Switcher**: Jika file Excel memiliki banyak sheet (tab), Anda bisa langsung beralih sheet dari dropdown di header.
- **Preview Data Mentah**: Menampilkan seluruh data mentah Excel dengan pencarian & pagination.
- **Data Sampel Demo**: Tersedia tombol coba data demo langsung jika ingin menguji fitur aplikasi tanpa file Excel sendiri.
- **Mode Gelap (Dark Mode) & Mode Terang**.

---

## 🛠️ Struktur Project

```
D:\Laporan Produktifitas Admin\
├── electron/
│   ├── main.js                 # Proses utama aplikasi desktop Electron
│   └── preload.js              # Preload bridge API
├── src/
│   ├── components/
│   │   ├── Header.jsx              # Header aplikasi, sheet selector, dark mode
│   │   ├── FileUpload.jsx          # Area upload drag-and-drop & fitur demo
│   │   ├── ExactPivotView.jsx      # Tabel Pivot Detail (Format Contoh Tabel)
│   │   ├── RekapProduktivitas.jsx  # Matriks User x Tanggal
│   │   ├── EvaluasiKinerjaHarian.jsx # Monitoring Kinerja, Nota Salah & Jam Kerja Admin
│   │   ├── FieldSelector.jsx       # Pengaturan dinamis field Baris, Kolom, Nilai
│   │   ├── SummaryCards.jsx        # Kartu metrik KPI produktivitas
│   │   ├── PivotTable.jsx          # Tabel Pivot Matriks, Heatmap & Subtotal/Grand Total
│   │   └── RawDataModal.jsx        # Modal preview data mentah Excel
│   ├── utils/
│   │   ├── excelParser.js          # Parser Excel (.xlsx, .xls, .csv) & auto-detector
│   │   ├── evaluasiEngine.js       # Engine kalkulasi nota harian, akurasi & durasi kerja
│   │   ├── evaluasiExporter.js     # Export Excel laporan evaluasi kinerja & absensi
│   │   ├── exactPivotEngine.js     # Engine pivot tabel detail
│   │   ├── exactExporter.js        # Exporter Excel tabel pivot detail
│   │   ├── pivotEngine.js          # Engine kalkulasi pivot matriks & agregasi
│   │   └── exporter.js             # Export ExcelJS & jsPDF ber-styling
│   ├── sampleData/
│   │   └── generateSample.js       # Generator data simulasi invoice & admin
│   ├── App.jsx                     # Komponen utama
│   ├── main.jsx                    # Entry point React
│   └── index.css                   # Styling Tailwind CSS
├── jalankan-aplikasi.bat           # Launcher 1-klik Aplikasi Desktop
├── buka-di-browser.bat             # Launcher 1-klik Mode Browser
└── package.json
```

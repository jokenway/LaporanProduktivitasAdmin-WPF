# 📊 Laporan Produktivitas Admin - Native WPF .NET 8 Studio

Aplikasi Desktop Native Windows berperforma tinggi berbasis **C#, .NET 8, dan WPF (Windows Presentation Foundation)** untuk mengolah data mentah dari workbook **Excel (`.xlsx`)** menjadi **Tabel Pivot Produktivitas**, analisis kinerja, monitoring absensi, dan kalkulasi nomor nota fakturis secara otomatis dan instan.

---

## 🚀 Cara Menjalankan Aplikasi

Cukup **Double-Click** file launcher utama:
* **`jalankan-aplikasi-wpf.bat`**

Atau jalankan langsung file executable tunggal (*standalone single-file zero-dependency*):
* `LaporanProduktivitasWPF\bin\Release\standalone\LaporanProduktivitasAdmin.exe`

---

## ✨ Fitur Utama

1. **📊 Dashboard Produktivitas**:
   - Ringkasan KPI: Grand Total Nota, Top Performer Admin, Rata-rata per Admin & Hari.
   - Grafik batang interaktif 10 Admin dengan volume nota tertinggi.

2. **📋 Tabel Pivot Detail (Exact Pivot Excel)**:
   - Dimensi: Row (`NMPG`), Column (`TGBON`), Nilai: `Count of NOINV`, `Sum of JMDOS`, `Sum of NETTO`.
   - Filter cepat berdasarkan Tanggal, Nama Admin, Jenis Barang, dan pencarian instan.
   - Export hasil pivot ke format CSV/Excel.

3. **⏱️ Monitoring Kinerja & Absensi Admin**:
   - Kalkulasi otomatis jumlah nota dibuat per admin per hari.
   - Kolom editable langsung di tabel: `Nota Salah`, `Jam Datang`, `Jam Pulang`, `Catatan`.
   - Tombol otomatisasi: *"Isi Jam Standar (08:00 - 17:00)"*.
   - Auto-calculate durasi kerja dan persentase akurasi kerja.
   - Penyimpanan data otomatis ke penyimpanan lokal (`%AppData%`).

4. **👥 Evaluasi Grup Admin Invoice**:
   - Analisis mendalam tim fakturis (`AKBAR`, `DIDIN`, `JOE`, `RONI`, `NOVIANI`).
   - Rasio kesalahan terhadap total grup tim, rata-rata harian, dan persentase kontribusi.

5. **⚙️ Custom Pivot Dinamis**:
   - Konfigurasi dimensi baris, kolom, nilai, dan rumus agregasi (`DISTINCT COUNT`, `COUNT`, `SUM`, `AVG`).
   - Preset otomatis untuk analisis cepat.

6. **⚡ Performa Tinggi & UI Virtualization**:
   - Mampu memuat puluhan ribu baris data (seperti file `JULI - CLOSING.xlsx` berisi 53.796 baris) secara instan tanpa lag atau UI freezing.
   - Header tabel jelas dengan kontras tinggi (*High-Contrast Bold Text*).

---

## 🛠️ Struktur Direktori Proyek

```
Laporan Produktifitas Admin/
├── jalankan-aplikasi-wpf.bat             # Launcher utama desktop Windows
├── Contoh tabel.xlsx                     # Dataset contoh demo
├── JULI - CLOSING.xlsx                   # Dataset produksi (53.796 baris)
├── README.md                             # Dokumentasi proyek
└── LaporanProduktivitasWPF/              # Source code C# / .NET 8 WPF
    ├── LaporanProduktivitasWPF.csproj    # File project modern .NET 8 SDK
    ├── App.xaml / App.xaml.cs            # Tema global & penanganan error
    ├── MainWindow.xaml / .cs             # Antarmuka 6 tab & navigasi
    ├── Models/Models.cs                  # Model data (Pivot, Evaluasi, Chart)
    ├── Services/
    │   ├── ExcelService.cs               # Parser OpenXML streaming cepat
    │   ├── ExactPivotEngine.cs           # Kalkulasi formula pivot Excel
    │   ├── EvaluasiEngine.cs             # Kalkulasi kinerja & durasi kerja
    │   └── StorageService.cs             # Penyimpanan JSON lokal
    └── ViewModels/
        ├── ViewModelBase.cs              # Base class MVVM INotifyPropertyChanged
        └── MainViewModel.cs              # Controller logika & data binding
```

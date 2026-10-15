# Pengujian dan TDD

Tes berada di `LaporanProduktivitasWPF.Tests` dan menggunakan xUnit dengan referensi
langsung ke proyek aplikasi. Tes menguji kode produksi, bukan salinan algoritmanya.
Jalankan pada Windows dengan .NET SDK dan .NET 8 Windows Desktop Runtime.
Pemulihan paket pertama membutuhkan akses NuGet.

```powershell
dotnet test LaporanProduktivitasAdmin.sln
dotnet build LaporanProduktivitasAdmin.sln -c Release
```

Untuk menyimpan hasil tes dan coverage lokal:

```powershell
dotnet test LaporanProduktivitasAdmin.sln --collect:"XPlat Code Coverage" --logger trx
```

Hasil dibuat di `TestResults` dan tidak diikutkan ke Git.

## Hasil penerapan awal (15 Oktober 2026)

| Tahap | Lulus | Gagal | Bukti lokal |
| --- | ---: | ---: | --- |
| Red, sebelum perubahan kode produksi | 54 | 10 | `LaporanProduktivitasWPF.Tests/TestResults/tdd-red.trx` |
| Green, setelah perbaikan | 64 | 0 | `LaporanProduktivitasWPF.Tests/TestResults/tdd-green.trx` |
| Release dengan collector coverage | 64 | 0 | `LaporanProduktivitasWPF.Tests/TestResults/tdd-release.trx` |
| Setelah refactor pemeriksaan periode | 64 | 0 | `LaporanProduktivitasWPF.Tests/TestResults/tdd-refactor.trx` |

Sepuluh kegagalan awal membuktikan tiga masalah: karakter asing/tanda negatif
di input jam dapat dibuang sehingga input rusak dianggap valid; periode evaluasi
mencampur tahun berbeda; dan alias kolom pivot dipakai saat filter tetapi belum
konsisten pada pengelompokan/pencarian. Ketiganya diperbaiki setelah hasil Red
tercatat. Pembacaan alias dan pemeriksaan periode dipusatkan agar konsisten.

Build solution Release setelah refactor berhasil dengan 0 warning dan 0 error.
Target legacy `LaporanProduktivitasWPF.Win7.csproj` belum dibangun atau dijalankan
dalam pengujian ini. File TRX dan coverage adalah hasil lokal yang dapat dibuat
ulang, bukan berkas yang diikutkan ke Git.

## Siklus perubahan berikutnya

1. Tulis tes untuk perilaku yang diinginkan atau kasus bug yang dapat direproduksi.
2. Jalankan tes dan pastikan gagal karena perilaku tersebut, bukan karena build atau konfigurasi.
3. Perbaiki kode secukupnya sampai tes lulus.
4. Rapikan kode tanpa mengubah perilaku, lalu jalankan seluruh tes dan build kembali.

## Cakupan

- Input jam: format singkat, titik/titik dua, input kosong, penolakan input rusak,
  mempertahankan nilai lama saat edit tidak valid, durasi lintas tengah malam.
- Evaluasi: tanggal Excel dan Indonesia, nota unik, hari Minggu, periode dominan
  berdasarkan tahun dan bulan, log manual, filter, ringkasan, anggota tim tanpa nota.
- Pivot: agregasi baris, dimensi tanggal/jenis barang, filter, grand total,
  nama kolom alternatif, pencarian, dan input kosong.
- Akun: izin admin/staff/viewer, termasuk nama akun khusus tanpa peran admin.
- Excel/CSV: worksheet relationship, shared strings, kolom kosong, tanggal serial,
  escaping CSV dan teks berawalan formula. Workbook sintetis dibuat di folder
  sementara dan dihapus setelah setiap tes.

Tes ini tidak membuka UI WPF dan tidak mengakses PostgreSQL atau konfigurasi akun
lokal. Login, navigasi, penyimpanan bersamaan, migrasi database, dan alur UI perlu
pengujian integrasi tersendiri. Keberhasilan tes ini tidak menyatakan seluruh
aplikasi bebas bug atau seluruh baris kode tercakup.

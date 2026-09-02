import ExcelJS from 'exceljs';
import { saveAs } from 'file-saver';

/**
 * Export Laporan Evaluasi Kinerja, Nota Salah, dan Absensi Harian ke file Excel (.xlsx)
 */
export async function exportEvaluasiToExcel({
  records = [],
  summary = {},
  selectedDate = 'ALL',
  selectedUser = 'ALL',
  filename = 'Laporan_Evaluasi_Kinerja_Admin.xlsx'
}) {
  const workbook = new ExcelJS.Workbook();
  workbook.creator = 'Laporan Produktivitas Admin';
  workbook.created = new Date();

  const worksheet = workbook.addWorksheet('Kinerja & Absensi Harian', {
    views: [{ showGridLines: true }]
  });

  // 1. Judul Laporan
  worksheet.mergeCells('A1:K1');
  const titleCell = worksheet.getCell('A1');
  titleCell.value = 'LAPORAN KINERJA, EVALUASI NOTA & ABSENSI ADMIN';
  titleCell.font = { name: 'Calibri', size: 14, bold: true, color: { argb: 'FFFFFFFF' } };
  titleCell.fill = {
    type: 'pattern',
    pattern: 'solid',
    fgColor: { argb: 'FF0F172A' } // Slate 900
  };
  titleCell.alignment = { vertical: 'middle', horizontal: 'center' };
  worksheet.getRow(1).height = 32;

  // 2. Info Filter & Parameter
  worksheet.mergeCells('A2:D2');
  worksheet.getCell('A2').value = `Periode / Tanggal: ${selectedDate === 'ALL' ? 'SEMUA TANGGAL' : selectedDate}`;
  worksheet.getCell('A2').font = { name: 'Calibri', size: 10, bold: true, color: { argb: 'FF334155' } };

  worksheet.mergeCells('E2:H2');
  worksheet.getCell('E2').value = `Admin / Fakturis: ${selectedUser === 'ALL' ? 'SEMUA ADMIN' : selectedUser}`;
  worksheet.getCell('E2').font = { name: 'Calibri', size: 10, bold: true, color: { argb: 'FF0284C7' } };

  worksheet.mergeCells('I2:K2');
  worksheet.getCell('I2').value = `Dicetak: ${new Date().toLocaleDateString('id-ID', { day: '2-digit', month: 'long', year: 'numeric' })}`;
  worksheet.getCell('I2').font = { name: 'Calibri', size: 9, italic: true, color: { argb: 'FF64748B' } };
  worksheet.getCell('I2').alignment = { horizontal: 'right' };
  worksheet.getRow(2).height = 20;

  // 3. Ringkasan KPI Header
  const kpiRow = worksheet.getRow(3);
  kpiRow.height = 24;
  kpiRow.values = [
    'RINGKASAN:',
    `Total Nota: ${summary.totalNota || 0}`,
    '',
    `Total Salah: ${summary.totalSalah || 0}`,
    '',
    `Total Valid: ${summary.totalValid || 0}`,
    `Akurasi: ${summary.overallAkurasi || 100}%`,
    '',
    `Total Durasi: ${summary.totalJamKerjaText || '0 jam'}`,
    '',
    ''
  ];
  kpiRow.eachCell((cell) => {
    cell.font = { name: 'Calibri', size: 9, bold: true, color: { argb: 'FF1E293B' } };
    cell.fill = {
      type: 'pattern',
      pattern: 'solid',
      fgColor: { argb: 'FFF1F5F9' }
    };
    cell.border = {
      top: { style: 'thin', color: { argb: 'FFCBD5E1' } },
      bottom: { style: 'thin', color: { argb: 'FFCBD5E1' } }
    };
  });

  // Empty row separator
  worksheet.getRow(4).height = 10;

  // 4. Table Header
  const headerRow = worksheet.getRow(5);
  headerRow.height = 28;
  headerRow.values = [
    'NO',
    'TANGGAL',
    'USER FAKTURIS (USID)',
    'TOTAL NOTA',
    'NOTA SALAH (ERROR)',
    'NOTA VALID',
    'AKURASI (%)',
    'JAM DATANG',
    'JAM PULANG',
    'DURASI KERJA',
    'KETERANGAN / ALASAN'
  ];

  headerRow.eachCell((cell, colNumber) => {
    cell.font = { name: 'Calibri', size: 10, bold: true, color: { argb: 'FFFFFFFF' } };
    // Warna header bertema
    let bgColor = 'FF1E293B'; // Dark Slate default
    if (colNumber === 4) bgColor = 'FF0284C7'; // Blue for Total Nota
    if (colNumber === 5) bgColor = 'FFE11D48'; // Red for Nota Salah
    if (colNumber === 6) bgColor = 'FF059669'; // Emerald for Nota Valid
    if (colNumber === 7) bgColor = 'FF7C3AED'; // Purple for Akurasi
    if (colNumber === 8 || colNumber === 9) bgColor = 'FFD97706'; // Amber for Jam
    if (colNumber === 10) bgColor = 'FF0D9488'; // Teal for Durasi

    cell.fill = {
      type: 'pattern',
      pattern: 'solid',
      fgColor: { argb: bgColor }
    };
    cell.alignment = {
      vertical: 'middle',
      horizontal: colNumber === 1 || colNumber === 2 || (colNumber >= 7 && colNumber <= 10) ? 'center' : (colNumber >= 4 && colNumber <= 6 ? 'right' : 'left')
    };
    cell.border = {
      top: { style: 'medium', color: { argb: 'FF0F172A' } },
      bottom: { style: 'medium', color: { argb: 'FF0F172A' } },
      left: { style: 'thin', color: { argb: 'FF334155' } },
      right: { style: 'thin', color: { argb: 'FF334155' } }
    };
  });

  // 5. Data Rows
  records.forEach((rec, idx) => {
    const rowIdx = idx + 6;
    const r = worksheet.getRow(rowIdx);
    r.height = 22;

    r.values = [
      idx + 1,
      rec.date,
      rec.user,
      rec.totalNota,
      rec.notaSalah,
      rec.notaValid,
      `${rec.akurasi}%`,
      rec.jamDatang || '-',
      rec.jamPulang || '-',
      rec.durasiText || '-',
      rec.keterangan || ''
    ];

    const isEven = idx % 2 === 0;
    const bgArgb = isEven ? 'FFFFFFFF' : 'FFF8FAFC';

    r.eachCell((cell, colNumber) => {
      cell.font = { name: 'Calibri', size: 10 };
      cell.fill = {
        type: 'pattern',
        pattern: 'solid',
        fgColor: { argb: bgArgb }
      };

      // Alignment
      if (colNumber === 1 || colNumber === 2 || (colNumber >= 7 && colNumber <= 10)) {
        cell.alignment = { vertical: 'middle', horizontal: 'center' };
      } else if (colNumber >= 4 && colNumber <= 6) {
        cell.alignment = { vertical: 'middle', horizontal: 'right' };
      } else {
        cell.alignment = { vertical: 'middle', horizontal: 'left' };
      }

      // Border
      cell.border = {
        top: { style: 'thin', color: { argb: 'FFE2E8F0' } },
        bottom: { style: 'thin', color: { argb: 'FFE2E8F0' } },
        left: { style: 'thin', color: { argb: 'FFE2E8F0' } },
        right: { style: 'thin', color: { argb: 'FFE2E8F0' } }
      };

      // Highlight Nota Salah > 0
      if (colNumber === 5 && rec.notaSalah > 0) {
        cell.font = { name: 'Calibri', size: 10, bold: true, color: { argb: 'FFE11D48' } };
        cell.fill = {
          type: 'pattern',
          pattern: 'solid',
          fgColor: { argb: 'FFFFF1F2' }
        };
      }

      // Format Numbers
      if (colNumber === 4 || colNumber === 5 || colNumber === 6) {
        cell.numFmt = '#,##0';
      }
    });
  });

  // 6. Grand Total Row
  const totalRowIdx = records.length + 6;
  const totalRow = worksheet.getRow(totalRowIdx);
  totalRow.height = 26;

  totalRow.values = [
    '',
    'TOTAL KESELURUHAN',
    `${summary.userCount || 0} User (${summary.recordCount || 0} Hari Kerja)`,
    summary.totalNota || 0,
    summary.totalSalah || 0,
    summary.totalValid || 0,
    `${summary.overallAkurasi || 100}%`,
    '',
    '',
    summary.totalJamKerjaText || '0 jam',
    ''
  ];

  totalRow.eachCell((cell, colNumber) => {
    cell.font = { name: 'Calibri', size: 10, bold: true, color: { argb: 'FFFFFFFF' } };
    cell.fill = {
      type: 'pattern',
      pattern: 'solid',
      fgColor: { argb: 'FF0F172A' }
    };

    if (colNumber === 1 || colNumber === 2 || (colNumber >= 7 && colNumber <= 10)) {
      cell.alignment = { vertical: 'middle', horizontal: 'center' };
    } else if (colNumber >= 4 && colNumber <= 6) {
      cell.alignment = { vertical: 'middle', horizontal: 'right' };
    } else {
      cell.alignment = { vertical: 'middle', horizontal: 'left' };
    }

    cell.border = {
      top: { style: 'medium', color: { argb: 'FF000000' } },
      bottom: { style: 'double', color: { argb: 'FF000000' } }
    };

    if (colNumber >= 4 && colNumber <= 6) {
      cell.numFmt = '#,##0';
    }
  });

  // 7. Set Column Widths
  worksheet.getColumn(1).width = 6;   // NO
  worksheet.getColumn(2).width = 14;  // TANGGAL
  worksheet.getColumn(3).width = 24;  // USER
  worksheet.getColumn(4).width = 16;  // TOTAL NOTA
  worksheet.getColumn(5).width = 18;  // NOTA SALAH
  worksheet.getColumn(6).width = 16;  // NOTA VALID
  worksheet.getColumn(7).width = 14;  // AKURASI
  worksheet.getColumn(8).width = 14;  // JAM DATANG
  worksheet.getColumn(9).width = 14;  // JAM PULANG
  worksheet.getColumn(10).width = 16; // DURASI
  worksheet.getColumn(11).width = 30; // KETERANGAN

  // 8. Generate and download file
  const buffer = await workbook.xlsx.writeBuffer();
  const blob = new Blob([buffer], {
    type: 'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet'
  });
  saveAs(blob, filename);
}

import ExcelJS from 'exceljs';
import { saveAs } from 'file-saver';

/**
 * Export Exact Pivot Table to Excel matching the format in "Contoh tabel.xlsx"
 */
export async function exportExactTableToExcel({
  filteredGroups = [],
  selectedUser = '',
  selectedDate = '',
  totalNota = 0,
  totalCountJMDOS = 0,
  totalSumNetto = 0,
  filename = 'Laporan_Pivot_Produktivitas.xlsx'
}) {
  const workbook = new ExcelJS.Workbook();
  workbook.creator = 'Laporan Produktivitas Admin';
  const worksheet = workbook.addWorksheet('Laporan Pivot', {
    views: [{ showGridLines: true }]
  });

  // Row 1: Header Top
  // ["NO", "Nama Fakturis", "JOE ANDREAS TAMBUNAN", "", "", "", 48]
  const row1 = worksheet.getRow(1);
  row1.height = 24;
  row1.values = ['NO', 'Nama Fakturis', selectedUser || 'SEMUA USER', '', '', '', totalNota];
  row1.getCell(1).font = { bold: true };
  row1.getCell(2).font = { bold: true };
  row1.getCell(3).font = { bold: true, color: { argb: 'FF0284C7' } };
  row1.getCell(7).font = { bold: true, size: 12, color: { argb: 'FF0F172A' } };
  row1.getCell(7).alignment = { horizontal: 'right' };

  // Row 2: Date
  // ["", "Tanggal", "03-07-2026", "", "", "", ""]
  const row2 = worksheet.getRow(2);
  row2.height = 22;
  row2.values = ['', 'Tanggal', selectedDate || 'SEMUA TANGGAL', '', '', '', ''];
  row2.getCell(2).font = { bold: true };
  row2.getCell(3).font = { bold: true, color: { argb: 'FF059669' } };

  // Row 3: Table Column Headers
  // ["", "NMPG", "NOINV", "KDPRC", "USID", "Count JMDOS", "Sum of NETTO"]
  const row3 = worksheet.getRow(3);
  row3.height = 26;
  row3.values = ['NO', 'NMPG', 'NOINV', 'KDPRC', 'USID', 'Count JMDOS', 'Sum of NETTO'];
  
  row3.eachCell((cell, colNumber) => {
    cell.font = { name: 'Calibri', size: 11, bold: true, color: { argb: 'FFFFFFFF' } };
    cell.fill = {
      type: 'pattern',
      pattern: 'solid',
      fgColor: { argb: colNumber >= 6 ? 'FF0369A1' : 'FF1E293B' }
    };
    cell.alignment = {
      vertical: 'middle',
      horizontal: colNumber === 1 ? 'center' : (colNumber >= 6 ? 'right' : 'left')
    };
    cell.border = {
      top: { style: 'thin', color: { argb: 'FF000000' } },
      bottom: { style: 'medium', color: { argb: 'FF000000' } }
    };
  });

  // Rows 4+: Data Rows
  let lastNmpg = '';
  filteredGroups.forEach((item, index) => {
    const rowIdx = index + 4;
    const r = worksheet.getRow(rowIdx);
    r.height = 20;

    // Show NMPG on change or every row
    const showNmpg = item.nmpg !== lastNmpg ? item.nmpg : '';
    lastNmpg = item.nmpg;

    r.values = [
      index + 1,
      showNmpg || item.nmpg, // show NMPG
      item.noinv,
      item.kdprc,
      item.usid,
      item.countJMDOS,
      item.sumNetto
    ];

    const isEven = index % 2 === 0;
    const bgArgb = isEven ? 'FFFFFFFF' : 'FFF8FAFC';

    r.eachCell((cell, colNumber) => {
      cell.font = { name: 'Calibri', size: 10 };
      cell.fill = {
        type: 'pattern',
        pattern: 'solid',
        fgColor: { argb: bgArgb }
      };
      cell.alignment = {
        vertical: 'middle',
        horizontal: colNumber === 1 || colNumber === 4 || colNumber === 5 ? 'center' : (colNumber >= 6 ? 'right' : 'left')
      };
      cell.border = {
        top: { style: 'thin', color: { argb: 'FFE2E8F0' } },
        bottom: { style: 'thin', color: { argb: 'FFE2E8F0' } },
        left: { style: 'thin', color: { argb: 'FFE2E8F0' } },
        right: { style: 'thin', color: { argb: 'FFE2E8F0' } }
      };

      if (colNumber === 6) {
        cell.numFmt = '#,##0';
      }
      if (colNumber === 7) {
        cell.numFmt = '#,##0.00';
      }
    });
  });

  // Grand Total Bottom Row
  const totalRowIdx = filteredGroups.length + 4;
  const totalRow = worksheet.getRow(totalRowIdx);
  totalRow.height = 26;
  totalRow.values = [
    '',
    'TOTAL',
    `${totalNota} Nota`,
    '',
    '',
    totalCountJMDOS,
    totalSumNetto
  ];

  totalRow.eachCell((cell, colNumber) => {
    cell.font = { name: 'Calibri', size: 11, bold: true, color: { argb: 'FFFFFFFF' } };
    cell.fill = {
      type: 'pattern',
      pattern: 'solid',
      fgColor: { argb: 'FF0F172A' }
    };
    cell.alignment = {
      vertical: 'middle',
      horizontal: colNumber >= 6 ? 'right' : 'left'
    };
    cell.border = {
      top: { style: 'medium', color: { argb: 'FF000000' } },
      bottom: { style: 'double', color: { argb: 'FF000000' } }
    };

    if (colNumber === 6) cell.numFmt = '#,##0';
    if (colNumber === 7) cell.numFmt = '#,##0.00';
  });

  // Column Widths
  worksheet.getColumn(1).width = 6;   // NO
  worksheet.getColumn(2).width = 46;  // NMPG
  worksheet.getColumn(3).width = 24;  // NOINV
  worksheet.getColumn(4).width = 12;  // KDPRC
  worksheet.getColumn(5).width = 14;  // USID
  worksheet.getColumn(6).width = 16;  // Count JMDOS
  worksheet.getColumn(7).width = 20;  // Sum of NETTO

  // Write and download
  const buffer = await workbook.xlsx.writeBuffer();
  const blob = new Blob([buffer], { type: 'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet' });
  saveAs(blob, filename);
}

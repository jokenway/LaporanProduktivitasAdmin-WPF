import ExcelJS from 'exceljs';
import { saveAs } from 'file-saver';
import jsPDF from 'jspdf';
import 'jspdf-autotable';

/**
 * Export Pivot Table to professionally styled Excel Workbook (.xlsx)
 */
export async function exportPivotToExcel({
  pivotResult,
  config,
  filename = 'Laporan_Produktivitas_Admin.xlsx',
  meta = {}
}) {
  const { columns, rowKeys, matrix, rowTotals, colTotals, grandTotal, flatData, summaryStats } = pivotResult;
  const { rowField = 'User', colField = 'Tanggal', valField = 'NoInv', aggType = 'COUNT' } = config;

  const workbook = new ExcelJS.Workbook();
  workbook.creator = 'Admin Productivity & Pivot Studio';
  workbook.created = new Date();

  // ==========================================
  // SHEET 1: MATRIX PIVOT TABLE
  // ==========================================
  const wsMatrix = workbook.addWorksheet('Matriks Produktivitas', {
    views: [{ showGridLines: true }]
  });

  // Title Block
  wsMatrix.mergeCells('A1:F1');
  const titleCell = wsMatrix.getCell('A1');
  titleCell.value = 'LAPORAN PRODUKTIVITAS ADMIN & PIVOT TABLE';
  titleCell.font = { name: 'Arial', size: 16, bold: true, color: { argb: 'FFFFFFFF' } };
  titleCell.fill = { type: 'pattern', pattern: 'solid', fgColor: { argb: 'FF0F172A' } };
  titleCell.alignment = { vertical: 'middle', horizontal: 'center' };
  wsMatrix.getRow(1).height = 36;

  // Subtitle / Info Block
  wsMatrix.mergeCells('A2:F2');
  const subCell = wsMatrix.getCell('A2');
  const aggLabel = aggType === 'DISTINCT_COUNT' ? 'Distinct Count (Jumlah Unik)' :
                   aggType === 'SUM' ? 'Total Nilai (Sum)' :
                   aggType === 'AVG' ? 'Rata-rata (Average)' : 'Jumlah Total (Count)';
  subCell.value = `Dimensi: Baris [${rowField}] x Kolom [${colField || 'Semua Tanggal'}] | Nilai: [${valField || 'Baris'}] (${aggLabel}) | Dicetak: ${new Date().toLocaleString('id-ID')}`;
  subCell.font = { name: 'Arial', size: 9, italic: true, color: { argb: 'FF475569' } };
  subCell.alignment = { vertical: 'middle', horizontal: 'left' };
  wsMatrix.getRow(2).height = 20;

  // KPI Quick Stats Row
  wsMatrix.getRow(3).height = 8; // spacer

  // Table Headers
  const headerRowIdx = 4;
  const headerRow = wsMatrix.getRow(headerRowIdx);
  headerRow.height = 26;

  // Column Headers
  const headerValues = ['No', rowField, ...columns, 'TOTAL KESELURUHAN'];
  headerRow.values = headerValues;

  headerRow.eachCell((cell, colNum) => {
    cell.font = { name: 'Arial', size: 10, bold: true, color: { argb: 'FFFFFFFF' } };
    cell.alignment = { vertical: 'middle', horizontal: colNum <= 2 ? 'left' : 'center' };
    cell.fill = {
      type: 'pattern',
      pattern: 'solid',
      fgColor: { argb: colNum === headerValues.length ? 'FF0369A1' : 'FF1E293B' }
    };
    cell.border = {
      top: { style: 'thin', color: { argb: 'FF000000' } },
      bottom: { style: 'medium', color: { argb: 'FF000000' } },
      left: { style: 'thin', color: { argb: 'FF334155' } },
      right: { style: 'thin', color: { argb: 'FF334155' } }
    };
  });

  // Table Rows
  let currentRowIdx = headerRowIdx + 1;

  rowKeys.forEach((rKey, index) => {
    const row = wsMatrix.getRow(currentRowIdx);
    row.height = 20;

    const rowData = [index + 1, rKey];
    columns.forEach((cKey) => {
      const val = matrix[rKey]?.[cKey] ?? 0;
      rowData.push(val === 0 ? '-' : val);
    });
    rowData.push(rowTotals[rKey] ?? 0);

    row.values = rowData;

    // Styling
    const isEven = index % 2 === 0;
    const bgArgb = isEven ? 'FFFFFFFF' : 'FFF8FAFC';

    row.eachCell((cell, colNum) => {
      const isTotalCol = colNum === headerValues.length;
      cell.font = {
        name: 'Arial',
        size: 9.5,
        bold: isTotalCol,
        color: { argb: isTotalCol ? 'FF0284C7' : 'FF1E293B' }
      };
      cell.fill = {
        type: 'pattern',
        pattern: 'solid',
        fgColor: { argb: isTotalCol ? 'FFF0F9FF' : bgArgb }
      };
      cell.alignment = {
        vertical: 'middle',
        horizontal: colNum === 1 ? 'center' : (colNum === 2 ? 'left' : 'center')
      };
      cell.border = {
        top: { style: 'thin', color: { argb: 'FFE2E8F0' } },
        bottom: { style: 'thin', color: { argb: 'FFE2E8F0' } },
        left: { style: 'thin', color: { argb: 'FFE2E8F0' } },
        right: { style: 'thin', color: { argb: 'FFE2E8F0' } }
      };
      if (typeof cell.value === 'number' && !isTotalCol) {
        cell.numFmt = '#,##0';
      } else if (isTotalCol && typeof cell.value === 'number') {
        cell.numFmt = '#,##0';
      }
    });

    currentRowIdx++;
  });

  // Grand Total Bottom Row
  const grandTotalRow = wsMatrix.getRow(currentRowIdx);
  grandTotalRow.height = 26;

  const grandTotalValues = ['', 'GRAND TOTAL (KESELURUHAN)'];
  columns.forEach((cKey) => {
    grandTotalValues.push(colTotals[cKey] ?? 0);
  });
  grandTotalValues.push(grandTotal);
  grandTotalRow.values = grandTotalValues;

  grandTotalRow.eachCell((cell, colNum) => {
    cell.font = { name: 'Arial', size: 10, bold: true, color: { argb: 'FFFFFFFF' } };
    cell.alignment = { vertical: 'middle', horizontal: colNum <= 2 ? 'left' : 'center' };
    cell.fill = {
      type: 'pattern',
      pattern: 'solid',
      fgColor: { argb: colNum === grandTotalValues.length ? 'FF0284C7' : 'FF0F172A' }
    };
    cell.border = {
      top: { style: 'medium', color: { argb: 'FF000000' } },
      bottom: { style: 'double', color: { argb: 'FF000000' } },
      left: { style: 'thin', color: { argb: 'FF334155' } },
      right: { style: 'thin', color: { argb: 'FF334155' } }
    };
    if (typeof cell.value === 'number') {
      cell.numFmt = '#,##0';
    }
  });

  // Auto column widths
  wsMatrix.getColumn(1).width = 6;
  wsMatrix.getColumn(2).width = 28;
  columns.forEach((col, idx) => {
    wsMatrix.getColumn(idx + 3).width = Math.max(12, String(col).length + 3);
  });
  wsMatrix.getColumn(columns.length + 3).width = 20;

  // ==========================================
  // SHEET 2: FLAT / RINCIAN GROUPED DATA
  // ==========================================
  const wsFlat = workbook.addWorksheet('Data Rinci Terkelompok');
  wsFlat.views = [{ showGridLines: true }];

  wsFlat.getRow(1).values = ['No', rowField, colField || 'Tanggal', `${aggType} ${valField || 'NoInv'}`];
  wsFlat.getRow(1).height = 24;
  wsFlat.getRow(1).eachCell((cell) => {
    cell.font = { name: 'Arial', size: 10, bold: true, color: { argb: 'FFFFFFFF' } };
    cell.fill = { type: 'pattern', pattern: 'solid', fgColor: { argb: 'FF1E293B' } };
    cell.alignment = { vertical: 'middle', horizontal: 'center' };
  });

  flatData.forEach((item, idx) => {
    const r = wsFlat.getRow(idx + 2);
    r.values = [
      idx + 1,
      item[rowField],
      item[colField] || '-',
      item[valField || 'Jumlah'] || 0
    ];
    r.eachCell((cell, colNum) => {
      cell.alignment = { vertical: 'middle', horizontal: colNum <= 3 ? 'left' : 'center' };
      cell.border = {
        top: { style: 'thin', color: { argb: 'FFE2E8F0' } },
        bottom: { style: 'thin', color: { argb: 'FFE2E8F0' } }
      };
    });
  });

  wsFlat.getColumn(1).width = 8;
  wsFlat.getColumn(2).width = 28;
  wsFlat.getColumn(3).width = 20;
  wsFlat.getColumn(4).width = 20;

  // Generate buffer and trigger browser download
  const buffer = await workbook.xlsx.writeBuffer();
  const blob = new Blob([buffer], { type: 'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet' });
  saveAs(blob, filename);
}

/**
 * Export Pivot Table to PDF
 */
export function exportPivotToPDF({
  pivotResult,
  config,
  filename = 'Laporan_Produktivitas_Admin.pdf'
}) {
  const { columns, rowKeys, matrix, rowTotals, colTotals, grandTotal, summaryStats } = pivotResult;
  const { rowField = 'User', colField = 'Tanggal', valField = 'NoInv', aggType = 'COUNT' } = config;

  const doc = new jsPDF({
    orientation: columns.length > 6 ? 'landscape' : 'portrait',
    unit: 'pt',
    format: 'a4'
  });

  // Header Title
  doc.setFontSize(16);
  doc.setTextColor(15, 23, 42);
  doc.text('LAPORAN PRODUKTIVITAS ADMIN & PIVOT TABLE', 40, 40);

  doc.setFontSize(9);
  doc.setTextColor(100, 116, 139);
  doc.text(`Field: ${rowField} x ${colField || 'Tanggal'} | Agregasi: ${aggType} (${valField || 'NoInv'}) | Waktu: ${new Date().toLocaleString('id-ID')}`, 40, 56);

  // Summary Metrics Banner
  doc.setDrawColor(226, 232, 240);
  doc.setFillColor(248, 250, 252);
  doc.roundedRect(40, 68, doc.internal.pageSize.width - 80, 40, 4, 4, 'FD');

  doc.setFontSize(9);
  doc.setTextColor(15, 23, 42);
  doc.text(`Total Admin: ${summaryStats.uniqueUsers}  |  Total Tanggal: ${columns.length}  |  Grand Total NoInv: ${grandTotal.toLocaleString('id-ID')}  |  Top User: ${summaryStats.topUser.name} (${summaryStats.topUser.total})`, 50, 92);

  // Build Table Columns & Rows
  const tableHeaders = ['No', rowField, ...columns, 'TOTAL'];
  const tableRows = [];

  rowKeys.forEach((rKey, idx) => {
    const row = [idx + 1, rKey];
    columns.forEach((cKey) => {
      const val = matrix[rKey]?.[cKey] ?? 0;
      row.push(val === 0 ? '-' : val.toLocaleString('id-ID'));
    });
    row.push((rowTotals[rKey] ?? 0).toLocaleString('id-ID'));
    tableRows.push(row);
  });

  // Grand Total row
  const grandTotalRow = ['', 'GRAND TOTAL'];
  columns.forEach((cKey) => {
    grandTotalRow.push((colTotals[cKey] ?? 0).toLocaleString('id-ID'));
  });
  grandTotalRow.push(grandTotal.toLocaleString('id-ID'));
  tableRows.push(grandTotalRow);

  // Generate Table with AutoTable
  doc.autoTable({
    startY: 120,
    head: [tableHeaders],
    body: tableRows,
    theme: 'grid',
    styles: {
      fontSize: 8,
      cellPadding: 4,
      valign: 'middle',
    },
    headStyles: {
      fillColor: [15, 23, 42],
      textColor: [255, 255, 255],
      fontStyle: 'bold',
      halign: 'center',
    },
    columnStyles: {
      0: { halign: 'center', cellWidth: 25 },
      1: { halign: 'left', fontStyle: 'bold' },
      [tableHeaders.length - 1]: { halign: 'center', fontStyle: 'bold', fillColor: [240, 249, 255], textColor: [2, 132, 199] }
    },
    footStyles: {
      fillColor: [15, 23, 42],
      textColor: [255, 255, 255],
      fontStyle: 'bold',
      halign: 'center'
    },
    didParseCell: (data) => {
      // Style bottom grand total row
      if (data.row.index === tableRows.length - 1) {
        data.cell.styles.fontStyle = 'bold';
        data.cell.styles.fillColor = [15, 23, 42];
        data.cell.styles.textColor = [255, 255, 255];
      }
    }
  });

  doc.save(filename);
}

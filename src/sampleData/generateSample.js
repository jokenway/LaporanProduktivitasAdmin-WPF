import * as XLSX from 'xlsx';

/**
 * Generates sample Excel workbook data for testing
 */
export function generateSampleData() {
  const users = [
    'Ahmad Fauzi', 'Siti Rahma', 'Budi Santoso', 'Dewi Lestari', 
    'Rian Hidayat', 'Nurul Aini', 'Eko Prasetyo', 'Maya Putri'
  ];

  const categories = ['Admin Online', 'Admin Kasir', 'Admin Marketplace', 'Admin Retur & CS', 'Admin Gudang'];
  const statuses = ['Selesai', 'Selesai', 'Selesai', 'Pending', 'Dibatalkan'];

  const rows = [];
  let invoiceCounter = 10001;

  // Generate 7 days of realistic transaction data
  const baseDate = new Date();
  baseDate.setDate(baseDate.getDate() - 6);

  for (let dayOffset = 0; dayOffset < 7; dayOffset++) {
    const currentDate = new Date(baseDate);
    currentDate.setDate(baseDate.getDate() + dayOffset);
    const dateStr = currentDate.toISOString().split('T')[0];

    // Each day, users handle different number of invoices
    users.forEach((user, uIdx) => {
      // Random productivity based on user skill/shifts (e.g. 5 to 25 invoices per day)
      const basePerformance = 8 + (uIdx % 4) * 4;
      const count = Math.floor(basePerformance + Math.random() * 8);

      for (let i = 0; i < count; i++) {
        const noinv = `INV/${dateStr.replace(/-/g, '')}/${invoiceCounter++}`;
        const kategori = categories[Math.floor(Math.random() * categories.length)];
        const status = statuses[Math.floor(Math.random() * statuses.length)];
        const nominal = Math.floor(50000 + Math.random() * 950000);

        rows.push({
          'No Invoice': noinv,
          'Nama Admin': user,
          'Tanggal': dateStr,
          'Kategori': kategori,
          'Nominal (Rp)': nominal,
          'Status': status
        });
      }
    });
  }

  return rows;
}

/**
 * Downloads a sample Excel file directly to test with
 */
export function downloadSampleExcel() {
  const sampleRows = generateSampleData();
  const worksheet = XLSX.utils.json_to_sheet(sampleRows);
  const workbook = XLSX.utils.book_new();
  XLSX.utils.book_append_sheet(workbook, worksheet, 'Data Mentah Transaksi');

  // Auto column widths
  const colWidths = [
    { wch: 25 }, // No Invoice
    { wch: 20 }, // Nama Admin
    { wch: 15 }, // Tanggal
    { wch: 20 }, // Kategori
    { wch: 18 }, // Nominal
    { wch: 15 }  // Status
  ];
  worksheet['!cols'] = colWidths;

  XLSX.writeFile(workbook, 'Contoh_Data_Mentah_Produktivitas_Admin.xlsx');
}

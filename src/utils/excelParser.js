import * as XLSX from 'xlsx';

/**
 * Normalizes Excel date values into YYYY-MM-DD string or readable date
 */
export function normalizeDate(val) {
  if (val === null || val === undefined || val === '') return '';

  // If it's a JavaScript Date object
  if (val instanceof Date && !isNaN(val.getTime())) {
    const year = val.getFullYear();
    const month = String(val.getMonth() + 1).padStart(2, '0');
    const day = String(val.getDate()).padStart(2, '0');
    return `${year}-${month}-${day}`;
  }

  // If it's a number (Excel serial date number, e.g. 45123)
  if (typeof val === 'number') {
    try {
      const dateObj = XLSX.SSF.parse_date_code(val);
      if (dateObj) {
        const year = dateObj.y;
        const month = String(dateObj.m).padStart(2, '0');
        const day = String(dateObj.d).padStart(2, '0');
        return `${year}-${month}-${day}`;
      }
    } catch {
      // fallback
    }
  }

  // If it's a string
  if (typeof val === 'string') {
    const trimmed = val.trim();
    if (!trimmed) return '';

    // Check if ISO format YYYY-MM-DD
    if (/^\d{4}[-/.]\d{1,2}[-/.]\d{1,2}/.test(trimmed)) {
      const parts = trimmed.split(/[-/.]/);
      const year = parts[0];
      const month = String(parts[1]).padStart(2, '0');
      const day = String(parts[2].slice(0, 2)).padStart(2, '0');
      return `${year}-${month}-${day}`;
    }

    // Check if DD/MM/YYYY or DD-MM-YYYY format
    if (/^\d{1,2}[-/.]\d{1,2}[-/.]\d{4}/.test(trimmed)) {
      const parts = trimmed.split(/[-/.]/);
      const day = String(parts[0]).padStart(2, '0');
      const month = String(parts[1]).padStart(2, '0');
      const year = parts[2].slice(0, 4);
      return `${year}-${month}-${day}`;
    }

    // Attempt Date parse
    const parsed = new Date(trimmed);
    if (!isNaN(parsed.getTime()) && parsed.getFullYear() > 1990 && parsed.getFullYear() < 2100) {
      const year = parsed.getFullYear();
      const month = String(parsed.getMonth() + 1).padStart(2, '0');
      const day = String(parsed.getDate()).padStart(2, '0');
      return `${year}-${month}-${day}`;
    }

    return trimmed;
  }

  return String(val);
}

export function parseDateValue(value) {
  if (value === null || value === undefined || value === '') return null;

  if (value instanceof Date && !isNaN(value.getTime())) return value;

  if (typeof value === 'number') {
    try {
      const dateObj = XLSX.SSF.parse_date_code(value);
      if (dateObj) {
        return new Date(dateObj.y, dateObj.m - 1, dateObj.d);
      }
    } catch {
      // fallback
    }
  }

  if (typeof value === 'string') {
    const trimmed = value.trim();
    if (!trimmed) return null;

    if (/^\d{4}[-/.]\d{1,2}[-/.]\d{1,2}/.test(trimmed)) {
      const [year, month, day] = trimmed.split(/[-/.]/);
      const parsed = new Date(Number(year), Number(month) - 1, Number(day));
      if (!isNaN(parsed.getTime())) return parsed;
    }

    if (/^\d{1,2}[-/.]\d{1,2}[-/.]\d{4}/.test(trimmed)) {
      const [day, month, year] = trimmed.split(/[-/.]/);
      const parsed = new Date(Number(year), Number(month) - 1, Number(day));
      if (!isNaN(parsed.getTime())) return parsed;
    }

    const parsed = new Date(trimmed);
    if (!isNaN(parsed.getTime())) return parsed;
  }

  return null;
}

export function compareDateStrings(a, b) {
  if (a === b) return 0;

  const dateA = parseDateValue(a);
  const dateB = parseDateValue(b);

  if (dateA && dateB) {
    return dateA.getTime() - dateB.getTime();
  }

  return String(a || '').localeCompare(String(b || ''));
}

/**
 * Parses an Excel / CSV File into worksheets, headers, and row objects
 */
export async function parseExcelFile(fileOrBuffer) {
  let data;
  if (fileOrBuffer instanceof ArrayBuffer) {
    data = fileOrBuffer;
  } else if (fileOrBuffer instanceof Blob) {
    data = await fileOrBuffer.arrayBuffer();
  } else if (typeof fileOrBuffer === 'string') {
    // base64 or path
    data = fileOrBuffer;
  } else {
    throw new Error('Unsupported file format');
  }

  const workbook = XLSX.read(data, {
    type: data instanceof ArrayBuffer ? 'array' : 'string',
    cellDates: true,
    cellNF: false,
    cellText: false,
  });

  const sheetNames = workbook.SheetNames;
  if (!sheetNames || sheetNames.length === 0) {
    throw new Error('File Excel tidak memiliki sheet yang dapat dibaca');
  }

  const sheetsData = {};

  sheetNames.forEach((sheetName) => {
    const worksheet = workbook.Sheets[sheetName];
    // Convert to 2D JSON raw
    const rawRows = XLSX.utils.sheet_to_json(worksheet, {
      header: 1,
      defval: '',
      blankrows: false,
      raw: true,
    });

    if (rawRows.length === 0) {
      sheetsData[sheetName] = {
        headers: [],
        rows: [],
        rowCount: 0,
        columnStats: {}
      };
      return;
    }

    // Find header row (usually the first row with non-empty strings)
    let headerRowIndex = 0;
    while (headerRowIndex < rawRows.length && (!rawRows[headerRowIndex] || rawRows[headerRowIndex].filter(Boolean).length === 0)) {
      headerRowIndex++;
    }

    if (headerRowIndex >= rawRows.length) {
      sheetsData[sheetName] = {
        headers: [],
        rows: [],
        rowCount: 0,
        columnStats: {}
      };
      return;
    }

    const rawHeaders = rawRows[headerRowIndex].map((h, idx) => {
      const headerStr = (h !== null && h !== undefined) ? String(h).trim() : '';
      return headerStr || `Kolom_${idx + 1}`;
    });

    // Make unique headers in case of duplicates
    const headers = [];
    const headerCounts = {};
    rawHeaders.forEach((h) => {
      if (!headerCounts[h]) {
        headerCounts[h] = 1;
        headers.push(h);
      } else {
        headerCounts[h]++;
        headers.push(`${h}_${headerCounts[h]}`);
      }
    });

    // Parse data rows
    const rows = [];
    for (let i = headerRowIndex + 1; i < rawRows.length; i++) {
      const rowArr = rawRows[i];
      if (!rowArr || rowArr.every(cell => cell === '' || cell === null || cell === undefined)) {
        continue; // skip blank rows
      }

      const rowObj = {};
      let hasData = false;

      headers.forEach((header, colIdx) => {
        let val = rowArr[colIdx];
        if (val === undefined || val === null) {
          val = '';
        }
        
        // Clean strings
        if (typeof val === 'string') {
          val = val.trim();
        }

        if (val !== '') hasData = true;
        rowObj[header] = val;
      });

      if (hasData) {
        rows.push(rowObj);
      }
    }

    // Detect column data types & statistics
    const columnStats = {};
    headers.forEach((h) => {
      let nonNullCount = 0;
      let numberCount = 0;
      let dateCount = 0;
      const sampleVals = [];

      rows.forEach((r) => {
        const v = r[h];
        if (v !== '' && v !== null && v !== undefined) {
          nonNullCount++;
          if (sampleVals.length < 5) sampleVals.push(v);

          if (typeof v === 'number' || (!isNaN(Number(v)) && v !== '')) {
            numberCount++;
          }
          if (v instanceof Date || (typeof v === 'string' && /^\d{4}[-/.]\d{1,2}[-/.]\d{1,2}/.test(v))) {
            dateCount++;
          }
        }
      });

      let detectedType = 'string';
      if (dateCount > nonNullCount * 0.7 && nonNullCount > 0) {
        detectedType = 'date';
      } else if (numberCount > nonNullCount * 0.7 && nonNullCount > 0) {
        detectedType = 'number';
      }

      columnStats[h] = {
        type: detectedType,
        totalCount: nonNullCount,
        sample: sampleVals
      };
    });

    sheetsData[sheetName] = {
      headers,
      rows,
      rowCount: rows.length,
      columnStats
    };
  });

  return {
    sheetNames,
    sheetsData,
    defaultSheet: sheetNames[0]
  };
}

/**
 * Intelligent guess for standard fields: noinv, user/admin, tanggal, amount
 */
export function autoDetectFields(headers) {
  const result = {
    noinv: '',
    user: '',
    date: '',
    amount: '',
    status: '',
    kategori: ''
  };

  const normalizeStr = (s) => s.toLowerCase().replace(/[^a-z0-9]/g, '');

  headers.forEach((h) => {
    const norm = normalizeStr(h);
    
    // No. Invoice detection
    if (!result.noinv) {
      if (['noinv', 'noinvoice', 'nomorinvoice', 'invno', 'invoiceno', 'noresi', 'notransaksi', 'nomortransaksi', 'idtransaksi', 'invoice', 'nofp', 'noreff'].includes(norm) ||
          norm.includes('noinv') || norm.includes('invoice')) {
        result.noinv = h;
      }
    }

    // User / Admin / Petugas detection (USID, NMPG, Admin, etc.)
    if (!result.user) {
      if (['usid', 'user', 'username', 'admin', 'namaadmin', 'nmpg', 'kdpg', 'petugas', 'namapetugas', 'pic', 'operator', 'kasir', 'pegawai', 'karyawan', 'createdby', 'inputby', 'agent'].includes(norm) ||
          norm.includes('admin') || norm.includes('user') || norm.includes('petugas') || norm === 'usid') {
        result.user = h;
      }
    }

    // Date / Tanggal detection (TGBON, Tanggal, etc.)
    if (!result.date) {
      if (['tgbon', 'tgfp', 'tgjth', 'tanggal', 'tgl', 'date', 'tgltransaksi', 'tanggaltransaksi', 'createdat', 'tgldibuat', 'tglinput', 'invoicedate', 'tglinvoice'].includes(norm) ||
          norm.includes('tanggal') || norm.includes('tgl') || norm.includes('date') || norm.includes('tgbon')) {
        result.date = h;
      }
    }

    // Amount / Nominal detection
    if (!result.amount) {
      if (['total', 'grandtotal', 'amount', 'nominal', 'harga', 'subtotal', 'nilai', 'rupiah', 'omset', 'netto', 'totald'].includes(norm) ||
          norm.includes('total') || norm.includes('nominal') || norm.includes('amount') || norm === 'netto') {
        result.amount = h;
      }
    }

    // Status detection
    if (!result.status) {
      if (['status', 'statustransaksi', 'state', 'kondisi', 'keterangan', 'jenis', 'jenisb', 'jnbnr'].includes(norm)) {
        result.status = h;
      }
    }

    // Kategori detection
    if (!result.kategori) {
      if (['kategori', 'category', 'jenis', 'tipe', 'jenistransaksi', 'divisi', 'departemen', 'nmkb', 'nmskb', 'nmar'].includes(norm)) {
        result.kategori = h;
      }
    }
  });

  return result;
}

import { normalizeDate } from './excelParser';

/**
 * Computes dynamic Pivot Table matrix and summary data from raw rows
 * 
 * @param {Array} rows - raw rows from Excel
 * @param {Object} config - {
 *    rowField: string,        // e.g. 'User' or 'Nama Admin'
 *    colField: string,        // e.g. 'Tanggal' or 'Date'
 *    valField: string,        // e.g. 'NoInv' or 'Nominal'
 *    aggType: string,         // 'COUNT' | 'DISTINCT_COUNT' | 'SUM' | 'AVG' | 'MIN' | 'MAX'
 *    dateFormat: string,      // 'daily' | 'monthly' | 'raw'
 *    filters: Object,         // { [fieldName]: [selectedValues] }
 *    dateRange: { start: string, end: string },
 *    sortBy: string,          // 'row_asc' | 'row_desc' | 'total_desc' | 'total_asc'
 * }
 */
export function buildPivotTable(rows = [], config = {}) {
  const {
    rowField = '',
    colField = '',
    valField = '',
    aggType = 'COUNT',
    dateFormat = 'daily',
    filters = {},
    dateRange = { start: '', end: '' },
    sortBy = 'total_desc',
    searchQuery = ''
  } = config;

  if (!rows || rows.length === 0 || !rowField) {
    return {
      columns: [],
      rowKeys: [],
      matrix: {},
      rowTotals: {},
      colTotals: {},
      grandTotal: 0,
      flatData: [],
      summaryStats: {
        totalRows: 0,
        filteredRows: 0,
        uniqueUsers: 0,
        uniqueDates: 0,
        uniqueInvoices: 0,
        grandTotalCalculated: 0,
        topUser: { name: '-', total: 0 },
        busiestDate: { date: '-', total: 0 },
      }
    };
  }

  // Helper to format date if column is date
  const formatCellDate = (val) => {
    if (!val) return '(Kosong)';
    const norm = normalizeDate(val);
    if (!norm) return String(val);

    if (dateFormat === 'monthly' && norm.length >= 7) {
      return norm.substring(0, 7); // YYYY-MM
    }
    return norm;
  };

  const normalizedSearch = searchQuery ? searchQuery.toLowerCase().trim() : '';

  // 1. Filter rows
  const filteredRows = rows.filter((row) => {
    // Global search query
    if (normalizedSearch) {
      const rowText = Object.values(row)
        .map(v => String(v ?? '').toLowerCase())
        .join(' ');
      if (!rowText.includes(normalizedSearch)) return false;
    }

    // Specific field filters
    for (const [fName, allowedVals] of Object.entries(filters)) {
      if (allowedVals && Array.isArray(allowedVals) && allowedVals.length > 0) {
        const val = String(row[fName] ?? '').trim();
        if (!allowedVals.includes(val)) {
          return false;
        }
      }
    }

    // Date range filter
    if (dateRange && (dateRange.start || dateRange.end)) {
      const dateKey = config.dateField || colField;
      if (row[dateKey]) {
        const rowDate = normalizeDate(row[dateKey]);
        if (dateRange.start && rowDate < dateRange.start) return false;
        if (dateRange.end && rowDate > dateRange.end) return false;
      }
    }

    return true;
  });

  // Track unique sets
  const uniqueRowKeySet = new Set();
  const uniqueColKeySet = new Set();
  const allInvoiceSet = new Set();
  const allUserSet = new Set();
  const allDateSet = new Set();

  // Cell storage: [rowKey][colKey] = Array of values
  const cellBuckets = {};

  filteredRows.forEach((row) => {
    const rawRowVal = row[rowField];
    const rowKey = (rawRowVal !== null && rawRowVal !== undefined && String(rawRowVal).trim() !== '')
      ? String(rawRowVal).trim()
      : '(Tanpa Nama/Kosong)';

    let colKey = '__TOTAL_ONLY__';
    if (colField) {
      const rawColVal = row[colField];
      colKey = (rawColVal !== null && rawColVal !== undefined && String(rawColVal).trim() !== '')
        ? formatCellDate(rawColVal)
        : '(Tanpa Tanggal/Kosong)';
      uniqueColKeySet.add(colKey);
    }

    uniqueRowKeySet.add(rowKey);
    allUserSet.add(rowKey);

    if (config.dateField || colField) {
      const dField = config.dateField || colField;
      if (row[dField]) {
        allDateSet.add(normalizeDate(row[dField]));
      }
    }

    // Track invoice identifiers
    const invoiceVal = valField ? row[valField] : (row['noinv'] || row['NoInv'] || row['No Invoice'] || row['invoice']);
    if (invoiceVal !== undefined && invoiceVal !== null && String(invoiceVal).trim() !== '') {
      allInvoiceSet.add(String(invoiceVal).trim());
    }

    if (!cellBuckets[rowKey]) {
      cellBuckets[rowKey] = {};
    }
    if (!cellBuckets[rowKey][colKey]) {
      cellBuckets[rowKey][colKey] = [];
    }

    const valueToStore = valField ? row[valField] : 1;
    cellBuckets[rowKey][colKey].push(valueToStore);
  });

  // Sort columns (chronologically for dates or alphabetical)
  const columns = Array.from(uniqueColKeySet).sort((a, b) => {
    if (a === '(Tanpa Tanggal/Kosong)') return 1;
    if (b === '(Tanpa Tanggal/Kosong)') return -1;
    return a.localeCompare(b, undefined, { numeric: true, sensitivity: 'base' });
  });

  // Calculate aggregation for a bucket
  const aggregateBucket = (values = []) => {
    if (!values || values.length === 0) return 0;

    switch (aggType) {
      case 'COUNT':
        return values.length;

      case 'DISTINCT_COUNT': {
        const distincts = new Set(values.filter(v => v !== null && v !== undefined && String(v).trim() !== ''));
        return distincts.size;
      }

      case 'SUM': {
        return values.reduce((sum, v) => {
          const num = Number(String(v).replace(/[^0-9.-]+/g, ''));
          return sum + (isNaN(num) ? 0 : num);
        }, 0);
      }

      case 'AVG': {
        const nums = values.map(v => Number(String(v).replace(/[^0-9.-]+/g, ''))).filter(n => !isNaN(n));
        if (nums.length === 0) return 0;
        return Number((nums.reduce((a, b) => a + b, 0) / nums.length).toFixed(2));
      }

      case 'MAX': {
        const nums = values.map(v => Number(String(v).replace(/[^0-9.-]+/g, ''))).filter(n => !isNaN(n));
        return nums.length > 0 ? Math.max(...nums) : 0;
      }

      case 'MIN': {
        const nums = values.map(v => Number(String(v).replace(/[^0-9.-]+/g, ''))).filter(n => !isNaN(n));
        return nums.length > 0 ? Math.min(...nums) : 0;
      }

      default:
        return values.length;
    }
  };

  // Build matrix, row totals, col totals, grand total
  const matrix = {};
  const rowTotals = {};
  const colTotals = {};
  const colBuckets = {}; // for accurate Distinct Count aggregation across column
  let allRowValues = [];

  columns.forEach(col => {
    colTotals[col] = 0;
    colBuckets[col] = [];
  });

  const rowKeysUnsorted = Array.from(uniqueRowKeySet);

  rowKeysUnsorted.forEach((rowKey) => {
    matrix[rowKey] = {};
    const rowValues = [];

    if (columns.length > 0) {
      columns.forEach((colKey) => {
        const bucket = cellBuckets[rowKey]?.[colKey] || [];
        const aggVal = aggregateBucket(bucket);
        matrix[rowKey][colKey] = aggVal;

        if (bucket.length > 0) {
          rowValues.push(...bucket);
          colBuckets[colKey].push(...bucket);
        }
      });
    } else {
      // No column dimension, just row aggregation
      const singleBucket = cellBuckets[rowKey]?.['__TOTAL_ONLY__'] || [];
      const aggVal = aggregateBucket(singleBucket);
      matrix[rowKey]['__TOTAL_ONLY__'] = aggVal;
      rowValues.push(...singleBucket);
    }

    rowTotals[rowKey] = aggregateBucket(rowValues);
    allRowValues.push(...rowValues);
  });

  // Calculate column totals
  columns.forEach((colKey) => {
    colTotals[colKey] = aggregateBucket(colBuckets[colKey]);
  });

  // Calculate Grand Total Keseluruhan
  const grandTotal = aggregateBucket(allRowValues);

  // Sort Row Keys
  const rowKeys = [...rowKeysUnsorted].sort((a, b) => {
    if (sortBy === 'row_asc') {
      return a.localeCompare(b, undefined, { numeric: true });
    }
    if (sortBy === 'row_desc') {
      return b.localeCompare(a, undefined, { numeric: true });
    }
    if (sortBy === 'total_asc') {
      return (rowTotals[a] || 0) - (rowTotals[b] || 0);
    }
    // Default total_desc
    return (rowTotals[b] || 0) - (rowTotals[a] || 0);
  });

  // Generate Flat Grouped List (for alternative list view or export)
  const flatData = [];
  rowKeys.forEach((rKey) => {
    if (columns.length > 0) {
      columns.forEach((cKey) => {
        const val = matrix[rKey]?.[cKey] || 0;
        if (val > 0) {
          flatData.push({
            [rowField]: rKey,
            [colField]: cKey,
            [valField || 'Jumlah']: val,
          });
        }
      });
    } else {
      flatData.push({
        [rowField]: rKey,
        [valField || 'Jumlah']: rowTotals[rKey] || 0
      });
    }
  });

  // Find Top User & Busiest Date
  let topUser = { name: '-', total: 0 };
  rowKeys.forEach(r => {
    const tot = rowTotals[r] || 0;
    if (tot > topUser.total) {
      topUser = { name: r, total: tot };
    }
  });

  let busiestDate = { date: '-', total: 0 };
  columns.forEach(c => {
    const tot = colTotals[c] || 0;
    if (tot > busiestDate.total) {
      busiestDate = { date: c, total: tot };
    }
  });

  const summaryStats = {
    totalRows: rows.length,
    filteredRows: filteredRows.length,
    uniqueUsers: allUserSet.size,
    uniqueDates: allDateSet.size || columns.length,
    uniqueInvoices: allInvoiceSet.size,
    grandTotalCalculated: grandTotal,
    topUser,
    busiestDate,
    avgPerUser: allUserSet.size > 0 ? Number((grandTotal / allUserSet.size).toFixed(1)) : 0,
    avgPerDate: (allDateSet.size || columns.length) > 0 ? Number((grandTotal / (allDateSet.size || columns.length)).toFixed(1)) : 0
  };

  return {
    columns,
    rowKeys,
    matrix,
    rowTotals,
    colTotals,
    grandTotal,
    flatData,
    summaryStats
  };
}

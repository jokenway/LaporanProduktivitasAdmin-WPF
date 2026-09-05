using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;

namespace LaporanProduktivitasWPF.Models
{
    public class RawRow
    {
        public Dictionary<string, string> Cells { get; set; }

        public RawRow()
        {
            Cells = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }

        public string Get(string fieldName)
        {
            if (string.IsNullOrEmpty(fieldName)) return string.Empty;
            string val;
            if (Cells.TryGetValue(fieldName, out val))
            {
                return val ?? string.Empty;
            }
            return string.Empty;
        }
    }

    public class ExactPivotItem
    {
        public int No { get; set; }
        public string NMPG { get; set; }
        public string NOINV { get; set; }
        public string KDPRC { get; set; }
        public string USID { get; set; }
        public int CountJMDOS { get; set; }
        public double SumNetto { get; set; }
        public string TGBON { get; set; }
        public string JENIS_B { get; set; }
    }

    public class EvaluasiItem : INotifyPropertyChanged
    {
        public string Key { get; set; }
        public int No { get; set; }
        public string Tanggal { get; set; }
        public string User { get; set; }
        public int TotalNota { get; set; }

        private int _notaSalah;
        public int NotaSalah
        {
            get { return _notaSalah; }
            set
            {
                if (_notaSalah != value)
                {
                    _notaSalah = Math.Max(0, value);
                    OnPropertyChanged("NotaSalah");
                    UpdateAkurasi();
                }
            }
        }

        private double _akurasi = 100.0;
        public double Akurasi
        {
            get { return _akurasi; }
            set { _akurasi = value; OnPropertyChanged("Akurasi"); }
        }

        private string _jamDatang = "";
        public string JamDatang
        {
            get { return _jamDatang; }
            set
            {
                if (TryParseTime(value, out string formatted))
                {
                    if (_jamDatang != formatted)
                    {
                        _jamDatang = formatted;
                        OnPropertyChanged("JamDatang");
                        UpdateDurasi();
                    }
                    else if (value != formatted)
                    {
                        OnPropertyChanged("JamDatang");
                    }
                }
                else
                {
                    OnPropertyChanged("JamDatang");
                }
            }
        }

        private string _jamPulang = "";
        public string JamPulang
        {
            get { return _jamPulang; }
            set
            {
                if (TryParseTime(value, out string formatted))
                {
                    if (_jamPulang != formatted)
                    {
                        _jamPulang = formatted;
                        OnPropertyChanged("JamPulang");
                        UpdateDurasi();
                    }
                    else if (value != formatted)
                    {
                        OnPropertyChanged("JamPulang");
                    }
                }
                else
                {
                    OnPropertyChanged("JamPulang");
                }
            }
        }

        /// <summary>
        /// Mengubah dan memvalidasi input teks menjadi format waktu standar (jam:menit / HH:mm).
        /// Mendukung input seperti: "08:00", "8:00", "08.00", "8.00", "0800", "8", "17", "1730", dll.
        /// </summary>
        public static bool TryParseTime(string input, out string formattedTime)
        {
            formattedTime = string.Empty;
            if (string.IsNullOrWhiteSpace(input))
            {
                return true; // input kosong valid untuk menghapus jam
            }

            string s = input.Trim().Replace('.', ':');

            // Format jam:menit standar (cth: "8:0", "08:00", "17:30")
            if (TimeSpan.TryParseExact(s, new[] { "h\\:m", "hh\\:mm", "h\\:mm", "hh\\:m" }, CultureInfo.InvariantCulture, out TimeSpan ts))
            {
                if (ts.TotalHours >= 0 && ts.TotalHours < 24)
                {
                    formattedTime = string.Format("{0:D2}:{1:D2}", ts.Hours, ts.Minutes);
                    return true;
                }
            }

            if (DateTime.TryParseExact(s, new[] { "H:m", "HH:mm", "H:mm", "HH:m" }, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime dt))
            {
                formattedTime = dt.ToString("HH:mm");
                return true;
            }

            // Input hanya angka (cth: "8" -> "08:00", "17" -> "17:00", "830" -> "08:30", "1730" -> "17:30")
            string digits = new string(s.Where(char.IsDigit).ToArray());
            if (digits.Length > 0 && digits.Length <= 4)
            {
                if (digits.Length <= 2)
                {
                    if (int.TryParse(digits, out int h) && h >= 0 && h < 24)
                    {
                        formattedTime = string.Format("{0:D2}:00", h);
                        return true;
                    }
                }
                else if (digits.Length == 3)
                {
                    if (int.TryParse(digits.Substring(0, 1), out int h) &&
                        int.TryParse(digits.Substring(1, 2), out int m) &&
                        h >= 0 && h < 24 && m >= 0 && m < 60)
                    {
                        formattedTime = string.Format("{0:D2}:{1:D2}", h, m);
                        return true;
                    }
                }
                else if (digits.Length == 4)
                {
                    if (int.TryParse(digits.Substring(0, 2), out int h) &&
                        int.TryParse(digits.Substring(2, 2), out int m) &&
                        h >= 0 && h < 24 && m >= 0 && m < 60)
                    {
                        formattedTime = string.Format("{0:D2}:{1:D2}", h, m);
                        return true;
                    }
                }
            }

            return false;
        }

        private int _durasiMinutes;
        public int DurasiMinutes
        {
            get { return _durasiMinutes; }
            set { _durasiMinutes = value; OnPropertyChanged("DurasiMinutes"); }
        }

        private string _durasiText = "-";
        public string DurasiText
        {
            get { return _durasiText; }
            set { _durasiText = value; OnPropertyChanged("DurasiText"); }
        }

        private string _keterangan = "";
        public string Keterangan
        {
            get { return _keterangan; }
            set { _keterangan = value ?? ""; OnPropertyChanged("Keterangan"); }
        }

        public void UpdateAkurasi()
        {
            int effectiveTotal = Math.Max(TotalNota, _notaSalah);
            if (effectiveTotal <= 0)
            {
                Akurasi = 100.0;
            }
            else
            {
                double ratio = Math.Max(0.0, 1.0 - ((double)_notaSalah / effectiveTotal));
                Akurasi = Math.Round(ratio * 100.0, 1);
            }
        }

        public void UpdateDurasi()
        {
            if (string.IsNullOrWhiteSpace(_jamDatang) || string.IsNullOrWhiteSpace(_jamPulang))
            {
                DurasiMinutes = 0;
                DurasiText = "-";
                return;
            }

            string[] p1 = _jamDatang.Split(':');
            string[] p2 = _jamPulang.Split(':');
            if (p1.Length < 2 || p2.Length < 2)
            {
                DurasiMinutes = 0;
                DurasiText = "-";
                return;
            }

            int h1, m1, h2, m2;
            if (!int.TryParse(p1[0], out h1) || !int.TryParse(p1[1], out m1) ||
                !int.TryParse(p2[0], out h2) || !int.TryParse(p2[1], out m2))
            {
                DurasiMinutes = 0;
                DurasiText = "-";
                return;
            }

            int startMins = h1 * 60 + m1;
            int endMins = h2 * 60 + m2;
            if (endMins < startMins)
            {
                endMins += 24 * 60;
            }

            int diff = endMins - startMins;
            DurasiMinutes = diff;
            int hours = diff / 60;
            int mins = diff % 60;
            DurasiText = mins > 0 ? string.Format("{0} jam {1:D2} mnt", hours, mins) : string.Format("{0} jam", hours);
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string propName)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(propName));
        }
    }

    public class AdminInvoiceMetric
    {
        public string User { get; set; }
        public int TotalNota { get; set; }
        public int TotalSalah { get; set; }
        public double SalahPercentage { get; set; }
        public double AverageNotaPerDay { get; set; }
        public double ContributionPercentage { get; set; }
    }

    public class UserNotaSummary
    {
        public string User { get; set; }
        public int TotalNota { get; set; }          // nota count from active filter
        public double ContributionPct { get; set; } // contribution % vs filtered total
    }

    public class ChartBarItem
    {
        public string Label { get; set; }
        public int Value { get; set; }
        public double Percentage { get; set; }
    }

    public class SavedFileMeta
    {
        public string Key { get; set; }
        public string Label { get; set; }
        public string MonthName { get; set; }
        public int Year { get; set; }
        public string FileName { get; set; }
        public long SavedAt { get; set; }
    }
}

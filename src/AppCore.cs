using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Web.Script.Serialization;

namespace LoanTracker
{
    public sealed class Loan
    {
        public string Id { get; set; }
        public string Lender { get; set; }
        public string Description { get; set; }
        public decimal Principal { get; set; }
        public string CreatedOn { get; set; }
    }

    public sealed class Repayment
    {
        public string Id { get; set; }
        public string LoanId { get; set; }
        public decimal Amount { get; set; }
        public string DueDate { get; set; }
        public bool IsPaid { get; set; }
    }

    public sealed class AppData
    {
        public int Version { get; set; }
        public List<Loan> Loans { get; set; }
        public List<Repayment> Repayments { get; set; }
    }

    public enum DueDateBand
    {
        None,
        Green,
        Yellow,
        Red
    }

    public static class AppCore
    {
        public static string DateKey(DateTime date)
        {
            return date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        }

        public static AppData CreateDefaultData()
        {
            return new AppData
            {
                Version = 1,
                Loans = new List<Loan>(),
                Repayments = new List<Repayment>()
            };
        }

        public static AppData Normalize(AppData data)
        {
            if (data == null)
                throw new InvalidDataException("The selected file does not contain a Loan Tracker backup.");
            if (data.Version != 1)
                throw new InvalidDataException("This data file was created by an unsupported Loan Tracker version.");
            if (data.Loans == null || data.Repayments == null)
                throw new InvalidDataException("The data file is incomplete or has an invalid format.");
            if (data.Loans.Count > 10000 || data.Repayments.Count > 100000)
                throw new InvalidDataException("The data file contains too many records.");

            var loanIds = new HashSet<string>(StringComparer.Ordinal);
            foreach (Loan loan in data.Loans)
            {
                if (loan == null || String.IsNullOrWhiteSpace(loan.Id) ||
                    String.IsNullOrWhiteSpace(loan.Lender) || loan.Principal <= 0m ||
                    loan.Principal > 1000000000000m)
                    throw new InvalidDataException("A loan in this file is invalid.");

                loan.Lender = loan.Lender.Trim();
                loan.Description = (loan.Description ?? "").Trim();
                if (loan.Id.Length > 64 || loan.Lender.Length > 120 || loan.Description.Length > 200 ||
                    !loanIds.Add(loan.Id))
                    throw new InvalidDataException("The data file contains a duplicate or invalid loan.");
                if (String.IsNullOrEmpty(loan.CreatedOn))
                    loan.CreatedOn = DateKey(DateTime.Today);
                ValidateDateKey(loan.CreatedOn);
            }

            var repaymentIds = new HashSet<string>(StringComparer.Ordinal);
            foreach (Repayment repayment in data.Repayments)
            {
                if (repayment == null || String.IsNullOrWhiteSpace(repayment.Id) ||
                    !loanIds.Contains(repayment.LoanId) || repayment.Amount <= 0m ||
                    repayment.Amount > 1000000000000m)
                    throw new InvalidDataException("A repayment in this file is invalid.");

                ValidateDateKey(repayment.DueDate);
                if (repayment.Id.Length > 64 || !repaymentIds.Add(repayment.Id))
                    throw new InvalidDataException("The data file contains a duplicate repayment.");
            }

            return data;
        }

        public static decimal GetTotalPrincipal(AppData data)
        {
            return data.Loans.Sum(loan => loan.Principal);
        }

        public static decimal GetRepaymentsDueThisMonth(AppData data, DateTime month)
        {
            string monthKey = month.ToString("yyyy-MM", CultureInfo.InvariantCulture);
            return data.Repayments
                .Where(repayment => repayment.DueDate.StartsWith(monthKey, StringComparison.Ordinal))
                .Sum(repayment => repayment.Amount);
        }

        public static DueDateBand GetDueDateBand(DateTime dueDate, DateTime today)
        {
            int daysUntilDue = (dueDate.Date - today.Date).Days;
            if (daysUntilDue <= 7)
                return DueDateBand.Red;
            if (daysUntilDue <= 14)
                return DueDateBand.Yellow;
            if (daysUntilDue <= 21)
                return DueDateBand.Green;
            return DueDateBand.None;
        }

        public static string Serialize(AppData data)
        {
            var serializer = new JavaScriptSerializer { MaxJsonLength = 10000000 };
            return serializer.Serialize(Normalize(data));
        }

        public static AppData Deserialize(string json)
        {
            if (String.IsNullOrWhiteSpace(json))
                throw new InvalidDataException("The selected data file is empty.");

            var serializer = new JavaScriptSerializer { MaxJsonLength = 10000000 };
            AppData data;
            try
            {
                data = serializer.Deserialize<AppData>(json);
            }
            catch (Exception ex)
            {
                throw new InvalidDataException("The data file is not valid JSON.", ex);
            }
            return Normalize(data);
        }

        private static void ValidateDateKey(string key)
        {
            DateTime parsed;
            if (String.IsNullOrEmpty(key) ||
                !DateTime.TryParseExact(key, "yyyy-MM-dd", CultureInfo.InvariantCulture,
                    DateTimeStyles.None, out parsed) ||
                parsed < new DateTime(1753, 1, 1) ||
                parsed > new DateTime(9998, 12, 31))
                throw new InvalidDataException("The data file contains an invalid date.");
        }
    }

    public sealed class DataStore
    {
        public string FilePath { get; private set; }

        public DataStore(string filePath)
        {
            FilePath = filePath;
        }

        public static DataStore ForCurrentUser()
        {
            string folder = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "LoanTracker");
            return new DataStore(Path.Combine(folder, "data.json"));
        }

        public AppData Load()
        {
            if (!File.Exists(FilePath))
            {
                AppData initial = AppCore.CreateDefaultData();
                Save(initial);
                return initial;
            }

            try
            {
                var file = new FileInfo(FilePath);
                if (file.Length > 10000000)
                    throw new InvalidDataException("The saved data file is larger than the supported 10 MB limit.");
                return AppCore.Deserialize(File.ReadAllText(FilePath));
            }
            catch (Exception ex)
            {
                throw new IOException(
                    "Loan Tracker could not load your saved data. The original data file has not been changed.\r\n\r\n" +
                    "File: " + FilePath + "\r\n\r\nDetails: " + ex.Message, ex);
            }
        }

        public void Save(AppData data)
        {
            string folder = Path.GetDirectoryName(FilePath);
            Directory.CreateDirectory(folder);
            string temporary = FilePath + ".tmp";
            try
            {
                File.WriteAllText(temporary, AppCore.Serialize(data));
                if (File.Exists(FilePath))
                    File.Replace(temporary, FilePath, FilePath + ".bak", true);
                else
                    File.Move(temporary, FilePath);
            }
            finally
            {
                if (File.Exists(temporary))
                    File.Delete(temporary);
            }
        }
    }
}

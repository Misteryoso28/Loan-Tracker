using System;
using System.IO;
using LoanTracker;

internal static class AppCoreTests
{
    private static int assertions;

    private static void Main()
    {
        TestTotalsAndMonthFiltering();
        TestDueDateBands();
        TestSerializationAndValidation();
        Console.WriteLine("Passed " + assertions + " Loan Tracker core assertions.");
    }

    private static void TestTotalsAndMonthFiltering()
    {
        var data = AppCore.CreateDefaultData();
        data.Loans.Add(new Loan { Id = "a", Lender = "Bank A", Principal = 1000m });
        data.Loans.Add(new Loan { Id = "b", Lender = "Bank B", Principal = 250m });
        data.Repayments.Add(new Repayment { Id = "r1", LoanId = "a", Amount = 100m, DueDate = "2026-10-10" });
        data.Repayments.Add(new Repayment { Id = "r2", LoanId = "a", Amount = 50m, DueDate = "2026-10-20" });
        data.Repayments.Add(new Repayment { Id = "r3", LoanId = "b", Amount = 75m, DueDate = "2026-11-01" });
        data.Repayments.Add(new Repayment { Id = "r4", LoanId = "b", Amount = 25m, DueDate = "2026-10-15", IsPaid = true });

        Equal(1250m, AppCore.GetTotalPrincipal(data), "principal total includes all loans");
        Equal(175m, AppCore.GetRepaymentsDueThisMonth(data, new DateTime(2026, 10, 1)),
            "monthly total includes all scheduled repayments");
    }

    private static void TestDueDateBands()
    {
        DateTime today = new DateTime(2026, 10, 1);
        Equal(DueDateBand.Red, AppCore.GetDueDateBand(today.AddDays(-1), today), "overdue dates are red");
        Equal(DueDateBand.Red, AppCore.GetDueDateBand(today.AddDays(7), today), "seven days is red");
        Equal(DueDateBand.Yellow, AppCore.GetDueDateBand(today.AddDays(8), today), "eight days is yellow");
        Equal(DueDateBand.Yellow, AppCore.GetDueDateBand(today.AddDays(14), today), "fourteen days is yellow");
        Equal(DueDateBand.Green, AppCore.GetDueDateBand(today.AddDays(15), today), "fifteen days is green");
        Equal(DueDateBand.Green, AppCore.GetDueDateBand(today.AddDays(21), today), "twenty-one days is green");
        Equal(DueDateBand.None, AppCore.GetDueDateBand(today.AddDays(22), today), "twenty-two days has no color");
        Equal(DueDateBand.None, AppCore.GetDueDateBand(today.AddDays(28), today), "twenty-eight days has no color");
        Equal(DueDateBand.None, AppCore.GetDueDateBand(today.AddDays(29), today), "later dates have no color");
    }

    private static void TestSerializationAndValidation()
    {
        var data = AppCore.CreateDefaultData();
        data.Loans.Add(new Loan { Id = "loan", Lender = "Lender", Principal = 800m, CreatedOn = "2026-10-01" });
        data.Repayments.Add(new Repayment { Id = "repayment", LoanId = "loan", Amount = 200m, DueDate = "2026-10-20" });
        AppData restored = AppCore.Deserialize(AppCore.Serialize(data));
        Equal(1, restored.Loans.Count, "round trip preserves loans");
        Equal(1, restored.Repayments.Count, "round trip preserves repayments");
        Equal(200m, restored.Repayments[0].Amount, "round trip preserves amount");

        data.Repayments[0].LoanId = "missing";
        Throws<InvalidDataException>(delegate { AppCore.Normalize(data); }, "unknown loan references are rejected");
        Throws<InvalidDataException>(delegate { AppCore.Deserialize("{"); }, "malformed JSON is rejected");
    }

    private static void Equal<T>(T expected, T actual, string message)
    {
        assertions++;
        if (!Object.Equals(expected, actual))
            throw new Exception(message + ": expected " + expected + ", got " + actual);
    }

    private static void Throws<T>(Action action, string message) where T : Exception
    {
        assertions++;
        try
        {
            action();
        }
        catch (T)
        {
            return;
        }
        throw new Exception(message + ": expected " + typeof(T).Name);
    }
}

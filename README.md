# Loan Tracker

Loan Tracker is an offline Windows desktop app for organizing loans and their scheduled repayments. It highlights upcoming due dates, shows the total loan principal, and summarizes repayments scheduled for the current month.

## Features

- Record loans with a lender, description, and principal amount.
- Schedule multiple repayments for a loan, each with a due date and amount.
- Mark repayments as paid and remove repayments or loans.
- Highlight upcoming repayments by due date: red within 7 days (including overdue), yellow within 14 days, green within 21 days, and no highlight within 22-28 days.
- Review total loan principal and all repayments scheduled for the current month.
- Save data locally in `%APPDATA%\LoanTracker\data.json`; no account or internet connection is required.

## Build and test

Run in PowerShell on Windows with the .NET Framework 4.x developer tools installed:

```powershell
.\build.ps1 -RunTests
```

The app executable is created in `dist\LoanTracker.exe`. The test executable is created in `dist\LoanTracker.CoreTests.exe`.

Loan data is stored on this computer and is not synchronized or backed up online. Keep a separate copy of the data file if you need a backup.

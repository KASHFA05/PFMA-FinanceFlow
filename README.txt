FINAL PROJECT - 07 Oct (use only THIS zip, delete older PFMA zips/folders)
=========================================================================
1. Extract this zip to a NEW folder (Desktop).
2. Open PFMA.sln in Visual Studio.
3. Build > Rebuild Solution. If Visual Studio asks "There were build errors ... run the last
   successful build?" click NO and send a screenshot of the Error List.
4. Run (https). The browser must open the animated WELCOME page first (tab title
   "Welcome - FinanceFlow (blue v3)"), then Get Started opens the new Login page.

PFMA - Personal Finance Management App (merged project)
=======================================================

Merge of FinanceFlow_Share (base) + WebApplication4 (FYP).

HOW TO RUN
1. Close Visual Studio. Delete your old PFMA folder (or use a new one).
2. Unzip this archive, open PFMA.sln in Visual Studio 2022/2026.
3. Build > Rebuild Solution (NuGet restore needs internet the first time).
4. Press the green https button.
   The database "PFMA_FinanceDB" is created automatically on first run (LocalDB),
   including demo data. No migration commands needed.

DEMO LOGINS
  Admin   : admin@financeflow.com   / admin123
  Advisor : ahmad123@gmail.com  / ahmad123   (Ahmad)
  User    : kashfa123@gmail.com / kashfa123  (Kashfa, has the demo data)
  User    : iqra123@gmail.com   / iqra123    (Iqra)

FEATURES
  From FinanceFlow : Login/Register (hashed passwords), Dashboard, Transactions,
                     Categories, Savings Goals, Bill Reminders, Analytics/Reports
                     (CSV + PDF), Profile, Advisor panel, Admin panel.
  From WebApplication4 (now per-user, DB driven): Budgets (with spent vs limit),
                     Debt Tracker (To Pay / To Receive), Bill QR Codes.

NOTES
  - If SQL Server LocalDB is not installed, change the connection string in
    appsettings.json.
  - Old FYP hard-coded logins, Identity scaffolding and old migrations were
    removed on purpose (they conflicted with the FinanceFlow design).

EXPORT / IMPORT (updated)
  - Reports page  : "Download PDF" now creates the PDF on the server (no internet or
                    JavaScript library needed). Same look as the FinanceFlow sample report:
                    header, 4 summary cards, Key Insights (calculated from your real data),
                    spending by category and the full transaction list, with page numbers.
  - Export to Excel (CSV) : Reports page and Transactions page.
  - Import CSV    : Transactions page > Import CSV. Columns: Date, Type, Category,
                    Description, Amount, Notes. Use "Download Template" for a sample.
                    The exported CSV can be imported again; duplicates are skipped.

COLORFUL DESIGN (updated)
  Savings Goals, Debt & Loan Tracker, Bills & Payment Alerts, Manage Categories,
  Transaction History and Admin Control Panel now use the colorful card design
  (css: wwwroot/css/colorful.css). Currency is Rs. everywhere.
  New: Categories are stored per user (icons, Income/Expense), Edit/Delete for goals,
  bills, debts and transactions, and the admin "View Details" inspection panel.
  The database is now named PFMA_FinanceDB (new tables were added). If you already
  ran an older build, nothing needs to be deleted - the new database is created.

ADMIN: USER DETAILS (updated)
  Admin Panel > click a user's name or "View Details" > full page with tabs:
  Overview, Transactions, Budgets, Savings Goals, Bills, Debts, Bill QR, Categories
  (admin can also EDIT or DELETE the user's transactions, budgets, goals, bills and debts).

ADVISOR DASHBOARD
  Spending Trends chart now has a Weekly (last 8 weeks) / Monthly toggle with real
  average and week-over-week / month-over-month change.

WELCOME PAGE
  The app now starts with an animated Welcome page (Views/Welcome/Index.cshtml) and
  opens the Login page when the user clicks Get Started (no auto redirect).
  If a user is already logged in, the Welcome page sends him straight to his dashboard.
  Refreshing the Login page (F5) shows the Welcome page again.
  Login page redesigned: split screen with an animated rotating showcase (left) and the form (right).
  Register page redesigned too: form on the left, animated cards on the right, password strength meter.
  Welcome page redesigned as a landing page (navbar, animated headline, dashboard + phone mockup, Open an Account / Sign In).

DASHBOARD CHART
  Spending Trends is now a neon glowing chart (income line with arrow, expenses line, savings bars) with a working 6 Months / 1 Year switch.
  Dashboard flow chart has Daily (30 days), Weekly (12 weeks), 6 Months and 1 Year views.
  Admin panel has a Platform Flow chart (all users) with Daily / Weekly / Monthly views.


NEW IN THIS VERSION (18 Oct)
=============================
1. MULTI-CURRENCY
   Profile > Currency: choose PKR (Rs.), USD, EUR, GBP, SAR, AED or INR. The symbol changes on every
   page and in the PDF report. The choice is saved per user (cookie). No amount conversion is done -
   amounts are shown exactly as entered, only the symbol changes.

2. BANK / JAZZCASH / EASYPAISA STATEMENT IMPORT
   Transactions > Import CSV now also accepts statements downloaded from a bank or wallet app.
   Save the statement as CSV, then upload it. Recognised columns:
     Date (or Transaction Date / Value Date)
     Description (or Narration / Details / Particulars / Remarks)
     Amount  OR  Debit + Credit (Withdrawal + Deposit)
   Debit = Expense, Credit = Income. Duplicates are skipped automatically.
   (Direct live linking to a bank account needs the bank's official API / merchant agreement,
   so the project uses statement import instead.)

3. MOBILE APP (PWA)
   The website can now be installed on a phone: open it in Chrome/Safari on the phone and choose
   "Add to Home screen" / "Install app". It then opens like a normal app with the FinanceFlow icon.
   (Needs https or localhost.)

4. ONLINE DEPLOYMENT READY
   See DEPLOYMENT.txt. appsettings.Production.json holds the server database connection string.
   In Production the database is never deleted automatically and demo passwords are not re-applied.

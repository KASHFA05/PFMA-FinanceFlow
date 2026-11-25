# FinanceFlow - Personal Finance Management Application (PFMA)

Final Year Project - Department of Computer Science / IT, University of Southern Punjab.

FinanceFlow is a secure web application for managing personal money: track income and expenses,
set budgets and savings goals, get bill and debt reminders, receive advisor recommendations,
and export reports as PDF / CSV.

## Roles
| Role | What it can do |
|---|---|
| **User** | Transactions, budgets, savings goals, bills, debts, categories, bill QR codes, dashboard charts, PDF/CSV export & import |
| **Advisor** | See assigned clients, write recommendations, weekly / monthly client chart |
| **Admin** | View, edit and delete users and their data, platform flow chart |

## Tech stack
- ASP.NET Core MVC (.NET 8), C#, Razor
- Entity Framework Core 8 + SQL Server LocalDB
- Chart.js, QRCoder, Bootstrap, jQuery, Font Awesome
- Custom PDF / CSV export

## How to run
1. Install **Visual Studio 2022** (ASP.NET workload) - LocalDB is included.
2. Clone or download this repository and open `PFMA.sln`.
3. Build > Rebuild Solution (internet needed the first time for NuGet packages).
4. Press the green **https** button. The database `PFMA_FinanceDB` and demo data are created automatically.

## Demo logins
| Role | Email | Password |
|---|---|---|
| Admin | admin@financeflow.com | admin123 |
| Advisor | ahmad123@gmail.com | ahmad123 |
| User | kashfa123@gmail.com | kashfa123 |
| User | laiba123@gmail.com | laiba123 |

> These are demo accounts for testing only.

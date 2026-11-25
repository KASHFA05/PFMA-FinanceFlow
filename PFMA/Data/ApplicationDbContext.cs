using System;
using Microsoft.EntityFrameworkCore;
using PFMA.Models;

namespace PFMA.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<Transaction> Transactions { get; set; }
        public DbSet<SavingsGoal> SavingsGoals { get; set; }
        public DbSet<BillReminder> BillReminders { get; set; }
        public DbSet<AdvisorRecommendation> AdvisorRecommendations { get; set; }
        public DbSet<AdvisorClient> AdvisorClients { get; set; }
        public DbSet<Budget> Budgets { get; set; }
        public DbSet<Debt> Debts { get; set; }
        public DbSet<BillQRCode> BillQRCodes { get; set; }
        public DbSet<Category> Categories { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure decimal precision
            modelBuilder.Entity<Transaction>()
                .Property(t => t.Amount)
                .HasPrecision(18, 2);

            modelBuilder.Entity<SavingsGoal>()
                .Property(g => g.TargetAmount)
                .HasPrecision(18, 2);

            modelBuilder.Entity<SavingsGoal>()
                .Property(g => g.CurrentAmount)
                .HasPrecision(18, 2);

            modelBuilder.Entity<BillReminder>()
                .Property(r => r.Amount)
                .HasPrecision(18, 2);

            modelBuilder.Entity<Budget>().Property(b => b.Amount).HasPrecision(18, 2);
            modelBuilder.Entity<Debt>().Property(d => d.TotalAmount).HasPrecision(18, 2);
            modelBuilder.Entity<Debt>().Property(d => d.ClearedAmount).HasPrecision(18, 2);
            modelBuilder.Entity<BillQRCode>().Property(q => q.Amount).HasPrecision(18, 2);

            // Avoid multiple cascade paths error in SQL Server
            modelBuilder.Entity<AdvisorClient>()
                .HasOne(ac => ac.Advisor)
                .WithMany()
                .HasForeignKey(ac => ac.AdvisorId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<AdvisorClient>()
                .HasOne(ac => ac.Client)
                .WithMany()
                .HasForeignKey(ac => ac.ClientId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<AdvisorRecommendation>()
                .HasOne(ar => ar.Advisor)
                .WithMany()
                .HasForeignKey(ar => ar.AdvisorId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<AdvisorRecommendation>()
                .HasOne(ar => ar.Client)
                .WithMany()
                .HasForeignKey(ar => ar.ClientId)
                .OnDelete(DeleteBehavior.Restrict);

            // Seed initial data
            // Hashed using simple SHA256 corresponding to: "admin123", "ahmad123", "kashfa123", "iqra123"
            // Let's use the actual pre-calculated hashes (SHA256 of the plain text base64 string)
            // "admin123" -> jGl25bex9uM586mq9j1A1Z9j37tJ112521m6g5 = eM78G09U/7d1+Ld/c2zZpP8XfXkYF23q34Vz0lYm9mY= (let's just seed text hashes or precomputed values)
            string adminHash = Convert.ToBase64String(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes("admin123")));
            string advisorHash = Convert.ToBase64String(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes("ahmad123")));
            string alexHash = Convert.ToBase64String(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes("kashfa123")));
            string janeHash = Convert.ToBase64String(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes("iqra123")));

            modelBuilder.Entity<User>().HasData(
                new User
                {
                    UserId = 1,
                    Name = "Admin",
                    Email = "admin@financeflow.com",
                    PasswordHash = adminHash,
                    Role = "Admin",
                    AccountType = "Standard",
                    ProfilePictureUrl = "/images/avatars/admin.png"
                },
                new User
                {
                    UserId = 2,
                    Name = "Ahmad",
                    Email = "ahmad123@gmail.com",
                    PasswordHash = advisorHash,
                    Role = "Advisor",
                    AccountType = "Premium",
                    ProfilePictureUrl = "/images/avatars/advisor.png"
                },
                new User
                {
                    UserId = 3,
                    Name = "Kashfa",
                    Email = "kashfa123@gmail.com",
                    PasswordHash = alexHash,
                    Role = "User",
                    AccountType = "Premium",
                    ProfilePictureUrl = "/images/avatars/alex.png",
                    TwoFactorEnabled = false,
                    EmailNotificationsEnabled = false,
                    LoginAlertsEnabled = false
                },
                new User
                {
                    UserId = 4,
                    Name = "Iqra",
                    Email = "iqra123@gmail.com",
                    PasswordHash = janeHash,
                    Role = "User",
                    AccountType = "Standard",
                    ProfilePictureUrl = "/images/avatars/jane.png"
                }
            );

            modelBuilder.Entity<AdvisorClient>().HasData(
                new AdvisorClient { Id = 1, AdvisorId = 2, ClientId = 3 },
                new AdvisorClient { Id = 2, AdvisorId = 2, ClientId = 4 }
            );

            modelBuilder.Entity<SavingsGoal>().HasData(
                new SavingsGoal
                {
                    GoalId = 1,
                    UserId = 3,
                    GoalName = "Emergency Fund",
                    TargetAmount = 15000.00m,
                    CurrentAmount = 12000.00m,
                    TargetDate = new DateTime(2026, 12, 26)
                },
                new SavingsGoal
                {
                    GoalId = 2,
                    UserId = 3,
                    GoalName = "House Deposit",
                    TargetAmount = 50000.00m,
                    CurrentAmount = 17500.00m,
                    TargetDate = new DateTime(2028, 06, 26)
                }
            );

            modelBuilder.Entity<BillReminder>().HasData(
                new BillReminder
                {
                    ReminderId = 1,
                    UserId = 3,
                    Title = "Monthly Internet Subscription",
                    Category = "Utility",
                    Provider = "Comcast",
                    Amount = 45.00m,
                    DueDate = new DateTime(2024, 05, 15),
                    Status = "Overdue"
                },
                new BillReminder
                {
                    ReminderId = 2,
                    UserId = 3,
                    Title = "Rent - June 2024",
                    Category = "Housing",
                    Provider = "Property Management",
                    Amount = 1200.00m,
                    DueDate = new DateTime(2024, 06, 01),
                    Status = "Upcoming"
                },
                new BillReminder
                {
                    ReminderId = 3,
                    UserId = 3,
                    Title = "Electricity Bill",
                    Category = "Utility",
                    Provider = "Clean Energy Co.",
                    Amount = 85.20m,
                    DueDate = new DateTime(2024, 06, 05),
                    Status = "Upcoming"
                },
                new BillReminder
                {
                    ReminderId = 4,
                    UserId = 3,
                    Title = "Spotify Family",
                    Category = "Entertainment",
                    Provider = "Digital Services",
                    Amount = 15.99m,
                    DueDate = new DateTime(2024, 05, 20),
                    Status = "Paid"
                }
            );

            // Transactions seeding for Kashfa (UserId = 3)
            // Income = $5,200.00 (Salary: $4250 + $950)
            // Expenses = $3,120.45 (Rent: $1200, Grocery: $84.50, Uber: $24.12, Utility: $142.00, Starbucks: $15.00, Dining: $354.83, Shopping: $800.00, Health: $500.00)
            // Total Balance = $12,450.80
            modelBuilder.Entity<Transaction>().HasData(
                new Transaction
                {
                    TransactionId = 1,
                    UserId = 3,
                    Amount = 4250.00m,
                    Type = "Income",
                    Category = "Salary",
                    Description = "Monthly Salary",
                    Date = new DateTime(2023, 10, 22),
                    Notes = "Main corporate salary transfer"
                },
                new Transaction
                {
                    TransactionId = 2,
                    UserId = 3,
                    Amount = 950.00m,
                    Type = "Income",
                    Category = "Salary",
                    Description = "TechCorp Monthly Salary",
                    Date = new DateTime(2023, 10, 23),
                    Notes = "Part-time consulting retainer"
                },
                new Transaction
                {
                    TransactionId = 3,
                    UserId = 3,
                    Amount = 84.50m,
                    Type = "Expense",
                    Category = "Food",
                    Description = "Grocery Supermarket",
                    Date = new DateTime(2023, 10, 24),
                    Notes = "Weekly groceries at Whole Foods"
                },
                new Transaction
                {
                    TransactionId = 4,
                    UserId = 3,
                    Amount = 1200.00m,
                    Type = "Expense",
                    Category = "Housing",
                    Description = "Central Apartments Rent",
                    Date = new DateTime(2023, 10, 20),
                    Notes = "Apartment 4B monthly rent"
                },
                new Transaction
                {
                    TransactionId = 5,
                    UserId = 3,
                    Amount = 24.12m,
                    Type = "Expense",
                    Category = "Transport",
                    Description = "Uber Ride",
                    Date = new DateTime(2023, 10, 19),
                    Notes = "Commute to office"
                },
                new Transaction
                {
                    TransactionId = 6,
                    UserId = 3,
                    Amount = 142.00m,
                    Type = "Expense",
                    Category = "Bills",
                    Description = "Utility Bill - Energy",
                    Date = new DateTime(2023, 10, 18),
                    Notes = "Monthly power consumption invoice"
                },
                new Transaction
                {
                    TransactionId = 7,
                    UserId = 3,
                    Amount = 120.00m,
                    Type = "Income",
                    Category = "Misc",
                    Description = "Marketplace Sale",
                    Date = new DateTime(2023, 10, 15),
                    Notes = "Sold old study desk"
                },
                new Transaction
                {
                    TransactionId = 8,
                    UserId = 3,
                    Amount = 15.00m,
                    Type = "Expense",
                    Category = "Food",
                    Description = "Starbucks Coffee",
                    Date = new DateTime(2023, 10, 21),
                    Notes = "Coffee with client"
                },
                new Transaction
                {
                    TransactionId = 9,
                    UserId = 3,
                    Amount = 354.83m,
                    Type = "Expense",
                    Category = "Food",
                    Description = "Dining & Drinks",
                    Date = new DateTime(2023, 10, 14),
                    Notes = "Weekend dinner party"
                },
                new Transaction
                {
                    TransactionId = 10,
                    UserId = 3,
                    Amount = 800.00m,
                    Type = "Expense",
                    Category = "Shopping",
                    Description = "Retail & Shopping",
                    Date = new DateTime(2023, 10, 12),
                    Notes = "New winter wardrobe items"
                },
                new Transaction
                {
                    TransactionId = 11,
                    UserId = 3,
                    Amount = 500.00m,
                    Type = "Expense",
                    Category = "Bills",
                    Description = "Health & Fitness Subscription",
                    Date = new DateTime(2023, 10, 10),
                    Notes = "Gym annual membership fee"
                }
            );

            modelBuilder.Entity<Budget>().HasData(
                new Budget { BudgetId = 1, UserId = 3, Category = "Food", Amount = 600m, Month = "2023-10" },
                new Budget { BudgetId = 2, UserId = 3, Category = "Transport", Amount = 150m, Month = "2023-10" },
                new Budget { BudgetId = 3, UserId = 3, Category = "Shopping", Amount = 500m, Month = "2023-10" }
            );

            modelBuilder.Entity<Debt>().HasData(
                new Debt { DebtId = 1, UserId = 3, Title = "Borrowed from Ali", Type = "To Pay", TotalAmount = 50000m, ClearedAmount = 15000m, DueDate = new DateTime(2026, 10, 15) },
                new Debt { DebtId = 2, UserId = 3, Title = "Lent to Sara", Type = "To Receive", TotalAmount = 20000m, ClearedAmount = 5000m, DueDate = new DateTime(2026, 9, 1) },
                new Debt { DebtId = 3, UserId = 3, Title = "University Fee Loan", Type = "To Pay", TotalAmount = 120000m, ClearedAmount = 120000m, DueDate = new DateTime(2026, 8, 1) }
            );

            modelBuilder.Entity<BillQRCode>().HasData(
                new BillQRCode { QRCodeId = 1, UserId = 3, Title = "WAPDA Electricity", ConsumerNumber = "0412345678901", Amount = 85.20m }
            );

            modelBuilder.Entity<AdvisorRecommendation>().HasData(
                new AdvisorRecommendation
                {
                    RecommendationId = 1,
                    AdvisorId = 2,
                    ClientId = 3,
                    Text = "Based on Kashfa's spending in Jun, transportation costs have spiked by 15%. Recommend reviewing recurring subscriptions next month to offset the increase.",
                    CreatedAt = new DateTime(2026, 06, 24)
                }
            );
        }
    }
}

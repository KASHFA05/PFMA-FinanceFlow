namespace PFMA.Models
{
    /// <summary>Everything the admin can see about one user.</summary>
    public class AdminUserDetailsViewModel
    {
        public User User { get; set; } = new User();
        public List<Transaction> Transactions { get; set; } = new();
        public List<Budget> Budgets { get; set; } = new();
        public List<SavingsGoal> Goals { get; set; } = new();
        public List<BillReminder> Bills { get; set; } = new();
        public List<Debt> Debts { get; set; } = new();
        public List<BillQRCode> QRCodes { get; set; } = new();
        public List<Category> Categories { get; set; } = new();

        public decimal TotalIncome { get; set; }
        public decimal TotalExpense { get; set; }
        public decimal NetSavings => TotalIncome - TotalExpense;
    }
}

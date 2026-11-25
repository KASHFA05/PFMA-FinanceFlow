using Microsoft.EntityFrameworkCore;
using PFMA.Data;

var builder = WebApplication.CreateBuilder(args);

// Database (SQL Server LocalDB)
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Session services (used for login)
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

builder.Services.AddControllersWithViews();
builder.Services.AddHttpContextAccessor();

var app = builder.Build();

// Create the database and seed data automatically on first run
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

    try
    {
        db.Database.EnsureCreated();

        // Check that the existing database has the same structure as the models.
        // An old database (made by an older version) misses new tables/columns.
        _ = db.Debts.Select(d => new { d.ClearedAmount, d.DueDate, d.Title }).FirstOrDefault();
        _ = db.Categories.Select(c => new { c.Icon, c.Type }).FirstOrDefault();
        _ = db.Budgets.Select(b => b.Month).FirstOrDefault();
        _ = db.BillQRCodes.Select(q => q.ConsumerNumber).FirstOrDefault();
    }
    catch (Microsoft.Data.SqlClient.SqlException) when (app.Environment.IsDevelopment())
    {
        // Only in Development: a live (Production) database is never deleted automatically.
        // Old structure found: delete and create the database again with demo data.
        logger.LogWarning("Database structure is old. Re-creating the database...");
        db.Database.EnsureDeleted();
        db.Database.EnsureCreated();
    }
}

// Rename the demo users in an existing database (safe to run many times).
// Skipped in Production so that demo passwords are never re-applied on a live server.
if (app.Environment.IsDevelopment())
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    try
    {
        void Rename(string oldEmail, string newName, string newEmail, string newPassword)
        {
            var u = db.Users.FirstOrDefault(x => x.Email == oldEmail);
            if (u == null) return;
            u.Name = newName;
            u.PasswordHash = PFMA.Helpers.PasswordHelper.HashPassword(newPassword);
            if (!db.Users.Any(x => x.Email == newEmail)) u.Email = newEmail;
        }

        // Ahmad (advisor)
        Rename("advisor@financeflow.com", "Ahmad", "ahmad123@gmail.com", "ahmad123");

        // Kashfa (was Alex Rivers / Laiba)
        Rename("alex.rivers@finflow.com", "Kashfa", "kashfa123@gmail.com", "kashfa123");
        Rename("laiba@finflow.com", "Kashfa", "kashfa123@gmail.com", "kashfa123");
        Rename("kashfa.demo@finflow.com", "Kashfa", "kashfa123@gmail.com", "kashfa123");

        // Iqra (was Jane Doe)
        Rename("jane.doe@finflow.com", "Laiba", "laiba123@gmail.com", "laiba123");
        Rename("iqra@finflow.com", "Laiba", "laiba123@gmail.com", "laiba123");

        db.SaveChanges();
    }
    catch (Exception ex)
    {
        scope.ServiceProvider.GetRequiredService<ILogger<Program>>().LogWarning(ex, "Could not rename demo users.");
    }
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

// Needed when the site runs behind a hosting proxy / load balancer (Azure, IIS, etc.)
app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedFor |
                       Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedProto
});

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

app.UseSession();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Welcome}/{action=Index}/{id?}");

app.Run();

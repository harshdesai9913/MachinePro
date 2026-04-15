using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.Cookies;
using MachinePro.Data;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();
var dbPath = Environment.GetEnvironmentVariable("DB_PATH") ?? "machinepro.db";
var dbDir = Path.GetDirectoryName(Path.GetFullPath(dbPath));
if (!string.IsNullOrEmpty(dbDir))
    Directory.CreateDirectory(dbDir);
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite($"Data Source={dbPath}"));

// Cookie authentication
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.LogoutPath = "/Account/Logout";
        options.ExpireTimeSpan = TimeSpan.FromHours(12);
    });

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated();

    // Add new columns to CustomerMasters if they don't exist (SQLite schema migration)
    var conn = db.Database.GetDbConnection();
    conn.Open();
    var cols = new List<string>();
    using (var cmd = conn.CreateCommand())
    {
        cmd.CommandText = "PRAGMA table_info(CustomerMasters)";
        using var reader = cmd.ExecuteReader();
        while (reader.Read()) cols.Add(reader.GetString(1));
    }
    using (var cmd = conn.CreateCommand())
    {
        if (!cols.Contains("City"))    { cmd.CommandText = "ALTER TABLE CustomerMasters ADD COLUMN City TEXT"; cmd.ExecuteNonQuery(); }
        if (!cols.Contains("State"))   { cmd.CommandText = "ALTER TABLE CustomerMasters ADD COLUMN State TEXT"; cmd.ExecuteNonQuery(); }
        if (!cols.Contains("Country")) { cmd.CommandText = "ALTER TABLE CustomerMasters ADD COLUMN Country TEXT NOT NULL DEFAULT 'India'"; cmd.ExecuteNonQuery(); }
    }

    // Jobs table migrations
    var jobCols = new List<string>();
    using (var cmd = conn.CreateCommand())
    {
        cmd.CommandText = "PRAGMA table_info(Jobs)";
        using var reader = cmd.ExecuteReader();
        while (reader.Read()) jobCols.Add(reader.GetString(1));
    }
    using (var cmd = conn.CreateCommand())
    {
        if (!jobCols.Contains("ItemCode"))            { cmd.CommandText = "ALTER TABLE Jobs ADD COLUMN ItemCode TEXT"; cmd.ExecuteNonQuery(); }
        if (!jobCols.Contains("MachineBuildNumber"))  { cmd.CommandText = "ALTER TABLE Jobs ADD COLUMN MachineBuildNumber TEXT"; cmd.ExecuteNonQuery(); }
    }
    // CapacityLedgerEntries migration
    using (var cmd = conn.CreateCommand())
    {
        cmd.CommandText = @"CREATE TABLE IF NOT EXISTS CapacityLedgerEntries (
            Id INTEGER PRIMARY KEY AUTOINCREMENT,
            EntryDate TEXT NOT NULL,
            Serial TEXT NOT NULL,
            Customer TEXT NOT NULL,
            Model TEXT NOT NULL,
            ModuleName TEXT NOT NULL,
            MachineNumber TEXT,
            QtyProduced INTEGER NOT NULL,
            Notes TEXT,
            EnteredBy TEXT NOT NULL,
            CreatedAt TEXT NOT NULL
        )";
        cmd.ExecuteNonQuery();
    }

    // CapacityLedgerEntries column migrations
    var capCols = new List<string>();
    using (var cmd = conn.CreateCommand())
    {
        cmd.CommandText = "PRAGMA table_info(CapacityLedgerEntries)";
        using var reader = cmd.ExecuteReader();
        while (reader.Read()) capCols.Add(reader.GetString(1));
    }
    using (var cmd = conn.CreateCommand())
    {
        if (!capCols.Contains("MachineBuildNumber")) { cmd.CommandText = "ALTER TABLE CapacityLedgerEntries ADD COLUMN MachineBuildNumber TEXT"; cmd.ExecuteNonQuery(); }
    }

    conn.Close();
}

app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();

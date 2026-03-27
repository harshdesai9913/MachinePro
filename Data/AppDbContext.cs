using Microsoft.EntityFrameworkCore;
using MachinePro.Models;

namespace MachinePro.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Job> Jobs => Set<Job>();
    public DbSet<ModuleEntry> ModuleEntries => Set<ModuleEntry>();
    public DbSet<PlannerHistory> PlannerHistories => Set<PlannerHistory>();
    public DbSet<CustomerMaster> CustomerMasters => Set<CustomerMaster>();
    public DbSet<ModelMaster> ModelMasters => Set<ModelMaster>();
    public DbSet<AppUser> AppUsers => Set<AppUser>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Job>()
            .HasMany(j => j.ModuleEntries)
            .WithOne(m => m.Job)
            .HasForeignKey(m => m.JobId)
            .OnDelete(DeleteBehavior.Cascade);

        // Single admin account to get started — add all other users from User Management
        modelBuilder.Entity<AppUser>().HasData(
            new AppUser { Id = 1, Username = "admin", Password = "admin", FullName = "Administrator", Role = "Manager" }
        );

        // Seed Customer Master
        modelBuilder.Entity<CustomerMaster>().HasData(
            new CustomerMaster { Id = 1, CustomerName = "Tata Motors" },
            new CustomerMaster { Id = 2, CustomerName = "Reliance Ind" },
            new CustomerMaster { Id = 3, CustomerName = "L&T Heavy" },
            new CustomerMaster { Id = 4, CustomerName = "Bajaj Auto" },
            new CustomerMaster { Id = 5, CustomerName = "Mahindra & Mahindra" }
        );

        modelBuilder.Entity<ModelMaster>().HasData(
            new ModelMaster { Id = 1, ModelName = "TM-400X" },
            new ModelMaster { Id = 2, ModelName = "TM-500Y" },
            new ModelMaster { Id = 3, ModelName = "RI-750" },
            new ModelMaster { Id = 4, ModelName = "LT-900K" },
            new ModelMaster { Id = 5, ModelName = "BA-200Z" },
            new ModelMaster { Id = 6, ModelName = "MM-600A" }
        );

        // Seed Jobs
        modelBuilder.Entity<Job>().HasData(
            new Job { Id = 1, Serial = "SN-001", Customer = "Tata Motors", Model = "TM-400X", Drawing = "DWG-2201", DrawingDescription = "Engine block housing plate", Qty = 12, InwardDate = "24/03/2026", Process1 = "VMC", Process2 = "Milling", Process3 = "Lathe" },
            new Job { Id = 2, Serial = "SN-002", Customer = "Reliance Ind", Model = "RI-750", Drawing = "DWG-3302", DrawingDescription = "Pump shaft coupling", Qty = 8, InwardDate = "24/03/2026", Process1 = "Lathe", Process2 = "Shaper" }
        );

        modelBuilder.Entity<ModuleEntry>().HasData(
            new ModuleEntry { Id = 1, JobId = 1, ModuleName = "VMC" },
            new ModuleEntry { Id = 2, JobId = 1, ModuleName = "Milling" },
            new ModuleEntry { Id = 3, JobId = 1, ModuleName = "Lathe" },
            new ModuleEntry { Id = 4, JobId = 2, ModuleName = "Lathe" },
            new ModuleEntry { Id = 5, JobId = 2, ModuleName = "Shaper" }
        );
    }
}

using System.ComponentModel.DataAnnotations;

namespace MachinePro.Models;

// ─── MASTER DATA (Manager uploads — independent entities) ───
public class CustomerMaster
{
    [Key]
    public int Id { get; set; }

    [Required]
    public string CustomerName { get; set; } = string.Empty;
    public string? City { get; set; }
    public string? State { get; set; }
    public string Country { get; set; } = "India";
}

public class ModelMaster
{
    [Key]
    public int Id { get; set; }

    [Required]
    public string ModelName { get; set; } = string.Empty;
}

// ─── JOB ───
public class Job
{
    [Key]
    public int Id { get; set; }

    [Required]
    public string Serial { get; set; } = string.Empty;

    [Required]
    public string Customer { get; set; } = string.Empty;

    [Required]
    public string Model { get; set; } = string.Empty;

    [Required]
    public string Drawing { get; set; } = string.Empty;

    public string? ItemCode { get; set; }

    public string? DrawingDescription { get; set; }

    [Required]
    public int Qty { get; set; }

    public string InwardDate { get; set; } = string.Empty;

    public int? Priority { get; set; } // 1-10, nullable if not set

    public bool IsCompleted { get; set; } // Moved to Finished module
    public string? CompletedDate { get; set; }

    // Processes (up to 5)
    public string? Process1 { get; set; }
    public string? Process2 { get; set; }
    public string? Process3 { get; set; }
    public string? Process4 { get; set; }
    public string? Process5 { get; set; }

    // Navigation
    public ICollection<ModuleEntry> ModuleEntries { get; set; } = new List<ModuleEntry>();

    public List<string> GetProcesses()
    {
        return new List<string?> { Process1, Process2, Process3, Process4, Process5 }
            .Where(p => !string.IsNullOrEmpty(p))
            .Select(p => p!)
            .ToList();
    }

    public string? GetProcessAt(int index) => index switch
    {
        0 => Process1, 1 => Process2, 2 => Process3, 3 => Process4, 4 => Process5, _ => null
    };

    public void SetProcessAt(int index, string? value)
    {
        switch (index)
        {
            case 0: Process1 = value; break;
            case 1: Process2 = value; break;
            case 2: Process3 = value; break;
            case 3: Process4 = value; break;
            case 4: Process5 = value; break;
        }
    }

    public int GetStepIndexFor(string moduleName)
    {
        var procs = new[] { Process1, Process2, Process3, Process4, Process5 };
        for (int i = 0; i < procs.Length; i++)
            if (procs[i] == moduleName) return i;
        return -1;
    }

    public bool IsAllComplete()
    {
        var procs = GetProcesses();
        if (!procs.Any()) return false;
        foreach (var p in procs)
        {
            if (p == "Other") continue;
            var entry = ModuleEntries.FirstOrDefault(e => e.ModuleName == p);
            if (entry == null || !entry.IsFinished) return false;
        }
        return true;
    }
}

public class ModuleEntry
{
    [Key]
    public int Id { get; set; }

    public int JobId { get; set; }
    public Job Job { get; set; } = null!;

    [Required]
    public string ModuleName { get; set; } = string.Empty;

    public string? MachineNumber { get; set; }
    public bool IsFinished { get; set; }
    public int? FinishedQty { get; set; }
    public string? FinishedDate { get; set; }
}

public class PlannerHistory
{
    [Key]
    public int Id { get; set; }

    public DateTime Timestamp { get; set; } = DateTime.Now;
    public string Serial { get; set; } = string.Empty;
    public string Customer { get; set; } = string.Empty;
    public string Step { get; set; } = string.Empty;
    public string OldValue { get; set; } = "(empty)";
    public string NewValue { get; set; } = "(empty)";
    public string ChangedBy { get; set; } = string.Empty;

    public string TimestampFormatted => Timestamp.ToString("dd/MM/yyyy HH:mm:ss");
}

public class CapacityLedgerEntry
{
    [Key]
    public int Id { get; set; }
    public string EntryDate { get; set; } = string.Empty;   // dd/MM/yyyy
    public string Serial { get; set; } = string.Empty;
    public string Customer { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public string ModuleName { get; set; } = string.Empty;  // VMC, Milling, Lathe, Shaper
    public string? MachineNumber { get; set; }
    public int QtyProduced { get; set; }
    public string? Notes { get; set; }
    public string EnteredBy { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.Now;
}

public class AppUser
{
    [Key]
    public int Id { get; set; }

    [Required]
    public string Username { get; set; } = string.Empty;

    [Required]
    public string Password { get; set; } = string.Empty;

    [Required]
    public string FullName { get; set; } = string.Empty;

    [Required]
    public string Role { get; set; } = string.Empty; // Manager, Planner, Technician
}

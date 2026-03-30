namespace MachinePro.Models;

public class DashboardViewModel
{
    public int TotalParts { get; set; }
    public int TotalJobs { get; set; }
    public int Unassigned { get; set; }
    public int Pending { get; set; }
    public Dictionary<string, ModuleStats> ModuleBreakdown { get; set; } = new();
    // date string (dd/MM) → count
    public List<(string Date, int Count)> InwardsLast5Days { get; set; } = new();
    // module → list of (date, count)
    public Dictionary<string, List<(string Date, int Count)>> ModuleFinishedLast3Days { get; set; } = new();
}

public class ModuleStats
{
    public int Total { get; set; }
    public int Finished { get; set; }
    public int Pending { get; set; }
    public int Unassigned { get; set; }
}

public class ModuleViewModel
{
    public string ModuleName { get; set; } = string.Empty;
    public int MachineCount { get; set; }
    public List<ModuleJobRow> Jobs { get; set; } = new();
}

public class ModuleJobRow
{
    public int JobId { get; set; }
    public string Serial { get; set; } = string.Empty;
    public string Customer { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public string Drawing { get; set; } = string.Empty;
    public string? ItemCode { get; set; }
    public string? DrawingDescription { get; set; }
    public int Qty { get; set; }
    public int StepIndex { get; set; }
    public string? MachineNumber { get; set; }
    public bool IsFinished { get; set; }
    public int? FinishedQty { get; set; }
    public int RemainingQty { get; set; }
    public int AvailableQty { get; set; }
    public string? FinishedDate { get; set; }
    public int? ModuleEntryId { get; set; }
}

public class PlannerViewModel
{
    public List<Job> Jobs { get; set; } = new();
}

public class TrackerViewModel
{
    public List<TrackerRow> Rows { get; set; } = new();
    public string? Search { get; set; }
}

public class TrackerRow
{
    public Job Job { get; set; } = null!;
    public bool IsComplete { get; set; }
    public List<ProcessChip> Chips { get; set; } = new();
}

public class ProcessChip
{
    public string Name { get; set; } = string.Empty;
    public bool IsDone { get; set; }
    public int RemainingQty { get; set; }
    public int TotalQty { get; set; }
    public int FinishedQty { get; set; }
}

public class FinishedViewModel
{
    public List<Job> Jobs { get; set; } = new();
    public string? Search { get; set; }
}

public class HistoryViewModel
{
    public List<PlannerHistory> Entries { get; set; } = new();
    public string? Search { get; set; }
}

// ─── REQUEST FORM (now uses dropdowns) ───
public class RequestsPageViewModel
{
    public List<Job> Jobs { get; set; } = new();
    public List<CustomerMaster> Customers { get; set; } = new();
    public List<ModelMaster> Models { get; set; } = new();
    public string NextSerial { get; set; } = string.Empty;
    public string Today { get; set; } = string.Empty;
}

public class SubmitRequestModel
{
    public string Customer { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public string Drawing { get; set; } = string.Empty;
    public string? DrawingDescription { get; set; }
    public int Qty { get; set; }
}

public class SavePlannerModel
{
    public int JobId { get; set; }
    public string? Process1 { get; set; }
    public string? Process2 { get; set; }
    public string? Process3 { get; set; }
    public string? Process4 { get; set; }
    public string? Process5 { get; set; }
    public int? Priority { get; set; }
    public string Role { get; set; } = "planner";
    public string? ReturnTo { get; set; }
}

public class SetMachineModel
{
    public int ModuleEntryId { get; set; }
    public string MachineNumber { get; set; } = string.Empty;
}

public class FinishJobModel
{
    public int JobId { get; set; }
    public string ModuleName { get; set; } = string.Empty;
    public int StepIndex { get; set; }
    public int FinishedQty { get; set; }
}

// ─── MASTER DATA MANAGEMENT ───
public class MasterDataViewModel
{
    public List<CustomerMaster> Customers { get; set; } = new();
    public List<ModelMaster> Models { get; set; } = new();
}

public class AddCustomerModel
{
    public string CustomerName { get; set; } = string.Empty;
    public string? City { get; set; }
    public string? State { get; set; }
    public string Country { get; set; } = "India";
}

public class AddModelModel
{
    public string ModelName { get; set; } = string.Empty;
}

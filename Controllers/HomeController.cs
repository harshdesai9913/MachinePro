using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using MachinePro.Data;
using MachinePro.Models;

namespace MachinePro.Controllers;

[Authorize]
public class HomeController : Controller
{
    private readonly AppDbContext _db;

    public HomeController(AppDbContext db) => _db = db;

    // ─── DASHBOARD ───
    public async Task<IActionResult> Index()
    {
        var jobs = await _db.Jobs.Include(j => j.ModuleEntries).Where(j => !j.IsCompleted).ToListAsync();
        var moduleTypes = new[] { "VMC", "Milling", "Lathe", "Shaper" };

        int unassigned = 0, pending = 0;
        var moduleBreakdown = moduleTypes.ToDictionary(m => m, _ => new ModuleStats());

        foreach (var job in jobs)
        {
            foreach (var proc in job.GetProcesses())
            {
                var entry = job.ModuleEntries.FirstOrDefault(e => e.ModuleName == proc);
                if (entry == null || string.IsNullOrEmpty(entry.MachineNumber)) unassigned++;
                if (entry == null || !entry.IsFinished) pending++;

                if (moduleBreakdown.ContainsKey(proc))
                {
                    moduleBreakdown[proc].Total++;
                    if (entry == null || string.IsNullOrEmpty(entry.MachineNumber)) moduleBreakdown[proc].Unassigned++;
                    if (entry != null && entry.IsFinished) moduleBreakdown[proc].Finished++;
                    else moduleBreakdown[proc].Pending++;
                }
            }
        }

        // Inwards last 5 days
        var today = DateTime.Today;
        var allJobs = await _db.Jobs.ToListAsync();
        var inwardsLast5 = new List<(string Date, int Count)>();
        for (int d = 4; d >= 0; d--)
        {
            var day = today.AddDays(-d);
            var dateStr = day.ToString("dd/MM/yyyy");
            var count = allJobs.Count(j => j.InwardDate == dateStr);
            inwardsLast5.Add((day.ToString("dd/MM"), count));
        }

        // Module finished last 3 days
        var allEntries = await _db.ModuleEntries.ToListAsync();
        var moduleFinished = moduleTypes.ToDictionary(m => m, m =>
        {
            var dayList = new List<(string Date, int Count)>();
            for (int d = 2; d >= 0; d--)
            {
                var day = today.AddDays(-d);
                var dateStr = day.ToString("dd/MM/yyyy");
                var count = allEntries.Count(e => e.ModuleName == m && e.IsFinished && e.FinishedDate == dateStr);
                dayList.Add((day.ToString("dd/MM"), count));
            }
            return dayList;
        });

        var vm = new DashboardViewModel
        {
            TotalParts = jobs.Sum(j => j.Qty),
            TotalJobs = jobs.Count,
            Unassigned = unassigned,
            Pending = pending,
            ModuleBreakdown = moduleBreakdown,
            InwardsLast5Days = inwardsLast5,
            ModuleFinishedLast3Days = moduleFinished
        };

        ViewBag.CurrentPage = "dashboard";
        return View(vm);
    }

    // ─── SUBMIT REQUEST ───
    public async Task<IActionResult> Requests()
    {
        var jobs = await _db.Jobs.OrderBy(j => j.Id).ToListAsync();
        var customers = await _db.CustomerMasters.OrderBy(c => c.CustomerName).ToListAsync();
        var models = await _db.ModelMasters.OrderBy(m => m.ModelName).ToListAsync();

        var nextNum = 1;
        while (jobs.Any(j => j.Serial == $"SN-{nextNum:D3}")) nextNum++;

        var vm = new RequestsPageViewModel
        {
            Jobs = jobs,
            Customers = customers,
            Models = models,
            NextSerial = $"SN-{nextNum:D3}",
            Today = DateTime.Now.ToString("dd/MM/yyyy")
        };

        ViewBag.CurrentPage = "requests";
        return View(vm);
    }

    [HttpPost]
    public async Task<IActionResult> SubmitRequest(string? Customer, string? Model, string? Drawing, string? ItemCode, string? DrawingDescription, int Qty)
    {
        // Using direct parameters instead of model binding to avoid validation conflicts
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(Customer)) errors.Add("Customer");
        if (string.IsNullOrWhiteSpace(Model)) errors.Add("Model");
        if (string.IsNullOrWhiteSpace(Drawing)) errors.Add("Drawing");
        if (Qty <= 0) errors.Add("Quantity");

        if (errors.Any())
        {
            TempData["Error"] = $"Please fill in: {string.Join(", ", errors)}";
            return RedirectToAction("Requests");
        }

        var count = await _db.Jobs.CountAsync();
        var nextNum = 1;
        while (await _db.Jobs.AnyAsync(j => j.Serial == $"SN-{nextNum:D3}")) nextNum++;

        var job = new Job
        {
            Serial = $"SN-{nextNum:D3}",
            Customer = Customer!.Trim(),
            Model = Model!.Trim(),
            Drawing = Drawing!.Trim(),
            ItemCode = ItemCode?.Trim(),
            DrawingDescription = DrawingDescription?.Trim(),
            Qty = Qty,
            InwardDate = DateTime.Now.ToString("dd/MM/yyyy")
        };

        _db.Jobs.Add(job);
        await _db.SaveChangesAsync();
        return RedirectToAction("Requests");
    }

    [HttpPost]
    public async Task<IActionResult> ImportCsv(IFormFile? csvFile)
    {
        if (csvFile == null || csvFile.Length == 0)
        {
            TempData["Error"] = "Please select a CSV file.";
            return RedirectToAction("Requests");
        }

        var lines = new List<string>();
        using (var reader = new System.IO.StreamReader(csvFile.OpenReadStream()))
        {
            while (!reader.EndOfStream)
            {
                var line = await reader.ReadLineAsync();
                if (line != null) lines.Add(line);
            }
        }

        if (lines.Count < 2)
        {
            TempData["Error"] = "CSV file must have a header row and at least one data row.";
            return RedirectToAction("Requests");
        }

        // Parse header row to find column indices
        var headers = ParseCsvRow(lines[0]);
        int idxCustomer = -1, idxModel = -1, idxDrawing = -1, idxItemCode = -1, idxDesc = -1, idxQty = -1;
        for (int i = 0; i < headers.Count; i++)
        {
            var h = headers[i].ToLowerInvariant().Trim();
            if (h is "customer" or "customer name") idxCustomer = i;
            else if (h is "model" or "machine model" or "customer model") idxModel = i;
            else if (h is "drawing" or "dwg no" or "dwg no." or "drawing no" or "drawing number" or "dwg") idxDrawing = i;
            else if (h is "item code" or "itemcode" or "item_code" or "part no" or "part number") idxItemCode = i;
            else if (h is "description" or "drawing description" or "desc") idxDesc = i;
            else if (h is "qty" or "quantity") idxQty = i;
        }

        if (idxCustomer < 0 || idxModel < 0 || idxDrawing < 0 || idxQty < 0)
        {
            TempData["Error"] = "CSV must have columns: Customer Name, Machine Model, DWG No., Quantity (and optionally Description).";
            return RedirectToAction("Requests");
        }

        int imported = 0, skipped = 0;
        var today = DateTime.Now.ToString("dd/MM/yyyy");

        for (int r = 1; r < lines.Count; r++)
        {
            if (string.IsNullOrWhiteSpace(lines[r])) continue;
            var cols = ParseCsvRow(lines[r]);

            var customer = idxCustomer < cols.Count ? cols[idxCustomer].Trim() : "";
            var model    = idxModel    < cols.Count ? cols[idxModel].Trim()    : "";
            var drawing   = idxDrawing   < cols.Count ? cols[idxDrawing].Trim()   : "";
            var itemCode  = idxItemCode >= 0 && idxItemCode < cols.Count ? cols[idxItemCode].Trim() : null;
            var desc      = idxDesc >= 0 && idxDesc < cols.Count ? cols[idxDesc].Trim() : null;
            var qtyStr   = idxQty      < cols.Count ? cols[idxQty].Trim()      : "";

            if (string.IsNullOrEmpty(customer) || string.IsNullOrEmpty(model) ||
                string.IsNullOrEmpty(drawing)  || !int.TryParse(qtyStr, out int qty) || qty <= 0)
            {
                skipped++;
                continue;
            }

            var nextNum = 1;
            while (await _db.Jobs.AnyAsync(j => j.Serial == $"SN-{nextNum:D3}")) nextNum++;

            _db.Jobs.Add(new Job
            {
                Serial = $"SN-{nextNum:D3}",
                Customer = customer,
                Model = model,
                Drawing = drawing,
                ItemCode = string.IsNullOrEmpty(itemCode) ? null : itemCode,
                DrawingDescription = string.IsNullOrEmpty(desc) ? null : desc,
                Qty = qty,
                InwardDate = today
            });
            await _db.SaveChangesAsync();
            imported++;
        }

        TempData["Success"] = skipped > 0
            ? $"Imported {imported} job(s). {skipped} row(s) skipped due to missing/invalid data."
            : $"Successfully imported {imported} job(s).";
        return RedirectToAction("Requests");
    }

    private static List<string> ParseCsvRow(string line)
    {
        var result = new List<string>();
        bool inQuotes = false;
        var current = new System.Text.StringBuilder();
        foreach (char c in line)
        {
            if (c == '"') { inQuotes = !inQuotes; }
            else if (c == ',' && !inQuotes) { result.Add(current.ToString()); current.Clear(); }
            else { current.Append(c); }
        }
        result.Add(current.ToString());
        return result;
    }

    // ─── PLANNER ───
    public async Task<IActionResult> Planner(string? filterCustomer, string? filterModel)
    {
        var query = _db.Jobs.Include(j => j.ModuleEntries).Where(j => !j.IsCompleted);
        if (!string.IsNullOrEmpty(filterCustomer)) query = query.Where(j => j.Customer == filterCustomer);
        if (!string.IsNullOrEmpty(filterModel)) query = query.Where(j => j.Model == filterModel);

        var jobs = await query
            .OrderBy(j => j.Priority == null ? 1 : 0)
            .ThenBy(j => j.Priority)
            .ThenBy(j => j.Id)
            .ToListAsync();

        ViewBag.CurrentPage = "planner";
        ViewBag.FilterCustomer = filterCustomer;
        ViewBag.FilterModel = filterModel;
        await LoadFilterLists();
        return View(new PlannerViewModel { Jobs = jobs });
    }

    [HttpPost]
    public async Task<IActionResult> SavePlanner(SavePlannerModel model)
    {
        var job = await _db.Jobs.Include(j => j.ModuleEntries).FirstOrDefaultAsync(j => j.Id == model.JobId);
        if (job == null) return RedirectToAction("Planner");

        var oldProcs = new[] { job.Process1, job.Process2, job.Process3, job.Process4, job.Process5 };
        var newProcs = new[] { model.Process1, model.Process2, model.Process3, model.Process4, model.Process5 };

        // Log only CHANGES (not initial assignments where old was empty)
        for (int i = 0; i < 5; i++)
        {
            var oldVal = string.IsNullOrEmpty(oldProcs[i]) ? null : oldProcs[i];
            var newVal = string.IsNullOrEmpty(newProcs[i]) ? null : newProcs[i];
            if (oldVal != newVal && oldVal != null) // only log if old value existed (skip initial assignment)
            {
                _db.PlannerHistories.Add(new PlannerHistory
                {
                    Timestamp = DateTime.Now,
                    Serial = job.Serial,
                    Customer = job.Customer,
                    Step = $"Process {i + 1}",
                    OldValue = oldVal,
                    NewValue = newVal ?? "(removed)",
                    ChangedBy = User.FindFirst("FullName")?.Value ?? User.Identity?.Name ?? "Unknown"
                });
            }
        }

        // Log priority changes — only when both old and new are actual priorities
        var oldPriority = job.Priority;
        var newPriority = (model.Priority.HasValue && model.Priority > 0 && model.Priority <= 10) ? model.Priority : null;
        if (oldPriority != newPriority && oldPriority.HasValue && newPriority.HasValue)
        {
            _db.PlannerHistories.Add(new PlannerHistory
            {
                Timestamp = DateTime.Now,
                Serial = job.Serial,
                Customer = job.Customer,
                Step = "Priority",
                OldValue = oldPriority.ToString()!,
                NewValue = newPriority.ToString()!,
                ChangedBy = User.FindFirst("FullName")?.Value ?? User.Identity?.Name ?? "Unknown"
            });
        }

        job.Process1 = string.IsNullOrEmpty(model.Process1) ? null : model.Process1;
        job.Process2 = string.IsNullOrEmpty(model.Process2) ? null : model.Process2;
        job.Process3 = string.IsNullOrEmpty(model.Process3) ? null : model.Process3;
        job.Process4 = string.IsNullOrEmpty(model.Process4) ? null : model.Process4;
        job.Process5 = string.IsNullOrEmpty(model.Process5) ? null : model.Process5;

        // Priority: auto-shift — if priority X is taken, push X and above up by 1
        if (model.Priority.HasValue && model.Priority > 0 && model.Priority <= 10)
        {
            var assignedPriority = model.Priority.Value;
            
            // Get all other prioritized jobs sorted ascending
            var otherJobs = await _db.Jobs
                .Where(j => j.Id != model.JobId && !j.IsCompleted && j.Priority != null)
                .OrderBy(j => j.Priority)
                .ToListAsync();

            // Remove current job's old priority from the list context
            job.Priority = assignedPriority;

            // Shift all jobs at or above the new priority up by 1
            foreach (var oj in otherJobs)
            {
                if (oj.Priority >= assignedPriority)
                {
                    oj.Priority++;
                    if (oj.Priority > 10) oj.Priority = null; // drop off if exceeds 10
                }
            }
        }
        else
        {
            job.Priority = null;
        }

        var currentModules = job.GetProcesses().Where(p => p != "Other").ToList();
        foreach (var proc in currentModules)
        {
            if (!job.ModuleEntries.Any(e => e.ModuleName == proc))
                job.ModuleEntries.Add(new ModuleEntry { ModuleName = proc });
        }

        var toRemove = job.ModuleEntries.Where(e => !currentModules.Contains(e.ModuleName)).ToList();
        _db.ModuleEntries.RemoveRange(toRemove);

        await _db.SaveChangesAsync();
        return RedirectToAction("Planner");
    }

    // ─── PRODUCTION TRACKER ───
    public async Task<IActionResult> Tracker(string? search, string? filterCustomer, string? filterModel)
    {
        var query = _db.Jobs.Include(j => j.ModuleEntries).Where(j => !j.IsCompleted);
        if (!string.IsNullOrEmpty(filterCustomer)) query = query.Where(j => j.Customer == filterCustomer);
        if (!string.IsNullOrEmpty(filterModel)) query = query.Where(j => j.Model == filterModel);

        var jobs = await query
            .OrderBy(j => j.Priority == null ? 1 : 0)
            .ThenBy(j => j.Priority)
            .ThenBy(j => j.Id)
            .ToListAsync();

        if (!string.IsNullOrEmpty(search))
        {
            var s = search.ToLower();
            jobs = jobs.Where(j =>
                j.Serial.ToLower().Contains(s) ||
                j.Customer.ToLower().Contains(s) ||
                j.Model.ToLower().Contains(s)
            ).ToList();
        }

        var vm = new TrackerViewModel
        {
            Search = search,
            Rows = jobs.Select(j =>
            {
                var procs = j.GetProcesses();
                var chips = procs.Select(p =>
                {
                    if (p == "Other") return new ProcessChip { Name = p, IsDone = true, TotalQty = j.Qty, FinishedQty = j.Qty, RemainingQty = 0 };
                    var entry = j.ModuleEntries.FirstOrDefault(e => e.ModuleName == p);
                    var finQty = entry?.FinishedQty ?? 0;
                    return new ProcessChip
                    {
                        Name = p,
                        IsDone = entry?.IsFinished ?? false,
                        TotalQty = j.Qty,
                        FinishedQty = finQty,
                        RemainingQty = j.Qty - finQty
                    };
                }).ToList();

                return new TrackerRow { Job = j, IsComplete = j.IsAllComplete(), Chips = chips };
            }).ToList()
        };

        ViewBag.CurrentPage = "tracker";
        ViewBag.FilterCustomer = filterCustomer;
        ViewBag.FilterModel = filterModel;
        await LoadFilterLists();
        return View(vm);
    }

    [Authorize(Roles = "Manager")]
    [HttpPost]
    public async Task<IActionResult> DeleteJob(int id)
    {
        var job = await _db.Jobs.Include(j => j.ModuleEntries).FirstOrDefaultAsync(j => j.Id == id);
        if (job != null)
        {
            _db.ModuleEntries.RemoveRange(job.ModuleEntries);
            _db.Jobs.Remove(job);
            await _db.SaveChangesAsync();

            // Cascade priorities
            await CascadePriorities();
        }
        return RedirectToAction("Tracker");
    }

    [Authorize(Roles = "Manager")]
    [HttpPost]
    public async Task<IActionResult> MoveToFinished(int id)
    {
        var job = await _db.Jobs.Include(j => j.ModuleEntries).FirstOrDefaultAsync(j => j.Id == id);
        if (job != null && job.IsAllComplete())
        {
            job.IsCompleted = true;
            job.CompletedDate = DateTime.Now.ToString("dd/MM/yyyy");
            job.Priority = null; // Remove from priority queue
            await _db.SaveChangesAsync();

            // Cascade priorities
            await CascadePriorities();
        }
        else
        {
            TempData["Error"] = "Cannot move to Finished. All process steps must be completed first.";
        }
        return RedirectToAction("Tracker");
    }

    // ─── FINISHED MODULE ───
    public async Task<IActionResult> Finished(string? search, string? filterCustomer, string? filterModel)
    {
        var query = _db.Jobs.Include(j => j.ModuleEntries).Where(j => j.IsCompleted);
        if (!string.IsNullOrEmpty(filterCustomer)) query = query.Where(j => j.Customer == filterCustomer);
        if (!string.IsNullOrEmpty(filterModel)) query = query.Where(j => j.Model == filterModel);

        var jobs = await query.OrderByDescending(j => j.CompletedDate).ToListAsync();

        if (!string.IsNullOrEmpty(search))
        {
            var s = search.ToLower();
            jobs = jobs.Where(j =>
                j.Serial.ToLower().Contains(s) ||
                j.Customer.ToLower().Contains(s) ||
                j.Model.ToLower().Contains(s)
            ).ToList();
        }

        ViewBag.CurrentPage = "finished";
        ViewBag.FilterCustomer = filterCustomer;
        ViewBag.FilterModel = filterModel;
        await LoadFilterLists();
        return View(new FinishedViewModel { Jobs = jobs, Search = search });
    }

    [Authorize(Roles = "Manager")]
    [HttpPost]
    public async Task<IActionResult> ReturnToProduction(int id)
    {
        var job = await _db.Jobs.Include(j => j.ModuleEntries).FirstOrDefaultAsync(j => j.Id == id);
        if (job == null) return RedirectToAction("Finished");

        job.IsCompleted = false;
        job.CompletedDate = null;

        // Reset all module entries — job resumes from last unfinished step
        // Keep finished steps as-is, only unflag the completion status on the Job
        // The sequential enforcement will ensure it continues from where it left off

        await _db.SaveChangesAsync();
        TempData["Success"] = $"Job {job.Serial} returned to Production Tracker. It will resume from the last unfinished step.";
        return RedirectToAction("Finished");
    }

    // ─── PRIORITY CASCADE ───
    private async Task CascadePriorities()
    {
        var prioritizedJobs = await _db.Jobs
            .Where(j => !j.IsCompleted && j.Priority != null)
            .OrderBy(j => j.Priority)
            .ToListAsync();

        for (int i = 0; i < prioritizedJobs.Count; i++)
        {
            prioritizedJobs[i].Priority = i + 1;
        }
        await _db.SaveChangesAsync();
    }

    // ─── FILTER LISTS ───
    private async Task LoadFilterLists()
    {
        ViewBag.Customers = await _db.CustomerMasters.OrderBy(c => c.CustomerName).ToListAsync();
        ViewBag.Models = await _db.ModelMasters.OrderBy(m => m.ModelName).ToListAsync();
    }

    [HttpGet]
    public async Task<IActionResult> ExportCsv()
    {
        var jobs = await _db.Jobs.ToListAsync();
        var csv = "Serial,Customer,Model,Drawing,Item Code,Description,Qty,Date,Process1,Process2,Process3,Process4,Process5\n";
        foreach (var j in jobs)
        {
            csv += $"{j.Serial},\"{j.Customer}\",\"{j.Model}\",\"{j.Drawing}\",\"{j.ItemCode}\",\"{j.DrawingDescription}\",{j.Qty},{j.InwardDate},{j.Process1},{j.Process2},{j.Process3},{j.Process4},{j.Process5}\n";
        }
        return File(System.Text.Encoding.UTF8.GetBytes(csv), "text/csv", "production_data.csv");
    }

    // ─── PLANNER HISTORY (Manager only) ───
    [Authorize(Roles = "Manager")]
    public async Task<IActionResult> History(string? search, string? filterCustomer, string? filterModel)
    {
        var query = _db.PlannerHistories
            .Where(h => h.OldValue != "(none)" && h.NewValue != "(none)")
            .OrderByDescending(h => h.Timestamp).AsQueryable();

        if (!string.IsNullOrEmpty(filterCustomer)) query = query.Where(h => h.Customer == filterCustomer);
        if (!string.IsNullOrEmpty(search))
        {
            var s = search.ToLower();
            query = query.Where(h =>
                h.Serial.ToLower().Contains(s) ||
                h.Customer.ToLower().Contains(s) ||
                h.Step.ToLower().Contains(s)
            );
        }

        var vm = new HistoryViewModel
        {
            Search = search,
            Entries = await query.ToListAsync()
        };

        ViewBag.CurrentPage = "history";
        ViewBag.FilterCustomer = filterCustomer;
        ViewBag.FilterModel = filterModel;
        await LoadFilterLists();
        return View(vm);
    }

    [Authorize(Roles = "Manager")]
    [HttpPost]
    public async Task<IActionResult> ClearHistory()
    {
        _db.PlannerHistories.RemoveRange(_db.PlannerHistories);
        await _db.SaveChangesAsync();
        return RedirectToAction("History");
    }

    // ─── MASTER DATA MANAGEMENT (Manager only) ───
    [Authorize(Roles = "Manager")]
    public async Task<IActionResult> MasterData()
    {
        var customers = await _db.CustomerMasters.OrderBy(c => c.CustomerName).ToListAsync();
        var models = await _db.ModelMasters.OrderBy(m => m.ModelName).ToListAsync();
        ViewBag.CurrentPage = "masterdata";
        return View(new MasterDataViewModel { Customers = customers, Models = models });
    }

    [HttpPost]
    public async Task<IActionResult> AddCustomer(AddCustomerModel model)
    {
        if (string.IsNullOrWhiteSpace(model.CustomerName))
        {
            TempData["Error"] = "Customer name is required.";
            return RedirectToAction("MasterData");
        }

        if (await _db.CustomerMasters.AnyAsync(c => c.CustomerName == model.CustomerName))
        {
            TempData["Error"] = "Customer already exists.";
            return RedirectToAction("MasterData");
        }

        _db.CustomerMasters.Add(new CustomerMaster
        {
            CustomerName = model.CustomerName,
            City = model.City?.Trim(),
            State = model.State?.Trim(),
            Country = string.IsNullOrWhiteSpace(model.Country) ? "India" : model.Country.Trim()
        });
        await _db.SaveChangesAsync();
        return RedirectToAction("MasterData");
    }

    [HttpPost]
    public async Task<IActionResult> AddModel(AddModelModel model)
    {
        if (string.IsNullOrWhiteSpace(model.ModelName))
        {
            TempData["Error"] = "Model name is required.";
            return RedirectToAction("MasterData");
        }

        if (await _db.ModelMasters.AnyAsync(m => m.ModelName == model.ModelName))
        {
            TempData["Error"] = "Model already exists.";
            return RedirectToAction("MasterData");
        }

        _db.ModelMasters.Add(new ModelMaster { ModelName = model.ModelName });
        await _db.SaveChangesAsync();
        return RedirectToAction("MasterData");
    }

    [HttpPost]
    public async Task<IActionResult> DeleteCustomer(int id)
    {
        var customer = await _db.CustomerMasters.FindAsync(id);
        if (customer != null)
        {
            _db.CustomerMasters.Remove(customer);
            await _db.SaveChangesAsync();
        }
        return RedirectToAction("MasterData");
    }

    [HttpPost]
    public async Task<IActionResult> DeleteModel(int id)
    {
        var model = await _db.ModelMasters.FindAsync(id);
        if (model != null)
        {
            _db.ModelMasters.Remove(model);
            await _db.SaveChangesAsync();
        }
        return RedirectToAction("MasterData");
    }

    [HttpPost]
    public async Task<IActionResult> UploadCustomerCsv(IFormFile csvFile)
    {
        if (csvFile == null || csvFile.Length == 0)
        {
            TempData["Error"] = "Please select a CSV file.";
            return RedirectToAction("MasterData");
        }

        using var reader = new StreamReader(csvFile.OpenReadStream());
        var lineNumber = 0;
        var added = 0;

        while (!reader.EndOfStream)
        {
            var line = await reader.ReadLineAsync();
            lineNumber++;
            if (lineNumber == 1) continue;

            if (string.IsNullOrWhiteSpace(line)) continue;

            var customerName = line.Split(',')[0].Trim().Trim('"');
            if (string.IsNullOrEmpty(customerName)) continue;

            if (!await _db.CustomerMasters.AnyAsync(c => c.CustomerName == customerName))
            {
                _db.CustomerMasters.Add(new CustomerMaster { CustomerName = customerName });
                added++;
            }
        }

        await _db.SaveChangesAsync();
        TempData["Success"] = $"Customer upload complete. {added} new customers added.";
        return RedirectToAction("MasterData");
    }

    [HttpPost]
    public async Task<IActionResult> UploadModelCsv(IFormFile csvFile)
    {
        if (csvFile == null || csvFile.Length == 0)
        {
            TempData["Error"] = "Please select a CSV file.";
            return RedirectToAction("MasterData");
        }

        using var reader = new StreamReader(csvFile.OpenReadStream());
        var lineNumber = 0;
        var added = 0;

        while (!reader.EndOfStream)
        {
            var line = await reader.ReadLineAsync();
            lineNumber++;
            if (lineNumber == 1) continue;

            if (string.IsNullOrWhiteSpace(line)) continue;

            var modelName = line.Split(',')[0].Trim().Trim('"');
            if (string.IsNullOrEmpty(modelName)) continue;

            if (!await _db.ModelMasters.AnyAsync(m => m.ModelName == modelName))
            {
                _db.ModelMasters.Add(new ModelMaster { ModelName = modelName });
                added++;
            }
        }

        await _db.SaveChangesAsync();
        TempData["Success"] = $"Model upload complete. {added} new models added.";
        return RedirectToAction("MasterData");
    }

    // ─── MACHINE MODULES ───
    public async Task<IActionResult> Module(string name, string? filterCustomer, string? filterModel)
    {
        var machineCount = name switch
        {
            "VMC" => 4, "Milling" => 3, "Lathe" => 4, "Shaper" => 1, _ => 1
        };

        var query = _db.Jobs.Include(j => j.ModuleEntries).Where(j => !j.IsCompleted);
        if (!string.IsNullOrEmpty(filterCustomer)) query = query.Where(j => j.Customer == filterCustomer);
        if (!string.IsNullOrEmpty(filterModel)) query = query.Where(j => j.Model == filterModel);

        var jobs = await query.ToListAsync();
        var moduleJobs = new List<ModuleJobRow>();

        foreach (var job in jobs)
        {
            var stepIndex = job.GetStepIndexFor(name);
            if (stepIndex < 0) continue;

            var entry = job.ModuleEntries.FirstOrDefault(e => e.ModuleName == name);
            var finQty = entry?.FinishedQty ?? 0;
            var remaining = job.Qty - finQty;

            // Available qty is capped by how much the previous step has completed
            var availableQty = remaining;
            if (stepIndex > 0)
            {
                var procs = job.GetProcesses();
                var prevProc = procs.ElementAtOrDefault(stepIndex - 1);
                if (prevProc != null && prevProc != "Other")
                {
                    var prevEntry = job.ModuleEntries.FirstOrDefault(e => e.ModuleName == prevProc);
                    var prevDone = prevEntry?.FinishedQty ?? 0;
                    availableQty = Math.Max(0, prevDone - finQty);
                }
            }

            moduleJobs.Add(new ModuleJobRow
            {
                JobId = job.Id,
                Serial = job.Serial,
                Customer = job.Customer,
                Model = job.Model,
                Drawing = job.Drawing,
                ItemCode = job.ItemCode,
                DrawingDescription = job.DrawingDescription,
                Qty = job.Qty,
                StepIndex = stepIndex,
                MachineNumber = entry?.MachineNumber,
                IsFinished = entry?.IsFinished ?? false,
                FinishedQty = entry?.FinishedQty,
                RemainingQty = remaining,
                AvailableQty = availableQty,
                FinishedDate = entry?.FinishedDate,
                ModuleEntryId = entry?.Id
            });
        }

        var vm = new ModuleViewModel
        {
            ModuleName = name,
            MachineCount = machineCount,
            Jobs = moduleJobs
        };

        ViewBag.CurrentPage = name.ToLower();
        ViewBag.FilterCustomer = filterCustomer;
        ViewBag.FilterModel = filterModel;
        await LoadFilterLists();
        return View("Module", vm);
    }

    [HttpPost]
    public async Task<IActionResult> SetMachine(int moduleEntryId, string machineNumber, string moduleName)
    {
        var entry = await _db.ModuleEntries.FindAsync(moduleEntryId);
        if (entry != null)
        {
            entry.MachineNumber = machineNumber;
            await _db.SaveChangesAsync();
        }
        return RedirectToAction("Module", new { name = moduleName });
    }

    [HttpPost]
    public async Task<IActionResult> RecordQty(int JobId, string ModuleName, int StepIndex, int RecordQty)
    {
        var job = await _db.Jobs.Include(j => j.ModuleEntries).FirstOrDefaultAsync(j => j.Id == JobId);
        if (job == null)
        {
            TempData["Error"] = "Job not found.";
            return RedirectToAction("Module", new { name = ModuleName });
        }

        var entry = job.ModuleEntries.FirstOrDefault(e => e.ModuleName == ModuleName);
        if (entry == null)
        {
            TempData["Error"] = "Module entry not found.";
            return RedirectToAction("Module", new { name = ModuleName });
        }

        if (entry.IsFinished)
        {
            TempData["Error"] = "This job is already finished.";
            return RedirectToAction("Module", new { name = ModuleName });
        }

        // CHECK: Machine number must be set
        if (string.IsNullOrEmpty(entry.MachineNumber))
        {
            TempData["Error"] = $"Please assign a {ModuleName} machine number first.";
            return RedirectToAction("Module", new { name = ModuleName });
        }

        if (RecordQty <= 0)
        {
            TempData["Error"] = "Please enter a quantity greater than 0.";
            return RedirectToAction("Module", new { name = ModuleName });
        }

        // CHECK: Sequential enforcement (partial qty allowed)
        var procs = job.GetProcesses();
        foreach (var p in procs)
        {
            var actualIdx = job.GetStepIndexFor(p);
            if (actualIdx < StepIndex && p != "Other")
            {
                var prevEntry = job.ModuleEntries.FirstOrDefault(e => e.ModuleName == p);
                var prevDone = prevEntry?.FinishedQty ?? 0;
                var thisDone = entry.FinishedQty ?? 0;
                if (prevDone == 0)
                {
                    TempData["Error"] = $"Cannot record qty for Step {StepIndex + 1} ({ModuleName}) before Step {actualIdx + 1} ({p}) has recorded any quantity.";
                    return RedirectToAction("Module", new { name = ModuleName });
                }
                if (thisDone + RecordQty > prevDone)
                {
                    TempData["Error"] = $"Cannot record {RecordQty} pcs for {ModuleName}. Step {actualIdx + 1} ({p}) has only completed {prevDone} pcs so far. Maximum you can record: {prevDone - thisDone}.";
                    return RedirectToAction("Module", new { name = ModuleName });
                }
            }
        }

        var currentDone = entry.FinishedQty ?? 0;
        var newTotal = currentDone + RecordQty;

        if (newTotal > job.Qty)
        {
            TempData["Error"] = $"Cannot record {RecordQty} pcs. Already done: {currentDone}, Job Qty: {job.Qty}. Maximum you can record: {job.Qty - currentDone}.";
            return RedirectToAction("Module", new { name = ModuleName });
        }

        entry.FinishedQty = newTotal;
        entry.FinishedDate = DateTime.Now.ToString("dd/MM/yyyy");

        // Auto-finish when full qty is reached
        if (newTotal == job.Qty)
        {
            entry.IsFinished = true;

            // If all steps complete, cascade priorities
            await _db.SaveChangesAsync();
            if (job.IsAllComplete())
            {
                await CascadePriorities();
            }
        }
        else
        {
            await _db.SaveChangesAsync();
        }

        return RedirectToAction("Module", new { name = ModuleName });
    }
}

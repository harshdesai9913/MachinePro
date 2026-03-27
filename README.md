# MachinePro — Machine Shop Management (.NET 8)

A complete ASP.NET Core MVC application for managing machine shop operations,
with SQLite database persistence via Entity Framework Core.

## Requirements

- .NET 8 SDK (https://dotnet.microsoft.com/download/dotnet/8.0)

## Quick Start

```bash
cd MachinePro
dotnet restore
dotnet run
```

Then open **https://localhost:5001** or **http://localhost:5000** in your browser.

## Project Structure

```
MachinePro/
├── Program.cs                    # App entry point & service config
├── MachinePro.csproj             # Project file with dependencies
├── Controllers/
│   └── HomeController.cs         # All routes and business logic
├── Data/
│   └── AppDbContext.cs           # EF Core context + seed data
├── Models/
│   ├── Job.cs                    # Job, ModuleEntry, PlannerHistory entities
│   └── ViewModels.cs             # View models for each page
├── Views/
│   ├── _ViewImports.cshtml
│   ├── _ViewStart.cshtml
│   ├── Shared/
│   │   └── _Layout.cshtml        # Sidebar layout + grey theme
│   └── Home/
│       ├── Index.cshtml           # Dashboard
│       ├── Requests.cshtml        # Submit Request
│       ├── Planner.cshtml         # Process Assignment
│       ├── Tracker.cshtml         # Production Tracker
│       ├── History.cshtml         # Planner History
│       └── Module.cshtml          # VMC / Milling / Lathe / Shaper
└── wwwroot/
    └── css/
        └── site.css               # Full grey theme stylesheet
```

## Features

| Feature                  | Description                                                        |
|--------------------------|--------------------------------------------------------------------|
| **Dashboard**            | Real-time stats: total parts, unassigned machines, pending jobs     |
| **Submit Request**       | Technicians create part requests with auto serial & today's date   |
| **Planner**              | Assign up to 5 processes per job (VMC, Milling, Lathe, Shaper)    |
| **Production Tracker**   | Red/green chips per process, green row when all complete, CSV export|
| **Planner History**      | Full audit log of every process change with timestamp & role       |
| **Machine Modules**      | Per-module views with machine # assignment, finished qty & date    |
| **Sequential Enforcement** | Cannot finish Step N+1 before Step N is complete                |
| **Machine # Required**   | Cannot finish a job without assigning a machine number first       |
| **Finished Qty Required** | Must enter quantity before marking a job complete                 |

## Database

Data is stored in **machinepro.db** (SQLite) created automatically on first run.
Demo data is seeded automatically. The database persists across application restarts.

## Differences from HTML Version

| HTML Version                  | .NET Version                              |
|-------------------------------|-------------------------------------------|
| localStorage (browser-only)   | SQLite database (server-side, persistent) |
| Single HTML file              | Full MVC architecture                     |
| Client-side JavaScript logic  | Server-side C# with Razor views           |
| No authentication             | Ready for Identity/auth integration       |
| Browser-only data             | Data survives browser clears              |

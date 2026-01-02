# FlexiTrack

A time tracking and task management application built with ASP.NET Core, React, and WPF.

## Tech Stack

- **Backend:** ASP.NET Core 10 with minimal APIs
- **Database:** SQL Server (LocalDB/SQL Express) with Entity Framework Core
- **Frontend:** React 18 with React Router v5 (loaded via CDN, no build step)
- **Desktop:** WPF (.NET 10) with system tray integration
- **Authentication:** JWT Bearer tokens with ASP.NET Core Identity

## Project Structure

```
FlexiTrack/
├── FlexiTrack.Api/           # Main API project
│   ├── Data/                 # Database context and entities
│   │   ├── Entities/         # Domain models (ApplicationUser, Company, TaskItem, TaskLog)
│   │   ├── AppDbContext.cs   # EF Core DbContext
│   │   └── DbSeeder.cs       # Database seeding
│   ├── Features/             # Vertical slice architecture
│   │   ├── Companies/        # Company management endpoints
│   │   ├── Tasks/            # Task logging endpoints
│   │   ├── TaskItems/        # Task items CRUD endpoints
│   │   ├── Users/            # User authentication endpoints
│   │   └── Health/           # Health check endpoints
│   ├── Migrations/           # EF Core database migrations
│   ├── wwwroot/              # Static files
│   │   ├── index.html        # Main HTML with embedded CSS
│   │   └── js/app.bundle.jsx # React application (JSX, compiled by Babel in browser)
│   └── Program.cs            # Application entry point
├── FlexiTrack.Desktop/       # WPF desktop application
│   ├── Infrastructure/       # Converters, messages
│   ├── Models/               # DTOs matching API contracts
│   ├── Services/             # API client, auth, token storage
│   ├── ViewModels/           # MVVM view models
│   ├── Views/                # XAML views and windows
│   └── Resources/            # Styles and resources
├── FlexiTrack.Mediator/      # Custom mediator pattern implementation
└── FlexiTrack.slnx           # Solution file
```

## Architecture Patterns

- **CQRS with Mediator:** Each feature uses Command/Query record types with Handlers
- **Vertical Slice:** Features are organized by domain area, not by technical concern
- **Code-First Database:** EF Core with migrations

## Key Conventions

- Entity classes use `required` modifier for required properties
- Soft deletes via `DateTime? Removed` property
- PascalCase for all C# identifiers
- DbSet properties use expression body syntax: `public DbSet<T> Items => Set<T>();`

## Database

- Connection string in `appsettings.json` points to `localhost\SQLEXPRESS`
- Uses EF Core migrations: `dotnet ef migrations add <Name>` and `dotnet ef database update`
- To reset database: drop it in SQL Server, run migrations

## Frontend

- Single `app.bundle.jsx` file containing all React components
- Uses Babel in-browser compilation (no webpack/build step)
- Cache buster query param on script tag for updates: `?v=YYYYMMDD`

## Desktop Application

The WPF desktop app runs in the Windows system tray and connects to the API.

### Tech Stack
- WPF (.NET 10.0-windows)
- CommunityToolkit.Mvvm (MVVM with source generators)
- Hardcodet.NotifyIcon.Wpf (system tray)
- Windows DPAPI for secure token storage

### Features
- System tray integration (left-click opens popup, right-click for menu)
- Quick task logging with client autocomplete
- Auto-set start time when date changes (last task's end time or user default)
- Entries list for selected date with client filter and total duration
- Today's summary with date/client filters
- Export with month dropdown (last 12 months) and consolidate option
- Export ZIP with all data grouped by month
- Edit and delete task logs
- Auto-restore session from saved token
- Shows Log screen automatically after login

### Running the Desktop App

```bash
cd FlexiTrack.Desktop
dotnet run
```

Requires the API to be running at http://localhost:5265

### Building the MSIX Installer

The desktop app can be packaged as an MSIX installer for distribution.

**Prerequisites:**
- Windows SDK Build Tools (automatically installed via NuGet)

**Build the MSIX package:**
```powershell
cd FlexiTrack.Desktop
.\build-msix.ps1                    # Build and sign with test certificate
.\build-msix.ps1 -SkipSign          # Build without signing
.\build-msix.ps1 -Version "1.2.0.0" # Specify version
```

Output: `AppPackages\FlexiTrack.Desktop_<version>.msix`

**Install the MSIX package (requires Administrator):**
```powershell
# Option 1: Use the installer script (recommended)
.\install-msix.ps1

# Option 2: Manual installation
certutil -addstore Root AppPackages\FlexiTrack.cer
certutil -addstore TrustedPeople AppPackages\FlexiTrack.cer
Add-AppxPackage -Path AppPackages\FlexiTrack.Desktop_1.0.0.0.msix
```

**Helper scripts:**
- `build-msix.ps1` - Builds the MSIX package
- `install-msix.ps1` - Installs certificate and MSIX (requires admin)
- `install-cert.ps1` - Exports and installs the certificate
- `generate-icons.ps1` - Regenerates placeholder icons

**For production release:**
1. Replace placeholder icons in `Assets/` with branded PNGs
2. Sign with a trusted code signing certificate

### Error Logging
Logs are written to: `%LocalAppData%\FlexiTrack\error.log`

## Running the Application

```bash
cd FlexiTrack.Api
dotnet run
```

App runs at http://localhost:5265

Default admin: `admin@flexitrack.com` / `Admin123!`

## Features

### Task Logging
- Log tasks with date, start time, end time, description, and optional client
- Edit and delete task logs
- Client autocomplete based on previously used clients
- Time overlap validation (prevents overlapping entries)
- Future date validation (prevents logging future dates)
- Auto-set start time based on last task's end time or user default

### Filtering & Export
- Date filters: Today, This Week, Last Week, Last Month, This Month
- Client filter: All Clients, No Client, or specific client
- Month dropdown for export (last 12 months, current month default)
- CSV export with two formats:
  - **Detail**: Date, Start Time, End Time, Duration, Client, Description
  - **Consolidated**: Date (dd MMM yy), Hours, Tasks (comma-separated)
- Filename format: `ClientName-YYYYMMDD.csv` (consolidated) or `ClientName-YYYYMMDD-detail.csv`
- Desktop app: Export ZIP with all data grouped by month

### Dashboard Charts
- Visual bar chart showing hours worked
- Updates based on date and client filters
- Chart views: daily for weeks/months
- Horizontal goal line when weekly target is set
- Current day/week highlighted in blue

### User Settings
- Weekly hours target (displayed as goal line on chart)
- Default start time (used when no previous task exists)
- Settings saved without page reload

## API Endpoints

### Authentication
- `POST /api/users/register` - Register new user
- `POST /api/users/login` - Login
- `POST /api/users/forgot-password` - Request password reset
- `GET /api/users/profile` - Get user profile

### User Settings
- `PUT /api/users/settings` - Update user settings (weekly target, default start time)

### Task Logs
- `GET /api/tasks/logs` - Get user's task logs
- `POST /api/tasks` - Create task log
- `PUT /api/tasks/{id}` - Update task log
- `DELETE /api/tasks/{id}` - Delete task log
- `GET /api/tasks/clients` - Get unique client names

### Admin
- `GET/POST/PUT/DELETE /api/companies` - Company management
- `GET /api/users` - List all users (system admin)
- `PUT /api/users/{id}/system-admin` - Set system admin status

## Git Workflow

- Main development branch: `claude/development`
- Remote: https://github.com/tonynorcross/FlexiTrack.git

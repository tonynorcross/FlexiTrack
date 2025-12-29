# FlexiTrack

A time tracking and task management application built with ASP.NET Core and React.

## Tech Stack

- **Backend:** ASP.NET Core 10 with minimal APIs
- **Database:** SQL Server (LocalDB/SQL Express) with Entity Framework Core
- **Frontend:** React 18 with React Router v5 (loaded via CDN, no build step)
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
│   ├── wwwroot/              # Static files
│   │   ├── index.html        # Main HTML with embedded CSS
│   │   └── js/app.bundle.jsx # React application (JSX, compiled by Babel in browser)
│   └── Program.cs            # Application entry point
├── FlexiTrack.Mediator/      # Custom mediator pattern implementation
└── FlexiTrack.slnx           # Solution file
```

## Architecture Patterns

- **CQRS with Mediator:** Each feature uses Command/Query record types with Handlers
- **Vertical Slice:** Features are organized by domain area, not by technical concern
- **Code-First Database:** EF Core with `EnsureCreatedAsync()` (no migrations)

## Key Conventions

- Entity classes use `required` modifier for required properties
- Soft deletes via `DateTime? Removed` property
- PascalCase for all C# identifiers
- DbSet properties use expression body syntax: `public DbSet<T> Items => Set<T>();`

## Database

- Connection string in `appsettings.json` points to `localhost\SQLEXPRESS`
- Database is auto-created on startup via `EnsureCreatedAsync()`
- To reset database: drop it in SQL Server, restart the app

## Frontend

- Single `app.bundle.jsx` file containing all React components
- Uses Babel in-browser compilation (no webpack/build step)
- Cache buster query param on script tag for updates: `?v=YYYYMMDD`

## Running the Application

```bash
cd FlexiTrack.Api
dotnet run
```

App runs at http://localhost:5265

Default admin: `admin@flexitrack.com` / `Admin123!`

## API Endpoints

- `POST /api/users/login` - Login
- `POST /api/users/register` - Register
- `GET /api/tasks/logs` - Get user's task logs
- `POST /api/tasks` - Create task log
- `GET/POST/PUT/DELETE /api/taskitems` - Task items CRUD
- `GET/POST/PUT/DELETE /api/companies` - Company management (admin)

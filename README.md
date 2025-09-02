# SNRT Employee Desktop App (WPF .NET 8)

Internal Windows desktop app for SNRT employees with authentication, per-user draggable titles, RSS feed, user activity logging, and admin dashboard.

## Prerequisites
- .NET SDK 8.x installed
- Windows 10/11

Optional (for DB browsing): DB Browser for SQLite

## 1) First-time setup (Step-by-step)
```powershell
# 1. Go to project root
cd "C:\Users\Mohamed\Desktop\projet aya"

# 2. Restore dependencies
dotnet restore

# 3. Build solution (Release)
dotnet build SNRT.sln -c Release

# 4. (Optional) Ensure EF CLI is available for manual migrations
dotnet tool install --global dotnet-ef --version 9.*
$env:PATH += ";$env:USERPROFILE\.dotnet\tools"

# 5. Apply database migrations (automatically done on app start, but you can run manually)
dotnet ef database update `
  -p SNRT.Infrastructure/SNRT.Infrastructure.csproj `
  -s SNRT.Desktop/SNRT.Desktop.csproj

# 6. Run the WPF app
dotnet run --project SNRT.Desktop
```

## 2) Running from Visual Studio
- Open `SNRT.sln`
- Set `SNRT.Desktop` as startup project
- Run (F5)

## 3) Creating users and Admin
- In the app, use "Sign up" to create a user (first name, last name, email, password)
- Login with the new user
- Promote to Admin using SQL in SQLite (e.g., with DB Browser):
```sql
UPDATE Users SET Role = 1 WHERE Email = 'admin@example.com';
```
- Re-login as the admin user → the "Admin Dashboard" button appears

## 4) Verify features (Checklist)
Run the app then check each item:
- Authentication
  - [ ] Sign up with a new email
  - [ ] Login with correct password → success
  - [ ] Login with wrong password → shows error, no crash
  - [ ] Logout → returns to login
- Titles (left pane)
  - [ ] 11 default titles visible
  - [ ] Drag with mouse to reorder
  - [ ] Close and reopen app → order persists for the same user
- RSS (right pane)
  - [ ] See items from `Hespress` and `Media24` (network dependent)
  - [ ] Errors (if any) are logged without crashing UI
- Admin Dashboard (requires Admin)
  - [ ] Open dashboard
  - [ ] Filter by user and date range
  - [ ] Export CSV works
- Logging (Serilog)
  - [ ] Console shows app events
  - [ ] Log file exists: `%LocalAppData%\SNRT\logs\snrt-YYYYMMDD.log`
  - [ ] Entries for signups, logins (success/failure), logouts, RSS refresh, title reordering
- Assets (Logo)
  - [ ] `SNRT.Desktop/Assets/logo.png` exists (place your PNG here)
  - [ ] On build, file is copied to: `SNRT.Desktop/bin/Release/net8.0-windows/Assets/logo.png`
  - [ ] Login window shows centered logo at top (max height 100px)
  - [ ] Main window shows centered logo above titles list (max height 100px)

## 5) Migrations (EF Core) – Add/Apply
Generate a new migration when you change the model:
```powershell
$env:PATH += ";$env:USERPROFILE\.dotnet\tools"
# Replace Name with your migration name
dotnet ef migrations add Name `
  -p SNRT.Infrastructure/SNRT.Infrastructure.csproj `
  -s SNRT.Desktop/SNRT.Desktop.csproj `
  -o Persistence\Migrations
```
Apply migrations to the local DB:
```powershell
$env:PATH += ";$env:USERPROFILE\.dotnet\tools"
dotnet ef database update `
  -p SNRT.Infrastructure/SNRT.Infrastructure.csproj `
  -s SNRT.Desktop/SNRT.Desktop.csproj
```

## 6) Tests
```powershell
dotnet test SNRT.sln -c Release
```
Covers:
- AuthService: signup, valid/invalid login
- RSS parsing: valid and invalid feeds

## 7) Troubleshooting
- EF CLI not found
  - Run: `dotnet tool install --global dotnet-ef --version 9.*` then update PATH: `$env:PATH += ";$env:USERPROFILE\.dotnet\tools"`
- DB locked / migration lock
  - Close the app, retry `dotnet ef database update`. If needed, delete lock files in `%LocalAppData%\SNRT`.
- RSS feed empty
  - It can be network related; check logs at `%LocalAppData%\SNRT\logs`.
- Logo missing at runtime
  - Ensure `SNRT.Desktop/Assets/logo.png` exists; rebuild. The `.csproj` conditionally copies it.
- Reset app state
  - Close the app and delete `%LocalAppData%\SNRT\snrt.db` (this removes all local data)

## 8) Logging (Serilog)
- Console + rolling file (7 days)
- Log file path: `%LocalAppData%\SNRT\logs\snrt-YYYYMMDD.log`
- Key events logged: signups, logins (success/failure), logouts, RSS refresh results, title reordering

## 9) Project Structure (Clean Architecture)
- SNRT.Domain: Entities and enums (`User`, `LoginLog`, `TitleItem`, `UserTitleOrder`, `UserRole`)
- SNRT.Application: Interfaces (e.g., `IAuthService`)
- SNRT.Infrastructure: EF Core context, migrations, services (e.g., `AuthService`)
- SNRT.Desktop: WPF UI (Login, Signup, Main, Admin Dashboard)
- SNRT.Tests: Unit tests

## 10) Quick commands (copy/paste)
```powershell
cd "C:\Users\Mohamed\Desktop\projet aya"
dotnet restore
$env:PATH += ";$env:USERPROFILE\.dotnet\tools"
dotnet build SNRT.sln -c Release
# optional: dotnet ef database update -p SNRT.Infrastructure/SNRT.Infrastructure.csproj -s SNRT.Desktop/SNRT.Desktop.csproj
dotnet run --project SNRT.Desktop
``` 
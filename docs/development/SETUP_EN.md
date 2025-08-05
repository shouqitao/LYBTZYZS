# Development Environment Setup Guide

## Table of Contents

1. [System Requirements](#system-requirements)
2. [Required Software](#required-software)
3. [Environment Configuration](#environment-configuration)
4. [Getting the Code](#getting-the-code)
5. [Building the Project](#building-the-project)
6. [Database Setup](#database-setup)
7. [Running the Project](#running-the-project)
8. [Development Tools Configuration](#development-tools-configuration)
9. [Common Issues](#common-issues)

## System Requirements

### Minimum Requirements

- **Operating System**: Windows 10 (1809+) / Windows 11
- **Processor**: Intel Core i5 or AMD Ryzen 5 equivalent
- **Memory**: 8GB RAM
- **Storage**: 10GB available space
- **Graphics**: DirectX 11 support

### Recommended Configuration

- **Operating System**: Windows 11 latest version
- **Processor**: Intel Core i7 or AMD Ryzen 7 equivalent
- **Memory**: 16GB RAM or higher
- **Storage**: SSD drive, 20GB available space
- **Graphics**: Dedicated graphics card with DirectX 12 support

## Required Software

### 1. .NET SDK

Install .NET 8.0 SDK or higher:

```bash
# Download URL
https://dotnet.microsoft.com/download/dotnet/8.0

# Verify installation
dotnet --version
# Should display: 8.0.x or higher
```

### 2. Visual Studio 2022

Recommended to use Visual Studio 2022 Community or higher:

- **Download URL**: https://visualstudio.microsoft.com/
- **Required Workloads**:
  - ASP.NET and web development
  - .NET desktop development
  - Data storage and processing

### 3. SQL Server

Install SQL Server 2019 or higher:

- **Download URL**: https://www.microsoft.com/sql-server/sql-server-downloads
- **Recommended Version**: SQL Server 2019 Express (free)
- **Management Tool**: SQL Server Management Studio (SSMS)

### 4. Git

Install Git version control tool:

```bash
# Download URL
https://git-scm.com/download/windows

# Verify installation
git --version
```

### 5. Other Recommended Tools

- **Postman**: API testing tool
- **Visual Studio Code**: Lightweight editor
- **PowerShell 7+**: Enhanced command-line tool

## Environment Configuration

### 1. Configure Git

```bash
# Set user information
git config --global user.name "Your Name"
git config --global user.email "your.email@example.com"

# Set line ending handling (Windows)
git config --global core.autocrlf true
```

### 2. Configure NuGet Sources

If in mainland China, it's recommended to add local mirror sources:

```bash
# Add Huawei Cloud NuGet source
dotnet nuget add source https://mirrors.huaweicloud.com/nuget/v3/index.json -n huawei

# List current sources
dotnet nuget list source
```

### 3. Configure SQL Server

1. Open SQL Server Management Studio
2. Connect to local server (usually `localhost` or `.\SQLEXPRESS`)
3. Create database:

```sql
CREATE DATABASE LYBTDB
GO
```

## Getting the Code

### 1. Clone Repository

```bash
# Clone from GitHub
git clone https://github.com/yourusername/LYBTZYZS.git

# Or clone from Gitee
git clone https://gitee.com/yourusername/LYBTZYZS.git

# Enter project directory
cd LYBTZYZS
```

### 2. Directory Structure

```
LYBTZYZS/
├── src/
│   ├── Backend/           # Backend projects
│   │   ├── Core/         # Core projects
│   │   ├── Modules/      # Business modules
│   │   └── Services/     # Service projects
│   ├── Frontend/         # Frontend projects
│   │   └── Desktop/      # WPF desktop application
│   └── Shared/           # Shared projects
├── tests/                # Test projects
├── scripts/              # Script files
├── docs/                 # Documentation
└── README.md
```

## Building the Project

### 1. Restore Dependencies

```bash
# In project root directory
dotnet restore
```

### 2. Build Solution

```bash
# Build entire solution
dotnet build LYBTZYZS.sln

# Or build specific projects
dotnet build src/Backend/Services/LYBT.WebAPI/LYBT.WebAPI.csproj
```

### 3. Common Build Issues

- **Missing SDK**: Ensure .NET 8.0 SDK is installed
- **NuGet errors**: Try clearing NuGet cache: `dotnet nuget locals all --clear`
- **Permission issues**: Run Visual Studio as administrator

## Database Setup

### 1. Configure Connection String

Edit `src/Backend/Services/LYBT.WebAPI/appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=LYBTDB;Trusted_Connection=True;TrustServerCertificate=true;MultipleActiveResultSets=true"
  }
}
```

### 2. Run Database Migrations

```bash
# Ensure you're in project root directory
# Add new migration
dotnet ef migrations add InitialCreate --project src/Backend/Core/LYBT.Infrastructure --startup-project src/Backend/Services/LYBT.WebAPI

# Update database
dotnet ef database update --project src/Backend/Core/LYBT.Infrastructure --startup-project src/Backend/Services/LYBT.WebAPI
```

### 3. Verify Database

1. Open SSMS
2. Connect to database
3. Check if all tables are created
4. Verify initial data

## Running the Project

### 1. Using Visual Studio

1. Open `LYBTZYZS.sln`
2. Set `LYBT.WebAPI` as startup project
3. Press F5 or click Run button

### 2. Using Command Line

```bash
# Run WebAPI
cd src/Backend/Services/LYBT.WebAPI
dotnet run

# API will be available at:
# https://localhost:7001
# http://localhost:5000
```

### 3. Using Script Files

```bash
# Quick start development server
scripts\start-dev.bat

# Run with development manager
scripts\dev-manager.bat
```

### 4. Access Swagger Documentation

Open browser and navigate to: https://localhost:7001/swagger

## Development Tools Configuration

### 1. Visual Studio Configuration

**Recommended Extensions**:
- ReSharper or CodeRush
- Entity Framework Core Tools
- Web Essentials

**Settings**:
- Enable EditorConfig support
- Configure code formatting rules
- Set up debugging options

### 2. Visual Studio Code Configuration

**Recommended Extensions**:
- C# (Microsoft)
- C# Extensions (jchannon)
- REST Client
- GitLens

**Settings** (`.vscode/settings.json`):

```json
{
  "editor.formatOnSave": true,
  "editor.codeActionsOnSave": {
    "source.organizeImports": true
  },
  "omnisharp.enableRoslynAnalyzers": true
}
```

### 3. Postman Configuration

1. Import API collection from `docs/api/postman-collections/`
2. Configure environment variables:
   - `baseUrl`: https://localhost:7001/api/v1
   - `token`: (will be set after login)

## Common Issues

### 1. Certificate Errors

If encountering HTTPS certificate errors:

```bash
# Trust development certificate
dotnet dev-certs https --trust
```

### 2. Port Conflicts

If port 7001 or 5000 is already in use:

1. Edit `Properties/launchSettings.json`
2. Change `applicationUrl` to different ports

### 3. Database Connection Issues

- Verify SQL Server service is running
- Check connection string correctness
- Ensure database exists
- Verify Windows Authentication is enabled

### 4. Migration Errors

```bash
# If migrations fail, try:
# 1. Delete existing database
dotnet ef database drop --project src/Backend/Core/LYBT.Infrastructure --startup-project src/Backend/Services/LYBT.WebAPI

# 2. Delete Migrations folder
# 3. Create new migration
dotnet ef migrations add InitialCreate --project src/Backend/Core/LYBT.Infrastructure --startup-project src/Backend/Services/LYBT.WebAPI

# 4. Update database
dotnet ef database update --project src/Backend/Core/LYBT.Infrastructure --startup-project src/Backend/Services/LYBT.WebAPI
```

### 5. Build Performance Issues

- Enable parallel builds: `dotnet build -m`
- Use incremental builds
- Consider using build cache

### 6. IntelliSense Not Working

- Restart OmniSharp: `Ctrl+Shift+P` → "Restart OmniSharp"
- Clear component cache
- Reinstall .NET SDK

## Next Steps

After successfully setting up the development environment:

1. Read [Architecture Documentation](../architecture/ARCHITECTURE.md)
2. Review [Coding Standards](CODING_STANDARDS.md)
3. Check [API Documentation](../api/)
4. Run test suite: `dotnet test`
5. Start developing!

## Getting Help

- Check project documentation in `docs/` folder
- Review common issues above
- Ask team members
- Submit issues on GitHub/Gitee
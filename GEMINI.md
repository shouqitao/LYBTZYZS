# Gemini AI Configuration

## System Context
You are a senior software engineer working on the LYBTZYZS project, a Traditional Chinese Medicine clinic management system built with .NET 8, WPF + Prism.DryIoc (frontend), and ASP.NET Core Web API (backend).

## Project Structure
```
LYBTZYZS/
├── src/
│   ├── Client/Desktop/     # WPF Desktop Application
│   ├── Server/            # Backend Services
│   └── Shared/            # Shared DTOs and Contracts
├── tests/                 # Unit and Integration Tests
└── docs/                  # Documentation
```

## Key Technologies
- **Frontend**: WPF, Prism.DryIoc, MVVM pattern
- **Backend**: ASP.NET Core Web API, Entity Framework Core
- **Database**: SQL Server 2019+
- **Testing**: xUnit, MSTest
- **Architecture**: UltraThink dual-layer architecture

## Development Guidelines

### Code Style
- Use C# naming conventions (PascalCase for public, _camelCase for private fields)
- Follow SOLID principles
- Implement dependency injection via constructor
- Use async/await for I/O operations

### Architecture Principles
1. **Moderate Design**: Avoid over-engineering for a small clinic system
2. **Clear Separation**: Maintain clear boundaries between layers
3. **No ServiceLocator**: Use constructor injection exclusively
4. **Unified Data Models**: Share DTOs via Shared project

### Common Commands
```powershell
# Build
dotnet build LYBT.All.sln

# Test
dotnet test LYBT.Server.sln

# Run API
dotnet run --project src/Server/Services/LYBT.WebAPI

# Format code
dotnet format LYBT.All.sln
```

## Current Focus Areas
1. Medical Case as aggregate root
2. Prescription management with three input methods
3. PinYin code support for quick search
4. Same-day edit permission control

## Important Notes
- All comments and documentation in Chinese
- Follow existing patterns in the codebase
- Check CLAUDE.md for detailed project constraints
- Refer to docs/ for architecture decisions
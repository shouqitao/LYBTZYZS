---
created: 2025-09-03T13:20:35Z
last_updated: 2025-09-03T13:20:35Z
version: 1.0
author: Claude Code PM System
---

# Project Overview

## System Architecture

### High-Level Architecture
```
┌─────────────────────────────────────┐
│      WPF Desktop Client             │
│   (Prism + MVVM + Material Design)  │
└─────────────┬───────────────────────┘
              │ HTTPS/REST
              │ JWT Auth
┌─────────────▼───────────────────────┐
│      ASP.NET Core Web API           │
│     (Controllers + Services)         │
└─────────────┬───────────────────────┘
              │ EF Core
              │ LINQ
┌─────────────▼───────────────────────┐
│        SQL Server Database          │
│      (Tables + Stored Procs)        │
└─────────────────────────────────────┘
```

### Frontend Architecture (UltraThink Dual-Layer)
- **Presentation Layer**: WPF Views with XAML
- **ViewModel Layer**: MVVM pattern with Prism
- **Service Layer**: 
  - QueryService: Complex queries and searches
  - BusinessService: Business logic and CRUD
- **Module Layer**: Pure delegation pattern
- **Infrastructure**: Refit HTTP clients, AutoMapper

### Backend Architecture (Traditional 3-Layer)
- **API Layer**: RESTful controllers
- **Service Layer**: Business logic
- **Repository Layer**: Data access with EF Core
- **Infrastructure**: Authentication, logging, caching
- **Database**: SQL Server with migrations

## Feature List

### 1. Authentication & Authorization Module
- **User Login**: Username/password authentication
- **JWT Tokens**: 8-hour session tokens
- **Remember Me**: 30-day refresh tokens
- **Role Management**: Admin, Doctor roles
- **Permission Control**: Feature-level access
- **Password Management**: Secure hashing, reset capability
- **Session Management**: Active session tracking
- **Audit Logging**: Login/logout tracking

### 2. User Management Module
- **User CRUD**: Create, read, update, delete users
- **Profile Management**: User profile editing
- **Role Assignment**: Assign roles to users
- **Status Control**: Enable/disable accounts
- **Password Policy**: Complexity requirements
- **User Search**: Filter by name, role, status
- **Batch Operations**: Bulk status updates
- **Activity Tracking**: Last login, action history

### 3. Patient Management Module
- **Patient Registration**: Complete patient profiles
- **Demographic Data**: Name, age, gender, contact
- **Medical History**: Past conditions, allergies
- **Visit History**: Consultation records
- **Document Management**: Attach reports, images
- **Search & Filter**: Advanced patient search
- **Quick Registration**: Simplified intake process
- **Data Export**: Excel export capability

### 4. Medical Case Management Module
- **Case Creation**: New medical case initiation
- **Case Workflow**: Status tracking (New → In Progress → Completed)
- **Case Templates**: Reusable case structures
- **Case Linking**: Connect to consultations
- **Progress Notes**: Treatment progress tracking
- **Case Analysis**: Outcome evaluation
- **Case Search**: Find by patient, date, status
- **Case Archive**: Completed case storage

### 5. Consultation Module (TCM Specific)
- **Four Diagnostics (四诊)**:
  - **Observation (望诊)**: Complexion, tongue, appearance
  - **Auscultation (闻诊)**: Voice, breathing, odor
  - **Interrogation (问诊)**: Symptom questionnaire
  - **Palpation (切诊)**: Pulse, abdomen examination
- **Syndrome Differentiation**: Pattern diagnosis
- **Treatment Principles**: Strategy documentation
- **Consultation Notes**: Detailed observations
- **Follow-up Planning**: Next visit scheduling

### 6. Prescription Management Module
- **Herb Selection**: Search and add herbs
- **Dosage Calculation**: Automatic calculations
- **Formula Application**: Use formula templates
- **Custom Prescriptions**: Manual creation
- **Compatibility Check**: Herb interaction warnings
- **Prescription Preview**: Review before finalizing
- **Print Formatting**: Professional layout
- **Prescription History**: Past prescriptions

### 7. Herb Management Module
- **Herb Database**: Comprehensive herb library
- **Herb Properties**:
  - Chinese name and Pinyin
  - Nature and flavor (性味)
  - Channel tropism (归经)
  - Functions (功效)
  - Indications (主治)
- **Dosage Guidelines**: Safe dosage ranges
- **Contraindications**: Usage warnings
- **Price Management**: Unit pricing
- **Search & Filter**: By name, function, property
- **Import/Export**: Excel data management

### 8. Formula Management Module
- **Classic Formulas**: Traditional prescriptions
- **Custom Formulas**: Doctor's personal formulas
- **Formula Categories**: Organized by function
- **Ingredient Management**: Herb compositions
- **Dosage Templates**: Standard dosages
- **Modification Rules**: Adjustment guidelines
- **Formula Search**: Find by name, function
- **Formula Sharing**: Between practitioners

## Current State

### Development Status
- **Architecture**: ✅ Complete (UltraThink + Traditional)
- **Core Modules**: ✅ 8 modules fully implemented
- **Interface Unification**: ✅ Completed (2025-01-31)
- **Compilation**: ✅ Zero errors, zero warnings
- **Testing**: ⚠️ 2.76% coverage (needs improvement)
- **Documentation**: ✅ Comprehensive

### Technical Achievements
- **48 Projects**: Full enterprise solution
- **Code Quality**: A+ grade
- **Architecture**: UltraThink implementation
- **Performance**: < 2 second response time
- **Security**: JWT + RBAC implemented

### Recent Improvements
1. **Interface Standardization** (January 2025)
   - Eliminated interface duplication
   - Unified service architecture
   - Improved maintainability

2. **Frontend Refactoring** (September 2024)
   - Modern C# 12 features
   - Prism framework integration
   - MVVM pattern implementation

## Integration Points

### External Integrations
- **GitHub**: Source control via Git
- **MCP Servers**: 
  - Serena (code analysis)
  - Context7 (context management)
  - Git (version control)
  - Fetch (web content)

### Internal Integrations
- **Cross-Module Communication**:
  - Shared AppDbContext
  - Common DTOs
  - Event aggregation
  - Service injection

### API Endpoints
- **Base URL**: `https://localhost:7001/api/v1/`
- **Authentication**: `/auth/login`, `/auth/logout`
- **Users**: `/users` (CRUD operations)
- **Patients**: `/patients` (CRUD + search)
- **Consultations**: `/consultations` (TCM diagnostics)
- **Prescriptions**: `/prescriptions` (Create, print)
- **Herbs**: `/herbs` (Database management)
- **Formulas**: `/formulas` (Template management)

## Deployment Architecture

### Development Environment
- **IDE**: Visual Studio 2022
- **Database**: LocalDB
- **API Host**: IIS Express
- **Client**: Debug build

### Production Environment
- **Server**: Windows Server 2019+
- **Database**: SQL Server Express/Standard
- **API Host**: IIS 10+
- **Client**: Release build with installer

### System Requirements
- **Client Requirements**:
  - Windows 10/11 (64-bit)
  - .NET 8 Runtime
  - 4GB RAM minimum
  - 1366x768 resolution

- **Server Requirements**:
  - Windows Server 2019+
  - SQL Server 2019+
  - IIS 10+
  - 8GB RAM minimum

## Security Features

### Authentication
- JWT Bearer tokens
- 8-hour token expiry
- Refresh token support
- Secure password hashing (BCrypt)

### Authorization
- Role-based access (RBAC)
- Feature-level permissions
- API endpoint protection
- Admin-only functions

### Data Protection
- HTTPS encryption
- SQL injection prevention
- XSS protection
- Input validation

### Audit & Compliance
- User action logging
- Data change tracking
- Login/logout records
- Error logging

## Performance Characteristics

### Response Times
- API calls: < 2 seconds
- Database queries: < 500ms
- UI rendering: < 100ms
- Report generation: < 5 seconds

### Scalability
- Concurrent users: 20
- Database size: Up to 10GB
- Transaction volume: 1000/day
- Session management: 100 active

### Optimization
- Memory caching
- Lazy loading
- Query optimization
- Connection pooling

## Monitoring & Maintenance

### Health Checks
- Database connectivity
- API availability
- Memory usage
- Disk space

### Logging
- Application logs
- Error logs
- Audit logs
- Performance logs

### Backup Strategy
- Daily database backup
- Weekly full backup
- Transaction log backup
- Offsite storage

### Update Process
- Version control
- Release notes
- Database migrations
- Client auto-update
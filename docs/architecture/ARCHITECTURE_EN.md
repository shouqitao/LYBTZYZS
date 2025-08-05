# System Architecture Documentation

## Table of Contents

1. [Overview](#overview)
2. [System Architecture](#system-architecture)
3. [Technology Stack](#technology-stack)
4. [Architecture Principles](#architecture-principles)
5. [System Components](#system-components)
6. [Data Architecture](#data-architecture)
7. [Security Architecture](#security-architecture)
8. [Performance Architecture](#performance-architecture)
9. [Deployment Architecture](#deployment-architecture)
10. [Integration Architecture](#integration-architecture)

## Overview

LYBT Traditional Chinese Medicine Clinic Management System (LYBTZYZS) is an enterprise-level TCM clinic management system based on .NET 8. The system adopts a front-end and back-end separation architecture, with ASP.NET Core Web API for the backend and WPF desktop application for the frontend.

### Core Features

- **Modular Design**: 15 independent business modules for easy maintenance and expansion
- **Unified Data Access**: All modules share a single data context
- **Clean Architecture**: Strict separation of concerns to improve code quality
- **Modern Technology Stack**: Using the latest .NET 8 technology
- **Secure and Reliable**: JWT authentication, role-based authorization, data encryption

## System Architecture

### Overall Architecture Diagram

```
┌─────────────────────────────────────────────────────────────────┐
│                      Frontend Layer (WPF Client)                 │
├─────────────────────────────────────────────────────────────────┤
│  Shell │ Authentication │ Doctor │ FrontDesk │ SystemManagement │
└────────────────────────┬────────────────────────────────────────┘
                         │ HTTP/HTTPS + JWT
┌────────────────────────┴────────────────────────────────────────┐
│                    API Gateway Layer (Web API)                   │
├─────────────────────────────────────────────────────────────────┤
│     Controllers │ Middleware │ Authentication │ Swagger         │
└────────────────────────┬────────────────────────────────────────┘
                         │
┌────────────────────────┴────────────────────────────────────────┐
│                     Business Module Layer                        │
├─────────────────────────────────────────────────────────────────┤
│ Auth │ Users │ Patients │ Doctors │ Registration │ Diagnosis   │
│ Prescriptions │ Herbs │ FormulaTemplates │ Pharmacy │ Billing  │
│ Records │ Queueing │ TreatmentRoom │ Sync                      │
└────────────────────────┬────────────────────────────────────────┘
                         │
┌────────────────────────┴────────────────────────────────────────┐
│                   Infrastructure Layer                           │
├─────────────────────────────────────────────────────────────────┤
│   AppDbContext │ Repositories │ Services │ AutoMapper │ Cache  │
└────────────────────────┬────────────────────────────────────────┘
                         │
┌────────────────────────┴────────────────────────────────────────┐
│                   Data Persistence Layer                         │
├─────────────────────────────────────────────────────────────────┤
│              SQL Server Database (LYBTDB)                       │
└─────────────────────────────────────────────────────────────────┘
```

### Architecture Patterns

#### 1. Clean Architecture

- **Domain Layer**: Core business logic and entities
- **Application Layer**: Business use cases and service interfaces
- **Infrastructure Layer**: Data access, external service integration
- **Presentation Layer**: Web API controllers and WPF views

#### 2. Modular Monolith

- Each business module is independently developed and maintained
- Modules communicate through well-defined interfaces
- Shared infrastructure and data context
- Easy evolution to microservices architecture in the future

## Technology Stack

### Backend Technologies

- **.NET 8**: Latest cross-platform development framework
- **ASP.NET Core Web API**: RESTful API services
- **Entity Framework Core 8.0.17**: ORM framework
- **AutoMapper**: Object mapping
- **JWT Bearer**: Authentication
- **Swagger/Swashbuckle 9.0.1**: API documentation
- **SQL Server**: Relational database

### Frontend Technologies

- **WPF (.NET 8)**: Windows desktop application framework
- **Prism.DryIoc 9.0.537**: MVVM framework and dependency injection
- **Refit**: Type-safe REST client
- **Material Design**: UI component library

### Development Tools

- **Visual Studio 2022**: Primary IDE
- **Git**: Version control
- **PowerShell/Batch**: Automation scripts

## Architecture Principles

### 1. Single Responsibility Principle (SRP)

- Each module is responsible for a single business domain
- Classes and methods remain simple and focused
- Clear responsibility boundaries

### 2. Dependency Inversion Principle (DIP)

- High-level modules don't depend on low-level modules
- Abstract dependencies through interfaces
- Use dependency injection container to manage dependencies

### 3. Open/Closed Principle (OCP)

- Open for extension, closed for modification
- Achieve extensibility through interfaces and abstract classes
- Plugin-based architecture design

### 4. DRY Principle

- Avoid duplicate code
- Extract common functionality to base classes or utility classes
- Use code generation to reduce boilerplate code

## System Components

### 1. Web API Layer

```csharp
// Base Controller
public abstract class BaseController : ControllerBase
{
    protected IActionResult ApiResponse<T>(T data, string message = "")
    {
        return Ok(new ApiResponse<T>
        {
            Success = true,
            Data = data,
            Message = message
        });
    }
}
```

### 2. Business Service Layer

```csharp
public interface IBaseService<TEntity, TDto>
{
    Task<ApiResponse<IEnumerable<TDto>>> GetAllAsync();
    Task<ApiResponse<TDto>> GetByIdAsync(Guid id);
    Task<ApiResponse<TDto>> CreateAsync(TDto dto);
    Task<ApiResponse<TDto>> UpdateAsync(TDto dto);
    Task<ApiResponse<bool>> DeleteAsync(Guid id);
}
```

### 3. Data Access Layer

```csharp
public interface IBaseRepository<TEntity> where TEntity : class
{
    IQueryable<TEntity> GetAll();
    Task<TEntity?> GetByIdAsync(Guid id);
    Task<TEntity> AddAsync(TEntity entity);
    Task UpdateAsync(TEntity entity);
    Task DeleteAsync(TEntity entity);
}
```

### 4. Unified Data Context

```csharp
public class AppDbContext : IdentityDbContext<User, Role, Guid>
{
    // DbSets for all business entities
    public DbSet<Patient> Patients { get; set; }
    public DbSet<Doctor> Doctors { get; set; }
    public DbSet<Registration> Registrations { get; set; }
    // ... other entities
}
```

## Data Architecture

### Database Design Principles

1. **Normalization**: At least third normal form
2. **Index Optimization**: Create indexes for frequently queried fields
3. **Audit Trail**: All tables include creation time and modification time
4. **Soft Delete**: Use IsDeleted flag instead of physical deletion
5. **Data Integrity**: Foreign key constraints and check constraints

### Core Data Entities

- **User-related**: User, Role, Permission
- **Patient-related**: Patient, PatientRecord, PatientHistory
- **Doctor-related**: Doctor, DoctorSchedule, DoctorSpecialty
- **Treatment-related**: Registration, Diagnosis, Treatment, Prescription
- **Herb-related**: Herb, HerbCategory, HerbStock
- **Financial-related**: Bill, Payment, Invoice

## Security Architecture

### 1. Authentication

- JWT Bearer Token authentication
- Token expiration: 8 hours (configurable)
- Remember Me feature: 30-day validity
- Refresh Token mechanism

### 2. Authorization Mechanism

- Role-Based Access Control (RBAC)
- Fine-grained permission control
- API endpoint-level authorization
- Data-level permission filtering

### 3. Data Security

- Encrypted storage for sensitive data
- HTTPS transport encryption
- SQL injection protection
- XSS attack protection

### 4. Audit Logging

- Operation logging
- Login logs
- Data change history
- Exception logs

## Performance Architecture

### 1. Caching Strategy

- In-memory cache: Hot data
- Distributed cache: Shared data
- Query result caching
- Static resource caching

### 2. Database Optimization

- Query optimization
- Indexing strategy
- Paginated queries
- Lazy loading

### 3. Asynchronous Processing

- Async API endpoints
- Background task queues
- Concurrency control
- Thread pool management

### 4. Performance Monitoring

- API response time monitoring
- Database query monitoring
- Resource usage monitoring
- Performance metric alerts

## Deployment Architecture

### Development Environment

```yaml
Server: localhost
Database: SQL Server (localhost)
API Port: https://localhost:7001
Environment Variable: ASPNETCORE_ENVIRONMENT=Development
```

### Production Environment

```yaml
API Server: Windows Server 2019+
Database Server: SQL Server 2019+
Load Balancer: Optional
Cache Server: Redis (Optional)
Monitoring Service: Application Insights
```

### Deployment Process

1. Code compilation and packaging
2. Database migration
3. Configuration file updates
4. Service deployment
5. Health checks
6. Monitoring configuration

## Integration Architecture

### 1. Third-Party Integration

- SMS Service: Verification code sending
- Email Service: Notification push
- Payment Interface: Online payment
- Printing Service: Prescription printing

### 2. Data Synchronization

- Master-slave database synchronization
- Cross-system data exchange
- Batch data import/export
- Real-time data push

### 3. API Integration Patterns

- RESTful API
- Message Queue (Optional)
- WebSocket (Real-time communication)
- gRPC (High-performance communication)

## Architecture Evolution Roadmap

### Phase 1: Monolithic Application (Current)

- Modular monolithic architecture
- Single database
- Vertical scaling

### Phase 2: Service-Oriented (6-12 months)

- Core module service-oriented
- API Gateway
- Service discovery

### Phase 3: Microservices (12-24 months)

- Complete microservices architecture
- Distributed data management
- Containerized deployment

## Best Practices

1. **Code Organization**
   - Follow Clean Architecture principles
   - Maintain module independence
   - Use dependency injection

2. **API Design**
   - RESTful standards
   - Unified response format
   - Version management

3. **Data Access**
   - Repository pattern
   - Unit of Work pattern
   - Asynchronous operations

4. **Error Handling**
   - Global exception handling
   - Unified error response
   - Detailed logging

5. **Testing Strategy**
   - Unit testing
   - Integration testing
   - API testing
   - Performance testing

## Summary

The LYBT Traditional Chinese Medicine Clinic Management System adopts modern architecture design, ensuring system stability and maintainability while reserving space for future expansion and evolution. Through modular design, clean architecture, and unified technology stack, the system can meet the complex business needs of TCM clinics and provide a good user experience.
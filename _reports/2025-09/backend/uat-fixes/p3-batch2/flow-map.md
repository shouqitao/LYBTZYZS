# P3-Batch2: Transaction Flow Map Analysis

## 典型写入调用链流程

### 1. 患者创建流程 (Patient Creation)
```
HTTP POST /api/v1/patients
    ↓
PatientsController.Add()
    ↓
PatientBusinessService.CreateAsync()
    ├── ❌ BeginTransactionAsync() (LINE 40)
    ├── Validation Logic
    ├── _context.Patients.Add()
    ├── ❌ _context.SaveChangesAsync() (LINE 74)
    └── ❌ transaction.CommitAsync() (LINE 75)
```

**问题**: Direct transaction conflicts with SqlServerRetryingExecutionStrategy

### 2. 用户创建流程 (User Creation)
```
HTTP POST /api/v1/users
    ↓
UsersController.CreateUser()
    ↓
UserBusinessService.[CreateMethod]()
    ├── ❌ BeginTransactionAsync() (LINE 368/435)
    ├── User validation and creation
    ├── Role assignment
    ├── ❌ _context.SaveChangesAsync()
    └── ❌ transaction.CommitAsync()
```

### 3. 处方创建流程 (Prescription Creation)
```
HTTP POST /api/v1/prescriptions
    ↓
PrescriptionsController.[CreateMethod]()
    ↓
PrescriptionBusinessService.CreateAsync()
    ├── ❌ BeginTransactionAsync() (LINE 39)
    ├── Prescription creation
    ├── Herb items association
    ├── Price calculation
    ├── ❌ Multiple SaveChangesAsync() calls
    └── ❌ transaction.CommitAsync()
```

**特点**: Multi-table operations, higher transaction complexity

### 4. 基础仓储操作流程 (Base Repository Operations)
```
Service Layer Method Call
    ↓
OptimizedBaseRepository.[Method]()
    ├── ❌ BeginTransactionAsync() (4 locations)
    ├── Bulk operations
    ├── Entity manipulation
    ├── ❌ _context.SaveChangesAsync()
    └── ❌ transaction.CommitAsync()
```

**影响**: System-wide impact on all entities

## 事务使用模式分析

### Pattern A: Simple Single-Entity Creation
**Current (Problematic)**:
```csharp
using var transaction = await _context.Database.BeginTransactionAsync();
// Single entity operation
_context.Entities.Add(entity);
await _context.SaveChangesAsync();
await transaction.CommitAsync();
```

**Fixed (ExecutionStrategy)**:
```csharp
await _context.Database.CreateExecutionStrategy().ExecuteAsync(async () =>
{
    await using var transaction = await _context.Database.BeginTransactionAsync(ct);
    _context.Entities.Add(entity);
    await _context.SaveChangesAsync(ct);
    await transaction.CommitAsync(ct);
});
```

**Alternative (No Manual Transaction)**:
```csharp
// Let EF Core handle automatically for simple cases
_context.Entities.Add(entity);
await _context.SaveChangesAsync(ct);
```

### Pattern B: Multi-Table Complex Operations
**Current (Problematic)**:
```csharp
using var transaction = await _context.Database.BeginTransactionAsync();
// Multiple entities, complex business logic
_context.Entities1.Add(entity1);
await _context.SaveChangesAsync(); // First save
// More business logic based on saved data
_context.Entities2.AddRange(relatedEntities);
await _context.SaveChangesAsync(); // Second save
await transaction.CommitAsync();
```

**Fixed (ExecutionStrategy)**:
```csharp
await _context.Database.CreateExecutionStrategy().ExecuteAsync(async () =>
{
    await using var transaction = await _context.Database.BeginTransactionAsync(ct);
    
    _context.Entities1.Add(entity1);
    await _context.SaveChangesAsync(ct);
    
    // Business logic
    _context.Entities2.AddRange(relatedEntities);
    await _context.SaveChangesAsync(ct);
    
    await transaction.CommitAsync(ct);
});
```

### Pattern C: Bulk Repository Operations
**Current (Problematic)**:
```csharp
using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
// Bulk operations with optimizations
foreach (var entity in entities)
{
    _context.Entry(entity).State = EntityState.Modified;
}
await _context.SaveChangesAsync(cancellationToken);
await transaction.CommitAsync(cancellationToken);
```

**Fixed (ExecutionStrategy)**:
```csharp
await _context.Database.CreateExecutionStrategy().ExecuteAsync(async () =>
{
    await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
    
    foreach (var entity in entities)
    {
        _context.Entry(entity).State = EntityState.Modified;
    }
    await _context.SaveChangesAsync(cancellationToken);
    await transaction.CommitAsync(cancellationToken);
});
```

## 依赖关系映射

### High-Level Dependencies
```
Controllers (Web API)
    ↓ Inject
BusinessServices (Domain Logic)
    ↓ Inject
Repositories (Data Access)
    ↓ Use
AppDbContext (EF Core)
    ↓ Configured with
SqlServerRetryingExecutionStrategy
```

### Transaction Ownership Analysis
```
BusinessService Level (Most Common):
├── PatientBusinessService ❌ 6 methods
├── UserBusinessService ❌ 2 methods
├── PrescriptionBusinessService ❌ 1 method
└── HerbBusinessService ❌ 1 method

Repository Level (System Infrastructure):
├── OptimizedBaseRepository ❌ 4 methods
└── PatientRepository ❌ 1 method

Controller Level (None Found): ✅
```

## CancellationToken 传播分析

### Current Issues
- ❌ Most transaction methods missing CancellationToken parameters
- ❌ Incomplete CancellationToken propagation to SaveChangesAsync
- ❌ Transaction operations not cancellable

### After Fix
- ✅ Full CancellationToken propagation chain
- ✅ Proper async cancellation support
- ✅ Better resource cleanup on cancellation

## Performance Impact Assessment

### Current State
- ❌ Transaction failures cause 500 errors
- ❌ No automatic retry on transient failures
- ❌ Manual transaction overhead

### After Fix
- ✅ Automatic retry for transient failures
- ✅ Better error handling and recovery
- ✅ Optimized transaction lifecycle
- ⚠️ Slight overhead from ExecutionStrategy wrapper

## Next Implementation Priority

### Phase 1: Critical Path (P0)
1. `PatientBusinessService.CreateAsync()` - Blocks UAT patient creation
2. `UserBusinessService` methods - Blocks user management

### Phase 2: Infrastructure (P1)  
1. `OptimizedBaseRepository` methods - System-wide impact
2. `PatientRepository` batch operations

### Phase 3: Remaining Modules (P2)
1. `PrescriptionBusinessService` - Multi-table operations
2. `HerbBusinessService` - Herb management

---

**Analysis Date**: 2025-09-15 22:50:00  
**Scope**: Transaction flow mapping for P3-Fix Batch2  
**Status**: Complete - Ready for refactoring implementation
# P3-Batch2: Database Transaction Governance Rules

## 📋 Transaction Usage Constraints & Guidelines

### 🚨 FORBIDDEN Patterns (Zero Tolerance)

#### ❌ Pattern 1: Direct BeginTransactionAsync() Usage
```csharp
// ❌ BANNED - Will cause ExecutionStrategy conflicts
using var transaction = await _context.Database.BeginTransactionAsync();
await _context.SaveChangesAsync();
await transaction.CommitAsync();
```

#### ❌ Pattern 2: TransactionScope Usage
```csharp
// ❌ BANNED - Not compatible with ExecutionStrategy
using var scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled);
await _context.SaveChangesAsync();
scope.Complete();
```

#### ❌ Pattern 3: Manual IDbContextTransaction
```csharp
// ❌ BANNED - Direct transaction management conflicts
var transaction = _context.Database.BeginTransaction();
```

### ✅ REQUIRED Patterns (Mandatory)

#### ✅ Pattern A: ExecutionStrategy + Transaction (Complex Operations)
```csharp
// ✅ REQUIRED for multi-step/multi-table operations
return await _context.Database.CreateExecutionStrategy().ExecuteAsync(async () =>
{
    await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
    try
    {
        // Multi-step business logic
        _context.Entities.Add(entity);
        await _context.SaveChangesAsync(cancellationToken);
        
        // Additional operations
        _context.RelatedEntities.AddRange(related);
        await _context.SaveChangesAsync(cancellationToken);
        
        await transaction.CommitAsync(cancellationToken);
        return result;
    }
    catch
    {
        await transaction.RollbackAsync(cancellationToken);
        throw;
    }
});
```

#### ✅ Pattern B: Simple Operations (EF Core Auto-Transaction)
```csharp
// ✅ ALLOWED for simple single-entity operations
_context.Patients.Add(patient);
await _context.SaveChangesAsync(cancellationToken);
```

#### ✅ Pattern C: Bulk Operations with ExecutionStrategy
```csharp
// ✅ REQUIRED for batch/bulk operations
foreach (var batch in items.Chunk(100))
{
    var processedCount = await _context.Database.CreateExecutionStrategy().ExecuteAsync(async () =>
    {
        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            // Bulk processing logic
            var batchResult = await ProcessBatch(batch);
            await transaction.CommitAsync(cancellationToken);
            return batchResult;
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    });
}
```

## 🎯 Implementation Decision Tree

### When to Use ExecutionStrategy + Transaction?
- ✅ **Multi-table operations**
- ✅ **Multiple SaveChangesAsync() calls**
- ✅ **Complex business logic with rollback needs**
- ✅ **Batch/bulk operations**
- ✅ **Operations requiring strong consistency**

### When to Use Simple SaveChangesAsync()?
- ✅ **Single entity creation/update**
- ✅ **Simple CRUD operations**
- ✅ **Operations with no complex interdependencies**

## 🛡️ Code Review Checklist

### Pre-Commit Validation
- [ ] **No direct BeginTransactionAsync() usage**
- [ ] **No TransactionScope usage**
- [ ] **All complex operations wrapped with ExecutionStrategy**
- [ ] **CancellationToken propagated throughout**
- [ ] **Proper using/await using for transaction disposal**
- [ ] **Rollback in catch blocks**

### Architecture Compliance
- [ ] **Repository layer**: No direct transaction management
- [ ] **Service layer**: ExecutionStrategy for business transactions
- [ ] **Controller layer**: No transaction management

## 🔍 Monitoring & Detection

### Code Analysis Rules
```xml
<!-- Add to .editorconfig or analyzer settings -->
<Rule Id="LYBT001" Action="Error" />  <!-- Direct BeginTransactionAsync usage -->
<Rule Id="LYBT002" Action="Error" />  <!-- TransactionScope usage -->
<Rule Id="LYBT003" Action="Warning" /> <!-- Missing ExecutionStrategy wrap -->
```

### Runtime Detection
- **Application startup validation**: Check EF Core configuration
- **Health checks**: Verify ExecutionStrategy configuration
- **Logging**: Monitor transaction failure patterns

## 📊 Quality Gates

### Definition of Done
- ✅ **Zero ExecutionStrategy conflicts**
- ✅ **All tests pass**
- ✅ **Code review approved**
- ✅ **No forbidden patterns detected**

### Regression Prevention
- **Unit tests**: Validate transaction behavior
- **Integration tests**: Test ExecutionStrategy scenarios
- **Load tests**: Verify retry mechanisms

## 🔧 Developer Guidelines

### New Development
1. **Plan transaction boundaries** before coding
2. **Use Pattern A for complex operations**
3. **Use Pattern B for simple operations**
4. **Always include CancellationToken support**
5. **Test retry scenarios**

### Code Migration
1. **Identify direct transaction usage**
2. **Wrap with ExecutionStrategy pattern**
3. **Add proper error handling**
4. **Validate with integration tests**

### Emergency Procedures
1. **If ExecutionStrategy conflicts occur**:
   - Immediately wrap with CreateExecutionStrategy().ExecuteAsync()
   - Test retry behavior
   - Deploy hotfix

## 📚 Training & Knowledge Transfer

### Required Reading
- [EF Core ExecutionStrategy Documentation](https://learn.microsoft.com/en-us/ef/core/miscellaneous/connection-resiliency)
- [Transaction Patterns in EF Core](https://learn.microsoft.com/en-us/ef/core/saving/transactions)
- Internal: P3-Fix Batch2 Implementation Guide

### Workshops
- **Transaction Best Practices** (2 hours)
- **ExecutionStrategy Deep Dive** (1 hour)
- **Code Review Standards** (30 minutes)

---

**Document Version**: 1.0  
**Created**: 2025-09-15 23:00:00  
**Authority**: Backend Architecture Team  
**Enforcement**: Immediate - Zero Tolerance Policy

**🎆 Transaction reliability is critical for production stability. These rules are non-negotiable.**
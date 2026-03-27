## Issue 2.3: Global API Rate Limiting - COMPLETED

**Date**: 2026-03-27

**Change**: Added global rate limiting policy "ApiCalls" in ApiServiceCollectionExtensions.cs

**Implementation Details**:
- Policy name: "ApiCalls"
- Algorithm: FixedWindowLimiter (same as Login policy)
- Partition key: IP Address (consistent with Login policy)
- PermitLimit: 100 requests per window
- Window: 1 minute (TimeSpan.FromMinutes(1))
- QueueLimit: 0 (no queuing)

**Pattern Consistency**:
- Followed existing Login policy structure exactly
- Used same IP-based partitioning for fair rate limiting per client
- Added issue reference comment for traceability

**Usage**:
The policy can be applied globally via [EnableRateLimiting("ApiCalls")] on controllers or endpoints.

**File Modified**: src/Server/Services/LYBT.WebAPI/Extensions/ApiServiceCollectionExtensions.cs (lines 208-220)

---

## Issue 3.3: Query Filter Double Configuration - COMPLETED

**Date**: 2026-03-27

**Problem**: Global query filters for soft delete (`e => !e.IsDeleted`) were configured in two places:
1. `BaseEntityConfiguration<T>.Configure()` - applied in individual entity configurations
2. `EntityOptimizationExtensions.ApplyOptimizations()` - applied globally via reflection to all BaseEntity types

**Solution**: Removed the duplicate `HasQueryFilter()` call from `BaseEntityConfiguration.cs` (line 46), keeping `EntityOptimizationExtensions` as the single source of truth.

**Key Insight**: When using a pattern where global query filters are applied via `ModelBuilder` extensions (like `ApplyOptimizations()`), individual entity configurations should NOT duplicate those filters. EF Core will throw if the same filter is configured twice.

**Pattern for Future**: 
- Centralized global filters → Use `EntityOptimizationExtensions.ApplyOptimizations()`
- Entity-specific filters only → Keep in individual configuration classes
- Document the dependency with a comment to prevent future confusion

**Files Modified**:
- `src/Server/Core/LYBT.Infrastructure/Data/Configurations/Base/BaseEntityConfiguration.cs`

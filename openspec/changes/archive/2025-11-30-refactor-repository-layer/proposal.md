# Change: 重构服务端仓库层架构（最优实现）

## Why

当前仓库层存在以下架构问题：
1. **接口位置错误**: IRepository/IReadRepository在Shared层，但仅Server端使用
2. **构造函数混乱**: 不同Repository构造函数签名不一致，部分缺少Logger
3. **代码大量重复**: 6个Repository各自重写GetPagedAsync，逻辑相似但重复实现
4. **命名不一致**: 部分使用实体别名（ConsultationEntity），部分直接使用类名

## What Changes

### Phase 1: 接口重组
- **BREAKING**: 将IRepository<T>和IReadRepository<T>从Shared层移至Infrastructure层
- 删除Shared层的接口文件
- 更新所有引用

### Phase 2: 基类重构
- BaseRepository引入模板方法模式
- 添加ApplyKeywordFilter()虚方法（子类覆盖提供过滤逻辑）
- 添加ApplyDefaultOrdering()虚方法（子类覆盖提供排序逻辑）
- BaseReadRepository同步添加Logger必须参数

### Phase 3: 子类统一
- 统一所有Repository构造函数为(AppDbContext, ILogger<T>)
- 移除所有实体别名（using alias）
- 移除FormulaRepository冗余构造函数
- 子类只需override过滤/排序方法，不再重写整个GetPagedAsync

### Phase 4: 清理
- 删除重复的分页查询代码
- 更新所有单元测试
- 更新DI注册

## Impact

- **BREAKING CHANGE**: 接口位置变更，所有引用需更新
- Affected code:
  - `src/Shared/LYBT.Shared.Models/Interfaces/` → 删除
  - `src/Server/Core/LYBT.Infrastructure/Interfaces/` → 新增
  - `src/Server/Core/LYBT.Infrastructure/Repositories/BaseRepository.cs`
  - `src/Server/Core/LYBT.Infrastructure/Repositories/BaseReadRepository.cs`
  - 所有模块的Repository实现
  - 所有Repository相关单元测试

## Expected Improvements

| 指标 | 改进前 | 改进后 |
|------|--------|--------|
| GetPagedAsync重复代码 | 6处 | 0处 |
| 构造函数变体 | 3种 | 1种 |
| 接口位置 | 错误（Shared） | 正确（Infrastructure） |
| 子类代码量 | ~100行/类 | ~30行/类 |

# Task Plan: Phase 2 - Desktop Architecture Optimization

## Goal

修正 Desktop 层对 Server Entity 的不当依赖。核心: `IDataSourceBase<TEntity>` -> `IDataSourceBase<TDetail, TInput>` 使用 DTO 替代 Entity。

## Phases

### Phase 2.1: Contracts 接口改双泛型 [complete]
- Task 1.1: IDataSourceBase 改 `<TDetail, TInput>` -- done
- Task 1.2: 5 个具体接口改 DTO 泛型 -- done
- Task 1.3: ILocalAuthService 返回 UserDetailDto -- done
- Task 1.4: 移除 Contracts 的 Entities 依赖 -- done, Contracts.csproj 不再引用 LYBT.Entities

### Phase 2.2: Remote DataSource 简化 [complete]
- Task 2: 5 个 Remote DataSource 删除 Entity mapper -- done
  - 新增 ListDto->DetailDto Mapperly 内部映射器 (无 Entity 依赖)
  - Get 方法直接返回 API response.Data
  - Create/Update 直接接收 InputDto 并传给 API
  - Remote/ 目录零 Entity 引用

### Phase 2.3: Local DataSource Mapper + 实现 [complete]
- Task 3: 新建 5 个 LocalData Mapper -- done
  - LocalData/Mappers/ 下 5 个 Mapperly mapper
  - 仅 Entity<->DTO 转换, 复用 Infrastructure 旧 mapper 的 Ignore 属性
- Task 4: 5 个 Local DataSource 返回 DTO -- done
  - 内部仍用 EF Entity 操作, 边界处通过 mapper 转换
  - LocalAuthService 也已更新
  - 修复 LocalFormulaMapper.TotalPrice (Entity 无 UnitPrice 字段)

### Phase 2.4: Repository 简化 [complete]
- Task 5: 5 个 Repository 删除 DataSourceMapper 调用 -- done
  - 移除 _mapper 字段和 using Mappers
  - Read: DataSource 直接返回 DetailDto, 无需 _mapper.ToDetailDto()
  - Write: 直接传 InputDto, 无需 _mapper.ToEntity()
  - FormulaRepository: 修复 e.Indication -> e.Indications
- Task 5b: 11 个测试文件同步更新 -- done
  - Mock 返回类型 Entity -> DetailDto
  - 方法参数 Entity -> InputDto
  - 移除 IsDeleted 断言 (DTO 无此属性)

### Phase 2.5: 清理旧 Mapper + 依赖 [complete]
- Task 6.1: 删除旧 Infrastructure/DataSources/Mappers/ (5 个文件) -- done
- Task 6.2: 移除 Infrastructure.csproj 的 Entities 依赖 -- done
- Task 6.3: 移除 DI 注册 (DataSourceRegistrationExtensions) -- done
- Task 6.4: 删除 5 个 Mapper 测试文件 -- done

### Phase 2.6: ICrossModuleService 迁移 [complete]
- 已在之前提交中完成 (582c466, 9df002f, 632fe03)

### Phase 2.7: 依赖方向修复 [complete]
- A-3: Models -> Foundation 无此依赖 (已验证)
- A-4: Patients -> MedicalCase 无此依赖 (已验证)

### Phase 2.8: 代码位置调整 [deferred]
- UnfinishedCaseDialogViewModel / ActiveConsultationService 位置优化
- 低优先级，可独立处理，不影响架构目标

### Phase 2.9: 全量验证 [complete]
- Build: 0 errors, 0 warnings
- Tests: 1276 passed, 0 failed (561 + 58 + 633 + 24)

## Decisions

| Decision | Rationale |
|----------|-----------|
| 双泛型 `<TDetail, TInput>` | Create/Update 用 InputDto 更准确，避免传入无意义的计算属性 |
| Remote 直接透传 DTO | 消除 DTO->Entity->DTO 无意义往返转换 |
| Remote 内部 ListDto->DetailDto mapper | API paged 端点返回 ListDto, 接口需要 DetailDto, 用 Mapperly 轻量转换 |
| Local 内部保留 Entity | EF Core 需要 Entity 操作，通过 Mapper 在边界转换 |
| BaseApiController 不动 | 循环依赖: Module 需继承但不能引用 WebAPI |

## Errors Encountered

| Error | Resolution |
|-------|-----------|
| LocalFormulaMapper.TotalPrice: FormulaHerbItem 无 UnitPrice | 改为 `TotalPrice = 0`, 由 Service 层计算 |
| LocalAuthService 返回类型不匹配 | 添加 LocalUserMapper, 返回 `_mapper.ToDetailDto(user)` |

## Current Build Status

- 全量编译: 0 errors, 0 warnings
- 测试: 1276 pass (561 Server + 58 Arch + 633 Desktop Unit + 24 Desktop Integration)
- Phase 2 完成! 核心目标: Contracts + Infrastructure 解除 Entities 依赖

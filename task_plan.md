# Task Plan: 跨模块编译期解耦

## Goal
消除所有业务模块间的直接 ProjectReference (7个)，通过 ISP 接口和 Provider 模式实现跨模块通信。

## Design
docs/plans/2026-02-23-cross-module-decoupling-design.md

## Plan
docs/plans/2026-02-23-cross-module-decoupling-plan.md

## Phases

### Phase 1: 基础设施 -- 接口与实现 [complete]
- [1] 创建 4 个 ISP 接口 (IPatient/IHerb/IUser CrossModuleService + ICrossModuleAuthService) -- done
- [2] CrossModuleService 实现新接口 + CheckReference/Exists 新方法 -- done
- [3] DI 注册 4 接口 (工厂模式复用同一实例) -- done
- [4] 旧 ICrossModuleService 标记 [Obsolete] -- done
- [5] 创建 2 个 Provider 接口 (IHerbSearchProvider + IFormulaSearchProvider) -- done

### Phase 2: Server 迁移 [complete]
- [6] SyncService: IHerbService/IPatientService -> ISP 接口 -- done
- [7] MedicalCase 3 Service: IPatientRepo/IUserRepo -> ISP 接口 -- done
- [8] 移除 5 个 Server ProjectReference -- done

### Phase 3: Desktop 迁移 [complete]
- [9] HerbSearchProvider + FormulaSearchProvider 实现 -- done
- [10] MedicalCase 2 ViewModel 迁移 -- done
- [11] 移除 2 个 Desktop ProjectReference -- done
- [附加] HerbItem/HerbList/FormulaView 控件迁移到 Infrastructure -- done

### Phase 4: 清理 [complete]
- [12] 架构测试合并 (ArchTests -> Tests.Architecture) -- done (38+20=58 tests)
- [13] 空壳目录删除 (Consultation + Prescriptions) -- done

## Decisions
| Decision | Rationale |
|----------|-----------|
| ISP 拆分而非 MediatR | 同步语义清晰，已有 ICrossModuleService 基础设施 |
| CrossModuleService 一类多接口 | 避免多实现类维护成本，共享 DbContext |
| Provider 接口在 Contracts | 所有 Desktop 模块已依赖 Contracts，零新增依赖 |
| 旧接口 [Obsolete] 而非立即删除 | 渐进迁移，不阻塞其他消费者 |
| 控件迁移到 Infrastructure | MedicalCase 使用 HerbList/FormulaView 控件需编译期可见 |
| Tests.Architecture 改 net8.0-windows | 合并 Desktop 架构测试需要 WPF 引用 |

## Errors Encountered
| Error | Resolution |
|-------|------------|
| CustomControlArchTests 白名单不完整 | 添加 HerbListChangeType/HerbItemChangeType 枚举到白名单 |

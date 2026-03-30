# 架构评估进度

## 评估时间: 2026-03-30

| 维度 | 状态 |
|------|------|
| 1. 项目结构与依赖方向 | ✅ 完成 |
| 2. Controller 层 | ✅ 完成 |
| 3. Service 层 | ✅ 完成 |
| 4. Repository & 数据层 | ✅ 完成 |
| 5. 安全 | ✅ 完成 |
| 6. 横切关注点 | ✅ 完成 |
| 7. 代码质量 | ✅ 完成 |

## Phase 改进进度

| Phase | 描述 | 状态 |
|-------|------|------|
| Phase 1 | Mapperly 统一映射 | ✅ 完成 |
| Phase 2 | 工作单元模式（AddWithoutSave/UpdateWithoutSave/DeleteWithoutSave/SaveAllChanges） | ✅ 完成 |
| Phase 3 | 消除 IConfiguration 直接注入 | ⏭️ 跳过 — 未发现任何 Service 注入 IConfiguration |
| Phase 4 | 解耦 Patients → MedicalCase 模块引用 | ✅ 完成 — 移除 csproj 直接引用，已通过 IMedicalCaseCrossModuleService 解耦 |
| Phase 5 | 统一文件编码为 UTF-8 | ✅ 完成 — .editorconfig charset 从 utf-8-bom 改为 utf-8 |

### Phase 2 详情
- `IRepository<T>` 新增：`AddWithoutSaveAsync`, `UpdateWithoutSaveAsync`, `DeleteWithoutSaveAsync`, `SaveAllChangesAsync`
- `BaseRepository<T>` 实现了以上方法，不调用 SaveChanges
- 原有 AddAsync/UpdateAsync/DeleteAsync 保持不变（向后兼容）

### Phase 4 详情
- Patients.csproj 引用 MedicalCase 模块 → 已注释掉
- PatientService 仅通过 Infrastructure 层的 `IMedicalCaseCrossModuleService` 接口交互
- DI 注册在 WebAPI 层完成，Patients 模块无需知道 MedicalCase 模块的存在

## 检查的关键文件
- 所有 .csproj（18个项目）
- 11个 Controller
- MedicalCase 9个 Service + Facade
- BaseRepository, BaseService, BaseApiController
- AuthService, JwtService, PasswordHelper
- Program.cs, 所有 Extensions
- 异常处理器（Business/System）
- 中间件（CorrelationId, SecurityHeaders, UnifiedMiddleware）
- Mapperly 映射器

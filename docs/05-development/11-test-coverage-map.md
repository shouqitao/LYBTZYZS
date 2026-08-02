# Tests — 测试覆盖地图

## Existing Coverage

| Use-Case | Rule | Expected Behavior | Evidence | Status |
|----------|------|-------------------|----------|:---:|
| 单实例启动 | Mutex 检查 | 拒绝第二次启动 | `App.xaml.cs:42-68` | ✅ |
| 登录锁定 | MaxFailedCount | 达到阈值锁定账户 | `AuthService.cs:87` | ✅ |
| 角色模块加载 | RoleRegistry | 按角色加载对应模块 | `RoleRegistry.cs` | ⚠️ C1 待修 |
| 架构分层 | Desktop 不依赖 Server | 编译时检查 | `DesktopLayerArchTests.cs` | ✅ |
| Repository 实现 | 接口必须有实现 | 编译时检查 | `ServerArchTests.cs` | ✅ |
| ViewModel 基类 | 必须继承标准基类 | 编译时检查 | `DesktopLayerArchTests.cs` | ✅ |
| 权限边界 | 匿名用户拒绝 | 401/403 | `PermissionBoundaryTests.cs` | ✅ |
| 医案状态机 | 状态转换规则 | 非法转换拒绝 | `MedicalCaseBusinessRules` | ✅ |
| 处方校验 | BR-003 规则 | 必填字段检查 | `MedicalCaseBusinessRules` | ✅ |
| 打印保护 | IsPrinted 检查 | 打印后修改需 EditReason | 待 D2 补回 | ❌ |
| 审计日志 | 20 字段 diff | 字段级变更记录 | 待 D1 补回 | ❌ |
| 令牌族旋转 | 重放检测 | 已用 Token 再提交→撤销族 | 待 D3 补回 | ❌ |
| 登录限流 | 5 次/分 | 超限返回 429 | 待 D3 补回 | ❌ |
| 历史聚合 | 跨医案查询 | 返回患者所有历史 | 待 D9 补回 | ❌ |

## Proposed Tests

| Use-Case | Rule | Test Type | Priority |
|----------|------|-----------|:---:|
| C1 模块加载修复 | Doctor 加载 6 个模块 | E2E | P0 |
| D1 审计日志 | 20 字段 diff 记录 | Unit + Integration | P0 |
| D2 打印回写 | PrintVersion 递增 | Unit | P0 |
| D3 限流 | 超限返回 429 | Integration | P0 |
| D3 登出撤销 | Token 失效 | Integration | P1 |
| D4 Restore | 软删除恢复 | Unit + E2E | P1 |
| D5 引用检查 | 删除前检查引用 | Unit | P0 |
| D7 权限修复 | Receptionist 可挂号 | E2E | P0 |
| D8 P0 Bug | BR-001 索引修复 | Integration | P0 |
| D9 历史聚合 | 跨医案查询 | Integration | P1 |
| 快捷键统一 | Ctrl+S/F5/Ctrl+P | Manual | P2 |
| 断网 UX | 状态栏变色+横幅 | Manual | P2 |
| DialogHost | 模态弹窗 | Manual | P2 |

## Gaps

| Rule | Documented In | Verification | Risk |
|------|---------------|:---:|------|
| 医疗审计日志 (MC-017) | `07-medical-cases.md` | ❌ 无 | **高** — 医疗合规 |
| 打印保护 (PRINT-004) | `09-printing.md` | ❌ 无 | **高** — 处方追溯 |
| Token 族旋转 (AUTH-006) | `02-auth.md` | ❌ 无 | **中** — 公网部署 |
| 登录限流 (AUTH-013) | `02-auth.md` | ❌ 无 | **中** — 暴力破解 |
| 历史聚合 (MC-008/009) | `07-medical-cases.md` | ❌ 无 | **中** — 复诊效率 |
| 引用检查 (BR-DEL-001) | 多模块 | ❌ 无 | **高** — 数据完整性 |
| 权限策略错配 (D7) | 多模块 | ❌ 无 | **高** — 安全 |
| 本地 /refresh 签名校验 | `02-auth.md` | ❌ 无 | **高** — 安全 |

## CI Requirements

| Gate | Scope | Status |
|------|-------|:---:|
| `dotnet build` | 全解决方案 | ✅ |
| `dotnet test tests/LYBT.Tests.Architecture/` | 架构约束 | ✅ |
| `dotnet test tests/LYBT.Tests.Server/` | Server 集成 | ✅ |
| `dotnet test tests/LYBT.Tests.Desktop/` | Desktop 集成 | ✅ |

# LYBTZYZS 整体整合方案（SSOT）

> 版本: v0.1（草稿） | 日期: 2026-08-08 | 维护者: 技术总监
> 状态: ⏳ 待 A-30 各阶段审查报告（S0-S3）填充
> 依据: `docs/compose/plans/2026-08-08-method-audit-plan.md` + A-30-S0/S1/S2/S3 报告
> 用户方针（2026-08-08）：**先收敛再完善**——本方案只做收敛（合并/集中定义），功能完善（B 类）冻结至收敛完成

---

## 一、审查结论摘要（S0-S3 填充）

| 阶段 | 对象 | 结论要点 | 报告 |
|------|------|---------|------|
| S0 | 全仓基线 | ✅ **1208 文件 / 1424 类型 / 6118 方法**（公开 3991 / 非公开 2127）；重复聚类 693 组（跨项目 408）；死方法候选 1095（占 17.9%，高置信 254）；Roslyn 校准漏报率 2.9% | `method-audit-baseline-*.md` 等 |
| S1 | Shared 5 项目 | ✅ **212 方法分级**（A139/B17/D56，含 4 整类死类 37 方法：Conflict/Api/Unauthorized/ValidationException 生产零构造）+ 日志/异常集中方案（ICorrelationIdProvider 零 DI 注册双机制分叉；异常→HTTP 映射三处独立实现分叉） | `method-audit-shared-*.md` |
| S2 | Server 10 项目 | ✅ **330 类型 / 970 方法分级**：29 死方法符号确认；跨模块门面双轨（ICrossModuleService 仅覆盖 3/6 域）；MedicalCase 验证管道丢失实锤（Validator 注册后 0 注入）；仓储镜像方法 4 仓储同构；**Herbs+Formula 合并可行（43% 方法同构）/ Auth+Users 合并可行**；日志 3 件套+异常处理器迁移点确认 | `method-audit-server-*.md` |
| S3 | Desktop 16 项目 | ✅ **2987 方法分级**：D 级 78 符号 + 2 死类（可安全删 14 / 复核 64）；VM 13 组跨模块同名命令（TestConnectionAsync 95% 同构 / Editor VM 模板 90% 4 份拷贝）；映射 5 Mapperly+13 手写并存（DtoConversionExtensions 活链确认）；**Herbs+Formula 桌面侧 85% 同构（推荐合并）**；**LocalJwtConfig 策略缺口 P0 确认**（本地 3 模块不可用）；Reports 双轨复刻+策略分歧（Local DoctorOrAdminOrReceptionist vs Remote DoctorOrAdmin）；Remote 5 死亡端点 | `method-audit-desktop-*.md` |

---

## 二、项目合并方案（35 → N）

> 由 S2/S3 合并候选分析填充。候选方向：
> - Shared：Configuration+ExceptionHandling+Logging 是否合一（S1 专项结论）
> - Server：模块合并可行性（方法集相似度证据）
> - Desktop：Core 合并（Controls+Printing?）+ 小模块合并（Auth/Users/Herbs/Formula?）

### 2.1 Shared 层（S1 结论）

### 2.2 Server 层（S2 结论）

### 2.3 Desktop 层（S3 结论）

---

## 三、机制集中定义方案

### 3.1 日志独立完整项目（专项 A）

**目标**：`LYBT.Shared.Logging` 升级为独立完整日志项目，全程接管 Server+Desktop 日志。

**现状分布**（技术总监预核实）：
| 职责 | 当前位置 | 目标 |
|------|---------|------|
| CorrelationId Provider/Enricher | Shared.Logging | ✅ 已集中 |
| Serilog 配置（Desktop） | Desktop.Infrastructure/Logging/DesktopSerilogConfiguration.cs | → Shared.Logging |
| MSSQL sink 扩展 | WebAPI/Extensions/SerilogMSSqlServerExtensions.cs | → Shared.Logging |
| 日志注册（Shell） | Shell/Extensions/LoggingRegistrationExtensions.cs | → Shared.Logging |
| HTTP 日志拦截 | Desktop.Foundation/Http/LoggingHttpHandler.cs | → Shared.Logging |
| API 日志过滤 | WebAPI/Filters/ApiLoggingFilter.cs | → Shared.Logging |
| CorrelationId Middleware | WebAPI/Middleware/CorrelationIdMiddleware.cs | → Shared.Logging |

**整合方案**（S1 报告填充）：
- 目标结构 / 对外接口（AddLybtLogging）/ 迁移清单 / 保留删除决策

### 3.2 异常统一设计（专项 B）

**目标**：`LYBT.Shared.ExceptionHandling` 成为异常完整职责项目（层次 + 处理器 + 注册扩展）。

**现状分布**（技术总监预核实）：
| 职责 | 当前位置 | 目标 |
|------|---------|------|
| AppException 层次 | Shared.ExceptionHandling ✅ | 已集中 |
| SystemExceptionHandler | Server.Infrastructure/ExceptionHandling/ | → Shared.ExceptionHandling |
| BusinessExceptionHandler | Server.Infrastructure/ExceptionHandling/ | → Shared.ExceptionHandling |
| DesktopExceptionHandler | Desktop.Infrastructure/ExceptionHandling/ | 统一设计评估 |
| ClientErrorMessageMapper | Desktop.Foundation/ExceptionHandling/ | 统一设计评估 |
| ErrorCode | Shared.Models/Primitives/ErrorCodes/ | 归属评估 |

**统一方案**（S1 报告填充）

### 3.3 其他机制（配置/映射/验证/常量）

---

## 四、执行批次规划（先收敛后完善）

| 批次 | 内容 | 依赖 | 预估 |
|------|------|------|------|
| C-1 | 日志集中定义落地 | 本方案 §3.1 | 1-2d |
| C-2 | 异常统一落地 | 本方案 §3.2 | 1d |
| C-3 | Shared 合并（若有） | §2.1 | 1d |
| C-4 | Server 合并（若有） | §2.2 | 2-3d |
| C-5 | Desktop 合并（若有） | §2.3 | 2-3d |
| C-6 | 死方法/重复清理 | S1-S3 D/C 清单 | 1-2d |

> B 类功能（产品完善）冻结至 C 批次完成。

---

## 五、风险与决策点

- [x] **LocalWebAPI 策略缺失（S1 发现，真实 bug）**：LocalJwtConfig 注册 5 策略但缺 `DoctorOrAdminOrReceptionist`，Patients/Registrations/Reports 3 控制器本地模式会 500（WebAPI 侧有注册 `AuthenticationServiceCollectionExtensions.cs:132`）→ 待修复批次
- [ ] 日志集中是否值得重构（收益 vs 破坏面）——S1 已出方案，待拍板
- [ ] 异常处理器跨端统一是否现实（Server/Desktop 差异）——S1 已出方案，待拍板
- [ ] 项目合并 ROI（A-27 曾评估"收益有限风险中等"——本方案以方法级证据重新评估）
- [ ] 合并后架构测试/蓝图同步成本

# 资深架构师深度评估报告

> 日期：2026-09-17 ｜ 基线：`e4699d8f2`
> 审查方式：3 个并行只读侦察代理分区扫描 + 主代理汇总
> 关联：`full-project-architecture-review-2026-09-16.md`、`full-project-architecture-rereview-2026-09-17.md`

---

## 一、总体评分

| 维度 | 评分 | 说明 |
|------|:----:|------|
| **架构分层** | A- | CQRS/DDD/模块边界/领域事件均已落地 |
| **设计模式** | A- | 模板方法/守卫/策略/代理/状态机使用教科书式 |
| **.NET 官方实践** | A- | 对齐度约 90%，核心决策正确 |
| **方法级质量** | B+ | 1 个 NRE Bug、3 个长方法、3 处重复代码 |
| **API 设计** | B+ | RESTful 骨架完整，缺幂等键/排序可配 |
| **错误处理** | B | 体系完整，Result 双轨 + Obsolete 别名债务 |
| **并发与异步** | A- | Server 端零 .Result/.Wait()，RowVersion 落地 |
| **性能微观** | B+ | AsNoTracking 普及，搜索 ToLower().Contains 无法走索引 |

---

## 二、P0 立即修复（1 个真实 Bug）

### Bug-1: MedicalCaseQueryService.GetAuditLogsAsync NRE
- **位置**：`MedicalCaseQueryService.cs:375-380`
- **问题**：第 377 行访问 `medicalCase.CreatedBy`，第 379 行才判 `medicalCase == null`
- **触发条件**：`medicalCase == null` 且 `operatorId.HasValue && !isAdmin`
- **修复**：将 null 检查移到权限检查之前

---

## 三、P1 应尽快修复（8 项）

| # | 问题 | 位置 | 工作量 |
|---|------|------|:------:|
| 1 | ReportsController 裸 BadRequest 破 ApiResponse 信封 | `ReportsController.cs:54,75` | 极小 |
| 2 | SaveChangesAsync 把 DbUpdateConcurrencyException 包成 InvalidOperationException，下游靠中文 message Contains 匹配 | `BaseRepository.cs:248-251` + `MedicalCaseServiceHelper.cs:134` | 小 |
| 3 | OperatorAccessor.ParseUserRole 失败默认授予 Doctor 权限（安全反模式） | `OperatorAccessor.cs:47-60` | 小 |
| 4 | LoginCommandHandler.Handle 187 行长方法 | `LoginCommandHandler.cs:54-241` | 中 |
| 5 | ComputeTokenHash 三处完全重复 | Identity 三个 Handler | 小 |
| 6 | EnsureCanEdit/EnsureCanDelete 近重复 | `MedicalCaseServiceHelper.cs:152-200` | 小 |
| 7 | Desktop CommandResult 无 ErrorCode 字段 | `CommandResult.cs:11` | 中 |
| 8 | 写操作无 Idempotency-Key（创建医案/挂号重试产生重复） | 全局 | 中 |

---

## 四、P2 规划内改进（16 项）

### Server 端
| # | 问题 | 位置 |
|---|------|------|
| 1 | UpdateAsync Reload 副作用：并发冲突应直接抛 409 | `BaseRepository.cs:138-147` |
| 2 | IMedicalCaseRepository 20+ 方法 ISP 违反 | `IMedicalCaseRepository.cs` |
| 3 | MedicalCaseCommandService 14 个构造参数 | `MedicalCaseCommandService.cs:46-76` |
| 4 | CatalogEntityCommandHandlerBase 10+ 抽象成员膨胀 | `CatalogEntityCommandHandlerBase.cs:44-74` |
| 5 | CompleteAsync 86 行 / CancelAsync 70 行长方法 | `MedicalCaseStateService.cs` |
| 6 | PrescriptionPrintHandler PrintPreview/ExportPdf 80% 重复 | `PrescriptionPrintHandler.cs:64-167` |
| 7 | ExportPdfAsync 直接调用 SaveFileDialog（UI 耦合） | `PrescriptionPrintHandler.cs:147` |
| 8 | OperationType=1 魔法数字 | `MedicalCaseCommandService.Audit.cs:50` |
| 9 | QueryRecentAsync 手动 14 行映射应走 Mapperly | `MedicalCaseQueryService.cs:302-333` |

### API 设计
| # | 问题 | 位置 |
|---|------|------|
| 10 | 排序全部服务端硬编码，无 sort/order 参数 | 各 Repository |
| 11 | 搜索 ToLower().Contains 无法走索引 | Herb/Patient/User/Formula Repository |
| 12 | 导出 pageSize=10000 伪分页 | `HerbsController.cs:163,182` |
| 13 | export-all 与 export 完全重复 | `HerbsController.cs:156-188` |
| 14 | FormulasController 批量端点绝对路由 | `FormulasController.cs:335` |
| 15 | HealthController 503+success:true 矛盾 + 缺 CT | `HealthController.cs:77,98-99` |
| 16 | 中间件顺序 HSTS 应在 HTTPS Redirect 之前 | `UnifiedMiddlewareConfiguration.cs:79-83` |

---

## 五、P3 长期债务（12 项）

| # | 问题 | 位置 |
|---|------|------|
| 1 | Result 上 3 个 Obsolete 别名未删除 | `Result.cs:19,26,30` |
| 2 | Options 手动 Bind 绕过验证链 | `DatabaseServiceCollectionExtensions.cs:31-38` |
| 3 | DataProtection 生产密钥未加密 | `ServiceCollectionExtensions.cs:236-245` |
| 4 | SensitiveDataJsonConverterFactory 用 static Dictionary+lock | `SensitiveDataJsonConverterFactory.cs:18-49` |
| 5 | Microsoft.Extensions.* 版本漂移 8.0.0~8.0.3 | `Directory.Packages.props:15-34` |
| 6 | 构造函数空检 ?? throw 与 ThrowIfNull 混用 | 全局 |
| 7 | BaseService 薄基类仅持 ILogger | `BaseService.cs:9-17` |
| 8 | MedicalCaseStateGuard 末尾 unreachable throw | `MedicalCaseStateGuard.cs:45` |
| 9 | NavigationManager 角色菜单 switch 硬编码 | `NavigationManager.cs:75-93` |
| 10 | ErrorCode 旧 30001~30008 与新 301xx 并行 | `ErrorCode.cs:283-284` |
| 11 | DesktopUpdateService Obsolete 同步兼容层 | `DesktopUpdateService.cs:173-180` |
| 12 | 缓存基建在但 Controller/Service 层零消费 | 全局 |

---

## 六、明确不建议引入

| 技术 | 理由 |
|------|------|
| HATEOAS | WPF + Refit 强契约客户端，零收益 |
| ValueTask | 无超高频缓存命中热路径 |
| Span/Memory | 非计算密集场景 |
| Cursor 分页 | 当前数据量 offset 足够 |
| Outbox 现在落地 | ADR-0018 已规划 v2.0，当前同步投递是正确分期 |

---

## 七、架构级正面清单（保持）

1. **双控制器树镜像** + 类级 `[Authorize]` + 方法级覆盖执行到位
2. **模板方法**（Batch/Catalog）教科书式，6 个可覆写钩子 + 3 个子类复用
3. **守卫模式**（MedicalCaseStateGuard/UserHierarchyGuard）消除散落 if 链
4. **领域事件**跨模块契约放 Infrastructure SharedKernel（P07 合规）
5. **审计切面** DbContextAuditExtensions 统一填审计字段
6. **结构化日志** + CorrelationId 端到端追踪
7. **Serilog 两阶段初始化** 符合官方推荐
8. **Options 模式** IOptions/IOptionsMonitor 混用得当
9. **设计决策留痕** ADR 引用 + 行内注释说明「为什么」

---

## 八、改进建议路线图

### 立即（本迭代）
1. 修复 NRE Bug（GetAuditLogsAsync）
2. ReportsController 裸 BadRequest 改 ValidationFail
3. OperatorAccessor.ParseUserRole 失败改抛 401/403
4. 提取 TokenHashHelper 消除三处重复
5. 合并 EnsureCanEdit/EnsureCanDelete

### 短期（1-2 周）
6. 拆分 LoginCommandHandler.Handle（187 行 → 4 方法）
7. 引入 ConcurrencyRetryableException 消除 message.Contains 匹配
8. Desktop CommandResult 增加 ErrorCode
9. 中间件顺序调整（HSTS 先于 HTTPS Redirect）
10. Options 手动 Bind 改标准 Configure 链

### 中期（1 月）
11. IMedicalCaseRepository 按职责拆分
12. MedicalCaseCommandService 依赖收敛（14→≤8）
13. 搜索改前缀匹配或全文索引
14. 导出端点去伪分页
15. 写操作引入 Idempotency-Key

### 长期（v2.0）
16. Outbox 模式落地
17. Microsoft.Extensions.* 统一升级 .NET 9
18. Result Obsolete 别名删除
19. ErrorCode 旧码收敛

---

*报告版本: v1.0 | 生成日期: 2026-09-17 | 审查基线: e4699d8f2*

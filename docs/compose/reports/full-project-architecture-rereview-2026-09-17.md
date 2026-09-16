# 全项目架构复审报告（修复后验证）

> 日期：2026-09-17 ｜ 基线：`83d75cdbe`（前次审查基线 `e42c5e477` + 5 个修复 commit）
> 审查方式：4 个并行只读侦察代理分区扫描 + 主代理汇总
> 关联：`full-project-architecture-review-2026-09-16.md`（前次审查）、`fix-p1/p2-architecture-issues.md`（修复 Spec）

---

## 一、修复验证结论

**前次全部 10 项 P1、抽样 10 项 P2、5 项架构级深度改进均已正确落地，无功能性回归。**

| 类别 | 验证项 | 结果 |
|------|--------|------|
| P1 修复 | 10/10 | 全部 ✅ |
| P2 修复（抽样） | 10/10 | 全部 ✅ |
| 深度改进 | 5/5 | 4 ✅ + 1 ⚠️（文档漂移） |
| 功能性回归 | 0 | 无 |

**轻微残留**（非回归）：
- `FormulaHerbItemNotFound` 未映射 404（落入默认 500）
- `LYBT.Module.Registrations/AGENTS.md` 标题仍用旧名
- `LYBT.Module.MedicalCases/AGENTS.md` Structure 未列 Application/ 目录

---

## 二、新发现问题汇总

### P1 严重（8 项）

| # | 维度 | 问题 | 位置 |
|---|------|------|------|
| R-1 | Server | 领域事件同步直投无失败隔离——Handler 异常导致「已落库 + 下游未联动 + 客户端 500」 | `MedicalCaseStateService.cs:175-189,300-318` |
| R-2 | Server | LocalWebAPI 异常路径未统一 ProblemDetails（SharedHost 仍写 ApiResponse） | `SharedHost.cs:56-80` |
| R-3 | Server | BatchDelete 不回滚挂号，与 DeleteAsync 语义分叉 | `Deletion.cs:46` vs `:101-103` |
| R-4 | 安全 | 限流配置是死代码——`Security:RateLimiting` 配置节定义了参数但代码硬编码 | `ApiServiceCollectionExtensions.cs:214-239` |
| R-5 | 安全 | AES-GCM 回退硬编码测试密钥——环境变量缺失时静默使用已知密钥加密患者数据 | `AesGcmValueConverter.cs:16-31` |
| R-6 | 性能 | 患者按身份证号查询全表扫描（AES-GCM 非确定性加密） | `PatientRepository.cs:85-90` |
| R-7 | 测试 | Server 真 SQL + Respawn 集成测试基建已删除，SQL 翻译缺陷无覆盖 | `tests/AGENTS.md` T2-1 |
| R-8 | 依赖 | 遗留 ASP.NET Core 2.3.9 包引用（Http.Abstractions/Mvc.Core） | `Directory.Packages.props:49-52` |

### P2 中等（19 项）

| # | 维度 | 问题 |
|---|------|------|
| R-9 | Server | CQRS 迁移半完成：BatchDelete/SetPrescriptionFlag/RecordPrint/UpdateStatus 仍直注 Service |
| R-10 | Server | 软删/创建仍走同步跨模块调用（事件模式未闭环） |
| R-11 | Server | Complete/Suspend/Cancel 命令返回实体 `Result<MedicalCase>`（应返回 DTO） |
| R-12 | Server | 模块级 DbContext 未接入 DbContextAuditExtensions |
| R-13 | Server | 无领域事件测试与架构守卫 |
| R-14 | Server | Remote/Local UpdateStatus 响应契约不一致 |
| R-15 | Server | 429 OnRejected 写 ApiResponse 而非 ProblemDetails |
| R-16 | Desktop | UserNotificationService 无 Dispatcher 保护（跨线程 MessageBox 风险） |
| R-17 | Desktop | NavigationCoordinator ViewRoleAccess 缺 6 个可导航视图 |
| R-18 | 安全 | AES-GCM Encrypt() 失败静默返回明文（fail-open） |
| R-19 | 安全 | LocalWebAPI JWT 365 天有效期（Remote 为 30-480 分钟） |
| R-20 | 性能 | FormulaRepository 列表页 Include(Herbs) 过度加载 |
| R-21 | 性能 | DesktopUpdateService `.Wait()` 同步阻塞 |
| R-22 | 测试 | SecurityHeadersMiddleware 无任何测试 |
| R-23 | 安全 | 写操作端点多数无限流标注 |
| R-24 | 安全 | ValidateTokenReplay = false（防重放关闭） |
| R-25 | 性能 | 报表两段式查询，大时间范围产生巨大 IN 子句 |
| R-26 | 依赖 | System.Net.Http 4.3.4 遗留包 |
| R-27 | 测试 | 已知失败测试未清理（3 类存量失败） |

### P3 轻微（17 项）

Server 7 项（死代码/空壳、AuthSession 内联配置、软删过滤器重复、SharedHost 反射静默、Handler 就地改写入参、BusinessExceptionHandler dynamic、双处 DomainEvent DI）

Desktop 3 项（启动步骤 Name 中英混用、ParallelGroup 死配置、PreloadModulesAsync 双数据源）

安全/性能/依赖 7 项（AsNoTracking 不一致、默认连接串 Encrypt=False、HSTS 无效、死配置项、旧包版本等）

---

## 三、与前次审查对比

| 指标 | 前次（e42c5e477） | 本次（83d75cdbe） | 变化 |
|------|:-:|:-:|:-:|
| P1 | 10 | 8（新发现） | 前次 P1 全部修复，新维度发现 8 个 |
| P2 | 35 | 19（新发现） | 前次 P2 大部分修复，新维度发现 19 个 |
| P3 | 31 | 17（新发现） | 前次部分修复，新维度发现 17 个 |
| 架构模式合规 | B+ | A- | 领域事件/CQRS/异常统一已落地 |
| 安全 | 未深入 | B | 新发现 2 个 P1 安全问题 |
| 性能 | 未深入 | B+ | 新发现 1 个 P1 性能问题 |
| 测试质量 | C+ | B- | E2E 删除、架构测试增强，但 SQL 集成缺失 |

---

## 四、改进建议路线图

### 短期（1-2 周）

1. **事件 Handler 加 try/catch + Error 日志**（R-1）——对齐 US-MC-017 审计隔离策略
2. **Local 异常路径统一 ProblemDetails**（R-2）——SharedHost 调 AddLybtExceptionHandling
3. **修复限流死代码**（R-4）——ConfigureRateLimiting 改读配置
4. **修复 AES-GCM 回退密钥**（R-5）——生产环境 fail-fast
5. **移除遗留 ASP.NET 2.3.9 包**（R-8）——改用 FrameworkReference
6. **补全 ViewRoleAccess**（R-17）——6 个遗漏视图
7. **UserNotificationService 加 Dispatcher 保护**（R-16）

### 中期（1-2 月）

8. **BatchDelete 复用 DeleteAsync 逻辑**（R-3）
9. **完成 CQRS 迁移**（R-9）——剩余写端点
10. **重建 SQL 集成测试基建**（R-7）
11. **患者身份证号查询优化**（R-6）——盲索引或确定性加密
12. **补领域事件测试与架构守卫**（R-13）
13. **统一 429 限流响应格式**（R-15）

### 长期（v2.0）

14. **Outbox 模式**——保证落库与事件原子
15. **LocalWebAPI 令牌生命周期**——365 天改短周期
16. **Microsoft.Extensions 统一升级**——.NET 9 时消除版本混用

---

*报告版本: v1.0 | 生成日期: 2026-09-17 | 审查基线: 83d75cdbe*

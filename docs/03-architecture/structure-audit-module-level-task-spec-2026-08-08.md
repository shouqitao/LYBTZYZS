# 模块级结构审计任务书（structure-audit-module-level-2026-08-08）

> 阶段 2 扩展梳理：全局审计（A-16 已完成）→ **模块级深度审计（本次）**
> 依据：产品负责人决策「结构梳理扩展到整个项目」
> 执行：技术总监独立报告 + Mimo 独立报告 + 交叉验证

## 背景

A-16 全局审计覆盖了：分层依赖图/Shared 归属/多机制并存/死代码/命名/数据流/契约双套。但**未深入每个模块内部**。本次审计每个模块内部结构，输出「健康度评分卡 + 三分决策（保留/修补/重写）」，与全局审计合并构成全项目结构梳理全景。

## 审计对象（模块级）

### A. Desktop 7 模块内部（重点）
`Modules/LYBT.Desktop.{Auth,Users,Patients,Herbs,Formula,MedicalCase,Registration}`

每模块审计：
1. **分层健康**：ViewModel→Service/Repository→IApiClient 是否守 MVVM？有无越层（VM 直连 Repository/HTTP）？
2. **职责单一**：文件是否过大（>600 行）？类是否多职责？
3. **Repository 组织**：用泛型基类（EntityApiClientRepositoryBase）还是手写？一致性？
4. **Mapper**：Mapperly vs 手动？与 A-18 P1-4 后的一致？
5. **ViewModel 模式**：CommunityToolkit [ObservableProperty]/[RelayCommand] 使用一致性？有无 code-behind 业务逻辑？
6. **死代码/重复**：模块内零引用、重复实现
7. **内部组织**：目录结构（ViewModels/Services/Repositories/Mappers/Models）是否清晰？

### B. Server 8 模块内部
`Modules/LYBT.Module.{Auth,Users,Patients,Herbs,Formula,MedicalCase,Registration,Reports}`

每模块审计：
1. **分层健康**：Controller→Service/Handler→Repository→DbContext（P10）
2. **Handler 组织**：Command/Query/Validator 是否齐全？命名一致？
3. **Service vs MediatR**：混合注入是否有规律（A-14 已文档化，核实一致性）
4. **Mapper**：Mapperly（A-18 P1-4 后）是否统一？有无漏网手动映射？
5. **模块内重复**：同逻辑多处实现、可抽公共但未抽
6. **死代码**：模块内零引用

### C. LocalWebAPI 内部
1. Controller 实现质量（与 WebAPI 语义差异——A-17 已补 CRUD，查其它差异）
2. Program.cs 组织、DI 注册
3. 有无重复业务逻辑（应走 Server 模块，核实）

### D. Shell/Core 层
1. Shell：Prism 组合根、模块注册、导航组织
2. Infrastructure：**F1-2 已发现的 14 类职责过载**（Http/ViewModels/Views/CardReader/Navigation/Behaviors/Roles/Security/Helpers/Commands/Events/LocalData/Performance）——评估拆分方向
3. Foundation/Contracts/Controls/Printing：职责与依赖

### E. F 类遗留（8 项逐个评估）
| ID | 遗留 | 三分决策方向 |
|----|------|-------------|
| F-01 | FeatureToggle：14 开关仅 1 被检查 | 评估：功能开关是否还需要？若需要接入方式是否统一？|
| F-02 | 桌面 Mapper 统一（PatientMapper + MedicalCaseMapper DI 不一致） | 评估：是否已由 A-18 P1-4 覆盖 |
| F-03 | LocalData Mapper Target 策略 | 评估：LocalData 是否还在用（Mimo 发现 LocalDbContext 休眠）|
| F-04 | SyncService CS8602 可空警告 | 评估：SyncService 是否仍存活 |
| F-05 | PatientMapper 死代码 | 评估：删除或启用 |
| F-06 | 打印模板扩展（低优先级） | 记录现状，决策后置 |
| F-07 | API 版本化准备（低优先级） | 记录现状，决策后置 |
| F-08 | 日志归档策略（低优先级） | 记录现状，决策后置 |

### F. Tools + 测试项目
1. Tools 4 个（ApiTester/LoginTester/PasswordHashGenerator/UserInfoVerifier）——是否值得保留/合并
2. 测试项目结构（Server/Desktop/Architecture）——与代码同步性

### G. B 类未做功能（结合「未完成功能直接按新架构开发」原则）
B-06 备份恢复 / B-07 初始化向导 / B-08 发布包 / B-09 自动更新 / B-11/B-12 模板 / B-13 验方校验 UI / B-14 排班 / B-15 离线同步 / B-16 Swagger
**审计重点**：这些功能若做，按当前架构（契约统一后）开发的前置条件是什么？哪些现有代码会影响/阻碍？

## 产出格式

每模块「健康度评分卡」：

```
## 模块名（LYBT.Desktop.Patients）
| 维度 | 评分(1-5) | 证据 | 问题 |
|------|----------|------|------|
| 分层健康 | 4/5 | ... | ... |
| 职责单一 | ... | ... | ... |
| Repository | ... | ... | ... |
| Mapper | ... | ... | ... |
| ViewModel 模式 | ... | ... | ... |
| 死代码/重复 | ... | ... | ... |
| 内部组织 | ... | ... | ... |

**三分决策：保留 / 修补（列出修补点）/ 重写（理由）**
```

## 深度要求

1. 符号级验证（serena/codebase-memory/codegraph），grep 只做初筛
2. 每发现写推理链 + 证据（文件:行号）
3. 追根溯源到根因层
4. 结合文档（对照 03-server/05-dual-mode/08-shared 等）
5. **与 A-16 全局审计衔接**：已有结论（如 A-18 已修的 Mapper/契约）标注「已修」，不重复审计，专注模块内部新发现

## 硬性约束

1. 只读审计，禁改代码
2. 严重度 P0/P1/P2 + 三分决策建议
3. 产出独立报告：
   - 技术总监：`structure-audit-module-level-2026-08-08.md`
   - Mimo：`structure-audit-module-level-mimo-2026-08-08.md`
4. 不 commit（技术总监统一提交）

## 明确不做

- ❌ 不修代码（审计先行）
- ❌ 不重新审计全局层（依赖/契约/双轨——A-16 已覆盖）
- ❌ 不评 UI/交互
- ❌ 不写修复方案细节（只写问题 + 决策建议方向）

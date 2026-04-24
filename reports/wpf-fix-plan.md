# WPF 架构审查问题修复计划

> **日期**: 2026-04-19
> **基于**: wpf-architecture-review-report.md
> **状态**: 待执行
> **约束**: 本机无 dotnet SDK，所有修改无法构建验证

---

## 执行环境分析

### 约束条件
- ❌ 本机无 dotnet SDK，无法 `dotnet build` 验证
- ✅ Git 已配置，可随时提交和回滚
- ✅ 所有修改为纯文本操作（文件移动/编辑/创建）

### 执行方式选择

| 方式 | 优势 | 劣势 | 评分 |
|------|------|------|------|
| **BOSS 模式** | 多Agent编排，适合全新项目 | 过于重量级，不适用修复任务 | ⭐⭐ |
| **ACP (Claude Code)** | 编码能力强 | 无SDK无法验证，ACP环境搭建复杂 | ⭐⭐⭐ |
| **观澜直接执行** | 最快，对项目结构已了解 | 单线程，无构建验证 | ⭐⭐⭐⭐ |

**结论**: 由观澜直接执行，用 sub-agent 并行处理独立任务。理由：
1. 所有修复都是文件操作，无需运行时验证
2. 我已深度了解项目结构（刚完成审查）
3. BOSS 适合从零构建，不适合针对性修复
4. ACP 在无SDK环境下无优势

---

## 修复任务清单

### 🔴 P0 — 必须解决（2项）

#### P0-01: Infrastructure 项目拆分
- **当前**: Infrastructure 17,437 行，包含 20+ 目录
- **目标**: 拆分为 3-4 个独立项目
- **影响范围**: 所有 csproj 引用、命名空间
- **风险**: 🔴 高（需修改大量 csproj、namespace）
- **步骤**:
  1. 创建新项目目录结构
  2. 创建新 csproj（继承现有 Directory.Build.props）
  3. 按目录迁移文件，保留原命名空间
  4. 更新 Infrastructure.csproj（移除迁移的目录）
  5. 更新 Shell.csproj 和其他引用方的 ProjectReference
  6. 更新 DI 注册中的 using 语句

**拆分方案**:
```
LYBT.Desktop.Infrastructure (保留，~5K行)
├── DependencyInjection/   (92行)
├── Commands/              (80行)
├── Configuration/         
├── Constants/             (346行)
├── Events/                (447行)
├── Extensions/            
├── Helpers/               (136行)
├── Http/                  (378行)
├── Interfaces/            (68行)
├── Logging/               (108行)
├── Models/                (199行)
├── Repositories/          (336行)
├── Security/              (145行)
├── Services/              (3544行，核心服务保留)
└── Bootstrapping/         

LYBT.Desktop.UI.Controls (新建，~6K行)
├── Controls/              (4669行)
├── Converters/            (1187行)
├── Behaviors/             (461行)
├── Themes/                
└── Views/                 (408行)

LYBT.Desktop.Navigation (新建，~4K行)
├── Navigation/            (2893行)
├── ViewModels/            (1246行，ViewModel基类)
├── Roles/                 (305行)
├── Performance/           (203行)
└── Windows/               
```

#### P0-02: MedicalCase 同步功能（延后）
- **当前**: Sync 模块 v1.0 仅支持 Herb/Patient/Formula
- **建议**: ⚠️ **建议延后** — 这需要理解完整的业务同步逻辑，无SDK验证风险极高
- **替代方案**: 在 Local 模式下禁用 MedicalCase 创建（添加 UI 限制）
- **步骤**（如果做限制方案）:
  1. 在 MedicalCase 创建按钮的 Visibility 上绑定 ConnectionMode
  2. 在 MedicalCaseCommandsViewModel 中检查 ConnectionMode，Local 时禁用创建

---

### 🟡 P1 — 应当解决（4项）

#### P1-01: 清理删除注释噪音
- **当前**: 34处 `[已删除]` / `[已移除]` 注释
- **风险**: 🟢 低
- **步骤**:
  1. 扫描所有 `[已删除]` `[已移除]` 注释
  2. 逐行确认后删除（保留有价值的上下文注释）
  3. 同时清理无用的空 using 块

#### P1-02: 添加 DI 容器验证
- **当前**: 缺少启动时容器验证
- **风险**: 🟢 低
- **步骤**:
  1. 在 Shell/Services/Bootstrap/ 下创建 ContainerValidationService.cs
  2. 在 App.xaml.cs 的 OnInitialized 中调用验证
  3. 验证所有关键服务可解析

#### P1-03: 清理 wpftmp 残留文件
- **当前**: 9个 _wpftmp.csproj 文件
- **风险**: 🟢 极低
- **步骤**: 直接删除所有 `*_wpftmp.csproj`

#### P1-04: Shell 动态模块加载
- **当前**: Shell.csproj 硬编码 8 Module + 3 Role
- **风险**: 🟡 中（需要修改启动流程）
- **步骤**:
  1. 修改 App.xaml.cs 的 RegisterTypes
  2. 根据用户角色动态注册 Module
  3. 移除 Shell.csproj 中部分硬编码引用
- **建议**: ⚠️ **延后** — 需要完整的角色-模块映射表，且与 P0-01 Infrastructure 拆分有冲突，应先完成拆分

---

### 🟢 P2 — 可以改进（3项）

#### P2-01: 添加 Desktop 单元测试
- **当前**: tests/LYBT.Tests.Desktop 已存在（EndToEnd/UnitTests 目录）
- **建议**: 在现有 UnitTests 目录下添加 ViewModel 测试
- **风险**: 🟢 低

#### P2-02: FUTURE/TODO 标记统一
- **当前**: 代码中散布 FUTURE 标记
- **步骤**: 扫描并整理为 GitHub Issues

#### P2-03: INavigationAware 解耦
- **建议**: ⚠️ **延后** — 影响所有 ViewModel（12处），需要引入抽象层，风险高

---

## 执行顺序

### Phase 1: 安全清理（无风险，立即执行）
| 序号 | 任务 | 预计时间 | 风险 |
|------|------|---------|------|
| 1.1 | 删除 wpftmp.csproj 文件 | 1min | 🟢 |
| 1.2 | 清理 [已删除]/[已移除] 注释 | 5min | 🟢 |
| 1.3 | Git commit: "chore: cleanup temp files and deleted comments" | 1min | 🟢 |

### Phase 2: DI 验证（低风险）
| 序号 | 任务 | 预计时间 | 风险 |
|------|------|---------|------|
| 2.1 | 创建 ContainerValidationService.cs | 5min | 🟢 |
| 2.2 | 在 App.xaml.cs 启动流程中集成 | 3min | 🟢 |
| 2.3 | Git commit: "feat: add DI container validation on startup" | 1min | 🟢 |

### Phase 3: Local 模式 MedicalCase 限制（低风险）
| 序号 | 任务 | 预计时间 | 风险 |
|------|------|---------|------|
| 3.1 | 添加 ConnectionMode 检查逻辑 | 5min | 🟢 |
| 3.2 | Git commit: "fix: disable MedicalCase creation in Local mode" | 1min | 🟢 |

### Phase 4: Infrastructure 拆分（高风险，需要 dotnet build 验证）
| 序号 | 任务 | 预计时间 | 风险 |
|------|------|---------|------|
| 4.1 | 创建新项目目录 + csproj | 10min | 🟡 |
| 4.2 | 迁移 Controls/Converters/Behaviors → UI.Controls | 10min | 🟡 |
| 4.3 | 迁移 Navigation/ViewModels → Navigation | 10min | 🟡 |
| 4.4 | 更新所有 ProjectReference | 10min | 🟡 |
| 4.5 | 更新所有 using 声明 | 15min | 🔴 |
| 4.6 | Git commit: "refactor: split Infrastructure into 3 projects" | 1min | 🟢 |

### ⚠️ 延后任务
| 任务 | 原因 |
|------|------|
| MedicalCase 同步实现 | 需要完整业务逻辑设计，非架构修复 |
| Shell 动态模块加载 | 依赖 Phase 4 完成，且需角色映射表 |
| INavigationAware 解耦 | 影响面大，需 SDK 验证 |

---

## 风险控制

1. **每个 Phase 完成后立即 Git commit**
2. **Phase 4 开始前创建分支**: `git checkout -b refactor/infrastructure-split`
3. **Phase 4 如果失败**: `git checkout main` 回滚
4. **最终验证**: 需要在有 dotnet SDK 的环境执行 `dotnet build`

---

## 产物清单

| 产物 | 位置 |
|------|------|
| 修复计划 | reports/wpf-fix-plan.md |
| 修复执行日志 | reports/wpf-fix-execution-log.md（执行时创建） |

---

**准备就绪，等待确认后开始执行。**

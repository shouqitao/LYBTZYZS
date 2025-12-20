# Proposal: optimize-desktop-core

## Summary

优化Desktop Core层架构，解决Foundation和Infrastructure项目职责混乱、过度设计和依赖方向错误等问题。通过提取独立项目、简化Token管理、修复依赖关系，使Core层架构更加清晰、可维护。

## Motivation

### 当前问题

1. **Token管理过度设计** - Foundation/Security目录有16个文件，存在接口重复（ITokenStorage vs ITokenStorageService）和实现冗余

2. **Infrastructure成为"杂货铺"** - 21个子目录混杂UI控件、转换器、事件、服务等不同职责，缺乏清晰边界

3. **HTTP处理职责分散** - HTTP相关代码分散在Foundation/Http和Infrastructure/Http两处

4. **依赖方向错误** - Models项目依赖Infrastructure，违反了ViewModel应只依赖抽象接口的原则

5. **职责边界模糊** - SessionManager、ValidationService等核心服务放在Infrastructure而非Foundation

### 影响范围

- Foundation: 30+文件 → 目标20以下
- Infrastructure: 21个子目录 → 精简到10个以下
- 提取新项目: LYBT.Desktop.Controls

## Goals

1. **简化Token管理** - 从16个文件减少到8个，消除接口重复
2. **提取Controls项目** - 将18+控件和17个转换器独立为可复用的UI组件库
3. **修复依赖方向** - Models只依赖Contracts接口，通过DI获取实现
4. **整合HTTP处理** - 合并分散的HTTP代码到Foundation/Http
5. **明确职责边界** - Foundation负责基础设施，Infrastructure负责服务实现

## Non-Goals

- 不改变业务逻辑
- 不重构各业务模块（Auth, Users, Patients等）
- 不修改Shared层项目
- 不引入新的技术栈或框架

## Design Overview

### 目标架构

```
Core/
├── Contracts/              # 接口定义层（保持不变）
│   ├── Api/               # API接口
│   ├── Services/          # 服务接口
│   └── Models/            # 模型定义
│
├── Foundation/            # 基础设施层（精简）
│   ├── Http/              # 合并所有HTTP处理
│   ├── Security/          # 简化Token管理（8文件）
│   ├── Caching/           # 缓存服务
│   └── Configuration/     # 配置服务
│
├── Infrastructure/        # 服务实现层（重新定位）
│   ├── Services/          # 业务服务实现
│   ├── Events/            # Prism事件
│   ├── DependencyInjection/
│   └── Localization/      # 本地化
│
├── Controls/              # 新项目：UI组件库
│   ├── Controls/          # XAML控件
│   ├── Converters/        # 值转换器
│   ├── Templates/         # 控件模板
│   └── Themes/            # 主题资源
│
└── Models/                # ViewModel层（解耦）
    └── ViewModels/        # 仅依赖Contracts
```

### 依赖关系

```
Contracts (接口层)
    ↑
Foundation (基础设施)
    ↑
Infrastructure (服务实现)
    ↑
Controls (UI组件，独立)
    
Models (ViewModel) ──依赖──> Contracts (仅接口)
```

## Implementation Phases

### Phase 1: 简化Token管理 (simplify-token-management)

**目标**: 从16个文件减少到8个

**Before**:
```
Security/
├── IAuthenticationService.cs
├── AuthenticationService.cs
├── ITokenStorage.cs              # 重复
├── ITokenStorageService.cs       # 重复
├── SecureTokenStorage.cs
├── TokenStorageService.cs        # 重复
├── ITokenValidator.cs
├── LocalTokenValidator.cs
├── ITokenLifecycleService.cs
├── TokenLifecycleService.cs      # 过度设计
├── TokenLifecycleState.cs
├── TokenLifecycleStateChangedEvent.cs
├── ISecureCredentialStorage.cs
├── SecureCredentialStorage.cs
├── IUsernameStorageService.cs
├── UsernameStorageService.cs
```

**After**:
```
Security/
├── IAuthenticationService.cs
├── AuthenticationService.cs
├── ITokenService.cs              # 合并存储和生命周期
├── TokenService.cs
├── ITokenValidator.cs
├── LocalTokenValidator.cs
├── ICredentialStorage.cs         # 合并凭证存储
├── SecureCredentialStorage.cs
```

**风险**: 中低（有完整测试覆盖）
**工作量**: 2-3天

### Phase 2: 提取Controls项目 (extract-desktop-controls)

**目标**: 创建独立的UI组件库

**移动内容**:
- Infrastructure/Controls/ → Controls/Controls/ (18+控件)
- Infrastructure/Converters/ → Controls/Converters/ (17转换器)
- Infrastructure/Templates/ → Controls/Templates/
- Infrastructure/Themes/ → Controls/Themes/

**风险**: 低（纯UI组件，无业务逻辑）
**工作量**: 3-4天

### Phase 3: 修复Models依赖 (fix-models-dependencies)

**目标**: Models只依赖Contracts

**变更**:
1. ViewModelBase移除对Infrastructure的直接依赖
2. 通过接口注入获取服务
3. 更新csproj引用

**风险**: 中（需要重构ViewModel基类）
**工作量**: 2-3天

### Phase 4: 整合Infrastructure服务 (consolidate-infrastructure-services)

**目标**: 精简Infrastructure职责

**变更**:
1. 合并Infrastructure/Http到Foundation/Http
2. 移动核心服务（SessionManager等）到Foundation
3. 清理冗余目录

**风险**: 中高（涉及多个服务移动）
**工作量**: 3-4天

## Risks and Mitigations

| 风险项 | 概率 | 影响 | 缓解措施 |
|-------|-----|-----|---------|
| 引用遗漏导致编译失败 | 中 | 编译阻断 | 每个Phase后全量编译验证 |
| DI注册遗漏导致运行时异常 | 中 | 运行时崩溃 | 启动冒烟测试 + DI诊断 |
| 测试失败 | 低 | 功能回归 | 执行完整测试套件 |
| 命名冲突 | 低 | 编译失败 | 使用IDE重构工具 |

## Success Criteria

1. [ ] Foundation项目文件数从30+降到20以下
2. [ ] Infrastructure子目录从21个精简到10个以下
3. [ ] 新建LYBT.Desktop.Controls项目包含所有UI组件
4. [ ] Models项目不再直接依赖Infrastructure
5. [ ] 所有现有测试通过
6. [ ] 应用程序启动和核心功能正常

## Timeline

- Phase 1: 2-3天
- Phase 2: 3-4天
- Phase 3: 2-3天
- Phase 4: 3-4天
- 总计: 10-14天

## Alternatives Considered

### 方案A: 激进重构
将Core层完全重写，采用Clean Architecture模式。
**否决原因**: 风险过高，与Pre-Release Stabilization阶段目标冲突。

### 方案B: 仅简化Token管理
只执行Phase 1，保持其他结构不变。
**否决原因**: 无法解决Infrastructure职责混乱的根本问题。

### 方案C: 渐进式优化（选定）
分4个Phase逐步优化，每个Phase独立验证。
**选择原因**: 风险可控，可随时暂停，符合稳定性优先原则。

## References

- 已完成的类似优化: `adopt-activity-api-tracing`, `consolidate-exception-handling`
- 项目架构文档: `docs/explanation/architecture/`
- 异常处理提取案例: `LYBT.Shared.ExceptionHandling`
- 日志处理提取案例: `LYBT.Shared.Logging`

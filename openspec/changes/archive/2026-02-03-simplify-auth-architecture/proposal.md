# simplify-auth-architecture

## Why

当前Desktop端认证架构过于复杂，存在以下问题：

### 发现的问题

| 位置 | 问题类型 | 当前状态 | 期望状态 |
|------|----------|----------|----------|
| Auth模块 | 代码膨胀 | ~5800行代码，26个文件 | ~1200行代码，精简文件 |
| 服务层 | 服务过多 | 6个服务，职责分散 | 3个核心服务，职责清晰 |
| 状态机 | 状态冗余 | 6个状态，含Expiring警告 | 5个状态，静默处理超时 |
| 存储层 | 存储分散 | 3种存储机制 | 2层存储（内存+持久） |

### 影响分析

- **变更范围**: Desktop端认证模块
- **影响模块**: Auth, Shell, 登录界面
- **风险等级**: Medium - 影响核心认证流程，但范围可控

## What Changes

### Phase 1: 核心服务重构

统一认证门面，整合分散的服务：

1. **IAuthService** - 统一认证门面 + 状态机管理
   - 合并 LoginStateManager 逻辑
   - 管理登录/登出/状态转换

2. **ICredentialVault** - 凭据持久化
   - 合并 SecureCredentialStorage
   - DPAPI加密 + HMAC校验

3. **ITokenManager** - Token内存管理
   - 合并 TokenRefreshService
   - 刷新逻辑 + 有效性检查

**注意**: 保留 IUserActivityTracker，已在之前的OpenSpec中优化过

### Phase 2: 状态机简化

移除Expiring状态的警告对话框，改为静默超时登出：

- 删除 `SessionExpiringDialog.xaml`
- 修改超时检测逻辑，直接触发logout
- 状态从6个减少到5个

### Phase 3: 存储架构统一

明确两层存储职责：

- **内存层**: AccessToken/RefreshToken（会话级）
- **持久层**: Username/AutoLoginToken（用户级）

### Phase 4: ViewModel适配

更新 LoginViewModel 以适配新的服务接口：

- 简化状态绑定
- 移除冗余的事件处理

### Phase 5: 清理冗余代码

删除合并后不再需要的文件：

- `SecureCredentialStorage.cs`
- `CredentialMigrationService.cs`
- `LoginStateManager.cs`
- `TokenRefreshService.cs`
- `SessionExpiringDialog.xaml`
- 多余的事件类和DTO

## Architecture

### 变更影响范围

```
src/Client/Desktop/
├── Core/LYBT.Desktop.Infrastructure/
│   └── Services/
│       ├── AuthService.cs          [重构]
│       ├── CredentialVault.cs      [重构]
│       ├── TokenManager.cs         [重构]
│       └── UserActivityTracker.cs  [保留]
│
├── Modules/LYBT.Desktop.Auth/
│   ├── ViewModels/
│   │   └── LoginViewModel.cs       [适配]
│   └── Views/
│       └── LoginView.xaml          [微调]
│
└── Shell/LYBT.Desktop.Shell/
    └── App.xaml.cs                 [DI注册更新]
```

### 服务架构

```
┌─────────────────────────────────────────────────────────────┐
│  IAuthService（统一门面）                                    │
├─────────────────────────────────────────────────────────────┤
│  LoginAsync() / AutoLoginAsync() / LogoutAsync()            │
│  CurrentState / StateChanged事件                            │
└─────────────────────────────────────────────────────────────┘
          │
          ├─────────────────┬─────────────────┐
          ▼                 ▼                 ▼
┌─────────────────┐ ┌─────────────────┐ ┌───────────────────┐
│ICredentialVault │ │ ITokenManager   │ │IUserActivityTracker│
│ (持久层)        │ │ (内存层)        │ │ (活动监控)        │
└─────────────────┘ └─────────────────┘ └───────────────────┘
```

## Impact

- **文件变更**: ~15个文件
- **代码精简**: 从~5800行减少到~1200行 (-79%)
- **风险等级**: Medium
- **测试要求**:
  - 登录/登出流程测试
  - 自动登录测试
  - 超时登出测试
  - 网络断开处理测试

## Risks

| 风险 | 缓解措施 |
|------|----------|
| 登录流程中断 | 分阶段实施，每阶段编译验证 |
| 凭据丢失 | 保留迁移逻辑，兼容旧格式 |
| 状态转换异常 | 完善状态机单元测试 |

## 关键设计决策

本提案基于详细的场景讨论，已确认以下关键决策：

| 序号 | 决策点 | 决策 |
|------|--------|------|
| 1 | 超时警告 | 静默登出，无警告对话框 |
| 2 | logout保留状态 | 保留AutoLoginToken，不改变"记住密码"状态 |
| 3 | 用户名输入触发 | 任何输入即清除AutoLoginToken |
| 4 | 网络断开处理 | 优雅降级，保持本地会话 |
| 5 | logout后登录页 | 需点击按钮触发登录 |
| 6 | Token撤销处理 | 清除所有凭据 + 提示原因 |
| 7 | 服务端logout失败 | 下次登录成功后处理队列 |

## 规范更新

实施时需同步更新以下OpenSpec规范：

| 规范文件 | 更新内容 |
|---------|---------|
| `login-state-machine/spec.md` | 移除Expiring状态，简化为5状态 |
| `authentication/spec.md` | AUTH-003改为静默超时 |
| `credential-vault/spec.md` | CVT-004更新logout行为 |

## References

- 设计文档: `docs/plans/2026-01-26-auth-architecture-refactor-design.md`
- 现有规范: `openspec/specs/authentication/`, `openspec/specs/credential-vault/`, `openspec/specs/token-management/`, `openspec/specs/login-state-machine/`

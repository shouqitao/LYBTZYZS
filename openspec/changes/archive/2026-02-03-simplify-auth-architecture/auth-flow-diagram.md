# 认证流程图 (simplify-auth-architecture)

## 整体认证流程

```mermaid
flowchart TB
    subgraph AppStart["应用启动"]
        A1[App.xaml.cs] --> A2[NavigateToLogin]
        A2 --> A3[LoginView.OnNavigatedTo]
    end

    subgraph AutoLogin["自动登录尝试"]
        A3 --> B1{_suppressAutoLogin?}
        B1 -->|Yes| B2[跳过自动登录<br/>返回登录页]
        B1 -->|No| B3[TryAutoLoginAsync]

        B3 --> B4{JWT Token有效?}
        B4 -->|Yes| B5[使用JWT登录]
        B4 -->|No| B6{AutoLoginToken存在?}

        B6 -->|Yes| B7[使用AutoLoginToken登录]
        B6 -->|No| B2

        B5 --> B8[登录成功流程]
        B7 -->|成功| B9[Token轮换<br/>保存新AutoLoginToken]
        B7 -->|失败| B10[清除无效Token]
        B9 --> B8
        B10 --> B2
    end

    subgraph ManualLogin["手动登录"]
        B2 --> C1[用户输入账号密码]
        C1 --> C2{勾选自动登录?}
        C2 -->|Yes| C3[rememberCredentials=true]
        C2 -->|No| C4[rememberCredentials=false]
        C3 --> C5[LoginAsync]
        C4 --> C5

        C5 --> C6[发送LoginRequest<br/>含RememberMe标记]
        C6 --> C7{服务端认证}
        C7 -->|成功| C8{RememberMe=true?}
        C7 -->|失败| C9[显示错误]

        C8 -->|Yes| C10[服务端生成AutoLoginToken<br/>返回给客户端]
        C8 -->|No| C11[不生成AutoLoginToken]

        C10 --> C12[保存到CredentialVault]
        C11 --> B8
        C12 --> B8
    end

    subgraph LoginSuccess["登录成功流程"]
        B8 --> D1[保存JWT到TokenStorage]
        D1 --> D2[启动会话]
        D2 --> D3[加载用户模块]
        D3 --> D4[导航到首页]
    end

    subgraph Logout["登出流程"]
        D4 --> E1[用户点击登出]
        E1 --> E2[LogoutAsync]
        E2 --> E3[结束会话]
        E3 --> E4[调用服务端登出API]
        E4 --> E5[清除本地JWT]
        E5 --> E6[设置_suppressAutoLogin=true]
        E6 --> E7[导航到登录页]
        E7 --> A3
    end

    subgraph ClearAutoLogin["取消自动登录"]
        F1[用户取消勾选自动登录] --> F2[RememberMe setter]
        F2 --> F3[ClearAutoLoginCredentialsAsync]
        F3 --> F4[清除CredentialVault中的Token]
    end
```

## 关键组件职责

| 组件 | 职责 |
|------|------|
| **LoginViewModel** | UI交互、触发登录/自动登录、管理RememberMe状态 |
| **LoginCoordinator** | 登录流程编排、状态机管理、自动登录抑制控制 |
| **AuthenticationService** | API调用、Token验证、登出 |
| **CredentialVault** | 安全存储AutoLoginToken (DPAPI加密) |
| **UsernameStorageService** | 存储用户名和RememberMe状态 |
| **TokenStorageService** | 存储JWT Token和LoginResponse |

## 关键标记

### `_suppressAutoLogin` (LoginCoordinator)
- **设置时机**: LogoutAsync执行时设为true
- **检查时机**: TryAutoLoginAsync开始时检查
- **重置时机**: 检查后立即重置为false
- **作用**: 防止登出后立刻触发自动登录形成死循环

### `RememberMe` (LoginRequest)
- **来源**: 用户勾选"自动登录"
- **传递**: LoginViewModel → LoginCoordinator → LoginRequest → 服务端
- **服务端行为**: 只有RememberMe=true时才生成AutoLoginToken

## 数据流

```
登录成功时:
  1. 用户名存储: UsernameStorageService.SaveUsernameAsync(username, true)
     - 始终保存用户名（无论是否勾选自动登录）
  2. AutoLoginToken: 只有RememberMe=true时
     → CredentialVault.SaveAutoLoginTokenAsync(username, token)

自动登录时:
  CredentialVault.GetAutoLoginTokenAsync(username) → autoLoginToken
  → AuthenticationService.LoginWithAutoTokenAsync(autoLoginToken)
  → 服务端验证 → 返回新LoginResponse(含新AutoLoginToken)
  → CredentialVault.SaveAutoLoginTokenAsync(username, newToken) [Token轮换]

登出时:
  不清除CredentialVault中的AutoLoginToken (保留用于下次启动)
  不清除UsernameStorage中的用户名 (保留用于下次登录)
  只清除JWT Token
  设置_suppressAutoLogin=true

取消勾选自动登录时:
  LoginViewModel.RememberMe setter
  → ClearAutoLoginCredentialsAsync
  → CredentialVault.ClearCredentialsAsync(username) [只清除AutoLoginToken]
  → 不清除UsernameStorage中的用户名
```

## 功能分离

| 功能 | 存储位置 | 清除时机 |
|------|---------|---------|
| **记住用户名** | UsernameStorageService | 永不自动清除（用户手动清除） |
| **自动登录** | CredentialVault (AutoLoginToken) | 取消勾选"自动登录"时 |

## 安全机制

1. **Token轮换**: 每次使用AutoLoginToken成功登录后，服务端生成新Token，旧Token失效
2. **DPAPI加密**: AutoLoginToken使用Windows DPAPI加密存储，与用户账户绑定
3. **HMAC验证**: 存储时计算HMAC，读取时验证防篡改
4. **服务端撤销**: 服务端可随时撤销AutoLoginToken，强制用户重新登录

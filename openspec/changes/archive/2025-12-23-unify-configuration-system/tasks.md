# Tasks: 统一配置系统重构

## Phase 1: 项目创建与基础设施

### Task 1.1: 创建配置项目
- [x] 创建 `src/Shared/LYBT.Shared.Configuration` 项目
- [x] 配置项目文件 (TargetFramework, NuGet 包)
- [x] 添加项目引用到解决方案
- [x] 创建目录结构: `Options/`, `Validation/`, `Extensions/`, `Constants/`

### Task 1.2: 定义配置节常量
- [x] 创建 `Constants/ConfigurationSections.cs`
- [x] 定义所有配置节名称常量

## Phase 2: Options 类定义

### Task 2.1: 通用配置类
- [x] 创建 `Options/Common/JwtOptions.cs`
- [x] 添加 DataAnnotations 验证属性
- [x] 编写 XML 文档注释

### Task 2.2: 服务端配置类
- [x] 创建 `Options/Server/DatabaseOptions.cs` (含嵌套类)
- [x] 创建 `Options/Server/SecurityOptions.cs` (含 RateLimiting 配置)
- [x] 创建 `Options/Server/SessionOptions.cs`
- [x] 创建 `Options/Server/LoggingOptions.cs`
- [x] 创建 `Options/Server/UserManagementOptions.cs`
- [x] 创建 `Options/Server/SystemAdminOptions.cs`
- [x] 创建 `Options/Server/PasswordPolicyOptions.cs`
- [x] 创建 `Options/Server/DefaultPasswordOptions.cs`
- [x] 创建 `Options/Server/MemoryCacheOptions.cs`

### Task 2.3: 客户端配置类
- [x] 创建 `Options/Client/ApiClientOptions.cs`
- [x] 创建 `Options/Client/FeatureToggleOptions.cs`
- [x] 创建 `Options/Client/ClinicSettingsOptions.cs`
- [x] 创建 `Options/Client/ClientSessionOptions.cs`
- [x] 创建 `Options/Client/PrescriptionOptions.cs`

## Phase 3: 验证器实现

### Task 3.1: 自定义验证器
- [x] 创建 `Validation/JwtOptionsValidator.cs`
- [x] 创建 `Validation/DatabaseOptionsValidator.cs`
- [x] 创建 `Validation/SecurityOptionsValidator.cs`
- [ ] 验证器单元测试

## Phase 4: 扩展方法实现

### Task 4.1: 服务端扩展
- [x] 创建 `Extensions/ServerConfigurationExtensions.cs`
- [x] 实现 `AddLybtServerConfiguration` 方法
- [x] 配置 ValidateOnStart

### Task 4.2: 客户端扩展
- [x] 创建 `Extensions/ClientConfigurationExtensions.cs` (IServiceCollection版本)
- [x] 创建 `Shell/Extensions/PrismConfigurationExtensions.cs` (Prism IContainerRegistry版本)
- [x] 实现 `AddLybtClientConfiguration` 方法
- [x] 配置 ValidateOnStart

## Phase 5: 服务端迁移

### Task 5.1: 重新设计配置文件
- [x] 重构 `appsettings.json` 结构，简化层级
- [x] 移除冗余配置项

### Task 5.2: WebAPI 启动配置
- [x] 在 `Program.cs` 添加 `AddLybtServerConfiguration`
- [x] 验证配置绑定正确

### Task 5.3: 模块迁移
- [x] Auth 模块: 直接替换为 `IOptions<JwtOptions>`
- [x] Users 模块: 直接替换为 `IOptions<UserManagementOptions>`
- [x] 其他模块直接替换

### Task 5.4: 删除旧代码
- [ ] 删除 `ConfigurationHelper.cs` (需要检查是否还有引用)
- [x] 删除所有 `IConfiguration` 直接访问代码 (服务端)

## Phase 6: 客户端迁移

### Task 6.1: 重新设计客户端配置
- [x] 重构客户端 `appsettings.json`，与服务端统一格式
- [x] 配置节扁平化 (从 `Lybt:Client:Api` 改为 `ApiClient`)

### Task 6.2: Shell 启动配置
- [x] 在 `ServiceCollectionExtensions.cs` 添加 `AddLybtClientConfiguration`
- [x] 验证配置绑定正确

### Task 6.3: Foundation 层迁移
- [x] `TokenRefreshHandler.cs` 使用 `ApiClientOptions`
- [x] `ApiHealthCheckService.cs` 使用 `ApiClientOptions`
- [x] `LocalTokenValidator.cs` 使用 `JwtOptions`
- [x] `ServiceCollectionExtensions.cs` 使用 `ApiClientOptions`, `ClientSessionOptions`

### Task 6.4: ViewModel 迁移
- [x] 功能开关已注册 `FeatureToggleOptions`
- [x] `ClinicSettingsService` 保留使用本地 `ClinicSettings` 类 (支持热更新)
- [x] 删除客户端所有 `"Lybt:"` 前缀配置路径

## Phase 7: 测试与验证

### Task 7.1: 单元测试
- [x] Options 类验证测试
- [x] 验证器逻辑测试
- [x] 扩展方法注册测试

### Task 7.2: 集成测试
- [x] 完整配置加载测试
- [x] ValidateOnStart 失败场景测试
- [x] 环境变量覆盖测试

### Task 7.3: 回归测试
- [x] 服务端启动验证 (WebAPI /health 返回 Healthy)
- [x] 客户端启动验证 (Shell 启动成功，无配置错误)
- [x] 全功能回归测试 (57个单元测试全部通过)

## Phase 8: 文档更新

### Task 8.1: 更新架构文档
- [x] 更新 `docs/state/architecture/` 相关文档
- [x] 添加配置系统说明 (`docs/state/architecture/shared/configuration-architecture.md`)

### Task 8.2: 更新开发指南
- [x] 配置使用示例 (含代码示例)
- [x] 新增配置项指南 (Options类设计规范)

---

## 依赖关系

```
Phase 1 → Phase 2 → Phase 3 → Phase 4
                                 ↓
                    Phase 5 ← Phase 4 → Phase 6
                        ↓                 ↓
                    Phase 7 ← ← ← ← ← ← ←
                        ↓
                    Phase 8
```

## 估算

| Phase | 复杂度 | 风险 | 状态 |
|-------|--------|------|------|
| Phase 1 | 低 | 低 | 完成 |
| Phase 2 | 中 | 低 | 完成 |
| Phase 3 | 中 | 低 | 完成 |
| Phase 4 | 低 | 低 | 完成 |
| Phase 5 | 高 | 中 | 完成 |
| Phase 6 | 高 | 中 | 完成 |
| Phase 7 | 中 | 低 | 完成 |
| Phase 8 | 低 | 低 | 完成 |

---
created: 2025-12-23
status: active
updated: 2025-12-23

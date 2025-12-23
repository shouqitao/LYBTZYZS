# Tasks: 统一配置系统重构

## Phase 1: 项目创建与基础设施

### Task 1.1: 创建配置项目
- [ ] 创建 `src/Shared/LYBT.Shared.Configuration` 项目
- [ ] 配置项目文件 (TargetFramework, NuGet 包)
- [ ] 添加项目引用到解决方案
- [ ] 创建目录结构: `Options/`, `Validation/`, `Extensions/`, `Constants/`

### Task 1.2: 定义配置节常量
- [ ] 创建 `Constants/ConfigurationSections.cs`
- [ ] 定义所有配置节名称常量

## Phase 2: Options 类定义

### Task 2.1: 通用配置类
- [ ] 创建 `Options/Common/JwtOptions.cs`
- [ ] 添加 DataAnnotations 验证属性
- [ ] 编写 XML 文档注释

### Task 2.2: 服务端配置类
- [ ] 创建 `Options/Server/DatabaseOptions.cs` (含嵌套类)
- [ ] 创建 `Options/Server/SecurityOptions.cs` (含 RateLimiting 配置)
- [ ] 创建 `Options/Server/SessionOptions.cs`
- [ ] 创建 `Options/Server/LoggingOptions.cs`
- [ ] 创建 `Options/Server/UserManagementOptions.cs`
- [ ] 创建 `Options/Server/SystemAdminOptions.cs`
- [ ] 创建 `Options/Server/PasswordPolicyOptions.cs`
- [ ] 创建 `Options/Server/DefaultPasswordOptions.cs`
- [ ] 创建 `Options/Server/MemoryCacheOptions.cs`

### Task 2.3: 客户端配置类
- [ ] 创建 `Options/Client/ApiClientOptions.cs`
- [ ] 创建 `Options/Client/FeatureToggleOptions.cs`
- [ ] 创建 `Options/Client/ClinicSettingsOptions.cs`
- [ ] 创建 `Options/Client/ClientSessionOptions.cs`
- [ ] 创建 `Options/Client/PrescriptionOptions.cs`

## Phase 3: 验证器实现

### Task 3.1: 自定义验证器
- [ ] 创建 `Validation/JwtOptionsValidator.cs`
- [ ] 创建 `Validation/DatabaseOptionsValidator.cs`
- [ ] 创建 `Validation/SecurityOptionsValidator.cs`
- [ ] 验证器单元测试

## Phase 4: 扩展方法实现

### Task 4.1: 服务端扩展
- [ ] 创建 `Extensions/ServerConfigurationExtensions.cs`
- [ ] 实现 `AddLybtServerConfiguration` 方法
- [ ] 配置 ValidateOnStart

### Task 4.2: 客户端扩展
- [ ] 创建 `Extensions/ClientConfigurationExtensions.cs`
- [ ] 实现 `AddLybtClientConfiguration` 方法
- [ ] 配置 ValidateOnStart

## Phase 5: 服务端迁移

### Task 5.1: 重新设计配置文件
- [ ] 重构 `appsettings.json` 结构，简化层级
- [ ] 移除冗余配置项

### Task 5.2: WebAPI 启动配置
- [ ] 在 `Program.cs` 添加 `AddLybtServerConfiguration`
- [ ] 验证配置绑定正确

### Task 5.3: 模块迁移
- [ ] Auth 模块: 直接替换为 `IOptions<JwtOptions>`
- [ ] Users 模块: 直接替换为 `IOptions<UserManagementOptions>`
- [ ] 其他模块直接替换

### Task 5.4: 删除旧代码
- [ ] 删除 `ConfigurationHelper.cs`
- [ ] 删除所有 `IConfiguration` 直接访问代码

## Phase 6: 客户端迁移

### Task 6.1: 重新设计客户端配置
- [ ] 重构客户端 `appsettings.json`，与服务端统一格式
- [ ] 移除与服务端重复的配置项

### Task 6.2: Shell 启动配置
- [ ] 在 `App.xaml.cs` 添加 `AddLybtClientConfiguration`
- [ ] 验证配置绑定正确

### Task 6.3: ViewModel 迁移
- [ ] 直接替换为 Options 注入
- [ ] 功能开关使用 `IOptionsMonitor<FeatureToggleOptions>`
- [ ] 删除所有 `IConfiguration` 直接访问

## Phase 7: 测试与验证

### Task 7.1: 单元测试
- [ ] Options 类验证测试
- [ ] 验证器逻辑测试
- [ ] 扩展方法注册测试

### Task 7.2: 集成测试
- [ ] 完整配置加载测试
- [ ] ValidateOnStart 失败场景测试
- [ ] 环境变量覆盖测试

### Task 7.3: 回归测试
- [ ] 服务端启动验证
- [ ] 客户端启动验证
- [ ] 全功能回归测试

## Phase 8: 文档更新

### Task 8.1: 更新架构文档
- [ ] 更新 `docs/state/architecture/` 相关文档
- [ ] 添加配置系统说明

### Task 8.2: 更新开发指南
- [ ] 配置使用示例
- [ ] 新增配置项指南

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

| Phase | 复杂度 | 风险 |
|-------|--------|------|
| Phase 1 | 低 | 低 |
| Phase 2 | 中 | 低 |
| Phase 3 | 中 | 低 |
| Phase 4 | 低 | 低 |
| Phase 5 | 高 | 中 |
| Phase 6 | 高 | 中 |
| Phase 7 | 中 | 低 |
| Phase 8 | 低 | 低 |

---
created: 2025-12-23
status: draft

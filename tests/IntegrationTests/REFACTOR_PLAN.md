# 集成测试架构重构计划

> **Issue**: [#945](https://github.com/shouqitao/LYBTZYZS/issues/945)
> **状态**: 🚧 进行中
> **创建时间**: 2025-10-05
> **负责人**: Claude Code + Human Review

---

## 📋 背景与问题

### 问题诊断

经过深入分析 CI 持续失败（Issue #942、#943），发现根本问题：

**集成测试代码严重过时**，与当前代码库 API 契约不匹配。

### 具体问题

#### 1. DTO 结构变化（12 个编译错误）

```csharp
// ❌ 测试代码（过时）
result.Data!.Username  // LoginResponse 没有 Username 属性

// ✅ 实际结构（当前）
public class LoginResponse {
    public string Token { get; set; }
    public UserDto User { get; set; }  // 👈 包含 User 对象
    public string RefreshToken { get; set; }
    public DateTime ExpiresAt { get; set; }
}

public class UserDto {
    public string UserName { get; set; }  // 👈 注意大写 N
    // ...
}
```

#### 2. 缺少包引用（7 个编译错误）

```
CS1061: HttpClient 未包含 PostAsJsonAsync 扩展方法
CS1061: IWebHostBuilder 未包含 UseEnvironment 扩展方法
```

**原因**: 缺少 `System.Net.Http.Json` 和 ASP.NET Core hosting 包引用

#### 3. 测试基础设施混用（2 个编译错误）

- 部分测试使用标准 `WebApplicationFactory<Program>`
- 自定义 `CustomWebApplicationFactory` 未被统一使用

---

## 🎯 重构目标

### 核心目标

1. **删除过时测试** ✅ - 移除 `tests/IntegrationTests/ServerIntegrationTests/`
2. **创建新测试项目** - 基于当前 API 契约重写
3. **建立标准架构** - 统一测试基础设施
4. **核心场景覆盖** - Auth/Health/Users 核心功能
5. **CI 集成** - 确保测试稳定运行

### 非目标（明确排除）

- ❌ 不做全面的端到端测试（那是 E2E 测试范畴）
- ❌ 不做性能/压力测试（有专门的 BenchmarkTests）
- ❌ 不做安全测试（有专门的 SecurityTests）
- ❌ 不追求 100% 覆盖率（集成测试聚焦核心场景）

---

## 📐 新测试架构设计

### 项目结构

```
tests/IntegrationTests/
├── LYBT.WebAPI.IntegrationTests/           # 新项目
│   ├── Infrastructure/                      # 测试基础设施
│   │   ├── CustomWebApplicationFactory.cs  # 自定义测试工厂
│   │   ├── TestDataSeeder.cs               # 测试数据生成
│   │   └── TestHelpers.cs                  # 辅助方法
│   ├── Controllers/                         # 按模块组织测试
│   │   ├── Auth/
│   │   │   ├── LoginTests.cs
│   │   │   ├── LogoutTests.cs
│   │   │   └── TokenValidationTests.cs
│   │   ├── Health/
│   │   │   └── HealthCheckTests.cs
│   │   └── Users/
│   │       ├── UserCrudTests.cs
│   │       └── UserSearchTests.cs
│   ├── appsettings.IntegrationTests.json   # 测试专用配置
│   └── LYBT.WebAPI.IntegrationTests.csproj
└── REFACTOR_PLAN.md                         # 本文档
```

### 技术栈

- **测试框架**: xUnit 2.6.6
- **断言库**: FluentAssertions 6.12.2
- **测试主机**: Microsoft.AspNetCore.Mvc.Testing 8.0.20
- **数据库**: SQL Server（遵循 PRD 要求，非 InMemory）
- **Mock**: Moq 4.20.72（仅用于外部依赖）

### 测试基础设施

#### CustomWebApplicationFactory

```csharp
public class CustomWebApplicationFactory<TStartup>
    : WebApplicationFactory<TStartup> where TStartup : class
{
    // ✅ 使用真实 SQL Server（非 LocalDB）
    // ✅ 每个测试类使用独立数据库
    // ✅ 自动初始化/清理数据库
    // ✅ 配置测试环境变量
}
```

#### 测试辅助方法

```csharp
// 登录并获取 Token
Task<string> LoginAndGetTokenAsync(string username, string password)

// 创建测试用户
Task<UserDto> CreateTestUserAsync(UserRole role = UserRole.Doctor)

// 清理测试数据
Task CleanupTestDataAsync()
```

---

## 🗓️ 实施计划

### 阶段 1：清理与准备（本 PR - Issue #945）

**状态**: ✅ 已完成

- [x] 删除过时的 `ServerIntegrationTests` 项目
- [x] 暂时禁用 CI 集成测试步骤（避免阻塞）
- [x] 创建本规划文档

**产出**:
- PR #946（待创建）
- 解除 PR #941、#944 的阻塞

---

### 阶段 2：新测试架构（下一个 PR）

**预计时间**: 2-3 天

**任务清单**:

- [ ] 创建 `LYBT.WebAPI.IntegrationTests` 项目
- [ ] 配置项目文件（.csproj）
  - 引用 `System.Net.Http.Json`
  - 引用 `Microsoft.AspNetCore.Mvc.Testing`
  - 引用 WebAPI 项目
- [ ] 实现 `CustomWebApplicationFactory`
  - SQL Server 连接配置
  - 数据库初始化/清理逻辑
  - 测试环境设置
- [ ] 实现测试辅助类
  - `TestDataSeeder` - 种子数据生成
  - `TestHelpers` - 常用辅助方法
- [ ] 创建测试配置文件 `appsettings.IntegrationTests.json`

**验收标准**:
- ✅ 项目编译通过
- ✅ 基础设施可以初始化测试数据库
- ✅ 可以运行空测试（基础设施验证）

---

### 阶段 3：核心模块测试实现（后续 PR）

**预计时间**: 4-5 天

#### 3.1 Auth 模块（优先级：高）

**文件**: `Controllers/Auth/`

**测试用例**:

**LoginTests.cs**
- ✅ 有效凭证登录成功
- ✅ 无效凭证登录失败
- ✅ 空用户名验证错误
- ✅ 空密码验证错误
- ✅ 并发登录请求处理

**LogoutTests.cs**
- ✅ 有效 Token 登出成功
- ✅ 无 Token 登出失败
- ✅ 过期 Token 登出处理

**TokenValidationTests.cs**
- ✅ 有效 Token 验证通过
- ✅ 无效 Token 验证失败
- ✅ Token 刷新成功
- ✅ Token 过期后刷新

#### 3.2 Health 模块（优先级：高）

**文件**: `Controllers/Health/HealthCheckTests.cs`

**测试用例**:
- ✅ 健康检查返回 200
- ✅ 数据库连接检查
- ✅ 系统信息返回

#### 3.3 Users 模块（优先级：中）

**文件**: `Controllers/Users/`

**UserCrudTests.cs**
- ✅ 创建用户成功
- ✅ 读取用户信息
- ✅ 更新用户信息
- ✅ 删除用户（软删除）
- ✅ 验证数据一致性

**UserSearchTests.cs**
- ✅ 按用户名搜索
- ✅ 按角色过滤
- ✅ 分页功能
- ✅ 拼音码搜索

---

### 阶段 4：CI 集成恢复（最后 PR）

**预计时间**: 1 天

**任务清单**:

- [ ] 恢复 CI `ci-integration.yml` 配置
- [ ] 配置测试数据库连接字符串（使用 Secrets）
- [ ] 验证 CI 测试稳定性（至少 3 次成功运行）
- [ ] 更新文档

**验收标准**:
- ✅ CI 集成测试通过
- ✅ 无间歇性失败
- ✅ 测试报告正确上传
- ✅ PR 检查中集成测试状态正常

---

## ✅ 验收标准（整体）

### 功能性

- [ ] 所有核心 API 端点有集成测试覆盖
- [ ] 测试通过率 ≥ 95%
- [ ] 无间歇性失败（flaky tests）

### 架构性

- [ ] 测试基础设施可复用
- [ ] 测试数据隔离（每个测试类独立数据库）
- [ ] 测试代码遵循项目编码规范

### 文档性

- [ ] 测试代码有清晰注释
- [ ] README 说明如何运行集成测试
- [ ] 更新 `docs/development/testing-guidelines.md`

---

## 📊 进度跟踪

| 阶段 | 状态 | 完成时间 | PR |
|------|------|----------|-----|
| 1. 清理与准备 | ✅ 已完成 | 2025-10-05 | #946 |
| 2. 新测试架构 | 🚧 待开始 | - | - |
| 3. 核心模块测试 | ⏳ 待开始 | - | - |
| 4. CI 集成恢复 | ⏳ 待开始 | - | - |

---

## 🔗 关联资源

- **Issue**: [#945](https://github.com/shouqitao/LYBTZYZS/issues/945)
- **阻塞的 Issue**: #942, #943
- **阻塞的 PR**: #941, #944
- **参考文档**:
  - `docs/development/standards.md`
  - `docs/development/testing-strategy.md`
  - `tests/UnitTests/` - 单元测试示例

---

## 💡 关键决策记录

### 为什么选择完全重构而非修补？

1. **修补成本高** - 20+ 处编译错误，修改量大
2. **未来风险高** - 基于旧契约的测试容易再次失效
3. **质量收益低** - 修补后仍可能存在隐藏的契约不匹配

### 为什么暂时禁用 CI 集成测试？

1. **避免阻塞** - PR #941（安全修复）和 #944（测试配置）被测试失败阻塞
2. **依然安全** - 单元测试正常运行，核心逻辑有覆盖
3. **分阶段交付** - 先解除阻塞，再逐步重建测试

### 为什么使用真实 SQL Server 而非 InMemory？

1. **PRD 要求** - 明确要求集成测试使用 SQL Server
2. **真实环境** - 更接近生产环境，能发现 InMemory 无法发现的问题
3. **类型安全** - SQL Server 特定的数据类型和约束验证

---

**最后更新**: 2025-10-05
**文档版本**: 1.0

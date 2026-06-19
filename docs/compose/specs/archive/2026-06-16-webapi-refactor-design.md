# WebAPI 重构设计（用户管理 + 双模式 + WPF）

> 日期: 2026-06-16
> 状态: 待审批

## [S1] 目标

重构 WebAPI 的用户管理模块，采用开源方案（ASP.NET Core Identity）替代自研，同时重新设计远程/本地切换逻辑和 WPF 登录体验。

**核心原则**：安全优先，核心是看诊。用户管理是基础设施，不是业务代码。

## [S2] 架构方案：分阶段实施

### 阶段 1：集成 ASP.NET Core Identity（1-2 周）
- Users 模块替换为 Identity（UserManager + SignInManager）
- Auth 模块保留自定义 JWT
- 远程模式：完整 Identity（密码策略、锁定、角色）
- 本地模式：保持简化认证（多账号支持，无锁定/2FA）

### 阶段 2：评估 ABP 框架（1-2 周验证）
- 创建 ABP 项目验证双模式兼容性
- 评估是否值得全面迁移

### 阶段 3：渐进式 ABP 迁移（如可行，3-6 个月）
- 逐模块迁移到 ABP
- 不中断现有业务

## [S3] 双模式架构

### 远程模式（主要生产环境）
- 多用户使用，全部数据
- 完整 Identity（密码策略、锁定、角色、JWT + SecurityStamp）
- 通过 HTTP API 连接远程 SQL Server

### 本地模式（单医生应急）
- 单医生使用，网络不可用时降级
- 简化认证（用户名+密码 → 简化 JWT，1 年有效期）
- 嵌入式 LocalWebAPI + LocalDB
- 数据隔离：每个医生只看到自己的数据

### 模式切换逻辑
- **零配置启动**：自动检测远程可用性
- **透明降级**：网络断开自动切换本地
- **状态显示**：状态栏显示当前模式（远程/本地）
- **手动切换**：设置中可切换模式

### 可用性验证
- 启动时检测：GET /api/v1/health（匿名，超时 3 秒）
- 任何 URL 响应 → 远程模式（选最快的）
- 全部超时 → 本地模式
- 双不可用：优雅降级提示

### 多远程服务器支持
- 支持配置多个远程 URL
- 自动故障转移（主服务器不可用 → 尝试备用）
- 手动切换（设置 → 选择服务器）

## [S4] WPF 前端设计

### 登录流程重设计
- **零配置启动**：自动检测模式
- **单登录界面**：用户名+密码，支持模式切换
- **模式指示器**：显示当前模式（远程/本地）
- **首次使用向导**：引导配置远程 URL

### 配置界面时机
- **登录前**：可配置远程 URL、测试连接
- **登录后（Admin）**：可修改服务器配置
- **登录后（其他）**：只读查看

### Health 端点（无需认证）
- GET /api/v1/health — 匿名
- GET /api/v1/health/ping — 匿名
- 用于登录前的可用性检测

### 最小改动清单
- 需要改：LoginViewModel、UserMasterDetailViewModel、角色常量
- 不需要改：MedicalCaseWorkspace、RegistrationQueue、HerbListControl、PrescriptionPrintService、CardReader、所有 Prism 导航

## [S5] 数据模型

### ApplicationUser（继承 IdentityUser）
```csharp
public class ApplicationUser : IdentityUser<Guid>
{
    public string RealName { get; set; }
    public DateTime? LastLoginAt { get; set; }
}
```

### 角色迁移
- 旧：UserRole enum (Receptionist=0, Doctor=1, Admin=10, SuperAdmin=100)
- 新：IdentityRole (4 个种子角色)

### DbContext
- 旧：AppDbContext : DbContext
- 新：AppDbContext : IdentityDbContext

## [S6] 安全特性（Identity 开箱即用）

- PBKDF2 密码哈希
- 账户锁定（可配置）
- 密码策略
- SecurityStamp（密码变更后失效旧令牌）
- 并发控制

## [S7] 迁移计划

1. 新增 ApplicationUser 实体
2. AppDbContext 改继承 IdentityDbContext
3. EF Migration（生成 AspNet* 表）
4. 数据迁移脚本（旧 Users → AspNetUsers）
5. 替换 UserService → UserManager
6. 更新 AuthController（SignInManager 验证）
7. 更新 UsersController
8. 删除旧 User 模块文件
9. 更新测试

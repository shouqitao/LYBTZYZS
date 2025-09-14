# Roles Unify to Doctor - Inventory Report

生成时间: 2025-09-14 08:50:00  
执行分支: `roles/unify-doctor`  

## 📋 盘点目标

全仓搜索并分类 UserRole、"User"、"Doctor"、"用户"、"医生"、"管理员" 等角色相关引用，为统一角色命名为 Doctor 做准备。

## 🔍 关键发现

### 🎯 核心枚举定义

**主要枚举定义位置**: `src/Shared/LYBT.Shared.Models/Enums/AuthEnums.cs:179`

```csharp
public enum UserRole
{
    /// <summary>管理员（系统管理、用户管理、系统配置）</summary>
    [Description("管理员")]
    Admin = 10,

    /// <summary>普通用户（诊疗、记录、查询等业务操作）</summary>
    [Description("普通用户")]
    User = 20,

    // 兼容性映射：旧角色保留以避免序列化错误，但标记为过时
    
    /// <summary>医生 - 已合并到User角色</summary>
    [Description("医生")]
    [Obsolete("Use User instead. Doctor role merged into User in Record-Only mode.", false)]
    Doctor = 1,

    /// <summary>药师 - 已合并到User角色</summary>
    [Description("药师")]
    [Obsolete("Use User instead. Pharmacist role merged into User in Record-Only mode.", false)]
    Pharmacist = 2,
    // ... 其他已过时角色
}
```

### 📊 搜索结果统计

| 类别 | 数量 | 主要位置 |
|------|------|----------|
| **UserRole枚举引用** | 150+ | 全项目 |
| **"User"字符串** | 45+ | Controllers, Services, ViewModels |
| **"Doctor"字符串** | 35+ | Controllers, Services, Extensions |
| **"Admin"字符串** | 30+ | Controllers, Authorization |
| **"用户"中文** | 20+ | UI, Converters, Extensions |
| **"医生"中文** | 15+ | UI, Display Names |
| **"管理员"中文** | 10+ | UI, Display Names |
| **[Authorize]特性** | 8个 | Controllers (仅基础Authorize，无Roles参数) |

## 🔍 详细分类分析

### 1. 枚举和常量定义

#### ✅ 核心枚举 (需要修改的)
- `src/Shared/LYBT.Shared.Models/Enums/AuthEnums.cs:179` - **主要目标**
- `src/Server/Core/LYBT.Entities/Users/UserModel.cs:53` - 默认值设定

#### 📝 文档中的枚举定义 (仅更新文档)
- 多个README和文档文件中的示例代码

### 2. 授权和认证系统

#### 🔐 JWT 和 Claims 处理
- `src/Server/Modules/LYBT.Module.Auth/Services/JwtAuthenticationService.cs:94,114`
  ```csharp
  var roleString = principal.FindFirst(ClaimTypes.Role)?.Value ?? "Doctor";
  ```
  **建议**: 保持 "Doctor" 作为默认值，符合统一目标

#### 🚫 Authorize 特性 (无需修改)
所有控制器使用基础 `[Authorize]`，**无角色限制**：
- UsersController, PrescriptionsController, PatientsController 等
- **无需修改** - 没有发现 `[Authorize(Roles="...")]` 的使用

### 3. 业务逻辑层

#### 🔧 字符串比较和角色判断
需要修改的关键位置：

**后端 Controllers**:
- `src/Server/Services/LYBT.WebAPI/Controllers/UsersController.cs:193,265-266`
  ```csharp
  Role = "Admin",  // 硬编码示例数据
  new { Value = "Admin", Label = "管理员" },
  new { Value = "Doctor", Label = "医生" },
  ```

**前端 ViewModels**:
- `src/Client/Desktop/Shell/ViewModels/MainWindowViewModel.cs:406,465,470,475`
  ```csharp
  CurrentUser.Role?.Equals("Admin", StringComparison.OrdinalIgnoreCase)
  await app.LoadRoleBasedModulesAsync("Admin");
  await app.LoadRoleBasedModulesAsync("Doctor");
  ```

**前端 Services**:
- `src/Client/Desktop/Shell/App.xaml.cs:282-294` - 模块注册
  ```csharp
  ["Doctor", "Admin"]  // 各种模块的角色限制
  ```

#### 🔄 角色映射和转换
- `src/Shared/LYBT.Shared.Models/Extensions/UserRoleExtensions.cs:22-34` - 角色简化映射
- `src/Client/Desktop/Workbenches/Core/WorkbenchRouter.cs:660-661` - 中英文角色映射
  ```csharp
  "管理员" => UserRole.Admin,
  "医生" or "用户" => UserRole.Doctor, // 关键映射点
  ```

### 4. 用户界面和显示

#### 🎨 显示名称和本地化
- `src/Client/Desktop/Services/PermissionService.cs:130-135` - 角色显示名称
- `src/Client/Desktop/Core/Converters/UserRoleDisplayConverter.cs:24-26,45` - 角色转换器
- `src/Client/Desktop/Workbenches/Core/WorkbenchRouter.cs:641-647` - 工作台角色显示

### 5. 数据层和种子数据

#### 🌱 默认值和种子数据
- `src/Server/Core/LYBT.Entities/Users/UserModel.cs:53`
  ```csharp
  public UserRole Role { get; set; } = UserRole.Doctor; // 默认值
  ```
- `src/Server/Modules/LYBT.Module.Auth/Interfaces/IJwtAuthenticationService.cs:46`
  ```csharp
  public UserRole Role { get; set; } = UserRole.Doctor; // DTO默认值
  ```

#### 💾 DTO 和数据传输
- `src/Shared/LYBT.Shared.Models/Contracts/Users/UserDtos.cs:28,85`
  ```csharp
  public string Role { get; set; } = "User"; // 注意：字符串形式
  ```

## 🎯 规划建议

### 🟢 第一优先级 - 核心定义修改
1. **UserRole 枚举重新定义**:
   - 将 `Doctor = 1` 提升为主要角色
   - 将 `User = 20` 标记为过时并映射到 Doctor
   - 保持 `Admin = 10` 不变

2. **默认值统一**:
   - 所有默认值改为 `UserRole.Doctor`
   - DTO 字符串默认值改为 `"Doctor"`

### 🟡 第二优先级 - 业务逻辑调整  
1. **字符串比较替换**:
   - `"User"` 检查 → `"Doctor"` 检查
   - 保持向后兼容的映射逻辑

2. **JWT Claims 标准化**:
   - 登录时将 "User" Claims 规范化为 "Doctor"
   - 保留原始值到 LegacyRole 用于审计

### 🟠 第三优先级 - 用户界面更新
1. **显示名称统一**:
   - "普通用户" → "医生"
   - 保持中文显示的一致性

2. **工作台和权限映射**:
   - "用户" 工作台映射 → "医生" 工作台

## 📋 变更清单 CSV

基于盘点结果，识别需要变更的关键位置：

| Path | Symbol | Kind | From | To | Reason |
|------|--------|------|------|----|---------| 
| AuthEnums.cs:184,188 | UserRole enum | Enum | Admin=10, User=20 | Admin=10, Doctor=1 | 统一主角色为Doctor |
| UserModel.cs:53 | Role default | Property | UserRole.Doctor | UserRole.Doctor | 保持不变(已正确) |
| UserDtos.cs:28,85 | Role default | Property | "User" | "Doctor" | 字符串默认值统一 |
| JwtAuthenticationService.cs:94,114 | Role fallback | Logic | ?? "Doctor" | ?? "Doctor" | 保持不变(已正确) |
| UsersController.cs:265-266 | Role options | Data | Value="Doctor" | Value="Doctor" | 保持不变(已正确) |
| MainWindowViewModel.cs:470,475 | LoadRoleBasedModules | Method | "Admin","Doctor" | "Admin","Doctor" | 保持不变 |
| WorkbenchRouter.cs:661 | Role mapping | Logic | "用户" => Doctor | "用户" => Doctor | 保持不变(已合理) |
| UserRoleExtensions.cs:26-30 | ToBasicRole | Method | Doctor => User | Doctor => Doctor | 移除简化映射 |
| PermissionService.cs:131 | GetRoleDisplayName | Method | Doctor => "医生" | Doctor => "医生" | 保持不变 |

## 🎯 总结

### 关键发现
1. **已有良好基础**: 系统大部分地方已经使用 `UserRole.Doctor` 作为默认值
2. **主要问题**: `UserRole.User = 20` 作为"普通用户"与实际业务不符
3. **向后兼容**: 现有的 `[Obsolete]` Doctor 定义需要反转
4. **影响范围可控**: 大部分代码已经在使用正确的角色逻辑

### 建议方案
将 `UserRole.Doctor` 从过时状态恢复为主要角色，将 `UserRole.User` 标记为过时并映射到 Doctor，最小化代码修改影响面。

### 风险评估
- **🟢 低风险**: 数据库不需要修改，枚举值保持兼容
- **🟡 中风险**: JWT Claims 需要规范化处理  
- **🟢 低风险**: UI 显示逻辑已经基本正确
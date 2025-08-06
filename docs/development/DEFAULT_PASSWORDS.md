# 系统默认密码配置

## 配置文件位置
`src/Backend/Services/LYBT.WebAPI/appsettings.json`

## 默认密码设置

### 1. 普通用户默认密码
- **配置项**: `UserOptions.DefaultUserPassword`
- **默认值**: `ChangeMe123`
- **用途**: 创建新用户时的默认密码

### 2. 系统管理员默认密码
- **配置项**: `SysAdminOptions.DefaultPassword`
- **默认值**: `Admin@123456`
- **用途**: 系统管理员账号的默认密码

## 在代码中使用默认密码

### 创建用户时使用默认密码示例

```csharp
// 从配置中读取默认密码
var defaultPassword = _configuration["UserOptions:DefaultUserPassword"] ?? "ChangeMe123";

// 使用PasswordHasher生成密码哈希
var passwordHasher = new PasswordHasher<object>();
var passwordHash = passwordHasher.HashPassword(null, defaultPassword);
```

### SQL脚本创建用户示例

```sql
-- 使用默认密码 ChangeMe123 的哈希值
-- 注意：这个哈希值是示例，实际使用时需要重新生成
INSERT INTO Users (
    Id, 
    UserName, 
    PasswordHash, 
    RealName, 
    Role, 
    IsActive
) VALUES (
    NEWID(),
    'newuser',
    'AQAAAAIAAYagAAAAE...', -- ChangeMe123 的哈希值
    '新用户',
    0,
    1
)
```

## 测试账号密码汇总

### 系统内置账号
- **系统管理员**: `sysadmin` / `Admin@123456`

### 测试账号（已创建）
- **前台人员1**: `frontdesk` / `Front@123456`
- **前台人员2**: `reception` / `Front@123456`
- **医生1**: `doctor1` / `Front@123456`
- **医生2**: `doctor2` / `Front@123456`

## 密码策略

### 密码要求
- 最小长度：6个字符
- 必须包含：大写字母、小写字母、数字或特殊字符

### 安全设置
- **最大失败登录次数**: 5次（`AuthOptions.MaxFailedLoginAttempts`）
- **账号锁定时长**: 15分钟（`AuthOptions.AccountLockoutDuration`）
- **首次登录要求修改密码**: 是（`SysAdminOptions.RequirePasswordChangeOnFirstLogin`）

## 注意事项

1. **生产环境**: 必须修改所有默认密码
2. **密码哈希**: 使用 ASP.NET Core Identity 的 PasswordHasher
3. **密码更新**: 用户应在首次登录后立即修改默认密码
4. **安全存储**: 不要在代码中硬编码密码，始终从配置文件读取

## 生成密码哈希的方法

### C# 代码生成
```csharp
using Microsoft.AspNetCore.Identity;

var passwordHasher = new PasswordHasher<object>();
var hash = passwordHasher.HashPassword(null, "ChangeMe123");
Console.WriteLine(hash);
```

### 使用已有的哈希值（仅用于测试）
- `ChangeMe123`: `AQAAAAIAAYagAAAAEKFcV+rEOz3qY7KMwU8GmDF0NXBkC2PwMqPc7WaJYYqH0YJpNdxL5BqMUk0cFGV3uw==`
- `Admin@123456`: `AQAAAAIAAYagAAAAEBZtKH/jLrWSCIstrn4KyQtIopjqYQNrjJ8ZTIZxjKrpJ1l0obDU19hLQMSNwBjbeQ==`
- `Front@123456`: `AQAAAAIAAYagAAAAEPxjZQ6uXz1vIpH5kB9HgT9S2JO9bvHmzUAX8Yl+7Yx3hKQNMJ0RKP4ZvN6HzxVxVg==`

**警告**: 这些哈希值仅供开发测试使用，生产环境必须重新生成！
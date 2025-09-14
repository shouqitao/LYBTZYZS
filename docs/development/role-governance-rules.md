# 角色管理治理规则

**生效日期**: 2025-09-14  
**适用范围**: 凌隐宝堂中医诊所系统全体开发人员  
**更新频率**: 按需更新，重大变更需团队评审  

## 🎯 核心原则

### 唯一正源原则
- ✅ **DO**: 使用 `RoleConstants.Doctor` 和 `RoleConstants.Admin`
- ❌ **DON'T**: 硬编码 `"Doctor"`, `"Admin"`, `"User"` 字符串

### 向后兼容原则
- ✅ **DO**: 保持 `UserRole.User` 的 `[Obsolete]` 标记
- ❌ **DON'T**: 移除或修改 `RoleConstants.RoleMapping`

## 📋 开发检查清单

### 代码编写时
- [ ] 使用 `RoleConstants.*` 而不是硬编码字符串
- [ ] 新增角色相关功能时调用 `ToUnifiedRole()` 方法
- [ ] API响应只返回 `Admin` 或 `Doctor`，不返回 `User`

### 代码审查时
- [ ] 搜索硬编码角色字符串: `"Admin"`, `"Doctor"`, `"User"`
- [ ] 验证角色枚举使用是否调用了统一化方法
- [ ] 确认API契约未破坏向后兼容性

### 测试验证时  
- [ ] 角色列表API返回 `['Admin', 'Doctor']`
- [ ] JWT Token中角色信息正确
- [ ] 编译无UserRole.User过时警告

## 🚨 禁止操作

### 绝对禁止
- ❌ 重新启用 `UserRole.User` 作为主要角色
- ❌ 删除 `RoleConstants.RoleMapping` 兼容映射  
- ❌ 在API响应中返回 `"User"` 角色值
- ❌ 硬编码角色字符串到业务逻辑中

### 谨慎操作
- ⚠️ 修改 `UserRole` 枚举值（需团队评审）
- ⚠️ 变更 `ClaimsNormalizer` 转换逻辑（需充分测试）
- ⚠️ 移除 `[Obsolete]` 标记（需评估影响范围）

## 🔧 常用代码模式

### ✅ 正确用法

```csharp
// 1. 角色常量使用
using LYBT.Infrastructure.Authorization;
var doctorRole = RoleConstants.Doctor;
var adminRole = RoleConstants.Admin;

// 2. 角色统一化  
var userRole = UserRole.User; // legacy input
var unifiedRole = userRole.ToUnifiedRole(); // -> UserRole.Doctor

// 3. API响应
var roles = new[] {
    new { Value = RoleConstants.Admin, Label = "管理员" },
    new { Value = RoleConstants.Doctor, Label = "医生" }
};

// 4. 权限检查
[Authorize(Roles = RoleConstants.Doctor + "," + RoleConstants.Admin)]
public async Task<ActionResult> SomeAction() { }
```

### ❌ 错误用法

```csharp
// 1. 硬编码字符串 - 禁止
var role = "Doctor";  // ❌ 
var adminCheck = user.Role == "Admin";  // ❌

// 2. 直接使用User枚举值 - 禁止
var defaultRole = UserRole.User;  // ❌

// 3. API返回User角色 - 禁止  
return Ok(new { Role = "User" });  // ❌
```

## 📊 监控指标

### 日常监控
- UserRole.User 使用次数（目标：趋向0）
- ClaimsNormalizer 转换活动（legacy支持）
- 角色相关编译警告数量（目标：0）

### 定期审查
- **1个月**: 检查生产环境角色使用情况
- **6个月**: 评估是否可以移除obsolete警告  
- **12个月**: 计划完全移除UserRole.User枚举值

## 🆘 应急处理

### 发现违规代码
1. 立即记录违规位置和类型
2. 评估影响范围（生产/开发环境）
3. 创建修复任务，按优先级处理
4. 更新代码审查检查清单

### 需要回滚时
1. 切换到 `master` 分支
2. 运行冒烟测试验证系统功能
3. 通知相关团队和用户
4. 制定重新实施计划

---

**规则制定**: Claude Code Assistant  
**最后更新**: 2025-09-14  
**下次审查**: 2025-12-14 或重大系统变更时
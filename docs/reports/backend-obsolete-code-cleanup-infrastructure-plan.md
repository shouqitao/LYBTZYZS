# LYBT.Infrastructure 过时代码清理计划

## 项目概览
- **项目名**: LYBT.Infrastructure (基础设施)
- **路径**: `src/Server/Core/LYBT.Infrastructure`
- **类型**: 基础设施和共享组件
- **当前状态**: 核心基础设施，包含过时配置选项

## 过时代码识别结果

### 1. 配置选项过时问题

#### SysAdminOptions.cs
- **行16**: `[Obsolete("请使用 DefaultPasswordOptions.SystemAdmin 替代", true)]`
- **行17-19**: DefaultPassword 属性标记为过时但仍有实现
  - **问题**: 过时属性仍然存在且有默认值
  - **影响**: 可能导致配置混乱，应该完全移除

#### UserOptions.cs  
- **行15**: `[Obsolete("请使用 DefaultPasswordOptions.NewUser 替代", true)]`
- **行16**: DefaultUserPassword 属性标记为过时但仍有实现
  - **问题**: 与SysAdminOptions相同问题
  - **影响**: 配置重复和混乱

### 2. 角色常量问题

#### RoleConstants.cs
- **行27**: `[Obsolete("请使用 Doctor 角色。User 角色已统一为 Doctor 角色。", false)]`
- **行28**: `public const string User = "User";`
- **行38**: AllRoles 数组仍包含过时的 User 角色
  - **问题**: 过时角色仍在系统中被引用
  - **影响**: 可能导致权限管理混乱

### 3. 数据库迁移过时问题

#### AddTransactionCoordinatorTables.cs
- **行9**: `[Obsolete("Complex transaction coordination tables removed in Record-Only mode.")]`
  - **问题**: 过时的迁移文件仍然存在
  - **影响**: 数据库迁移历史混乱

### 4. 仓储层过时问题

#### OptimizedBaseRepository.cs
- **文件存在**: 可能是重复或优化版本的仓储实现
  - **需要验证**: 是否与标准BaseRepository重复
  - **清理建议**: 统一仓储实现

## 清理优先级

### 高优先级 (立即清理)

1. **删除过时配置属性**
   - SysAdminOptions.DefaultPassword
   - UserOptions.DefaultUserPassword
   - 这些属性标记为 `Obsolete(true)` 应该完全移除

2. **清理角色常量定义**
   - 考虑是否完全移除 User 常量
   - 更新 AllRoles 数组移除过时角色

### 中优先级 (计划清理)

1. **数据库迁移文件**
   - 评估是否可以安全删除过时迁移
   - 考虑迁移历史完整性

2. **仓储实现统一**
   - 检查 OptimizedBaseRepository 与标准实现的关系
   - 统一仓储接口

### 低优先级 (可选清理)

1. **配置选项简化**
   - 评估UserOptions中的复杂配置是否都需要
   - 考虑小型诊所的实际需求

## 具体清理行动

### 第一阶段: 过时配置清理

#### SysAdminOptions.cs 清理
```csharp
public class SysAdminOptions
{
    public const string SectionName = "SysAdminOptions";

    // 删除过时的 DefaultPassword 属性
    // [Obsolete] public string DefaultPassword { get; set; }

    /// <summary>
    /// 是否要求首次登录时更改密码
    /// </summary>
    public bool RequirePasswordChangeOnFirstLogin { get; set; } = true;

    /// <summary>
    /// 是否启用账户锁定
    /// </summary>
    public bool EnableAccountLockout { get; set; } = false;
}
```

#### UserOptions.cs 清理
```csharp
public class UserOptions
{
    public const string SectionName = "UserOptions";

    // 删除过时的 DefaultUserPassword 属性
    // [Obsolete] public string DefaultUserPassword { get; set; }

    // 保留其他有效配置...
}
```

### 第二阶段: 角色常量清理

#### RoleConstants.cs 更新策略
```csharp
// 选项1: 完全移除 User 角色（推荐）
public static readonly string[] ValidRoles = { Admin, Doctor };
public static readonly string[] AllRoles = { Admin, Doctor }; // 移除 User

// 选项2: 保留兼容性（如果系统中仍有引用）
// 保持当前实现，但增加弃用警告
```

### 第三阶段: 数据库迁移清理

#### 迁移文件评估
- **检查依赖**: 确认没有其他迁移依赖过时的迁移
- **备份策略**: 在删除前确保有完整备份
- **渐进清理**: 先标记，后删除

## 影响评估

### 风险评估: 中等

#### 高风险项
- **配置属性删除**: 可能影响现有配置文件
- **角色常量变更**: 可能影响权限检查逻辑

#### 低风险项
- **迁移文件标记**: 不影响运行时
- **仓储统一**: 主要影响代码结构

### 测试要求

1. **配置测试**
   - 验证新的 DefaultPasswordOptions 工作正常
   - 确认过时配置不再被使用

2. **权限测试**
   - 验证角色检查逻辑正常工作
   - 确认 User -> Doctor 映射正常

3. **数据库测试**
   - 验证迁移可以正常运行
   - 确认过时表不影响系统

## 清理时间表

### 第1周: 配置选项清理
- 删除过时的密码配置属性
- 更新相关的配置绑定代码

### 第2周: 角色常量清理  
- 评估系统中 User 角色的使用情况
- 逐步替换或移除 User 角色引用

### 第3周: 验证和测试
- 全面测试配置和权限功能
- 确保系统稳定运行

## 结论

LYBT.Infrastructure项目的过时代码主要集中在：

1. **配置选项重复** - 需要清理过时的密码配置
2. **角色定义混乱** - 需要统一角色常量
3. **迁移历史复杂** - 需要清理过时迁移

建议优先处理配置选项清理，这是最直接和安全的改进。角色常量的清理需要更加谨慎，需要先评估系统中的使用情况。
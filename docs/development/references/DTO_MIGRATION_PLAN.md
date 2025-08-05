# DTO迁移计划

## 问题分析

### 当前状态（不规范）
```
LYBT.Models/Users/Dtos/          ❌ 错误位置
├── ChangePasswordDto.cs
├── ChangeProfileDto.cs
├── ResetPasswordDto.cs
├── UserDetailDto.cs
├── UserQueryDto.cs
├── UserCreateDto.cs         (重复)
├── BatchIdsDto.cs
└── UserDto.cs               (重复)

LYBT.Shared.Models/Contracts/Users/  ✅ 正确位置
├── UserPagedQueryDto.cs
├── UserDto.cs
├── UserUpdateDto.cs
└── UserCreateDto.cs
```

### 目标状态（规范）
```
LYBT.Models/Users/               ✅ 只包含Entity
└── UserModel.cs

LYBT.Shared.Models/Contracts/Users/  ✅ 包含所有DTO
├── UserDto.cs               (列表显示)
├── UserDetailDto.cs         (详细信息)
├── UserCreateDto.cs         (创建用户)
├── UserUpdateDto.cs         (更新用户)
├── UserPagedQueryDto.cs     (分页查询)
├── ChangePasswordDto.cs     (修改密码)
├── ChangeProfileDto.cs      (修改资料)
├── ResetPasswordDto.cs      (重置密码)
└── BatchIdsDto.cs          (批量操作)
```

## 迁移步骤

### 第一步：检查重复和冲突
1. `UserDto.cs` - 两边都有，需要合并
2. `UserCreateDto.cs` - 两边都有，需要合并
3. `UserDetailDto.cs` - 只在Models中，需要迁移
4. 其他DTO - 直接迁移

### 第二步：迁移非冲突DTO
```bash
# 需要迁移的文件
- ChangePasswordDto.cs
- ChangeProfileDto.cs
- ResetPasswordDto.cs
- UserDetailDto.cs
- UserQueryDto.cs → 改名为 UserSearchDto.cs（避免与PagedQueryDto混淆）
- BatchIdsDto.cs → 考虑改为通用的 BatchOperationDto.cs
```

### 第三步：更新引用
1. 更新所有Service中的using语句
2. 更新Controller中的引用
3. 更新AutoMapper配置

### 第四步：删除旧文件
删除 `LYBT.Models/Users/Dtos/` 整个文件夹

## 其他模块检查

需要检查的其他模块：
- Patients/Dtos/
- Doctors/Dtos/
- Herbs/Dtos/
- Prescriptions/Dtos/
- Billing/Dtos/
- Registration/Dtos/
- 等等...

## 注意事项

### 1. 渐进式迁移
- 不要一次性迁移所有模块
- 先迁移一个模块，测试通过后再继续
- 保持系统稳定运行

### 2. 向后兼容
- 可以先保留旧的DTO，添加过时标记
- 给前端足够的时间适配
- 确保API不会突然中断

### 3. 命名规范
```csharp
// 共享DTO命名规范
[Entity]Dto              // 列表显示
[Entity]DetailDto        // 详细信息
[Entity]CreateDto        // 创建
[Entity]UpdateDto        // 更新
[Entity]PagedQueryDto    // 分页查询
[Entity][Action]Dto      // 特定操作（如ChangePasswordDto）
```

## 实施建议

1. **优先级**：中等
   - 不影响功能，但影响代码组织
   - 建议在下次大版本更新时执行

2. **风险评估**：低
   - 只是文件位置变化
   - 不涉及业务逻辑修改

3. **测试要求**：
   - 编译通过
   - 所有API测试通过
   - 前端功能正常
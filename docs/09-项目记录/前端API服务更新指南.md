# 前端API服务更新指南

## 概述

基于后端控制器接口优化，前端API服务需要进行相应更新，以确保前后端接口调用的一致性。

**更新状态**: ✅ 已完成主要更新（2025年8月7日）

## 更新清单

### 1. IUserApiService 接口更新

**已完成的更新**：
- ✅ `GetPagedUsersAsync` → `GetUsersAsync` (从POST改为GET方法)
- ✅ 移除 `GetByIdAsync` (使用 `GetUserByIdAsync`)
- ✅ `CreateUserAsync` 路由从 `/add` 改为 `/`
- ✅ `UpdateUserAsync` 路由从 `/update` 改为 `/{id}`，增加id参数
- ✅ 移除 `DisableUserAsync` 和 `EnableUserAsync` (使用 `ToggleStatusAsync`)

**需要更新的服务调用**：
```csharp
// UserService.cs 中需要更新：
// 1. GetPagedUsersAsync 改为 GetUsersAsync
var response = await _userApiService.GetUsersAsync(
    page: request.CurrentPage,
    pageSize: request.PageSize,
    keyword: request.SearchKeyword
);

// 2. UpdateUserAsync 需要传入ID参数
await _userApiService.UpdateUserAsync(dto.Id, dto);

// 3. DisableUserAsync/EnableUserAsync 改为 ToggleStatusAsync
await _userApiService.ToggleStatusAsync(userId);
```

### 2. IPatientsApiService 接口更新

**已完成的更新**：
- ✅ 移除 `AddAsync` (使用 `CreatePatientAsync`)
- ✅ 移除 `EnableAsync` 和 `DisableAsync` (使用 `ToggleStatusAsync`)
- ✅ 移除 `GetPagedAsync` (使用 `GetPatientsAsync`)
- ✅ 移除 `BatchDisableAsync` 和 `BatchEnableAsync` (未实现功能)
- ✅ 移除 `ImportAsync` (未实现功能)
- ✅ 移除 `GetHistoryAsync` (未实现功能)

**需要更新的服务调用**：
```csharp
// PatientService.cs 中需要更新：
// 1. AddAsync 改为 CreatePatientAsync
await _patientApiService.CreatePatientAsync(dto);

// 2. GetPagedAsync 改为 GetPatientsAsync  
var response = await _patientApiService.GetPatientsAsync(
    page: query.CurrentPage,
    pageSize: query.PageSize,
    keyword: query.SearchKeyword
);

// 3. EnableAsync/DisableAsync 改为 ToggleStatusAsync
await _patientApiService.ToggleStatusAsync(patientId);
```

### 3. IHerbApiService 接口更新

**已完成的更新**：
- ✅ 移除 `GetPagedHerbsAsync` (使用 `GetHerbsAsync`)
- ✅ `GetHerbsAsync` 返回类型从 List 改为 PaginatedResult
- ✅ `UpdateHerbAsync` 路由从 `/` 改为 `/{id}`，增加id参数
- ✅ 移除 `DeleteHerbAsync` (使用软删除策略)
- ✅ `UpdateStatusAsync` 从PUT改为PATCH，路由调整
- ✅ 新增 `ToggleStatusAsync` 方法

**需要更新的服务调用**：
```csharp
// HerbService.cs 中需要更新：
// 1. GetPagedHerbsAsync 改为 GetHerbsAsync
var response = await _herbApiService.GetHerbsAsync(
    page: query.CurrentPage,
    pageSize: query.PageSize,
    keyword: query.SearchKeyword
);

// 2. UpdateHerbAsync 需要传入ID参数
await _herbApiService.UpdateHerbAsync(dto.Id, dto);

// 3. DeleteHerbAsync 改为 ToggleStatusAsync
await _herbApiService.ToggleStatusAsync(herbId);
```

### 4. IMedicalCaseApiService 接口更新

**已完成的更新**：
- ✅ `UpdateAsync` 路由从 `/` 改为 `/{id}`，增加id参数

**需要更新的服务调用**：
```csharp
// MedicalCaseService.cs 中需要更新：
// UpdateAsync 需要传入ID参数
await _medicalCaseApiService.UpdateAsync(dto.Id, dto);
```

### 5. IPrescriptionApiService 接口更新

**已完成的更新**：
- ✅ 移除 `GetPagedListAsync` (使用 `GetListAsync`)
- ✅ `UpdatePrescriptionAsync` 路由从 `/` 改为 `/{id}`，增加id参数

**需要更新的服务调用**：
```csharp
// PrescriptionService.cs 中需要更新（如果存在）：
// 1. GetPagedListAsync 改为 GetListAsync
var response = await _prescriptionApiService.GetListAsync(
    page: query.CurrentPage,
    pageSize: query.PageSize
);

// 2. UpdatePrescriptionAsync 需要传入ID参数
await _prescriptionApiService.UpdatePrescriptionAsync(dto.Id, dto);
```

## 通用更新模式

### 1. 分页查询统一化
- 移除所有 `POST /paged` 接口
- 统一使用 `GET /` 接口，通过Query参数传递查询条件

### 2. RESTful路由规范化
- 创建：`POST /` (移除 `/add`)
- 更新：`PUT /{id}` (移除 `/update`)
- 状态管理：`PATCH /{id}/toggle-status` (移除单独的enable/disable)

### 3. 软删除策略
- 移除所有物理删除接口
- 使用状态切换代替删除操作

## 实施建议

### 第一步：更新接口定义
1. 按照上述清单更新所有API接口定义
2. 确保Refit属性正确（路由、HTTP方法、参数）

### 第二步：更新服务实现
1. 查找所有使用旧接口的服务类
2. 更新方法调用，传递正确的参数
3. 处理返回值类型的变化

### 第三步：更新视图模型
1. 检查ViewModel中的API调用
2. 更新分页查询逻辑
3. 更新状态管理逻辑

### 第四步：测试验证
1. 运行所有受影响的功能
2. 验证分页查询是否正常
3. 验证CRUD操作是否正常
4. 验证状态切换是否正常

## 注意事项

1. **向后兼容性**：某些接口提供了别名方法，可以暂时保持兼容
2. **错误处理**：更新后需要测试错误处理逻辑是否正常
3. **性能考虑**：GET方法的查询参数可能影响缓存策略
4. **安全性**：确保敏感操作仍然有适当的权限控制

## 后续工作

1. 更新单元测试以反映新的API调用方式
2. 更新API文档和开发指南
3. 考虑添加API版本管理策略
4. 监控生产环境的API调用情况
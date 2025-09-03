# UltraThink 双层架构标准

## 架构设计原则

### 双层架构模式
```
Module (纯委托层)
    ├── QueryService (查询专业层) - 所有GET操作
    └── BusinessService (业务逻辑层) - 所有POST/PUT/DELETE操作
```

## 层次职责定义

### 1. QueryService层 - 查询专业化
- **职责**: 分页查询、搜索、筛选、统计
- **对应**: 所有GET端点
- **命名**: I{Module}QueryService / {Module}QueryService
- **方法示例**:
  ```csharp
  Task<ServiceResult<PagedResult<TDto>>> GetPagedAsync(TQueryDto query)
  Task<ServiceResult<TDto>> GetByIdAsync(Guid id)
  Task<ServiceResult<List<TDto>>> SearchAsync(string keyword)
  Task<ServiceResult<List<TDto>>> GetActiveAsync()
  Task<ServiceResult<TStatisticsDto>> GetStatisticsAsync()
  ```

### 2. BusinessService层 - 业务逻辑和CRUD
- **职责**: 创建、更新、删除、业务流程、验证
- **对应**: 所有POST/PUT/DELETE/PATCH端点
- **命名**: I{Module}BusinessService / {Module}BusinessService  
- **方法示例**:
  ```csharp
  Task<ServiceResult<TDto>> CreateAsync(TCreateDto createDto)
  Task<ServiceResult<TDto>> UpdateAsync(Guid id, TUpdateDto updateDto)
  Task<ServiceResult<bool>> DeleteAsync(Guid id)
  Task<ServiceResult<bool>> ToggleStatusAsync(Guid id)
  Task<ServiceResult<TDto>> CopyAsync(Guid id, string newName)
  ```

### 3. Module层 - 纯委托模式
- **职责**: 统一服务入口，请求路由分发
- **接口**: 实现 I{Module}Service 
- **模式**: 无业务逻辑，纯粹委托调用
- **方法示例**:
  ```csharp
  public async Task<ServiceResult<TDto>> CreateAsync(TCreateDto createDto)
      => await _businessService.CreateAsync(createDto);
      
  public async Task<ServiceResult<PagedResult<TDto>>> GetPagedAsync(TQueryDto query)
      => await _queryService.GetPagedAsync(query);
  ```

## 后端API契约一致性

### API端点映射规则
1. **GET** → QueryService
2. **POST/PUT/DELETE/PATCH** → BusinessService  
3. **不存在的端点** → 删除对应前端方法

### 标准DTO映射
- **PagedQueryDto**: 分页查询参数
- **CreateDto**: 创建实体参数
- **UpdateDto**: 更新实体参数
- **ServiceResult<T>**: 统一返回格式

## 实施清单

### ✅ 必须遵循
1. **移除CoreService层** - 合并到BusinessService
2. **统一接口命名** - I{Module}QueryService, I{Module}BusinessService
3. **删除无对应端点的方法** - 清理过度开发
4. **使用C# 12主构造函数** - 现代化语法
5. **统一异常处理** - ServiceResult模式

### ❌ 禁止行为
1. 混合使用直接API客户端和三层架构
2. 在Module层包含业务逻辑
3. 实现后端不支持的方法
4. 使用不一致的接口命名

## 模块清单

需要统一重构的8个模块：
- ✅ **Prescriptions**: 已符合标准 (参考模板)
- 🔄 **Users**: 需要从直接API模式转换
- 🔄 **Auth**: 需要简化复杂三层架构  
- 🔄 **Herbs**: 需要移除CoreService层
- 🔄 **Formula**: 需要统一接口
- 🔄 **Patients**: 需要检查一致性
- 🔄 **MedicalCase**: 需要检查一致性  
- 🔄 **Consultation**: 需要检查一致性

## 成功标准

1. **编译通过**: 0个编译错误
2. **接口一致**: 所有模块使用相同模式
3. **契约匹配**: 前端方法与后端端点一一对应
4. **代码精简**: 平均减少50%+冗余代码
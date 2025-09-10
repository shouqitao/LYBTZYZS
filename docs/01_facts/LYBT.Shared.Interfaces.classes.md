# LYBT.Shared.Interfaces 类和方法文档

> **版本**: 2.1.0-interfaces-enterprise  
> **生成日期**: 2025-09-10  
> **模块**: 共享接口契约层  
> **架构**: UltraThink双层架构支持  

## 📋 项目概述和定位

**项目名称**: LYBT.Shared.Interfaces  
**主要职责**: 前后端统一接口契约层，确保API调用的类型安全和一致性  
**技术定位**: 基于Refit的强类型REST客户端接口定义  
**架构价值**: UltraThink双层架构的标准接口支持，企业级契约管理

### 技术栈详情
- **目标框架**: .NET 8.0
- **C#语言版本**: 12.0 (现代化语法)
- **核心依赖**: Refit 8.0.0 (类型安全REST客户端)
- **设计特性**: Nullable引用类型，XML文档生成

## 🏗️ 接口分类架构

### 1. API客户端接口层 (8个核心模块)
**位置**: `Api/` 目录  
**用途**: 前端调用后端Web API的Refit客户端接口定义

### 2. 业务服务接口层 (8个核心模块)  
**位置**: `Services/` 目录  
**用途**: UltraThink双层架构的统一服务接口定义

### 3. 缓存服务接口层 (1个专业模块)
**位置**: `Caching/` 目录  
**用途**: 简化缓存服务的标准接口定义

## 🔌 API客户端接口详细分析

### 1. IAuthApi - 身份认证API客户端
**源码位置**: `Api/IAuthApi.cs`  
**功能范围**: JWT身份认证、会话管理、密码操作、健康检查  
**安全特性**: JWT Bearer Token认证、8小时过期、Remember Me 30天

#### 核心方法清单
| 方法签名 | 返回类型 | 用途 | HTTP方法 |
|---------|----------|------|----------|
| `LoginAsync([Body] LoginRequest)` | `Task<ApiResponse<LoginResponse>>` | 用户登录认证 | POST |
| `LogoutAsync()` | `Task<ApiResponse<object>>` | 用户登出操作 | POST |
| `GetCurrentUserAsync()` | `Task<ApiResponse<UserDto>>` | 获取当前用户信息 | GET |
| `RefreshTokenAsync()` | `Task<ApiResponse<LoginResponse>>` | JWT令牌刷新 | POST |
| `ChangePasswordAsync([Body] ChangePasswordRequest)` | `Task<ApiResponse<object>>` | 修改用户密码 | POST |
| `HealthCheckAsync()` | `Task<string>` | 健康状态检查 | GET |

#### 业务分析
- **统一认证入口**: 支持多种认证场景，包括记住密码和令牌刷新
- **类型安全**: 基于Refit的强类型JWT令牌管理
- **完整生命周期**: 覆盖登录、认证、令牌刷新、登出的完整流程

### 2. IUserApi - 用户管理API客户端
**源码位置**: `Api/IUserApi.cs`  
**功能范围**: 用户CRUD、权限管理、批量操作、密码重置

#### 核心方法清单
| 方法签名 | 返回类型 | 用途 | 特殊参数 |
|---------|----------|------|----------|
| `GetUsersAsync(多参数查询)` | `Task<ApiResponse<PagedResult<UserDto>>>` | 分页查询用户 | 9个查询参数 |
| `GetUserByIdAsync(Guid id)` | `Task<ApiResponse<UserDto>>` | 根据ID获取用户 | 路径参数 |
| `CreateUserAsync([Body] UserMutationDto)` | `Task<ApiResponse<UserDto>>` | 创建新用户 | Body参数 |
| `UpdateUserAsync(Guid id, [Body] UserMutationDto)` | `Task<ApiResponse<UserDto>>` | 更新用户信息 | ID+Body参数 |
| `BatchDisableAsync([Body] BatchIdsDto)` | `Task<ApiResponse<object>>` | 批量禁用用户 | 批量ID列表 |
| `BatchEnableAsync([Body] BatchIdsDto)` | `Task<ApiResponse<object>>` | 批量启用用户 | 批量ID列表 |
| `ResetPasswordAsync(Guid id)` | `Task<ApiResponse<object>>` | 重置用户密码 | 用户ID |

#### 设计特点
- **复杂查询支持**: 9个查询参数，支持灵活的用户搜索和筛选
- **批量操作优化**: 提供批量启用/禁用功能，提升管理效率
- **统一响应格式**: 所有API使用`ApiResponse<T>`包装格式

### 3. IPatientApi - 患者管理API客户端
**源码位置**: `Api/IPatientApi.cs`  
**功能范围**: 患者档案管理、处方关联、批量导入导出

#### 核心方法清单
| 方法签名 | 返回类型 | 用途 | 特色功能 |
|---------|----------|------|----------|
| `GetPatientsAsync(分页查询)` | `Task<ApiResponse<PagedResult<PatientDto>>>` | 分页查询患者 | 多条件筛选 |
| `CreatePatientAsync([Body] PatientCreateDto)` | `Task<ApiResponse<PatientDto>>` | 创建患者档案 | 完整信息录入 |
| `GetPrescriptionsAsync(Guid id)` | `Task<ApiResponse<List<PrescriptionDto>>>` | 获取患者处方历史 | 业务关联 |
| `ImportPatientsAsync([Body] List<PatientImportDto>)` | `Task<ApiResponse<int>>` | 批量导入患者 | Excel导入支持 |
| `ExportPatientsAsync()` | `Task<ApiResponse<List<PatientDto>>>` | 导出患者数据 | 数据导出 |
| `GetImportTemplateAsync()` | `Task<ApiResponse<byte[]>>` | 获取导入模板 | Excel模板 |

#### 业务价值
- **完整患者生命周期管理**: 从创建到处方历史跟踪
- **Excel集成**: 支持批量导入导出，提升数据录入效率
- **业务关联**: 与处方系统的深度集成

### 4. IMedicalCaseApi - 医疗案例API客户端
**源码位置**: `Api/IMedicalCaseApi.cs`  
**功能范围**: 诊疗流程管理、状态跟踪、统计分析

#### 核心方法清单
| 方法签名 | 返回类型 | 用途 | 状态管理 |
|---------|----------|------|----------|
| `GetPagedAsync(分页查询)` | `Task<ApiResponse<PagedResult<MedicalCaseDto>>>` | 分页查询医案 | 多条件查询 |
| `CreateAsync([Body] MedicalCaseCreateDto)` | `Task<ApiResponse<MedicalCaseDto>>` | 创建医疗案例 | 初始状态 |
| `UpdateStatusAsync(Guid id, [Body] MedicalCaseStatus)` | `Task<ApiResponse<bool>>` | 更新案例状态 | 状态机控制 |
| `CompleteAsync(Guid id, [Body] CompleteMedicalCaseDto)` | `Task<ApiResponse<bool>>` | 完成医疗案例 | 业务完成 |
| `SuspendAsync(Guid id, [Body] SuspendMedicalCaseDto)` | `Task<ApiResponse<bool>>` | 暂停医疗案例 | 状态暂停 |
| `GetByPatientIdAsync(Guid patientId)` | `Task<ApiResponse<List<MedicalCaseDto>>>` | 按患者查询案例 | 患者关联 |
| `GetStatisticsAsync(DateTime?, DateTime?)` | `Task<ApiResponse<object>>` | 获取统计信息 | 数据分析 |

#### 架构设计
- **完整状态机管理**: 支持创建→进行→完成/暂停/归档的状态流转
- **复杂业务场景**: 支持暂停、恢复、归档等高级业务操作
- **统计分析功能**: 内置数据分析和报表支持

### 5. IConsultationApi - 看诊诊断API客户端
**源码位置**: `Api/IConsultationApi.cs`  
**功能范围**: 中医四诊数据记录、诊断流程、统计分析

#### 核心方法清单
| 方法签名 | 返回类型 | 用途 | 中医特色 |
|---------|----------|------|----------|
| `GetConsultationsAsync(7参数查询)` | `Task<ApiResponse<PagedResult<ConsultationDto>>>` | 查询看诊记录 | 多维度查询 |
| `StartConsultationAsync([Body] ConsultationStartDto)` | `Task<ApiResponse<ConsultationDetailDto>>` | 开始看诊诊断 | 四诊开始 |
| `CompleteConsultationAsync(Guid id, [Body] ConsultationCompleteDto)` | `Task<ApiResponse<object>>` | 完成看诊记录 | 诊断完成 |
| `GetTodayConsultationsByDoctorAsync(Guid doctorId)` | `Task<ApiResponse<List<ConsultationDto>>>` | 医生今日看诊 | 工作量统计 |
| `GetPatientHistoryAsync(Guid patientId)` | `Task<ApiResponse<List<ConsultationDto>>>` | 患者诊疗历史 | 病史跟踪 |
| `GetDoctorConsultationCountAsync(Guid doctorId, 日期范围)` | `Task<ApiResponse<int>>` | 医生看诊统计 | 绩效评估 |

#### 专业特性
- **中医四诊支持**: 完整的望、闻、问、切诊断流程
- **多维度查询**: 支持医生、患者、时间、状态等复杂查询条件
- **统计功能**: 支持医生绩效和工作量统计分析

### 6. IPrescriptionApi - 处方管理API客户端
**源码位置**: `Api/IPrescriptionApi.cs`  
**功能范围**: 处方开具、状态管理、作废控制

#### 核心方法清单
| 方法签名 | 返回类型 | 用途 | 处方特性 |
|---------|----------|------|----------|
| `GetListAsync(10参数高级查询)` | `Task<ApiResponse<PagedResult<PrescriptionDto>>>` | 高级查询处方 | 复杂搜索 |
| `CreatePrescriptionAsync([Body] PrescriptionCreateDto)` | `Task<ApiResponse<PrescriptionDto>>` | 创建处方记录 | 处方开具 |
| `UpdatePrescriptionAsync(Guid id, [Body] PrescriptionEditDto)` | `Task<ApiResponse<PrescriptionDto>>` | 更新处方信息 | 处方修改 |
| `CancelPrescriptionAsync(Guid id)` | `Task<ApiResponse<PrescriptionDto>>` | 作废处方 | 安全作废 |
| `DeletePrescriptionAsync(Guid id)` | `Task<ApiResponse<bool>>` | 删除处方 | 物理删除 |

#### 设计精髓
- **10参数高级查询**: 支持极其复杂的处方搜索场景
- **处方作废机制**: 保证处方数据的完整性和可追溯性
- **与业务系统集成**: 与医疗案例、患者系统的深度关联

### 7. IHerbApi - 中药材管理API客户端
**源码位置**: `Api/IHerbApi.cs`  
**功能范围**: 药材信息管理、处方用药支持、批量导入导出

#### 核心方法清单
| 方法签名 | 返回类型 | 用途 | 药材特性 |
|---------|----------|------|----------|
| `GetHerbsAsync(10参数查询)` | `Task<ApiResponse<PagedResult<HerbDto>>>` | 查询药材信息 | 全面查询 |
| `CreateHerbAsync([Body] HerbCreateDto)` | `Task<ApiResponse<HerbDto>>` | 创建药材记录 | 药材录入 |
| `GetAvailableHerbsAsync()` | `Task<ApiResponse<List<HerbDto>>>` | 获取可用药材 | 处方专用 |
| `GetStatisticsAsync()` | `Task<ApiResponse<Dictionary<int, int>>>` | 获取状态统计 | 药材概览 |
| `ImportHerbsAsync([Body] List<HerbImportDto>)` | `Task<ApiResponse<int>>` | 批量导入药材 | Excel导入 |
| `GetImportTemplateAsync()` | `Task<ApiResponse<byte[]>>` | 获取导入模板 | 模板下载 |

#### 特色功能
- **专门的可用药材接口**: 直接支持处方开具业务
- **状态统计**: 提供药材库存和状态的统计概览
- **完整导入导出**: Excel格式的批量数据处理

### 8. IFormulaApi - 验方管理API客户端
**源码位置**: `Api/IFormulaApi.cs`  
**功能范围**: 验方模板管理、批量操作、Excel导入导出

#### 核心方法清单
| 方法签名 | 返回类型 | 用途 | 验方特性 |
|---------|----------|------|----------|
| `GetFormulasAsync(分类查询)` | `Task<ApiResponse<PagedResult<FormulaDto>>>` | 查询验方模板 | 分类管理 |
| `CreateFormulaAsync([Body] FormulaCreateDto)` | `Task<ApiResponse<FormulaDto>>` | 创建验方模板 | 验方录入 |
| `CopyFormulaAsync(Guid id, [Query] string newName)` | `Task<ApiResponse<FormulaDto>>` | 复制验方模板 | 模板复制 |
| `GetCategoriesAsync()` | `Task<ApiResponse<List<string>>>` | 获取验方分类 | 分类管理 |
| `ImportFormulasAsync(批量导入)` | `Task<ApiResponse<FormulaImportResultDto>>` | 批量导入验方 | Excel导入 |
| `ValidateImportDataAsync(验证数据)` | `Task<ApiResponse<FormulaImportResultDto>>` | 验证导入数据 | 数据验证 |
| `ExportToExcelAsync([Body] List<Guid>)` | `Task<ApiResponse<byte[]>>` | 导出到Excel | 数据导出 |

#### 企业级特性
- **完整的导入验证机制**: 导入前数据校验和错误提示
- **Excel文件支持**: 专业的Excel导入导出功能
- **验方复制和模板管理**: 支持验方模板的快速复制和管理

## 🔧 业务服务接口详细分析

### UltraThink双层架构模式
所有业务服务接口都遵循UltraThink双层架构设计：
- **QueryService专业负责**: 复杂查询、搜索、统计功能
- **BusinessService专业负责**: CRUD操作、业务逻辑、事务管理  
- **Module纯委托模式**: 统一服务入口，请求分发

### 1. IAuthService - 身份认证服务接口
**源码位置**: `Services/IAuthService.cs`  
**架构模式**: UltraThink双层架构支持

#### 核心方法清单
| 方法签名 | 返回类型 | 委托层级 | 用途 |
|---------|----------|----------|------|
| `LoginAsync(LoginRequest)` | `Task<ServiceResult<LoginResponse>>` | BusinessService | 用户登录认证 |
| `LogoutAsync(LogoutRequest)` | `Task<ServiceResult<bool>>` | BusinessService | 用户登出操作 |
| `RefreshTokenAsync(string refreshToken)` | `Task<ServiceResult<LoginResponse>>` | BusinessService | JWT令牌刷新 |
| `ValidateTokenAsync(string token)` | `Task<ServiceResult<bool>>` | QueryService | 令牌验证查询 |
| `ChangeSysAdminPasswordAsync(ChangeSysAdminPassword)` | `Task<ServiceResult<bool>>` | BusinessService | 管理员密码修改 |

### 2. IUserService - 用户服务接口 (UltraThink标准实现)
**源码位置**: `Services/IUserService.cs`  
**架构示范**: 最完整的UltraThink双层架构方法分工示例

#### QueryService专业负责 (6个方法)
| 方法签名 | 返回类型 | 专业定位 | XML文档 |
|---------|----------|----------|---------|
| `GetByIdAsync(Guid id)` | `Task<ServiceResult<UserDto>>` | 单一查询 | 完整注释 |
| `GetPagedAsync(UserPagedQueryDto query)` | `Task<ServiceResult<PagedResult<UserDto>>>` | 分页查询 | 缓存策略 |
| `GetByUsernameAsync(string username)` | `Task<ServiceResult<UserDto>>` | 唯一查询 | 权限说明 |
| `GetActiveUsersAsync()` | `Task<ServiceResult<List<UserDto>>>` | 状态筛选 | 使用场景 |
| `SearchAsync(string keyword)` | `Task<ServiceResult<List<UserDto>>>` | 关键字搜索 | 搜索算法 |
| `ValidateUsernameAsync(string username)` | `Task<ServiceResult<bool>>` | 验证查询 | 验证逻辑 |

#### BusinessService专业负责 (13个方法)
| 方法签名 | 返回类型 | 业务类型 | 事务要求 |
|---------|----------|----------|----------|
| `CreateAsync(UserMutationDto dto)` | `Task<ServiceResult<UserDto>>` | 创建操作 | 事务控制 |
| `UpdateAsync(UserMutationDto dto)` | `Task<ServiceResult<UserDto>>` | 更新操作 | 乐观锁 |
| `DeleteAsync(Guid id)` | `Task<ServiceResult<bool>>` | 删除操作 | 软删除 |
| `EnableAsync(Guid id)` | `Task<ServiceResult<bool>>` | 状态管理 | 审计日志 |
| `DisableAsync(Guid id)` | `Task<ServiceResult<bool>>` | 状态管理 | 级联处理 |
| `BatchEnableAsync(List<Guid> ids)` | `Task<ServiceResult<int>>` | 批量操作 | 批量事务 |
| `BatchDisableAsync(List<Guid> ids)` | `Task<ServiceResult<int>>` | 批量操作 | 回滚机制 |
| `ResetPasswordAsync(Guid id, string newPassword)` | `Task<ServiceResult<bool>>` | 密码管理 | 安全日志 |
| `ChangePasswordAsync(Guid id, string oldPassword, string newPassword)` | `Task<ServiceResult<bool>>` | 密码变更 | 验证逻辑 |
| `ChangeProfileAsync(ChangeProfileDto dto)` | `Task<ServiceResult<bool>>` | 个人资料 | 数据验证 |

#### 文档质量标准
每个方法都包含完整的XML注释：
- **委托关系**: 说明调用的具体QueryService或BusinessService方法
- **缓存策略**: 描述缓存使用和过期策略
- **权限要求**: 明确方法的权限和角色要求
- **使用场景**: 详细的业务使用场景和注意事项

### 3-8. 其他业务服务接口
**统一设计模式**: 所有服务接口遵循相同的架构规范
- **ServiceResult包装**: 统一的服务结果类型包装
- **异步优先设计**: 所有方法都是异步模式
- **DTO参数模式**: 严格使用数据传输对象作为参数

## 🧠 缓存服务接口分析

### ISimplifiedCacheService - 简化缓存服务
**源码位置**: `Caching/ISimplifiedCacheService.cs`  
**设计理念**: 从复杂14方法精简至核心8方法，专注实用性和开发效率

#### 同步操作 (4个方法) - 高频快速访问
| 方法签名 | 返回类型 | 响应特性 | 使用场景 |
|---------|----------|----------|----------|
| `Get<T>(string key)` | `T?` | 微秒级响应 | 热数据访问 |
| `Set<T>(string key, T value, TimeSpan?)` | `void` | 立即缓存 | 数据存储 |
| `Remove(string key)` | `bool` | 立即失效 | 缓存清理 |
| `Clear()` | `void` | 全部清空 | 批量清理 |

#### 异步操作 (4个方法) - 复杂数据处理
| 方法签名 | 返回类型 | 处理特性 | 核心价值 |
|---------|----------|----------|----------|
| `GetAsync<T>(string key)` | `Task<T?>` | 异步查询 | 流程集成 |
| `SetAsync<T>(string key, T value, TimeSpan?)` | `Task` | 异步存储 | 非阻塞 |
| `RemoveAsync(string key)` | `Task<bool>` | 异步删除 | 批量处理 |
| `GetOrSetAsync<T>(string key, Func<Task<T>> factory, TimeSpan?)` | `Task<T>` | **核心模式** | 一站式缓存 |

#### 架构价值
- **GetOrSetAsync**: 核心缓存模式，一次调用处理完整缓存逻辑
- **双模式支持**: 同步快速访问 + 异步流程集成
- **智能过期**: 基于IMemoryCache的LRU淘汰策略

## 🔗 调用关系和依赖分析

### 前端实现者
- **WPF客户端**: 通过Refit自动生成API客户端实现
- **ViewModel层**: 注入IService接口，使用UltraThink双层架构
- **缓存集成**: 使用ISimplifiedCacheService提升响应性能

### 后端实现者  
- **API Controllers**: 实现所有IxxxApi接口定义的REST端点
- **Business Services**: 实现IxxxService接口，承载核心业务逻辑
- **Repository层**: 支撑Service层的数据持久化操作

### 依赖关系图
```
前端WPF ViewModel
    ↓ 依赖注入
IxxxService接口 (UltraThink双层架构)
    ↓ Refit客户端
IxxxApi接口 (REST契约)
    ↓ HTTP调用
后端API Controller
    ↓ 委托调用
后端Service实现
    ↓ 数据访问
Repository层
```

## 🎯 架构价值和设计决策

### 1. 统一契约保证
**设计价值**:
- **类型安全**: Refit强类型客户端消除运行时API调用错误
- **接口一致**: 前后端共享相同的接口定义，避免不一致
- **版本管理**: 统一的版本号管理，支持向后兼容

### 2. UltraThink架构支持
**架构标准**:
- **职责分离**: Query和Business方法明确分工
- **委托模式**: Module层纯委托，简化依赖关系
- **文档驱动**: 完整的XML注释支持自动化文档生成

### 3. 企业级设计模式
**质量标准**:
- **ServiceResult包装**: 统一的服务结果类型，包含成功/失败状态
- **分页查询标准**: PagedResult<T>统一分页结果格式
- **DTO模式**: 严格的数据传输对象模式，保护内部实体

### 4. 性能优化考虑
**优化策略**:
- **异步优先**: 所有API调用都是异步模式
- **缓存集成**: ISimplifiedCacheService提供智能缓存支持
- **批量操作**: 支持批量导入导出，提升数据处理效率

### 5. 可维护性设计
**维护特性**:
- **模块化**: 8个业务模块清晰分离，独立演进
- **接口稳定**: 接口变更影响范围可控
- **文档完整**: 每个方法都有使用场景和注意事项说明

## 📊 技术特色统计

### 接口规模统计
- **API客户端接口**: 8个核心业务模块，90+个API方法
- **业务服务接口**: 8个服务接口，110+个服务方法
- **缓存服务接口**: 1个专业接口，8个核心方法
- **总计方法数**: 200+个接口方法，完整覆盖业务需求

### UltraThink架构成果
- **职责清晰**: Query查询专业化，Business业务逻辑化
- **委托模式**: Module层零业务逻辑，纯粹的请求分发
- **文档标准**: 每个接口方法都有完整XML注释
- **类型安全**: 100%强类型接口定义，消除运行时错误

### 企业级特征
- **统一响应**: 所有API使用ApiResponse<T>包装格式
- **异常处理**: ServiceResult统一错误处理模式
- **分页标准**: PagedResult<T>标准分页格式
- **版本管理**: 统一版本策略，支持接口演进

## 结论

LYBT.Shared.Interfaces项目是LYBT中医诊所系统的**契约核心**，通过17个精心设计的接口(8个API客户端 + 8个业务服务 + 1个缓存服务)，实现了：

1. **前后端统一**: 确保WPF客户端与Web API的完全一致性
2. **架构标准**: 支持UltraThink双层架构的标准化实施  
3. **类型安全**: 基于Refit的强类型REST客户端消除运行时错误
4. **企业级**: 完整的文档、统一的错误处理、标准的分页模式
5. **高性能**: 异步优先、智能缓存、批量操作优化

该项目是整个LYBT系统架构稳定性和可维护性的重要保障，为20人以下中小型中医诊所提供了企业级的接口契约管理解决方案。
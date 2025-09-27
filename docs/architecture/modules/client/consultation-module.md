# 客户端Consultation模块设计文档

## 1. 模块概述

### 1.1 模块定位
客户端Consultation模块是凌隐宝堂中医诊所管理系统中的核心诊疗功能模块，负责提供中医诊疗过程的前端界面和业务逻辑。该模块采用简化设计理念，专为小型中医诊所的实际需求而设计，避免过度工程化。

### 1.2 核心功能
- **诊疗记录创建与管理**：支持新建、编辑、查看诊疗记录
- **中医四诊录入**：提供望、闻、问、切四诊的结构化录入界面
- **患者历史查询**：实用化的患者诊疗历史查看功能
- **四诊模板应用**：内置常用中医症候的录入模板
- **处方开具界面**：集成处方创建功能（界面已实现，业务逻辑待完善）

### 1.3 技术特点
- 基于WPF + Prism.DryIoc架构
- 遵循MVVM设计模式
- 采用依赖注入和服务分离
- 与服务器端通过RESTful API通信

## 2. 架构设计（MVVM模式）

### 2.1 整体架构
```
Consultation Module
├── Views/                  # XAML界面层
├── ViewModels/            # 视图模型层
├── Services/              # 前端服务层
├── Models/                # UI数据模型
└── ConsultationModule.cs  # 模块注册
```

### 2.2 MVVM架构映射
- **Model**: ConsultationDto（共享层）+ ConsultationItem（UI模型）
- **View**: ConsultationMainView + ConsultationManagementView
- **ViewModel**: ConsultationMainViewModel + ConsultationManagementViewModel

### 2.3 依赖关系
```
View → ViewModel → Service → API → Server
  ↓        ↓         ↓
 XAML   Commands   HTTP    REST    Business Logic
```

## 3. ViewModels设计

### 3.1 ConsultationMainViewModel
**职责**：诊疗主界面的核心逻辑控制

**关键属性**：
- `ObservableCollection<PatientDto> Patients`：患者列表
- `PatientDto? SelectedPatient`：当前选中患者
- `ConsultationDto Consultation`：当前诊疗记录
- `bool IsLoading`：加载状态标识

**核心命令**：
- `LoadPatientsCommand`：加载患者列表
- `SaveConsultationCommand`：保存诊疗记录
- `ViewPatientHistoryCommand`：查看患者历史（P0-02功能）
- `ShowTemplateMenuCommand`：显示四诊模板（P0-04功能）
- `ClearDataCommand`：清理数据

**特色功能实现**：

#### P0-02: 患者历史诊疗查询
```csharp
private async Task ViewPatientHistoryAsync()
{
    // 1. 获取患者医案历史
    // 2. 为每个医案获取诊疗记录
    // 3. 构建历史详情列表
    // 4. 显示格式化的历史信息
}
```

#### P0-04: 四诊录入模板功能
```csharp
private async Task ShowTemplateMenuAsync()
{
    // 1. 获取内置常用模板（风寒感冒、脾胃虚弱等）
    // 2. 显示模板选择界面
    // 3. 应用选定模板到四诊录入区域
}
```

### 3.2 ConsultationManagementViewModel
**职责**：诊疗记录管理界面的业务逻辑

**关键属性**：
- `ObservableCollection<ConsultationDto> Consultations`：诊疗记录列表
- `ConsultationDto? SelectedConsultation`：选中的记录
- `string SearchKeyword`：搜索关键词

**核心命令**：
- `LoadDataCommand`：加载数据
- `SearchCommand`：搜索记录
- `RefreshCommand`：刷新数据
- `ViewDetailsCommand`：查看详情

## 4. Views界面设计

### 4.1 ConsultationMainView
**布局结构**：
```
Grid (3列布局)
├── 左侧 (300px): 患者列表区域
│   ├── 搜索框
│   └── 患者ListBox
├── 中间 (*): 诊疗信息录入区域
│   ├── 患者基本信息展示
│   ├── 主诉录入
│   ├── 四诊合参录入（改进版）
│   ├── 中医诊断录入
│   └── 诊疗备注
└── 右侧 (350px): 处方开具区域
    ├── 处方项列表
    ├── 验方快速应用
    └── 手动添加药材
```

**界面特色**：
1. **患者历史查询按钮**：位于患者信息区域，提供快速历史查看
2. **四诊模板功能**：集成在四诊录入区域，支持一键应用常用模板
3. **改进的四诊录入界面**：
   - 统一录入区域，避免四个分离的输入框
   - 底部提示栏显示常见录入内容
   - 字数统计功能
   - 工具提示指导录入

### 4.2 ConsultationManagementView
**功能**：诊疗记录的列表管理（当前为简化实现）

**界面元素**：
- 搜索和刷新功能
- 分页数据展示
- 基本的详情查看

## 5. 前端服务层

### 5.1 ConsultationService
**位置**：`src/Client/Desktop/Modules/Consultation/Services/ConsultationService.cs`

**接口实现**：`IConsultationService`

**核心方法**：
- `GetPagedAsync()`：分页查询诊疗记录
- `GetByIdAsync()`：获取单个记录详情
- `CreateAsync()`：创建新的诊疗记录
- `UpdateAsync()`：更新诊疗记录
- `DeleteAsync()`：删除诊疗记录
- `GetByMedicalCaseIdAsync()`：根据医案ID查询（支持历史查询功能）
- `StartAsync()`：开始新的诊疗会话

**错误处理**：集成`IExceptionHandler`统一处理异常

**特点**：
- 简化的CRUD操作，避免复杂的业务流程
- 异步操作，提高用户体验
- 统一的错误处理和日志记录

## 6. 数据绑定与验证

### 6.1 数据模型映射

#### ConsultationItem (UI模型)
**用途**：专为UI展示设计的本地数据模型

**特点**：
- 继承`BindableBase`支持属性变更通知
- 包含UI特有的属性（如IsSelected、IsExpanded）
- 提供与ConsultationDto的相互转换方法
- 增加计算属性（如StatusText、StatusColor、DurationMinutes）

**关键映射**：
```csharp
// DTO → UI Model
public static ConsultationItem FromDto(ConsultationDto dto)

// UI Model → DTO  
public ConsultationDto ToDto()
```

### 6.2 数据绑定策略
- **双向绑定**：表单输入字段使用`{Binding Property, UpdateSourceTrigger=PropertyChanged}`
- **单向绑定**：只读展示字段使用`{Binding Property}`
- **命令绑定**：按钮操作使用`{Binding Command}`
- **集合绑定**：列表控件使用`ObservableCollection<T>`

### 6.3 数据验证
当前为简化实现，主要依赖：
- 必填字段的空值检查
- 业务逻辑验证在ViewModel中实现
- 服务器端验证作为最终保障

## 7. 路由与导航

### 7.1 模块注册
**文件**：`ConsultationModule.cs`

**注册内容**：
```csharp
public void RegisterTypes(IContainerRegistry containerRegistry)
{
    // 注册服务
    containerRegistry.RegisterSingleton<IConsultationService, ConsultationService>();
    
    // TODO: 注册视图和视图模型
}
```

### 7.2 导航支持
- `ConsultationMainViewModel`实现`INavigationAware`
- 支持通过导航参数传递`MedicalCaseId`
- 导航生命周期方法：
  - `OnNavigatedTo()`：接收导航参数
  - `IsNavigationTarget()`：确定是否可复用实例
  - `OnNavigatedFrom()`：导航离开时的清理

### 7.3 区域管理
- 主界面通过Prism Region系统管理
- 支持与其他模块的界面集成
- 遵循工作台模式的区域划分

## 8. 状态管理

### 8.1 本地状态
- **加载状态**：`IsLoading`属性控制加载指示器
- **选择状态**：`SelectedPatient`、`SelectedConsultation`管理当前选中项
- **输入状态**：各输入字段通过双向绑定同步状态

### 8.2 会话状态
- 通过`ISessionManager`获取当前用户信息
- 诊疗记录与当前登录用户关联
- 支持会话超时和重新认证

### 8.3 缓存策略
- 患者列表本地缓存，减少服务器请求
- 诊疗记录实时保存，避免数据丢失
- 模板数据内置在代码中，无需网络请求

## 9. API集成

### 9.1 API接口
**接口定义**：`IConsultationApi` (Shared层)

**支持的端点**：
- `GET /api/v1/consultations`：分页查询
- `GET /api/v1/consultations/{id}`：获取详情
- `POST /api/v1/consultations`：创建记录
- `PUT /api/v1/consultations/{id}`：更新记录
- `DELETE /api/v1/consultations/{id}`：删除记录

### 9.2 HTTP客户端
- 基于Refit生成HTTP客户端
- 通过`UnifiedApiClientManager`统一管理
- 支持认证token自动附加
- 集成重试机制和错误处理

### 9.3 数据传输对象
**请求DTO**：
- `ConsultationCreateDto`：创建诊疗记录
- `ConsultationUpdateDto`：更新诊疗记录

**响应DTO**：
- `ConsultationDto`：诊疗记录详情
- `PagedResult<ConsultationDto>`：分页结果

### 9.4 API调用流程
```
ViewModel Command → Service Method → API Client → HTTP Request → Server
                 ←                ←            ←              ←
```

## 10. 实现状态

### 10.1 已完成功能 ✅
- [x] 基础MVVM架构搭建
- [x] 诊疗记录CRUD操作
- [x] 患者列表加载和选择
- [x] 四诊录入界面（改进版设计）
- [x] 患者历史查询功能（P0-02）
- [x] 四诊录入模板功能（P0-04）
- [x] API集成基础框架
- [x] 错误处理和日志记录
- [x] 基础数据绑定和命令系统

### 10.2 部分实现功能 ⚠️
- [⚠️] 处方开具界面（UI已实现，业务逻辑待完善）
- [⚠️] 诊疗记录管理界面（基础功能已实现）
- [⚠️] 数据验证机制（基础验证已实现）
- [⚠️] 模块注册（服务已注册，视图注册待完成）

### 10.3 待实现功能 ❌
- [ ] 处方药材的添加和管理逻辑
- [ ] 验方快速应用功能
- [ ] 诊疗记录的打印功能
- [ ] 高级搜索和筛选功能
- [ ] 诊疗统计和报表功能
- [ ] 离线数据缓存和同步

### 10.4 技术债务
1. **API方法缺失**：`GetByMedicalCaseIdAsync`在API接口中未定义
2. **处方集成**：处方模块的集成度有待提高
3. **用户体验**：需要添加更多的加载状态和用户反馈
4. **数据验证**：客户端验证规则需要完善
5. **错误处理**：用户友好的错误提示需要改进

## 11. 开发指南

### 11.1 添加新功能
1. 在对应的ViewModel中添加属性和命令
2. 在Service层实现业务逻辑
3. 更新View的XAML绑定
4. 如需新API，先在Shared层定义接口
5. 编写单元测试验证功能

### 11.2 数据流处理
1. **输入验证**：在ViewModel中进行基础验证
2. **业务验证**：在Service层进行业务规则验证
3. **服务器验证**：API层进行最终验证
4. **错误反馈**：通过统一的错误处理机制返回用户友好信息

### 11.3 性能优化建议
1. 使用虚拟化控件处理大数据集
2. 实现数据分页和懒加载
3. 缓存常用数据减少网络请求
4. 使用异步操作避免UI阻塞

### 11.4 维护注意事项
1. 保持Service层的职责单一
2. ViewModel不应包含UI特定逻辑
3. 及时更新API接口文档
4. 遵循现有的命名和编码规范

---

## 文档版本信息
- **创建日期**：2025-09-27
- **版本**：v1.0
- **最后更新**：2025-09-27
- **维护者**：开发团队

## 相关文档
- [服务器端Consultation模块设计](../server/consultation-module.md)
- [API接口规范](../../api/consultation-api.md)
- [前端架构总览](../../client/frontend-architecture.md)
- [开发规范指南](../../../development/standards.md)
# 患者管理模块 (Patients Module)

**最后更新**: 2025-09-01  
**模块状态**: ✅ 生产就绪  
**对应后端**: LYBT.Module.Patients  
**需求参考**: [功能需求-患者管理模块](../../../../../docs/requirements/functional-requirements.md#2️⃣-患者管理模块-patients)

---

## 📋 模块概览

### 业务定位
专为简单中医诊所设计的患者档案管理模块，支持2-5人小团队的患者管理需求。

### 核心功能
- ✅ **患者档案管理**: 基础CRUD操作，支持完整患者信息
- ✅ **智能搜索**: 姓名模糊搜索、电话精确匹配、年龄性别筛选
- ✅ **数据导入导出**: Excel/CSV批量处理，标准化格式
- ✅ **就诊历史**: 患者的完整医案历史记录查看
- ❌ **已简化功能**: 移除复杂统计分析、标签系统、高级搜索 (2025-09-01)

### 技术架构
- **前端框架**: WPF + Prism.DryIoc
- **架构模式**: MVVM (移除Coordinator抽象层)
- **API通信**: Refit REST客户端
- **数据绑定**: ObservableCollection + INotifyPropertyChanged

---

## 🚨 UltraThink架构重构方案

### 当前架构问题

**🔴 严重架构问题**：
- **PatientModule.cs**: **700+行巨无霸单体类**
- **职责严重混乱**: 患者管理、搜索、导入导出、历史查看等多个职责混合
- **违背UltraThink原则**: 与后端Patients模块三层架构完全不一致
- **维护困难**: 任何患者相关功能修改都可能影响整个模块

### 重构目标架构

**🎯 UltraThink三层架构重构**：
```csharp
PatientModule (纯委托层 - 约50行)
    ├── PatientCoreService (核心操作层 - 约160行)
    │   ├── API通信: CallCreatePatientApi, CallUpdatePatientApi
    │   ├── 基础CRUD: GetPatientById, GetAllPatients
    │   └── 数据验证: ValidatePatientData, ValidateContactInfo
    ├── PatientQueryService (查询专业层 - 约130行)
    │   ├── 搜索功能: SearchPatients, FilterByAge, FilterByGender
    │   ├── 统计分析: GetPatientStatistics, GetVisitTrends
    │   └── 历史查询: GetMedicalHistory, GetVisitHistory
    └── PatientBusinessService (业务逻辑层 - 约180行)
        ├── 患者管理: CreatePatient, UpdatePatient, DeletePatient
        ├── 导入导出: ImportFromExcel, ExportToExcel
        ├── 关联管理: LinkMedicalCases, GetPatientCases
        └── 档案管理: UpdateProfile, ManageContacts
```

### 重构详细方案

#### 📋 重构任务清单
- [ ] **第一阶段**: 创建三层服务接口 (4个接口文件)
- [ ] **第二阶段**: 实现PatientCoreService (API通信和基础操作)
- [ ] **第三阶段**: 实现PatientQueryService (搜索、统计、历史)
- [ ] **第四阶段**: 实现PatientBusinessService (管理、导入导出)
- [ ] **第五阶段**: 重构PatientModule为纯委托层
- [ ] **第六阶段**: 更新依赖注入配置
- [ ] **第七阶段**: 数据导入导出功能测试

#### 🎯 代码质量目标
- **重构前**: 700+行单体类，多个职责混合
- **重构后**: 4个文件，职责清晰分离
  - PatientModule: ≤50行 (纯委托)
  - PatientCoreService: ≤160行 (核心操作)
  - PatientQueryService: ≤130行 (查询功能)
  - PatientBusinessService: ≤180行 (业务逻辑)

#### ⚡ 预期效果
- ✅ **数据安全性**: 患者隐私数据处理逻辑独立
- ✅ **功能模块化**: 搜索、导入导出等功能独立维护
- ✅ **性能优化**: 查询功能专业化处理，提升检索效率
- ✅ **架构一致性**: 与后端Patients模块架构完全统一

### 重构优先级

**🔴 高优先级**: 患者数据是核心业务数据，重构后便于数据安全和隐私保护

## 🏗️ 模块结构

### 当前结构
```
src/Client/Desktop/Modules/Patients/
├── Services/
│   └── PatientModule.cs           # 🔴 700+行巨无霸 (需要重构)
├── ViewModels/
│   ├── PatientManagementViewModel.cs      # 患者列表管理
│   ├── PatientDetailViewModel.cs          # 患者详情编辑
│   └── PatientSearchViewModel.cs          # 搜索功能
├── Views/
│   ├── PatientManagementView.xaml         # 患者列表界面
│   ├── PatientDetailView.xaml             # 患者详情界面
│   ├── PatientAddEditDialog.xaml          # 新增编辑对话框
│   └── [其他视图文件]
└── README.md                      # 本文档
```

### 核心类说明
- **PatientModule.cs**: Prism模块注册，依赖注入配置
- **PatientManagementViewModel**: 患者列表、搜索、分页逻辑
- **PatientDetailViewModel**: 单个患者详情的增删改查
- **PatientService**: HTTP API调用，与后端通信

---

## 🔌 API接口集成

### 后端API对接
```csharp
// 主要API端点 (对应LYBT.Module.Patients)
GET    /api/v1/patients          // 获取患者列表(分页)
GET    /api/v1/patients/{id}     // 获取患者详情
POST   /api/v1/patients          // 创建新患者
PUT    /api/v1/patients/{id}     // 更新患者信息
DELETE /api/v1/patients/{id}     // 删除患者(软删除)
GET    /api/v1/patients/search   // 患者搜索
POST   /api/v1/patients/import   // 批量导入
GET    /api/v1/patients/export   // 批量导出
```

### 数据传输对象
```csharp
// 主要DTO类 (来自LYBT.Shared.Models)
- PatientDto: 患者基础信息
- PatientCreateDto: 创建患者请求
- PatientUpdateDto: 更新患者请求
- PatientSearchDto: 搜索条件
- PagedResult<PatientDto>: 分页结果
```

---

## 💻 开发指南

### 开发环境设置
1. **前置条件**: Visual Studio 2022, .NET 8 SDK
2. **依赖项目**: LYBT.Shared.Models, LYBT.Desktop.Core
3. **运行要求**: 后端API服务 (localhost:5001) 必须启动

### 新增功能开发
```csharp
// 1. 在PatientManagementViewModel中添加新功能
public class PatientManagementViewModel : ViewModelBase
{
    private readonly IPatientService _patientService;
    
    // 新功能示例
    private async Task<bool> NewFeatureAsync()
    {
        try 
        {
            var result = await _patientService.CallNewApiAsync();
            return result.IsSuccess;
        }
        catch (Exception ex)
        {
            // 统一异常处理
            await ShowErrorAsync(ex.Message);
            return false;
        }
    }
}

// 2. 在PatientService中添加API调用
public interface IPatientService
{
    Task<ApiResponse<ResultDto>> CallNewApiAsync();
}
```

### 界面开发规范
```xml
<!-- XAML开发规范 -->
<UserControl x:Class="LYBT.Desktop.Patients.Views.ExampleView"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
    
    <!-- 使用统一的样式资源 -->
    <Grid Style="{StaticResource ContentGridStyle}">
        <!-- 数据绑定到ViewModel -->
        <DataGrid ItemsSource="{Binding Patients}" 
                  SelectedItem="{Binding SelectedPatient}"/>
    </Grid>
</UserControl>
```

---

## 🧪 测试指南

### 单元测试 (计划中)
```csharp
// 推荐测试结构
[TestClass]
public class PatientManagementViewModelTests
{
    [TestMethod]
    public async Task LoadPatients_ShouldReturnPagedResult()
    {
        // Arrange
        var mockService = new Mock<IPatientService>();
        var viewModel = new PatientManagementViewModel(mockService.Object);
        
        // Act
        await viewModel.LoadPatientsAsync();
        
        // Assert
        Assert.IsTrue(viewModel.Patients.Count > 0);
    }
}
```

### 手动测试清单
- [ ] 患者列表加载和分页
- [ ] 搜索功能 (姓名、电话、年龄、性别)
- [ ] 新增患者并验证数据
- [ ] 编辑患者信息并保存
- [ ] 删除患者并确认软删除
- [ ] 导入Excel文件并验证结果
- [ ] 导出患者数据为Excel
- [ ] 查看患者就诊历史

---

## 🔧 配置说明

### 模块注册 (PatientModule.cs)
```csharp
public class PatientModule : IModule
{
    public void RegisterTypes(IContainerRegistry containerRegistry)
    {
        // Service注册
        containerRegistry.Register<IPatientService, PatientService>();
        
        // ViewModel注册  
        containerRegistry.Register<PatientManagementViewModel>();
        containerRegistry.Register<PatientDetailViewModel>();
        
        // Navigation注册
        containerRegistry.RegisterForNavigation<PatientManagementView>();
        containerRegistry.RegisterForNavigation<PatientDetailView>();
    }
}
```

### API配置
```csharp
// Refit API配置 (在Services项目中)
[Headers("Authorization: Bearer")]
public interface IPatientApi
{
    [Get("/api/v1/patients")]
    Task<ApiResponse<PagedResult<PatientDto>>> GetPatientsAsync(
        [Query] int page = 1, 
        [Query] int pageSize = 20);
        
    [Post("/api/v1/patients")]
    Task<ApiResponse<PatientDto>> CreatePatientAsync([Body] PatientCreateDto patient);
}
```

---

## 🐛 故障排除

### 常见问题
1. **患者列表为空**
   - 检查API服务是否启动 (localhost:5001)
   - 检查数据库连接和患者表数据

2. **搜索功能异常**  
   - 验证搜索参数格式
   - 检查后端API搜索接口返回

3. **导入导出失败**
   - 确认Excel文件格式符合要求
   - 检查文件权限和路径访问

4. **界面响应慢**
   - 检查是否在UI线程进行长时间操作
   - 使用异步操作和进度指示

### 调试技巧
```csharp
// 启用详细日志
private static readonly ILogger _logger = LogManager.GetCurrentClassLogger();

public async Task<bool> LoadPatientsAsync()
{
    _logger.Info("开始加载患者列表");
    try
    {
        var result = await _patientService.GetPatientsAsync();
        _logger.Info($"成功加载{result.Data?.Items?.Count ?? 0}个患者");
        return true;
    }
    catch (Exception ex)
    {
        _logger.Error(ex, "加载患者列表失败");
        return false;
    }
}
```

---

## 📊 模块状态

### 当前实现状态 (v1.0)
- ✅ 基础CRUD功能完整
- ✅ 搜索功能正常
- ✅ 导入导出功能可用
- ✅ 就诊历史查看正常
- ✅ 零编译警告，生产就绪

### 已简化功能 (2025-09-01)
- ❌ 复杂统计分析 (不适合小诊所)
- ❌ 患者标签系统 (增加操作复杂度)
- ❌ 高级搜索功能 (基础搜索已足够)
- ❌ 患者关系管理 (过度设计)

### 计划功能 (v2.0)
- 🔮 患者照片管理
- 🔮 就诊提醒功能  
- 🔮 患者满意度收集

---

## 📚 相关文档

### 需求文档
- [功能需求-患者管理](../../../../../docs/requirements/functional-requirements.md#2️⃣-患者管理模块-patients)
- [系统总览](../../../../../docs/requirements/system-overview.md)

### 技术文档
- [前端架构设计](../../../../../docs/requirements/architecture-design.md#📱-前端架构设计)
- [API响应标准](../../../../../docs/API响应标准.md)

### 开发规范
- [WPF开发规范](../../../../../CLAUDE.md#🎨-资源管理规范重要)
- [MVVM模式指南](../../../../../docs/development/)

---

**维护说明**: 本文档反映Patients模块的当前实现状态。功能变更时及时更新对应章节，确保与需求文档保持同步。
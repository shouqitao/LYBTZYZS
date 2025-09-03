# LYBT.Desktop.Workbench.Pharmacist

凌隐宝堂中医诊所系统 - 药剂师工作台模块

## 项目概述

药剂师工作台是专为药剂师设计的药品管理和调配环境，提供中药材管理、处方调配、库存管理、用药指导等专业功能。采用现代化WPF界面和Prism MVVM架构，支持完整的中医药药事管理流程。

## 目录结构

```
PharmacistWorkbench/
├── ViewModels/                        # 视图模型
│   └── PharmacistMainViewModel.cs     # 药剂师主工作台视图模型
├── Views/                            # 用户界面
│   ├── PharmacistMainView.xaml        # 药剂师主工作台视图
│   └── PharmacistMainView.xaml.cs     # 主视图代码后置
└── PharmacistWorkbenchModule.cs       # Prism模块定义
```

## 核心功能

### 1. 药剂师主工作台
- **统一药事管理界面**: 集成所有药事管理功能的中央操作台
- **处方调配**: 接收和处理医生开具的中药处方
- **质量控制**: 确保药品质量和调配准确性

### 2. 中药材管理 (集成HerbManagementView)
- **药材库管理**: 维护中药材基础信息和规格标准
- **品质管理**: 管理药材产地、等级、有效期等质量指标
- **价格维护**: 更新药材采购价格和零售价格
- **供应商管理**: 维护药材供应商信息和采购渠道

### 3. 处方调配流程
#### 标准调配流程
1. **处方接收**: 接收医生开具的中药处方
2. **处方审核**: 审核处方的合理性和安全性
3. **配伍检查**: 检查药物配伍禁忌和相互作用
4. **药材调配**: 按处方要求称量和调配药材
5. **质量检查**: 验证调配药材的质量和数量
6. **包装标识**: 完成药品包装并标注用法用量
7. **发药交付**: 向患者发放药品并提供用药指导

#### 质量保证措施
- **双人复核**: 重要处方实行双人复核制度
- **称量验证**: 使用精密天平确保药材重量准确
- **批次记录**: 记录每批次药材的来源和质量信息
- **效期管理**: 严格控制药材有效期，先进先出

### 4. 预留功能模块
目前为未来扩展预留了以下专业功能：

- **DrugPreparationView**: 药品调配管理 (待实现)
  - 处方调配工作台
  - 调配记录和追溯
  - 调配质量控制

- **InventoryManagementView**: 库存管理系统 (待实现)
  - 药材库存监控
  - 进销存管理
  - 库存预警和补货

- **MedicationGuidanceView**: 用药指导服务 (待实现)
  - 患者用药咨询
  - 用药方法指导
  - 不良反应监测

## 技术架构

### 框架技术栈
- **.NET 8.0-windows**: 现代.NET平台
- **WPF**: Windows桌面应用程序框架
- **Prism.DryIoc 8.1.97**: MVVM框架和依赖注入
- **LYBT.Desktop.Core**: 桌面应用程序核心框架

### 设计模式
- **MVVM模式**: 视图-视图模型-模型分离
- **依赖注入**: 使用DryIoc容器管理依赖关系
- **模块化架构**: Prism模块化应用程序结构
- **工作流模式**: 处方调配的标准化工作流程

## 模块注册

### PharmacistWorkbenchModule
Prism模块定义，负责药剂师工作台的初始化和服务注册：

```csharp
public class PharmacistWorkbenchModule : IModule
{
    public void RegisterTypes(IContainerRegistry containerRegistry)
    {
        // 注册药剂师工作台主视图
        containerRegistry.RegisterForNavigation<PharmacistMainView>();
        
        // UltraThink Phase 3.3: 注册集成的中药材管理功能
        containerRegistry.RegisterForNavigation<HerbManagementView>();
        
        // 预留：未来可注册药剂师相关的其他视图和服务
        // containerRegistry.RegisterForNavigation<DrugPreparationView>(); // 待实现
        // containerRegistry.RegisterForNavigation<InventoryManagementView>(); // 待实现
        // containerRegistry.RegisterForNavigation<MedicationGuidanceView>(); // 待实现
    }
    
    public void OnInitialized(IContainerProvider containerProvider)
    {
        // 注册自定义的ViewModel映射
        ViewModelLocationProvider.Register<PharmacistMainView, PharmacistMainViewModel>();
    }
}
```

## 中医药学专业特色

### 1. 中药配伍理论
- **君臣佐使**: 按中医理论分析处方中药材的配伍关系
- **七情配伍**: 检查药物间的相须、相使、相畏、相杀、相恶、相反关系
- **十八反十九畏**: 自动检测配伍禁忌，防止用药错误

### 2. 炮制工艺管理
- **炮制方法**: 记录不同药材的炮制工艺要求
- **炮制标准**: 维护各类药材的炮制质量标准
- **工艺追溯**: 追溯药材从原料到成品的炮制过程

### 3. 质量控制体系
- **感官检验**: 通过外观、气味、口感等感官指标评价药材质量
- **理化检验**: 记录药材的水分、灰分、浸出物等理化指标
- **微生物检验**: 监控药材的微生物污染情况

## 用户界面

### 药剂师主工作台界面
- **处方队列**: 显示待处理的处方列表，按优先级排序
- **调配区域**: 当前正在调配的处方详细信息
- **药材选择器**: 快速选择和定位所需药材
- **称量记录**: 实时记录各药材的称量数据

### 中药材管理界面
- **药材库**: 显示所有中药材的详细信息
- **搜索功能**: 按药材名称、功效、归经等条件搜索
- **库存状态**: 实时显示药材库存状况和预警信息
- **质量档案**: 查看药材的质量检验报告和认证信息

## 业务流程

### 处方调配工作流
```csharp
// 示例处方调配流程
public async Task<PrescriptionResult> ProcessPrescriptionAsync(PrescriptionDto prescription)
{
    // 1. 接收处方
    var receivedPrescription = await ReceivePrescriptionAsync(prescription.Id);
    
    // 2. 处方审核
    var auditResult = await AuditPrescriptionAsync(receivedPrescription);
    if (!auditResult.IsValid)
    {
        return PrescriptionResult.Failed(auditResult.ErrorMessage);
    }
    
    // 3. 配伍检查
    var compatibilityCheck = await CheckDrugCompatibilityAsync(receivedPrescription.Items);
    if (!compatibilityCheck.IsSafe)
    {
        await NotifyPhysicianAsync(compatibilityCheck.Warnings);
    }
    
    // 4. 药材调配
    var preparationResult = await PrepareMedicationAsync(receivedPrescription);
    
    // 5. 质量检查
    var qualityCheck = await PerformQualityCheckAsync(preparationResult);
    
    // 6. 包装发药
    await PackageAndDispenseAsync(preparationResult, prescription.Patient);
    
    return PrescriptionResult.Success();
}
```

### 库存管理工作流
```csharp
// 示例库存管理流程
public async Task<InventoryResult> ManageInventoryAsync()
{
    // 1. 库存盘点
    var inventoryStatus = await CheckInventoryStatusAsync();
    
    // 2. 识别短缺药材
    var shortageItems = inventoryStatus.Items
        .Where(item => item.CurrentStock <= item.MinimumStock)
        .ToList();
    
    // 3. 生成采购建议
    var purchaseRecommendation = await GeneratePurchaseRecommendationAsync(shortageItems);
    
    // 4. 检查即将过期药材
    var expiringItems = await GetExpiringItemsAsync(TimeSpan.FromDays(30));
    
    // 5. 处理过期药材
    await HandleExpiringItemsAsync(expiringItems);
    
    return InventoryResult.Success(purchaseRecommendation);
}
```

## 集成接口

### 与业务模块的集成
- **处方模块**: 接收和处理医生开具的处方
- **中药材模块**: 管理药材库存和基础信息
- **患者模块**: 获取患者信息用于用药指导
- **财务模块**: 药品成本核算和售价管理

### 外部系统集成
```csharp
// 药监系统集成示例
public async Task SyncWithDrugRegulationSystemAsync()
{
    // 上报药品使用数据
    var usageData = await GetDrugUsageReportAsync();
    await _drugRegulationApi.SubmitUsageReportAsync(usageData);
    
    // 获取药品警示信息
    var alerts = await _drugRegulationApi.GetDrugAlertsAsync();
    await ProcessDrugAlertsAsync(alerts);
}
```

## 质量保证

### 调配质量控制
- **标准作业程序(SOP)**: 严格按照标准化流程操作
- **关键控制点**: 识别和监控调配过程中的关键质量控制点
- **偏差处理**: 建立调配偏差的报告和纠正机制
- **持续改进**: 定期评估和改进调配工艺

### 数据完整性
- **调配记录**: 完整记录每个处方的调配过程
- **质量数据**: 保存药材质量检验的原始数据
- **追溯性**: 确保从原料到成品的完整追溯链

## 权限管理

### 药剂师权限级别
- **初级药师**: 基础调配操作，需要高级药师复核
- **主管药师**: 独立调配权限，可以复核他人操作
- **药学主任**: 质量管理权限，处方审核和工艺改进

### 关键操作控制
```csharp
// 权限验证示例
public async Task<bool> ValidatePharmacistOperationAsync(string operation, int pharmacistLevel)
{
    var requiredLevel = await GetRequiredLevelForOperationAsync(operation);
    
    if (pharmacistLevel < requiredLevel)
    {
        await LogUnauthorizedAccessAsync(operation, pharmacistLevel);
        return false;
    }
    
    return true;
}
```

## 开发状态

### 已实现功能
- ✅ 基础工作台框架
- ✅ 中药材管理集成 (HerbManagementView)
- ✅ Prism模块注册和依赖注入
- ✅ 基础权限管理

### 待实现功能 (v2.0)
- 🔄 药品调配管理 (DrugPreparationView)
- 🔄 库存管理系统 (InventoryManagementView)
- 🔄 用药指导服务 (MedicationGuidanceView)
- 🔄 配伍禁忌检查引擎
- 🔄 质量管理体系
- 🔄 电子药历系统

## 开发指南

### 添加新药学功能
1. **创建专业视图**: 根据药学专业需求设计界面
2. **实现业务逻辑**: 编写符合药学规范的业务逻辑
3. **集成质量控制**: 添加相应的质量控制检查点
4. **权限配置**: 设置合适的药师权限要求

### 中医药知识库集成
```csharp
// 中医药知识库查询示例
public async Task<HerbInfo> GetHerbInfoAsync(string herbName)
{
    var herbInfo = await _tcmKnowledgeBase.GetHerbAsync(herbName);
    
    return new HerbInfo
    {
        Name = herbInfo.Name,
        Properties = herbInfo.Properties, // 性味归经
        Efficacy = herbInfo.Efficacy,     // 功效主治
        Dosage = herbInfo.Dosage,         // 用法用量
        Contraindications = herbInfo.Contraindications // 禁忌
    };
}
```

## 测试策略

### 专业功能测试
- **配伍检查测试**: 验证药物配伍禁忌检查的准确性
- **调配流程测试**: 测试完整的处方调配工作流
- **质量控制测试**: 验证质量控制措施的有效性

### 安全性测试
- **权限边界测试**: 测试不同级别药师的操作权限
- **数据完整性测试**: 验证调配记录的完整性和准确性
- **追溯性测试**: 测试药品从原料到成品的追溯能力

## 相关文档

- [LYBT.Desktop.Workbench.Core](../Core/README.md) - 工作台核心框架
- [LYBT.Desktop.Herbs](../../Modules/Herbs/README.md) - 中药材管理模块
- [药剂师操作指南](../../../docs/guides/pharmacist-operation-guide.md) - 药剂师专业操作手册
- [中医药配伍指南](../../../docs/guides/tcm-compatibility-guide.md) - 中药配伍禁忌参考
- [质量管理标准](../../../docs/guides/quality-management-standards.md) - 药品质量管理规范

---

**项目状态**: 🔄 开发中 (v1.0基础框架+中药材管理集成) | **最后更新**: 2025-01-01
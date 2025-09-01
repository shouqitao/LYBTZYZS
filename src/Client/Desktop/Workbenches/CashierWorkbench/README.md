# LYBT.Desktop.Workbench.Cashier

凌隐宝堂中医诊所系统 - 收银工作台模块

## 项目概述

收银工作台是专为收银员设计的财务管理环境，提供收费结算、账单管理、财务报表等核心功能。采用现代化WPF界面和Prism MVVM架构，支持完整的收银结算流程。

## 目录结构

```
CashierWorkbench/
├── ViewModels/                        # 视图模型
│   └── CashierMainViewModel.cs        # 收银主工作台视图模型
├── Views/                            # 用户界面
│   ├── BillingManagementView.xaml     # 账单管理视图
│   ├── BillingManagementView.xaml.cs  # 账单管理视图代码后置
│   ├── CashierMainView.xaml           # 收银主工作台视图
│   └── CashierMainView.xaml.cs        # 收银主视图代码后置
└── CashierWorkbenchModule.cs          # Prism模块定义
```

## 核心功能

### 1. 收银主工作台
- **统一收银界面**: 集成所有收银相关功能的中央操作台
- **快速结算**: 提供快速收费和结算操作
- **实时账单**: 显示当前待结算账单和收费状态

### 2. 账单管理 (BillingManagementView)
- **账单生成**: 基于处方和诊疗服务生成收费账单
- **费用明细**: 详细显示药品费用、诊疗费用等明细项
- **账单状态**: 管理账单的待付、已付、部分付款等状态
- **打印功能**: 支持账单和收据的打印输出

### 3. 收银流程管理
#### 标准收银流程
1. **账单查询**: 根据患者信息或处方号查询待结算账单
2. **费用确认**: 确认收费项目和金额
3. **收款操作**: 处理现金、刷卡、移动支付等收款方式
4. **票据打印**: 打印收费收据和发票
5. **账单归档**: 完成收银流程，更新账单状态

#### 退费流程
1. **退费申请**: 处理患者退费申请
2. **权限验证**: 验证退费操作权限
3. **退费处理**: 执行退费操作并更新账单
4. **退费凭证**: 打印退费凭证和相关票据

### 4. 预留功能模块
目前为未来扩展预留了以下功能：

- **PaymentManagementView**: 支付方式管理 (待实现)
- **FinancialReportsView**: 财务报表功能 (待实现)
- **收银统计**: 收银员业绩统计
- **财务对账**: 日终对账功能

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
- **状态管理**: 账单和支付状态的统一管理

## 模块注册

### CashierWorkbenchModule
Prism模块定义，负责收银工作台的初始化和服务注册：

```csharp
public class CashierWorkbenchModule : IModule
{
    public void RegisterTypes(IContainerRegistry containerRegistry)
    {
        // 注册收银员工作台主视图
        containerRegistry.RegisterForNavigation<CashierMainView>();
        
        // 注册占位视图
        containerRegistry.RegisterForNavigation<BillingManagementView>();
        
        // 预留：未来可注册收银相关的其他视图和服务
        // containerRegistry.RegisterForNavigation<PaymentManagementView>(); // 待实现
        // containerRegistry.RegisterForNavigation<FinancialReportsView>(); // 待实现
    }
    
    public void OnInitialized(IContainerProvider containerProvider)
    {
        // 注册自定义的ViewModel映射
        ViewModelLocationProvider.Register<CashierMainView, CashierMainViewModel>();
    }
}
```

## 用户界面

### 收银主工作台界面
- **患者信息区**: 显示当前患者的基本信息
- **账单明细区**: 显示待结算账单的详细项目
- **收费操作区**: 提供收款、找零、打印等操作按钮
- **历史记录区**: 显示当日收银记录和统计信息

### 账单管理界面
- **账单列表**: 显示所有待处理和已处理的账单
- **搜索筛选**: 按患者姓名、时间范围等条件筛选账单
- **操作按钮**: 提供查看、打印、退费等操作
- **状态标识**: 清晰标识账单的支付状态

## 业务流程

### 标准收费流程
```csharp
// 示例收费流程
public async Task<bool> ProcessPaymentAsync(PaymentRequest request)
{
    // 1. 验证账单
    var bill = await ValidateBillAsync(request.BillId);
    
    // 2. 处理支付
    var payment = await ProcessPaymentMethodAsync(request.PaymentMethod, request.Amount);
    
    // 3. 更新账单状态
    await UpdateBillStatusAsync(bill.Id, BillStatus.Paid);
    
    // 4. 打印收据
    await PrintReceiptAsync(bill, payment);
    
    // 5. 记录收银日志
    await LogCashierOperationAsync(request.CashierId, bill.Id, request.Amount);
    
    return true;
}
```

### 退费处理流程
```csharp
// 示例退费流程
public async Task<bool> ProcessRefundAsync(RefundRequest request)
{
    // 1. 验证退费权限
    await ValidateRefundPermissionAsync(request.CashierId);
    
    // 2. 查找原始账单
    var originalBill = await GetOriginalBillAsync(request.BillId);
    
    // 3. 创建退费记录
    var refund = await CreateRefundRecordAsync(originalBill, request.RefundAmount);
    
    // 4. 处理退款
    await ProcessRefundPaymentAsync(refund);
    
    // 5. 打印退费凭证
    await PrintRefundReceiptAsync(refund);
    
    return true;
}
```

## 集成接口

### 与业务模块的集成
- **处方模块**: 获取处方费用信息用于账单生成
- **患者模块**: 查询患者信息和医保状态
- **诊疗模块**: 获取诊疗费用和服务项目
- **用户模块**: 验证收银员权限和操作记录

### 财务系统集成
```csharp
// 财务数据同步示例
public async Task SyncFinancialDataAsync()
{
    // 同步收银数据到财务系统
    var dailyRevenue = await CalculateDailyRevenueAsync();
    await _financialService.SyncRevenueDataAsync(dailyRevenue);
    
    // 同步支付方式统计
    var paymentSummary = await GetPaymentMethodSummaryAsync();
    await _financialService.SyncPaymentSummaryAsync(paymentSummary);
}
```

## 权限管理

### 收银员权限
- **基础收银权限**: 处理日常收费和结算
- **退费权限**: 处理小额退费（可配置限额）
- **查询权限**: 查看收银记录和账单状态

### 管理员权限
- **大额退费**: 处理超过限额的退费申请
- **财务报表**: 查看详细的财务报表
- **系统配置**: 配置收费标准和支付方式

## 报表功能 (规划中)

### 收银日报
- **日收入统计**: 按支付方式分类的收入统计
- **收银员业绩**: 各收银员的收费笔数和金额
- **异常记录**: 退费、作废等异常操作记录

### 财务月报
- **月度收入趋势**: 收入变化趋势图
- **收费项目分析**: 各类收费项目的占比分析
- **欠费统计**: 未结清账单统计

## 开发状态

### 已实现功能
- ✅ 基础工作台框架
- ✅ 账单管理基础界面
- ✅ Prism模块注册和依赖注入

### 待实现功能 (v2.0)
- 🔄 支付方式管理 (PaymentManagementView)
- 🔄 财务报表功能 (FinancialReportsView)
- 🔄 收银统计和分析
- 🔄 财务对账功能
- 🔄 移动支付集成

## 开发指南

### 添加新收银功能
1. **创建视图**: 在Views目录创建对应的XAML文件
2. **创建视图模型**: 在ViewModels目录创建ViewModel
3. **注册模块**: 在CashierWorkbenchModule中注册新视图
4. **添加权限**: 配置相应的权限验证

### 集成支付方式
```csharp
// 添加新支付方式示例
public class MobilePaymentProvider : IPaymentProvider
{
    public string ProviderName => "移动支付";
    
    public async Task<PaymentResult> ProcessPaymentAsync(PaymentRequest request)
    {
        // 调用移动支付API
        return await CallMobilePaymentAPIAsync(request);
    }
}
```

## 测试策略

### 功能测试
- **收银流程测试**: 验证完整的收费流程
- **退费流程测试**: 测试各种退费场景
- **权限测试**: 验证不同角色的操作权限

### 集成测试
- **财务系统集成**: 测试与财务系统的数据同步
- **支付接口集成**: 测试各种支付方式的接口调用
- **打印功能测试**: 测试收据和报表的打印功能

## 相关文档

- [LYBT.Desktop.Workbench.Core](../Core/README.md) - 工作台核心框架
- [LYBT.Desktop.Workbench.Consultation](../ConsultationWorkbench/README.md) - 诊疗工作台
- [收银操作指南](../../../docs/guides/cashier-operation-guide.md) - 收银员操作手册
- [财务管理规范](../../../docs/guides/financial-management-guide.md) - 财务管理标准

---

**项目状态**: 🔄 开发中 (v1.0基础框架完成) | **最后更新**: 2025-01-01
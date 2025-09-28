# Issue #778: Desktop层MVP功能清理

## 问题描述
根据最新的MVP需求文档（mvp-requirements-final-2025-09-27.md），Desktop层存在大量超出设计范围的功能代码。这些功能在MVP阶段明确标记为"不实现"，但在代码中仍有大量残留，需要进行清理。

## 验收标准
- [ ] 移除所有收费管理相关代码
- [ ] 移除所有库存管理相关代码  
- [ ] 移除所有统计报表相关代码
- [ ] 移除所有智能建议相关代码
- [ ] 移除全文搜索相关代码
- [ ] 移除离线模式相关代码
- [ ] 清理后所有解决方案可正常编译
- [ ] 清理后核心功能正常运行

## 清理范围

### 1. 收费管理功能 [CLI-PAY]
**位置**: `src/Client/Desktop/Services/PermissionService.cs`
- 移除权限定义: PaymentProcess, InvoiceManagement, RefundProcess, PaymentReports, CashierReports
- 相关代码行: 197-200

### 2. 库存管理功能 [CLI-INV]
**涉及文件**:
- `src/Client/Desktop/Core/Converters/PerformanceConverters.cs` - StockStatusToColorConverter类
- `src/Client/Desktop/Core/Converters/StockStatusConverter.cs` - 整个文件
- `src/Client/Desktop/Core/Validation/CommonValidators.cs` - Stock验证规则(275-278行)
- `src/Client/Desktop/Modules/Herbs/Models/HerbItem.cs` - Stock相关属性(134-135, 248-303行)
- `src/Client/Desktop/Services/PermissionService.cs` - InventoryManagement权限(208行)

### 3. 统计报表功能 [CLI-STAT]
**涉及文件**:
- `src/Client/Desktop/Core/Async/AsyncOptimization.cs` - OperationStatistics类(337-419行)
- `src/Client/Desktop/Core/Events/EnhancedEventAggregator.cs` - EventStatistics类(446-552行)
- `src/Client/Desktop/Core/Events/EventManager.cs` - ManagerEventStatistics类(193-311行)
- `src/Client/Desktop/Core/Models/Cache/CacheEntry.cs` - CacheStatistics类(335-373行)
- `src/Client/Desktop/Core/Models/Cache/CacheOptions.cs` - EnableStatistics属性(30-33行)
- `src/Client/Desktop/Core/Services/Configuration/ConfigurationManagerService.cs` - ConfigurationStatistics类(62-66, 918-922行)
- `src/Client/Desktop/Core/Services/Configuration/FeatureToggleService.cs` - FeatureUsageStatistics类(59-63, 432-443, 765-769行)
- `src/Client/Desktop/Services/PermissionService.cs` - 各种Reports权限

### 4. 智能建议功能 [CLI-AI]
**涉及文件**:
- `src/Client/Desktop/Core/Services/Configuration/FeatureToggleService.cs` - SmartDiagnosis功能(107-111行)
- `src/Client/Desktop/Core/Managers/SearchManager.cs` - 智能搜索提及(第8行)
- `src/Client/Desktop/Shell/App.xaml.cs` - 智能模块加载提及(30, 138, 170, 197, 221行)
- `src/Client/Desktop/Modules/Consultation/ViewModels/ConsultationMainViewModel.cs` - 智能处理提及(18行)
- 所有包含"建议"、"推荐"、"Smart"、"Intelligent"的代码段

### 5. 全文搜索功能 [CLI-SEARCH]
- 当前未找到具体实现，但SearchManager类可能需要简化

### 6. 离线模式功能 [CLI-OFFLINE]
**涉及文件**:
- `src/Client/Desktop/Core/Converters/BooleanToOnlineBrushConverter.cs` - 整个文件
- `src/Client/Desktop/Core/Converters/BooleanToOnlineStatusConverter.cs` - 整个文件
- `src/Client/Desktop/Core/Converters/Unified/StatusToColorConverter.cs` - Offline相关(46行)
- `src/Client/Desktop/Core/Converters/Unified/UnifiedBooleanConverter.cs` - 离线状态(200行)

### 7. Web版本功能 [CLI-WEB]
- FolderBrowserDialog相关代码保留（这是文件选择对话框，不是Web功能）

## 清理策略

### 第一阶段：权限清理
1. 修改PermissionService.cs，移除所有非MVP权限定义
2. 确保权限系统仍能正常工作

### 第二阶段：模型清理
1. 清理HerbItem.cs中的库存相关属性
2. 保留基础药材信息功能

### 第三阶段：转换器清理
1. 删除库存状态转换器
2. 删除离线/在线状态转换器
3. 确保UI不受影响

### 第四阶段：统计功能清理
1. 移除或简化统计类
2. 保留必要的性能监控功能

### 第五阶段：智能功能清理
1. 移除所有"智能"、"建议"相关功能
2. 保留基础数据录入功能

## 风险评估
- **高风险**: 权限系统修改可能影响登录和功能访问
- **中风险**: 移除转换器可能导致XAML绑定错误
- **低风险**: 移除统计功能对核心业务影响较小

## 实施计划
1. 创建feature分支: `feature/desktop-mvp-cleanup`
2. 按模块并行执行清理（使用Serena MCP工具）
3. 每个模块清理后运行编译测试
4. 完成后进行集成测试
5. 提交PR进行代码审查

## 相关文档
- [MVP需求文档](../requirements/mvp-requirements-final-2025-09-27.md)
- [Desktop架构文档](../architecture/desktop-architecture-overview.md)

## 标签
`cleanup` `mvp` `desktop` `refactoring` `technical-debt`

## 优先级
**高** - MVP发布前必须完成

## 预计工时
- 权限清理: 1小时
- 模型清理: 2小时
- 转换器清理: 1小时
- 统计功能清理: 2小时
- 智能功能清理: 1小时
- 测试验证: 2小时
- **总计**: 9小时

## 负责人
AI助手 + 开发团队

## 创建日期
2025-09-28

## 更新记录
- 2025-09-28: 初始创建，识别需清理功能
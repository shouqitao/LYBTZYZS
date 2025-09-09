# 过度功能清场安全防护清单

**生成时间**: 2025-09-09  
**分析范围**: 反射/IoC/XAML/序列化等动态引用检测  
**风险等级**: 需人工确认的潜在引用  

---

## ⚠️ 执行前必读

**严重警告**: 以下清单包含可能被动态调用但静态分析难以检测的代码引用。在执行清理计划前，**必须逐项人工确认**这些符号是否被使用。

### 检测方法建议
1. **全局文本搜索**: 在IDE中搜索类名、方法名
2. **运行时测试**: 启动应用程序，执行核心业务流程
3. **XAML检查**: 检查所有.xaml文件中的绑定引用
4. **配置文件扫描**: 检查appsettings.json、IoC容器配置等
5. **序列化配置**: 检查Json.NET、AutoMapper等配置

---

## 🔍 反射调用疑似引用

### 1. ViewModel工厂动态创建 (高风险)
**涉及文件**: `ViewModelFactory.cs` (计划删除)

**疑似动态引用**:
```csharp
// 可能被反射调用的ViewModel类型
- PlaceholderPatientViewModel
- PlaceholderPrescriptionViewModel  
- PlaceholderConsultationViewModel
- PlaceholderReportViewModel
- PlaceholderSettingsViewModel
- PlaceholderHelpViewModel
```

**检查方法**:
```bash
# 搜索字符串形式的类型引用
grep -r "PlaceholderPatientViewModel" src/ --include="*.cs" --include="*.xaml" --include="*.json"
grep -r "PlaceholderPrescriptionViewModel" src/ --include="*.cs" --include="*.xaml" --include="*.json"
# ... 对每个Placeholder类重复此检查
```

**人工确认要求**: ✋ **必须确认无字符串形式的类型引用**

### 2. 事务步骤动态注册 (极高风险)
**涉及文件**: `Transactions/` 目录 (计划删除)

**疑似动态引用**:
```csharp
// 可能被反射扫描和注册的事务步骤
- CreatePrescriptionStep
- ValidatePrescriptionStep  
- CalculatePriceStep
- SavePrescriptionStep
- NotifyCompletionStep
- ValidateCompatibilityStep
- ValidatePrerequisitesStep
- UpdateMedicalCaseStep
```

**已知风险**: 🚨 **90个编译错误证实存在静态引用**

**检查方法**:
```bash
# 搜索事务步骤的注册配置
grep -r "TransactionStep" src/ --include="*.cs" --include="*.json"
grep -r "ITransactionStep" src/ --include="*.cs"
grep -r "CreatePrescriptionStep" src/ --include="*.cs"
```

**人工确认要求**: ✋ **必须先解决90个编译错误再删除**

### 3. Redux状态动作类型 (中等风险)
**涉及文件**: `Redux/StateActions.cs` (计划删除)

**疑似动态引用**:
```csharp
// 可能被字符串形式引用的动作类型
- "USER_LOGIN"
- "USER_LOGOUT"  
- "PATIENT_SELECTED"
- "LOADING_START"
- "LOADING_END"
- "NAVIGATION_CHANGE"
```

**检查方法**:
```bash
# 搜索动作类型字符串
grep -r "USER_LOGIN" src/ --include="*.cs" --include="*.xaml" --include="*.json"
grep -r "PATIENT_SELECTED" src/ --include="*.cs"
```

**人工确认要求**: ✋ **确认无硬编码的动作类型字符串**

---

## 🎯 XAML绑定引用检查

### 1. 测试视图绑定 (中等风险)
**涉及文件**: `TestView.xaml` (计划删除)

**XAML绑定检查**:
```xml
<!-- 需要确认的绑定引用 -->
<Button Command="{Binding TestCommand}"/>
<TextBlock Text="{Binding TestMessage}"/>
<Grid DataContext="{Binding TestViewModel}"/>
```

**检查方法**:
```bash
# 搜索XAML中的测试相关绑定
grep -r "TestCommand" src/ --include="*.xaml"
grep -r "TestMessage" src/ --include="*.xaml" 
grep -r "TestViewModel" src/ --include="*.xaml"
```

**人工确认要求**: ✋ **确认无其他XAML文件引用测试绑定**

### 2. 占位符属性绑定 (低风险)
**涉及文件**: `PlaceholderViewModels.cs` (计划删除)

**潜在绑定属性**:
```csharp
// 可能被XAML绑定的属性
- PatientName
- SaveCommand
- IsSelected
- DisplayText
- Status
```

**检查方法**:
```bash
# 搜索占位符属性的XAML绑定
find src/ -name "*.xaml" -exec grep -l "PatientName\|SaveCommand" {} \;
```

**人工确认要求**: ✋ **确认占位符属性未被实际界面使用**

---

## 📋 IoC容器注册检查

### 1. 服务注册配置 (极高风险)
**涉及文件**: 依赖注入配置文件

**需要检查的注册项**:
```csharp
// Prism模块注册 - 检查App.xaml.cs或Bootstrapper
services.AddTransient<PlaceholderPatientViewModel>();
services.AddTransient<PlaceholderPrescriptionViewModel>();

// 工厂注册
services.AddSingleton<IViewModelFactory, ViewModelFactory>();

// 事务服务注册  
services.AddScoped<ITransactionCoordinator<CreatePrescriptionRequest>, PrescriptionTransactionCoordinator>();

// Redux服务注册
services.AddSingleton<IStateStore, StateStore>();
```

**检查文件清单**:
```bash
# 检查依赖注入配置
grep -r "PlaceholderPatientViewModel" src/ --include="*.cs"
grep -r "IViewModelFactory" src/ --include="*.cs"
grep -r "ITransactionCoordinator" src/ --include="*.cs"
grep -r "IStateStore" src/ --include="*.cs"
```

**人工确认要求**: ✋ **删除文件前必须先清理IoC注册**

### 2. Prism模块配置 (高风险)
**检查文件**: `src/Client/Desktop/App.xaml.cs`

**潜在模块注册**:
```csharp
// 检查模块目录注册
protected override void ConfigureModuleCatalog(IModuleCatalog moduleCatalog)
{
    // 可能包含对删除文件的引用
    moduleCatalog.AddModule<ShellModule>();
    // 检查是否注册了测试模块
}

// 检查视图-ViewModel映射
protected override void ConfigureViewModelLocator()
{
    // 可能包含对Placeholder ViewModel的映射
}
```

**人工确认要求**: ✋ **确认Prism配置中无对删除文件的引用**

---

## 🔧 序列化配置检查

### 1. AutoMapper配置 (中等风险)
**检查文件**: 所有`MappingProfile.cs`文件

**潜在映射配置**:
```csharp
// 可能存在的映射配置
CreateMap<PlaceholderPatientViewModel, PatientDto>();
CreateMap<TestDataModel, TestViewModel>();

// 检查是否有对删除类型的映射
```

**检查方法**:
```bash
find src/ -name "*MappingProfile.cs" -exec grep -l "Placeholder\|Test" {} \;
```

**人工确认要求**: ✋ **删除类型前必须先删除相关映射**

### 2. JSON序列化配置 (低风险)
**检查文件**: `JsonSerializerOptions`配置

**潜在序列化类型**:
```csharp
// 检查是否有自定义序列化器配置
options.Converters.Add(new PlaceholderConverter());
options.Converters.Add(new TestDataConverter());
```

**检查方法**:
```bash
grep -r "JsonConverter" src/ --include="*.cs"
grep -r "JsonSerializer" src/ --include="*.cs"
```

---

## 🚨 高风险操作确认清单

### 操作前必检项 (阻塞性)
- [ ] **编译错误解决**: 90个事务相关编译错误必须先解决
- [ ] **IoC注册清理**: 所有被删除类型的依赖注入注册必须先移除  
- [ ] **Prism模块配置**: App.xaml.cs中的模块和视图注册必须检查
- [ ] **XAML绑定验证**: 所有.xaml文件中的绑定引用必须确认

### 操作中监控项 (警告性)
- [ ] **运行时测试**: 删除后必须启动应用验证核心功能
- [ ] **单元测试**: 运行所有单元测试确保无新增失败
- [ ] **集成测试**: 执行端到端测试验证业务流程  
- [ ] **性能监控**: 监控内存使用和响应时间变化

### 操作后验证项 (确认性)
- [ ] **功能回归**: 完整的手工测试所有核心业务功能
- [ ] **错误日志**: 检查应用日志确保无新增异常
- [ ] **用户体验**: 确认界面响应正常，无缺失组件
- [ ] **回滚就绪**: 确保Git分支和回滚脚本准备就绪

---

## 🔍 动态引用检测工具推荐

### 静态分析工具
1. **Visual Studio**: "查找所有引用" (Shift+F12)
2. **ReSharper**: "Find Usages" 功能
3. **Rider**: 全局搜索和依赖分析
4. **SonarQube**: 死代码检测

### 运行时分析工具  
1. **dotTrace**: 运行时方法调用分析
2. **PerfView**: .NET运行时事件跟踪
3. **Application Insights**: 运行时遥测数据
4. **自定义日志**: 在可疑方法中添加临时日志

### 检测脚本示例
```bash
#!/bin/bash
# 全面的引用检测脚本

echo "=== 检测反射调用引用 ==="
grep -r "typeof(" src/ --include="*.cs" | grep -E "(Placeholder|Test|Transaction|Redux)"

echo "=== 检测字符串类型引用 ==="  
grep -r '".*ViewModel"' src/ --include="*.cs" --include="*.xaml"

echo "=== 检测XAML绑定引用 ==="
find src/ -name "*.xaml" -exec grep -H "Binding.*Test\|Binding.*Placeholder" {} \;

echo "=== 检测IoC容器注册 ==="
grep -r "AddTransient\|AddSingleton\|AddScoped" src/ --include="*.cs" | grep -E "(Placeholder|Test|Factory)"
```

---

## 📞 应急响应计划

### 发现遗漏引用时
1. **立即停止清理操作**
2. **记录发现的引用位置和类型**  
3. **评估影响范围和修复成本**
4. **更新清理计划，调整删除顺序**
5. **重新执行完整的引用检测**

### 清理后发现问题时  
1. **立即执行Git分支回滚**
2. **记录问题现象和错误信息**
3. **分析根本原因和遗漏的引用**
4. **更新safeguards.md文档**  
5. **制定修复计划和预防措施**

---

## ✅ 安全确认签名区

在开始执行清理计划前，项目负责人必须确认以下检查项：

**技术负责人确认** (签名: _____________ 日期: _______)
- [ ] 已完成所有反射调用引用检查
- [ ] 已完成所有XAML绑定引用检查  
- [ ] 已完成所有IoC容器注册检查
- [ ] 已准备完整的回滚方案

**测试负责人确认** (签名: _____________ 日期: _______)  
- [ ] 已准备完整的回归测试计划
- [ ] 已确认核心业务功能测试用例
- [ ] 已准备性能基准对比数据
- [ ] 已确认测试环境和生产环境一致性

**项目经理确认** (签名: _____________ 日期: _______)
- [ ] 已评估业务风险和影响范围
- [ ] 已准备用户沟通和应急响应计划  
- [ ] 已确认清理时间窗口和资源安排
- [ ] 已获得相关干系人的批准

---

**风险提醒**: 此清单并非100%完整，仍可能存在未检测到的动态引用。建议在非生产环境中完整验证所有功能后，再考虑在生产环境执行清理操作。

**生成工具**: Claude Code Assistant  
**检测覆盖**: 反射调用、XAML绑定、IoC注册、序列化配置  
**建议更新频率**: 每次代码结构重大变更后
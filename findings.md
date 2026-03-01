# Findings: Phase 5 Desktop UI 拆分 + 测试补全

## XAML 拆分发现

### StaticResource 跨文件解析
- WPF MergedDictionaries 中，先加载的字典资源可被后加载的字典通过 StaticResource 引用
- 关键: DesignTokens.xaml 必须在其他样式文件之前 merge
- BasedOn 链 (如 DangerButton BasedOn PrimaryButton) 必须在同一文件内

### 原始文件中的编码问题
- UnifiedComponents.xaml 中有大量 GB2312 乱码注释 (例如 `Type Ramp - 字号大小系统` 显示为 `Type Ramp - xxxxCxxx`)
- 拆分后在新文件中使用正确 UTF-8 中文注释替换

## ServiceCollectionExtensions 拆分发现

### 命名空间依赖分析
- LoggingRegistrationExtensions 需要引用 ~35 个命名空间 (涉及所有模块的 Logger 注册)
- HttpServiceRegistrationExtensions 需要 Security/Contracts/Foundation 命名空间
- 拆分后 ServiceCollectionExtensions 保留 Foundation/Infrastructure/Shell 服务注册

### 死代码确认
- ErrorHandlingServiceExtensions.cs: `RegisterErrorHandlingAndLogging()` 从未被调用
- Styles/CommonStyles.xaml: 不在 App.xaml MergedDictionaries 中

## 测试发现

### Architecture 测试修复
- Batch2_ConfigurationDirectRead: 原逻辑注释后仍有效，WebAPI 层无直接配置读取方法
- Should_Use_Unified_Navigation_Service: 改为检查 ViewModel 构造函数不注入 IRegionManager

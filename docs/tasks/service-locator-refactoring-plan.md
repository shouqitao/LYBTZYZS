# Service Locator 反模式重构计划

## 问题分析

经过代码扫描，发现以下使用Service Locator反模式的位置：

### 1. Desktop Shell (App.xaml.cs)
**问题代码位置**：
- 第42行：`Container.Resolve<MainWindow>()`
- 第75行：`Container.Resolve<MainWindowViewModel>()`
- 第92行：`Container.Resolve<IApplicationInitializationService>()`
- 第112行：`Container.Resolve<IStartupOptimizationService>()`
- 第131行：`Container.Resolve<IErrorHandlingService>()`
- 第158行：`Container.Resolve<ILogger<App>>()`
- 第312-314行：多个Container.Resolve调用

**影响**：
- App.xaml.cs 中大量使用Container.Resolve
- 违反了依赖倒置原则
- 难以进行单元测试
- 增加了耦合度

### 2. Server端 KeyRotationBackgroundService
**问题代码位置**：
- 使用IServiceProvider直接解析服务
- 第52行：`scope.ServiceProvider.GetRequiredService<IKeyManagementService>()`

**影响**：
- 后台服务依赖ServiceProvider
- 违反了显式依赖原则

## 重构策略

### Phase 1：Desktop端App.xaml.cs重构

#### 目标
将App.xaml.cs中的所有Container.Resolve调用改为构造函数注入或工厂模式

#### 方案

1. **CreateShell方法**
   - 保留Container.Resolve调用（这是Prism框架要求的标准做法）

2. **ConfigureViewModelLocator**
   - 创建ViewModelFactory服务
   - 通过工厂模式解决ViewModel创建

3. **OnInitialized方法**
   - 创建ApplicationBootstrapper服务
   - 将所有初始化逻辑移到该服务中
   - App类只负责调用bootstrapper

4. **模块加载逻辑**
   - 创建ModuleLoadingService
   - 封装模块加载的所有逻辑

### Phase 2：Server端KeyRotationBackgroundService重构

#### 目标
移除IServiceProvider依赖，使用工厂模式或Func<T>注入

#### 方案

1. **使用工厂模式**
   ```csharp
   public interface IKeyManagementServiceFactory
   {
       IKeyManagementService CreateKeyManagementService();
   }
   ```

2. **注入工厂而非ServiceProvider**
   ```csharp
   private readonly IKeyManagementServiceFactory _keyManagementServiceFactory;
   ```

## 实施步骤

### Step 1：创建必要的服务接口和实现
- [ ] IApplicationBootstrapper
- [ ] IViewModelFactory
- [ ] IModuleLoadingService
- [ ] IKeyManagementServiceFactory

### Step 2：重构Desktop端
- [ ] 实现ApplicationBootstrapper
- [ ] 重构App.OnInitialized方法
- [ ] 实现ViewModelFactory
- [ ] 重构ConfigureViewModelLocator
- [ ] 实现ModuleLoadingService
- [ ] 重构模块加载逻辑

### Step 3：重构Server端
- [ ] 实现KeyManagementServiceFactory
- [ ] 重构KeyRotationBackgroundService
- [ ] 更新DI容器注册

### Step 4：测试验证
- [ ] 编写单元测试
- [ ] 运行集成测试
- [ ] 验证应用程序启动
- [ ] 验证模块加载
- [ ] 验证密钥旋转功能

## 预期收益

1. **提高可测试性**
   - 所有依赖显式声明
   - 易于Mock和测试

2. **降低耦合度**
   - 消除对DI容器的直接依赖
   - 符合SOLID原则

3. **提高代码可维护性**
   - 依赖关系清晰
   - 易于理解和修改

4. **更好的错误处理**
   - 编译时即可发现依赖问题
   - 而非运行时才暴露

## 风险评估

- **低风险**：重构影响范围可控
- **测试覆盖**：需要确保充分的测试覆盖
- **回退计划**：保留原有分支，可随时回退

## 时间估计

- Desktop端重构：16小时
- Server端重构：8小时
- 测试编写：6小时
- 总计：30小时
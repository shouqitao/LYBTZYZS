# #757 Service Locator反模式重构 - 任务清单

## 任务概述
重构系统中的Service Locator反模式，采用标准的依赖注入模式。

## 分析结果
- **影响范围**：主要集中在WPF客户端启动流程（App.xaml.cs）
- **服务器端**：设计良好，已采用构造函数注入
- **总工作量**：约22小时（3个工作日）

## 详细任务清单

### Phase 1: 准备工作（2小时）
- [ ] 创建重构分支：`feature/remove-service-locator`
- [ ] 备份当前App.xaml.cs
- [ ] 设置单元测试项目：LYBT.Desktop.Shell.Tests
- [ ] 创建AppBootstrapper类框架

### Phase 2: 重构App.xaml.cs（8小时）

#### 2.1 创建AppBootstrapper（3小时）
```csharp
// 文件：src/Client/Desktop/Shell/LYBT.Desktop.Shell/Bootstrap/AppBootstrapper.cs
public class AppBootstrapper
{
    private readonly IContainerProvider _container;
    private readonly IModuleCatalog _moduleCatalog;
    private readonly IRegionManager _regionManager;
    
    public AppBootstrapper(
        IContainerProvider container,
        IModuleCatalog moduleCatalog,
        IRegionManager regionManager)
    {
        _container = container;
        _moduleCatalog = moduleCatalog;
        _regionManager = regionManager;
    }
    
    public Window CreateShell()
    {
        return _container.Resolve<ShellWindow>();
    }
    
    public async Task InitializeApplicationAsync()
    {
        await CheckSystemRequirements();
        await InitializeServices();
        ConfigureUserInterface();
    }
}
```

#### 2.2 重构CreateShell方法（2小时）
- [ ] 移除Container.Resolve<ShellWindow>()
- [ ] 使用AppBootstrapper.CreateShell()
- [ ] 更新Shell初始化逻辑

#### 2.3 重构OnInitialized方法（3小时）
- [ ] 移除6处Container.Resolve调用
- [ ] 通过构造函数注入所需服务
- [ ] 重构服务初始化流程

### Phase 3: 依赖注入配置更新（4小时）

#### 3.1 更新RegisterTypes（2小时）
- [ ] 注册AppBootstrapper
- [ ] 配置生命周期管理
- [ ] 添加缺失的服务注册

#### 3.2 创建服务工厂（2小时）
```csharp
public interface IServiceFactory
{
    T CreateService<T>() where T : class;
}

public class ServiceFactory : IServiceFactory
{
    private readonly IContainerProvider _container;
    
    public ServiceFactory(IContainerProvider container)
    {
        _container = container;
    }
    
    public T CreateService<T>() where T : class
    {
        return _container.Resolve<T>();
    }
}
```

### Phase 4: 单元测试（6小时）

#### 4.1 AppBootstrapper测试（2小时）
- [ ] 测试CreateShell方法
- [ ] 测试InitializeApplicationAsync
- [ ] 测试异常处理

#### 4.2 App类测试（2小时）
- [ ] 测试应用启动流程
- [ ] 测试模块加载
- [ ] 测试导航初始化

#### 4.3 集成测试（2小时）
- [ ] 端到端启动测试
- [ ] 模块注册验证
- [ ] 服务解析验证

### Phase 5: 验收和文档（2小时）
- [ ] 代码审查
- [ ] 更新架构文档
- [ ] 性能测试对比
- [ ] 创建PR

## 验收标准
- [ ] 所有Service Locator使用已移除
- [ ] 所有单元测试通过
- [ ] 应用正常启动
- [ ] 模块正常加载
- [ ] 代码审查通过

## 风险和缓解
| 风险 | 影响 | 缓解措施 |
|-----|------|----------|
| 启动失败 | 高 | 保留回滚分支 |
| 模块加载顺序问题 | 中 | 详细测试各种场景 |
| 性能影响 | 低 | 进行性能基准测试 |

## 相关文件
- src/Client/Desktop/Shell/LYBT.Desktop.Shell/App.xaml.cs
- src/Client/Desktop/Shell/LYBT.Desktop.Shell/Bootstrap/AppBootstrapper.cs
- tests/Desktop/Shell.Tests/AppBootstrapperTests.cs

## 进度跟踪
- 开始时间：_____
- 预计完成：_____
- 实际完成：_____
- 执行人：_____
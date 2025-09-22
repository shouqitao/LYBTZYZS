# Desktop UltraThink架构重构计划 (2025版)

> 基于：Desktop-Prism-Issues-Status-Report.md
> 版本：Prism 8.1.97
> 日期：2025-01-23

## 重构目标

1. **解决Module命名混淆**：将业务Module重命名为Service
2. **集中导航管理**：创建统一的NavigationService
3. **完善生命周期文档**：明确服务注册策略
4. **保持架构稳定**：维持UltraThink三层架构（Service + QueryService + BusinessService）

## 重构策略

### Phase 1: 业务Module重命名 (避免与Prism IModule混淆)

#### 重命名方案
```
AuthModule → AuthService
UserModule → UserService
PatientModule → PatientService
HerbModule → HerbService
FormulaModule → FormulaService
ConsultationModule → ConsultationService
PrescriptionsModule → PrescriptionsService
MedicalCaseModule → MedicalCaseService
```

#### 保持不变
```
// Prism模块（实现IModule接口）
AuthenticationModule : IModule
UsersModule : IModule
PatientsModule : IModule
// ... 其他Prism模块
```

### Phase 2: 创建集中式NavigationService

#### 新增服务
```csharp
public interface INavigationService
{
    void NavigateTo(string viewName, NavigationParameters parameters = null);
    void NavigateTo(string regionName, string viewName, NavigationParameters parameters = null);
    void NavigateBack();
    bool CanNavigateBack { get; }
    string CurrentView { get; }
    event EventHandler<NavigationEventArgs> Navigated;
}
```

### Phase 3: 服务生命周期规范

#### 生命周期策略
- **Singleton**: 基础设施、认证、会话管理
- **Scoped**: 业务服务、API客户端
- **Transient**: 临时处理器、工厂产品

## 执行步骤

1. 重命名所有业务Module类文件
2. 更新类名和接口实现
3. 更新ServiceCollectionExtensions注册
4. 创建NavigationService实现
5. 替换分散的导航调用
6. 添加架构决策文档
7. 运行测试验证

## 风险评估

- **低风险**：重命名仅影响内部实现
- **中风险**：导航服务需要全面测试
- **影响范围**：约50个文件需要更新引用

## 预期收益

1. 消除Module命名歧义
2. 统一导航管理
3. 提高代码可维护性
4. 符合UltraThink架构原则
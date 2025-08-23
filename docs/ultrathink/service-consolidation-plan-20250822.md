# WPF客户端服务优化计划

## 📊 当前状态分析

**服务统计**:
- 总服务数量: 78
- 目标服务数量: 40
- 需要优化: 38个服务

**按类型分布**:
- Singleton: 33个 (需要合并)
- Navigation: 41个 (需要删除未使用的)
- Transient: 3个
- Instance: 1个

## 🎯 优化策略

### Phase 1: 删除未使用的Navigation视图 (预计减少20-25个)

#### 1.1 工作台模块简化
**删除实验性/未完成的工作台**:
- CashierWorkbenchModule (4个视图) - 未实现完整功能
- PharmacistWorkbenchModule (5个视图) - 功能重复
- ReceptionistWorkbenchModule (4个视图) - 功能可整合到Patients模块
- TherapistWorkbenchModule (4个视图) - 非核心功能
- SystemWorkbenchModule (保留 - 系统管理必需)

**保留核心工作台**:
- ConsultationWorkbenchModule (2个视图) - 核心诊疗功能

#### 1.2 模块内视图精简
**Prescriptions模块**:
- 删除: PrescriptionView (已被PrescriptionComposerView替代)
- 删除: FormulaTemplateDialog (功能重复)
- 保留: PrescriptionComposerView, PrescriptionsMainView, PrescriptionManagementView

**其他模块**:
- 合并重复的Add/Edit对话框
- 删除临时/测试视图

### Phase 2: 合并冗余的Singleton服务 (预计减少10-15个)

#### 2.1 缓存服务合并
**当前问题**:
```csharp
// 重复的缓存服务
containerRegistry.RegisterSingleton<IMemoryCache, MemoryCache>();
containerRegistry.RegisterSingleton<LYBT.Desktop.Services.MemoryCacheService>();
```

**解决方案**:
```csharp
// 统一使用IMemoryCache
containerRegistry.RegisterSingleton<IMemoryCache, MemoryCache>();
// 删除MemoryCacheService，直接使用IMemoryCache
```

#### 2.2 会话管理服务合并
**当前问题**:
```csharp
// 功能重叠的会话服务
containerRegistry.RegisterSingleton<ITokenManager, TokenManager>();
containerRegistry.RegisterSingleton<IUserSessionManager, UserSessionManager>();
containerRegistry.RegisterSingleton<ISessionManager, SessionManager>();
```

**解决方案**:
```csharp
// 合并为单一的SessionService
containerRegistry.RegisterSingleton<ISessionService, UnifiedSessionService>();
```

#### 2.3 对话框服务合并
**当前问题**:
```csharp
// 重复的对话框服务
containerRegistry.RegisterSingleton<ICustomDialogService, WpfDialogService>();
containerRegistry.RegisterSingleton<SimpleDialogService>();
```

**解决方案**:
```csharp
// 统一使用一个对话框服务
containerRegistry.RegisterSingleton<IDialogService, UnifiedDialogService>();
```

#### 2.4 API服务简化
**当前问题**:
- 通用ApiService和具体的API接口重复
- 多个API模块可能未被使用

**解决方案**:
- 审查实际使用的API接口
- 删除未使用的API模块
- 合并相似功能的API服务

### Phase 3: 模块结构简化 (预计减少3-5个)

#### 3.1 删除独立的API模块
**评估删除**:
- AuthApi模块 (如果功能简单可直接整合)
- ConsultationApi模块
- FormulaApi模块
- HerbsApi模块
- MedicalCaseApi模块
- PatientsApi模块
- PrescriptionsApi模块

**整合方案**:
- API接口直接在主模块中注册
- 减少模块文件数量

## 📋 具体实施计划

### Week 3.1: Navigation清理 (减少20个)
1. 删除4个实验性工作台模块
2. 清理模块内未使用的视图
3. 合并重复的对话框视图

### Week 3.2: Service合并 (减少15个)
1. 实现UnifiedSessionService
2. 实现UnifiedDialogService
3. 简化缓存服务架构
4. 合并API服务

### Week 3.3: 模块结构优化 (减少3个)
1. 评估并删除未使用的API模块
2. 简化服务注册流程
3. 验证功能完整性

## 🎯 预期结果

**优化后的服务分布**:
- Singleton: 18个 (减少15个)
- Navigation: 18个 (减少23个)
- Transient: 3个 (保持)
- Instance: 1个 (保持)
- **总计: 40个服务**

**主要保留的服务**:
1. 核心业务服务 (Auth, Users, Patients, Consultation, etc.)
2. 必要的基础设施服务 (Logging, AutoMapper, HTTP)
3. 核心导航视图 (主要业务功能)
4. 系统管理服务

## ✅ 质量保证

1. **功能完整性检查**: 确保核心业务功能不受影响
2. **依赖关系验证**: 检查服务间依赖是否正确
3. **集成测试**: 验证合并后的服务正常工作
4. **性能监控**: 确保优化后启动时间改善

## 📝 风险控制

1. **渐进式实施**: 分阶段进行，每次只删除/合并少量服务
2. **备份现有配置**: 保留当前注册代码作为回滚选项
3. **功能测试**: 每次修改后进行基本功能测试
4. **文档更新**: 及时更新服务依赖文档
# Tasks: refactor-viewmodel-layer

## Phase 1: 创建ViewModel设计规范

### 1.1 创建viewmodel-conventions spec
- [ ] 1.1.1 定义VM-001 ViewModel大小限制规范
- [ ] 1.1.2 定义VM-002 Components分层模式规范
- [ ] 1.1.3 定义VM-003 命令初始化模式规范
- [ ] 1.1.4 定义VM-004 错误处理模式规范
- [ ] 1.1.5 定义VM-005 异步模式一致性规范
- [ ] 1.1.6 定义VM-006 导航模式规范
- [ ] 1.1.7 定义VM-007 基类继承规范

### 1.2 验证规范
- [ ] 1.2.1 运行 `openspec validate refactor-viewmodel-layer --strict`
- [ ] 1.2.2 确保与service-conventions、client-api-conventions无冲突

## Phase 2: MedicalCase模块Components分层

### 2.1 创建Components基础结构
- [ ] 2.1.1 创建 `MedicalCase/ViewModels/Components/` 目录
- [ ] 2.1.2 创建 `MedicalCaseCommandHandler.cs` - CRUD操作
- [ ] 2.1.3 创建 `MedicalCaseDataManager.cs` - 数据加载和缓存
- [ ] 2.1.4 创建 `MedicalCaseValidator.cs` - 业务规则验证
- [ ] 2.1.5 创建 `MedicalCaseStateManager.cs` - 状态机管理

### 2.2 重构MedicalCaseWorkspaceViewModel
- [ ] 2.2.1 提取患者信息管理逻辑到DataManager
- [ ] 2.2.2 提取医案CRUD逻辑到CommandHandler
- [ ] 2.2.3 提取验证逻辑到Validator
- [ ] 2.2.4 提取状态管理逻辑到StateManager
- [ ] 2.2.5 更新ViewModel为协调器模式
- [ ] 2.2.6 确保重构后行数 < 400行

### 2.3 更新DI注册
- [ ] 2.3.1 在 `MedicalCaseModule.cs` 注册新Components
- [ ] 2.3.2 更新 `MedicalCaseWorkspaceViewModel` 构造函数注入

### 2.4 单元测试
- [ ] 2.4.1 为 `MedicalCaseCommandHandler` 编写单元测试
- [ ] 2.4.2 为 `MedicalCaseDataManager` 编写单元测试
- [ ] 2.4.3 为 `MedicalCaseValidator` 编写单元测试
- [ ] 2.4.4 为 `MedicalCaseStateManager` 编写单元测试
- [ ] 2.4.5 更新 `MedicalCaseWorkspaceViewModel` 单元测试

## Phase 3: 代码模式统一

### 3.1 命令初始化工厂
- [ ] 3.1.1 创建 `CommandFactory.cs` 在 `LYBT.Desktop.Foundation/Commands/`
- [ ] 3.1.2 实现 `CreateAsyncWithLoadingGuard()` 方法
- [ ] 3.1.3 实现 `CreateWithParameter<T>()` 方法
- [ ] 3.1.4 为CommandFactory编写单元测试

### 3.2 错误处理增强
- [ ] 3.2.1 在 `ViewModelBase` 添加 `ExecuteWithErrorHandlingAsync()` 方法
- [ ] 3.2.2 更新方法文档和使用示例
- [ ] 3.2.3 为新方法编写单元测试

### 3.3 迁移现有代码（可选，按需）
- [ ] 3.3.1 评估哪些ViewModel需要迁移到新模式
- [ ] 3.3.2 迁移高优先级ViewModel（如PatientManagementViewModel）
- [ ] 3.3.3 迁移其他中型ViewModel

## Phase 4: 验证和文档

### 4.1 集成验证
- [ ] 4.1.1 运行所有单元测试
- [ ] 4.1.2 执行医案模块手动测试（创建、编辑、关闭流程）
- [ ] 4.1.3 验证编译无错误无警告

### 4.2 文档更新
- [ ] 4.2.1 更新 `docs/` 相关开发指南
- [ ] 4.2.2 添加Components模式示例代码

## 验收标准

- [ ] viewmodel-conventions spec通过 `openspec validate --strict`
- [ ] MedicalCaseWorkspaceViewModel行数 < 400行
- [ ] 所有新Components有单元测试
- [ ] 编译通过，0 errors, 0 warnings
- [ ] 医案模块功能测试通过

## 依赖关系

```
Phase 1 (规范)
    │
    ├─► Phase 2 (MedicalCase重构)
    │       │
    │       └─► Phase 4 (验证)
    │
    └─► Phase 3 (代码模式)
            │
            └─► Phase 4 (验证)
```

**说明**: Phase 2和Phase 3可以并行执行，都依赖Phase 1完成。

## 工作量估算

| Phase | 预计工作量 | 风险等级 |
|-------|-----------|---------|
| Phase 1 | 小 (规范文档) | 低 |
| Phase 2 | 中 (核心重构) | 中 |
| Phase 3 | 小 (辅助工具) | 低 |
| Phase 4 | 小 (验证) | 低 |

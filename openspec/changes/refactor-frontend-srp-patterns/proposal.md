# refactor-frontend-srp-patterns

## Why

前端架构分析发现48个问题，整体评分6.8/10。核心问题集中在：
1. **SRP违规** - 多个核心类职责过重（500+行，4-8个职责）
2. **代码重复** - Master-Detail控件、角色层View存在40-50%重复
3. **架构风险** - ElementName绑定跨NameScope、缓存键无用户隔离

### 发现的问题

#### HIGH Priority (必须解决)

| # | 位置 | 问题类型 | 当前状态 | 期望状态 |
|---|------|----------|----------|----------|
| H1 | MedicalCaseService | SRP违规 | 605行/4职责 | 拆分为4个专职服务 |
| H2 | UserMasterDetailViewModel | SRP违规 | 584行/8职责 | 提取Handler组件 |
| H3 | MasterDetailViewModelBase | 过重 | 563行/6职责 | 组合模式重构 |
| H4 | XAML ElementName绑定 | 架构风险 | 跨NameScope | 统一RelativeSource |
| H5 | TokenManager缓存键 | 安全隐患 | 无用户隔离 | 添加UserId前缀 |

#### MEDIUM Priority (短期改进)

| # | 位置 | 问题类型 | 当前状态 | 期望状态 |
|---|------|----------|----------|----------|
| M1 | PatientService | 位置错误 | ViewModels/Components/ | Services/ |
| M2 | 对话框服务 | 重复 | 3个相似类 | 合并为统一服务 |
| M3 | 对话框ViewModel | 继承不一致 | 3个直接继承ObservableObject | 继承DialogViewModelBase |
| M4 | ViewModel构造函数 | 参数过多 | 6-8个参数 | IViewModelServices聚合 |
| M5 | Master-Detail控件 | 代码重复 | 40-50%重复 | 提取公共基类 |
| M6 | 角色层View | 完全重复 | AdminHome/ClinicalHome相似 | 模板化 |

#### LOW Priority (长期优化)

| # | 位置 | 问题类型 | 当前状态 | 期望状态 |
|---|------|----------|----------|----------|
| L1 | Repository参数顺序 | 不一致 | GetByIdAsync参数顺序不同 | 统一签名 |
| L2 | Mapper注册 | 可优化 | 手动注册 | 自动扫描注册 |
| L3 | 日志前缀 | 不统一 | [SVC]/[VM]/[HDL]混用 | 统一[层级]格式 |
| L4 | 异步命名 | 不完整 | 部分缺少Async后缀 | 全部添加Async |
| L5 | XML注释 | 覆盖不足 | 约60%覆盖率 | 公开API 100% |

### 影响分析

- **SRP违规影响**: 代码难以测试、难以维护、变更风险高
- **重复代码影响**: 修改需要同步多处、容易遗漏
- **架构风险影响**: ElementName绑定在Template/DataTemplate中可能失效

## What Changes

### Phase 1: SRP核心修复 (HIGH H1-H3)

**1.1 MedicalCaseService拆分**

将605行的MedicalCaseService拆分为：
- `MedicalCaseQueryService` - 查询职责
- `MedicalCasePersistenceService` - 持久化职责
- `MedicalCaseValidationService` - 验证职责
- `MedicalCaseLifecycleService` - 生命周期职责

**1.2 UserMasterDetailViewModel重构**

提取Handler组件：
- `UserSearchHandler` - 搜索逻辑
- `UserSelectionHandler` - 选择逻辑
- `UserEditHandler` - 编辑逻辑
- `UserPermissionHandler` - 权限逻辑

**1.3 MasterDetailViewModelBase优化**

采用组合模式：
- `MasterListComponent` - 列表管理
- `DetailViewComponent` - 详情视图
- `EditModeComponent` - 编辑模式
- `SearchComponent` - 搜索过滤

### Phase 2: 架构风险修复 (HIGH H4-H5)

**2.1 ElementName绑定统一**

- 替换所有ElementName绑定为RelativeSource
- 特别处理DataTemplate/ControlTemplate内的绑定
- 使用`{x:Reference}`作为安全替代方案

**2.2 缓存键用户隔离**

- 添加UserId前缀到所有缓存键
- 格式: `user_{userId}_{cacheKey}`
- 用户切换时清理前用户缓存

### Phase 3: 代码质量改进 (MEDIUM M1-M6)

**3.1 服务位置规范化**

- 移动PatientService到正确目录
- 统一服务目录结构

**3.2 对话框服务合并**

- 创建统一的`DialogService`
- 移除重复的对话框服务类

**3.3 对话框ViewModel继承统一**

- 所有Dialog ViewModel继承DialogViewModelBase
- 移除直接继承ObservableObject的实现

**3.4 构造函数参数聚合**

- 创建`IViewModelServices`聚合接口
- 注入单一聚合服务替代多参数

**3.5 Master-Detail控件抽象**

- 提取`MasterDetailControlBase`
- 子控件继承并复写差异部分

**3.6 角色层View模板化**

- 创建`RoleHomeViewTemplate`
- 通过DataTemplate差异化

### Phase 4: 规范统一 (LOW L1-L5)

**4.1 Repository签名统一**
**4.2 Mapper自动注册**
**4.3 日志前缀规范**
**4.4 异步命名补全**
**4.5 XML注释补全**

## Architecture

### 变更影响范围

```
src/Client/Desktop/
├── Core/
│   ├── LYBT.Desktop.Contracts/Services/     # 新增聚合接口
│   ├── LYBT.Desktop.Infrastructure/
│   │   ├── Services/                        # 服务拆分
│   │   ├── ViewModels/                      # 基类重构
│   │   └── Controls/                        # 控件抽象
│   └── LYBT.Desktop.Models/ViewModels/Base/ # ViewModel基类
├── Modules/
│   ├── LYBT.Desktop.MedicalCase/Services/   # MedicalCaseService拆分
│   ├── LYBT.Desktop.Patients/Services/      # PatientService移动
│   └── LYBT.Desktop.Users/ViewModels/       # Handler提取
└── Roles/
    ├── LYBT.Desktop.Admin/Views/            # 模板化
    └── LYBT.Desktop.Clinical/Views/         # 模板化
```

### 服务拆分架构图

```
[Before]
MedicalCaseService (605 lines, 4 responsibilities)
    ├── Query methods
    ├── Persistence methods
    ├── Validation methods
    └── Lifecycle methods

[After]
IMedicalCaseService (Facade)
    ├── IMedicalCaseQueryService
    ├── IMedicalCasePersistenceService
    ├── IMedicalCaseValidationService
    └── IMedicalCaseLifecycleService
```

## Impact

- **文件变更**: 预估50-70个文件
- **风险等级**: Medium-High (涉及核心业务类)
- **测试要求**:
  - 单元测试覆盖新拆分的服务
  - 集成测试验证功能完整性
  - UI测试验证绑定正确性

## Risks

| 风险 | 概率 | 影响 | 缓解措施 |
|------|------|------|----------|
| 服务拆分引入Bug | 中 | 高 | 分阶段执行，每阶段编译验证 |
| 绑定修改导致UI异常 | 中 | 中 | 运行时测试关键页面 |
| 缓存隔离影响性能 | 低 | 低 | 监控缓存命中率 |
| 重构范围膨胀 | 中 | 中 | 严格按Phase执行，不扩展范围 |

## Success Criteria

- [ ] 所有HIGH问题解决
- [ ] 所有MEDIUM问题解决
- [ ] 所有LOW问题解决
- [ ] Desktop解决方案编译通过
- [ ] 无绑定错误
- [ ] 核心功能手动测试通过

## References

- 前端架构分析报告 (2026-01-17)
- 项目架构规范: `openspec/project.md`
- SOLID原则参考

---

**提案者**: Claude Code
**日期**: 2026-01-17
**状态**: 待确认

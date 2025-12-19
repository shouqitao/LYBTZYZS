# OpenSpec Proposal: optimize-entity-data-flow

**状态**: Draft
**创建日期**: 2025-12-18
**关联**: refactor-dto-simplification (前置依赖)

## 1. 问题陈述

### 1.1 当前状态

经过 `refactor-dto-simplification` 重构后，系统存在两套并行架构：

**过期DTO (18个类已标记[Obsolete])**:
| 模块 | 过期类 | 替代类 |
|------|--------|--------|
| Prescription | PrescriptionDetailDtoLegacy | PrescriptionDetailDto |
| Prescription | PrescriptionInputBaseDto | PrescriptionInputDto |
| Prescription | PrescriptionCreateDto | PrescriptionInputDto |
| Prescription | PrescriptionEditDto | PrescriptionInputDto |
| Prescription | PrescriptionInputDtoLegacy | PrescriptionInputDto |
| Prescription | PrescriptionItemInputDtoLegacy | PrescriptionItemInputDto |
| Prescription | PrescriptionQueryDto | Controller方法参数 |
| Prescription | PrescriptionSearchDto | Controller方法参数 |
| Formula | FormulaDetailDtoLegacy | FormulaDetailDtoNew |
| Formula | FormulaQueryDto | Controller方法参数 |
| Formula | FormulaSearchDto | Controller方法参数 |
| Herb | HerbDetailDtoLegacy | HerbDetailDtoNew |
| Herb | HerbQueryDto | Controller方法参数 |
| Herb | HerbSearchDto | Controller方法参数 |
| MedicalCase | MedicalCaseDetailDto | MedicalCaseDetailDtoNew |
| User | UserDto | UserDetailDtoNew |
| User | UserQueryDto | Controller方法参数 |
| User | UserSearchDto | Controller方法参数 |

**Desktop层两套视图系统并存**:
| 模块 | Management视图(旧) | MasterDetail视图(新) |
|------|-------------------|---------------------|
| Formula | FormulaManagementView/ViewModel | FormulaMasterDetailView/ViewModel |
| Herb | HerbManagementView/ViewModel | HerbMasterDetailView/ViewModel |
| Patient | PatientManagementView/ViewModel | PatientMasterDetailView/ViewModel |
| User | UserManagementView/ViewModel | UserMasterDetailView/ViewModel |
| MedicalCase | MedicalCaseManagementView/ViewModel | MedicalCaseMasterDetailView/ViewModel |

### 1.2 问题影响

1. **维护成本倍增**: 两套视图系统需要同时维护
2. **新人困惑**: 不清楚应该使用哪套架构
3. **潜在Bug**: 修复一处可能遗漏另一处
4. **代码膨胀**: 过期代码占用项目空间

## 2. 迁移策略

### 2.1 核心原则

1. **渐进式迁移**: 确保系统始终可运行
2. **先完善后废弃**: MasterDetail完整后才标记Management过时
3. **自底向上**: 先迁移DTO，再迁移UI层
4. **测试覆盖**: 每个迁移步骤都需验证

### 2.2 依赖关系分析

```
Server层DTO → Desktop层API契约 → Desktop层Repository → Desktop层ViewModel → Desktop层View
```

迁移顺序必须从右到左（先UI后数据），以保证兼容性。

### 2.3 HttpClient层评估结论

**现状**: 当前HttpClient层架构已规范化，采用Refit + Repository模式。

```
ViewModel → Repository(RepositoryBase<TDto,TCreateDto,TUpdateDto,TApi>) → Refit API(IUserApi等) → Server
```

**评估结果**: **不需要预先重构HttpClient层**

| 因素 | 说明 |
|------|------|
| 架构规范 | Refit + Repository是业界最佳实践 |
| 增量扩展 | 通过添加新API方法支持ListDto，无需改泛型基类 |
| 风险控制 | Pre-Release阶段避免大规模基础层变更 |
| 非阻塞 | DTO迁移不依赖HttpClient层重构 |

**迁移策略**: 增量添加API方法，保持原有方法不变
```csharp
// IUserApi - 添加新方法
[Refit.Get("/api/v1/users/list")]
Task<ApiResponse<PagedResult<UserListDto>>> GetUsersListAsync(...);

// UserRepository - 添加新方法
public Task<PagedResult<UserListDto>> GetPagedListAsync(...);
```

## 3. 迁移方案

### Phase 1: 确保MasterDetail完整性 (优先级: P0)

**目标**: 验证所有MasterDetail视图功能完整，可完全替代Management视图

**任务清单**:
- [ ] 验证FormulaMasterDetailView CRUD完整性
- [ ] 验证HerbMasterDetailView CRUD完整性
- [ ] 验证PatientMasterDetailView CRUD完整性
- [ ] 验证UserMasterDetailView CRUD完整性
- [ ] 验证MedicalCaseMasterDetailView CRUD完整性
- [ ] 确认所有导航入口指向MasterDetail

**验收标准**:
- 从AdminHome导航到每个模块，所有CRUD操作正常
- 无运行时异常

### Phase 2: 标记Management为[Obsolete] (优先级: P1)

**目标**: 标记旧Management组件为过时，引导开发者使用MasterDetail

**任务清单**:
- [ ] FormulaManagementViewModel 添加 [Obsolete]
- [ ] FormulaManagementView 添加 [Obsolete]
- [ ] HerbManagementViewModel 添加 [Obsolete]
- [ ] HerbManagementView 添加 [Obsolete]
- [ ] PatientManagementViewModel 添加 [Obsolete]
- [ ] PatientManagementView 添加 [Obsolete]
- [ ] UserManagementViewModel 添加 [Obsolete]
- [ ] UserManagementView 添加 [Obsolete]
- [ ] MedicalCaseManagementViewModel 添加 [Obsolete]
- [ ] MedicalCaseManagementView 添加 [Obsolete]
- [ ] 更新模块注册，注释说明Management已废弃

**代码示例**:
```csharp
/// <summary>
/// 用户管理ViewModel - 旧版列表视图
/// </summary>
/// <remarks>
/// OpenSpec: optimize-entity-data-flow - 请使用 UserMasterDetailViewModel
/// </remarks>
[Obsolete("请使用UserMasterDetailViewModel，此类将在后续版本移除")]
public class UserManagementViewModel : UnifiedListViewModelBase<UserDto>
```

### Phase 3: 迁移MasterDetail中的过期DTO引用 (优先级: P1)

**目标**: 将MasterDetail视图模型中的过期DTO替换为新DTO

**当前使用情况**:
| MasterDetail | 当前使用 | 目标迁移 |
|--------------|----------|----------|
| FormulaMasterDetailViewModel | FormulaDto | FormulaListDto (列表) |
| HerbMasterDetailViewModel | HerbDto | HerbListDto (列表) |
| PatientMasterDetailViewModel | PatientDto | PatientListDto (列表) |
| UserMasterDetailViewModel | UserDto [Obsolete] | UserListDto (列表) |
| MedicalCaseMasterDetailViewModel | MedicalCaseItem | MedicalCaseListDto (列表) |

**任务清单**:
- [ ] UserMasterDetailViewModel 迁移到 UserListDto
- [ ] 更新 IUserApi 返回类型
- [ ] 更新 UserRepository 方法签名
- [ ] 其他模块按需迁移（FormulaDto等未标记Obsolete，可延后）

**注意**: UserDto是唯一被MasterDetail使用且已标记[Obsolete]的DTO，需优先迁移。

### Phase 4: 服务层DTO迁移 (优先级: P2)

**目标**: 迁移Server层Controller/Service中的过期DTO使用

**范围**:
- Controller返回类型
- Service方法签名
- AutoMapper配置

**此阶段可在Post-Release执行**

### Phase 5: 移除过期代码 (优先级: P3)

**目标**: 清理所有标记[Obsolete]的代码

**前置条件**:
- 所有引用已迁移
- 编译0警告（Obsolete相关）

**此阶段在v2.0版本执行**

## 4. 风险评估

| 风险 | 影响 | 缓解措施 |
|------|------|----------|
| MasterDetail功能不完整 | 高 | Phase 1验证覆盖 |
| DTO迁移破坏API兼容性 | 中 | 保持旧DTO直到Server层完全迁移 |
| 测试覆盖不足 | 中 | 手动验证关键路径 |

## 5. 执行计划

### 近期执行 (Pre-Release)

1. **Phase 1**: 验证MasterDetail完整性 (1-2小时)
2. **Phase 2**: 标记Management为Obsolete (30分钟)
3. **Phase 3 部分**: 迁移UserMasterDetailViewModel (1小时)

### 延后执行 (Post-Release)

4. **Phase 3 完整**: 其他模块DTO迁移
5. **Phase 4**: 服务层DTO迁移
6. **Phase 5**: 移除过期代码

## 6. 完成标准

- [ ] 所有Management组件标记[Obsolete]
- [ ] UserMasterDetailViewModel使用UserListDto
- [ ] 编译通过，运行正常
- [ ] 文档更新

## 7. 附录：文件清单

### 需标记[Obsolete]的文件

**ViewModels**:
- src/Client/Desktop/Modules/LYBT.Desktop.Formula/ViewModels/FormulaManagementViewModel.cs
- src/Client/Desktop/Modules/LYBT.Desktop.Herbs/ViewModels/HerbManagementViewModel.cs
- src/Client/Desktop/Modules/LYBT.Desktop.Patients/ViewModels/PatientManagementViewModel.cs
- src/Client/Desktop/Modules/LYBT.Desktop.Users/ViewModels/UserManagementViewModel.cs
- src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/ViewModels/MedicalCaseManagementViewModel.cs

**Views**:
- src/Client/Desktop/Modules/LYBT.Desktop.Formula/Views/FormulaManagementView.xaml(.cs)
- src/Client/Desktop/Modules/LYBT.Desktop.Herbs/Views/HerbManagementView.xaml(.cs)
- src/Client/Desktop/Modules/LYBT.Desktop.Patients/Views/PatientManagementView.xaml(.cs)
- src/Client/Desktop/Modules/LYBT.Desktop.Users/Views/UserManagementView.xaml(.cs)
- src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/Views/MedicalCaseManagementView.xaml(.cs)

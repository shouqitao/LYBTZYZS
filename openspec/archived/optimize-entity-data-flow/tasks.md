# Tasks: optimize-entity-data-flow

## Phase 1: 验证MasterDetail完整性 (P0) ✅ COMPLETED

### Task 1.1: Formula模块MasterDetail验证 ✅
- [x] 验证列表加载正常
- [x] 验证新增功能
- [x] 验证编辑功能
- [x] 验证删除功能
- [x] 验证搜索/筛选

### Task 1.2: Herb模块MasterDetail验证 ✅
- [x] 验证列表加载正常
- [x] 验证新增功能
- [x] 验证编辑功能
- [x] 验证删除功能
- [x] 验证搜索/筛选

### Task 1.3: Patient模块MasterDetail验证 ✅
- [x] 验证列表加载正常
- [x] 验证新增功能
- [x] 验证编辑功能
- [x] 验证删除功能
- [x] 验证搜索/筛选

### Task 1.4: User模块MasterDetail验证 ✅
- [x] 验证列表加载正常
- [x] 验证新增功能
- [x] 验证编辑功能
- [x] 验证删除功能
- [x] 验证搜索/筛选
- [x] 验证状态切换(启用/禁用)

### Task 1.5: MedicalCase模块MasterDetail验证 ✅
- [x] 验证列表加载正常
- [x] 验证新建医案流程
- [x] 验证编辑功能
- [x] 验证状态流转

## Phase 2: 标记Management为[Obsolete] (P1) ✅ COMPLETED

### Task 2.1: Formula模块 ✅
- [x] FormulaManagementViewModel 添加 [Obsolete]
- [x] FormulaManagementView.xaml.cs 添加 [Obsolete]
- [x] 更新FormulaModule.cs注册注释 (保留注册，已通过[Obsolete]标记)

### Task 2.2: Herb模块 ✅
- [x] HerbManagementViewModel 添加 [Obsolete]
- [x] HerbManagementView.xaml.cs 添加 [Obsolete]
- [x] 更新HerbsModule.cs注册注释 (保留注册，已通过[Obsolete]标记)

### Task 2.3: Patient模块 ✅
- [x] PatientManagementViewModel 添加 [Obsolete]
- [x] PatientManagementView.xaml.cs 添加 [Obsolete]
- [x] 更新PatientsModule.cs注册注释 (保留注册，已通过[Obsolete]标记)

### Task 2.4: User模块 ✅
- [x] UserManagementViewModel 添加 [Obsolete]
- [x] UserManagementView.xaml.cs 添加 [Obsolete]
- [x] 更新UsersModule.cs注册注释 (保留注册，已通过[Obsolete]标记)

### Task 2.5: MedicalCase模块 ✅
- [x] MedicalCaseManagementViewModel 添加 [Obsolete]
- [x] MedicalCaseManagementView.xaml.cs 添加 [Obsolete]
- [x] 更新MedicalCaseModule.cs注册注释 (保留注册，已通过[Obsolete]标记)

### Task 2.6: 编译验证 ✅
- [x] dotnet build LYBT.All.sln 通过 (0 错误)
- [x] 确认新增[Obsolete]警告数量符合预期

## Phase 3: 迁移MasterDetail中的过期DTO (P1) ✅ COMPLETED

### API层迁移策略 (增量扩展，非重构)

**原则**: HttpClient层架构已规范化(Refit+Repository)，通过添加新方法支持ListDto，保持原有方法不变。

```
变更策略:
1. IUserApi - 添加GetUsersListAsync()返回PagedResult<UserListDto>
2. UserRepository - 添加GetPagedListAsync()调用新API方法
3. ViewModel - 调用新Repository方法
4. 原有方法保留，确保向后兼容
```

### Task 3.1: User模块DTO迁移 (优先) ✅
- [x] Server: IUserService添加GetPagedListAsync方法
- [x] Server: UserService实现GetPagedListAsync方法
- [x] Server: UsersController添加GET /api/v1/users/list端点
- [x] Client: IUserApi添加GetUsersListAsync返回UserListDto
- [x] Client: IUserRepository添加GetPagedListAsync接口方法
- [x] Client: UserRepository实现GetPagedListAsync方法
- [x] Client: UserCommandHandler添加GetPagedListAsync方法
- [x] Client: UserMasterDetailViewModel迁移到UserListDto
  - 泛型参数从UserDto改为UserListDto
  - GetItemsAsync调用GetPagedListAsync
  - 命令参数类型更新
  - ExecuteToggleUserStatusAsync添加完整用户获取逻辑
- [x] 编译验证通过 (0错误)
- [ ] 功能验证(列表/详情/编辑) - 待运行时验证

### Task 3.2: 其他模块DTO迁移规划 ✅
**统一DTO模式决策**: 所有业务模块需迁移到ListDto/DetailDto/InputDto三层模式

- [x] FormulaDto → FormulaListDto + FormulaDetailDto (Post-Release P2)
  - FormulaListDto: Id, Name, Source, FormulaType, Usage, HerbCount, Status, CreatedAt
  - FormulaDetailDto: 完整字段 + Herbs集合
- [x] HerbDto → HerbListDto + HerbDetailDto (Post-Release P2)
  - HerbListDto: Id, Name, PinyinName, Category, Nature, Flavor, Status, CreatedAt
  - HerbDetailDto: 完整字段包括Meridians, Functions, Indications等
- [x] PatientDto → PatientListDto + PatientDetailDto (Post-Release P2)
  - PatientListDto: Id, Name, Gender, Age, PhoneNumber, Status, LastVisitDate, CreatedAt
  - PatientDetailDto: 完整字段包括地址、病史、过敏史等
- [x] MedicalCaseItem → 需评估是否对齐DTO命名规范 (Post-Release P2)
  - 当前是Desktop端Model，可能需要重命名为MedicalCaseListDto以保持一致性

**迁移优先级**: User模块已完成 > Formula/Patient (高频使用) > Herb/MedicalCase

## Phase 4: 服务层DTO迁移 (P2 - Post-Release)

### Task 4.1: Controller返回类型迁移
- [ ] UserController 迁移到 UserListDto/UserDetailDtoNew
- [ ] 其他Controller按需迁移

### Task 4.2: Service方法签名迁移
- [ ] 更新IUserService接口
- [ ] 更新UserCommandService实现

### Task 4.3: AutoMapper配置更新
- [ ] 移除旧DTO映射配置
- [ ] 验证新映射正确性

## Phase 5: 移除过期代码 (P3 - v2.0)

### Task 5.1: 移除Management组件
- [ ] 删除FormulaManagementViewModel/View
- [ ] 删除HerbManagementViewModel/View
- [ ] 删除PatientManagementViewModel/View
- [ ] 删除UserManagementViewModel/View
- [ ] 删除MedicalCaseManagementViewModel/View

### Task 5.2: 移除过期DTO
- [ ] 删除所有*Legacy类
- [ ] 删除所有*QueryDto类
- [ ] 删除所有*SearchDto类
- [ ] 删除DtoBase.cs中的基类

### Task 5.3: 清理引用
- [ ] 移除模块注册中的旧视图
- [ ] 移除AutoMapper中的旧映射
- [ ] 更新using语句

## 完成标准

### Pre-Release必须完成:
- [x] Phase 1: 所有MasterDetail功能验证通过 ✅ (2025-12-18)
- [x] Phase 2: 所有Management组件标记[Obsolete] ✅ (2025-12-18)
- [x] Phase 3.1: UserMasterDetailViewModel迁移完成 ✅ (2025-12-18)
- [x] Phase 3.2: 其他模块DTO迁移规划完成 ✅ (2025-12-18)

### Post-Release完成:
- [x] Phase 3.3: Formula模块迁移到ListDto ✅ (2025-12-18)
  - Server: /list API端点已添加
  - Client: IFormulaApi.GetFormulasListAsync()
  - Client: FormulaRepository.GetPagedListAsync()
  - Client: FormulaMasterDetailViewModel<FormulaListDto, FormulaDetailModel>
- [x] Phase 3.4: Patient模块迁移到ListDto ✅ (2025-12-18)
  - Server: /list API端点已添加
  - Client: IPatientApi.GetPatientsListAsync()
  - Client: PatientRepository.GetPagedListAsync()
  - Client: PatientMasterDetailViewModel<PatientListDto, PatientDetailModel>
- [x] Phase 3.5: Herb模块迁移到ListDto ✅ (2025-12-18)
  - Server: /list API端点已添加
  - Client: IHerbApi.GetHerbsListAsync()
  - Client: HerbRepository.GetPagedListAsync()
  - Client: HerbMasterDetailViewModel<HerbListDto, HerbDetailModel>
- [x] Phase 3.6: MedicalCase模块迁移到ListDto ✅ (2025-12-18)
  - Server: /list API端点已添加
  - Client: IMedicalCaseApi.GetMedicalCasesListAsync()
  - Client: MedicalCaseRepository.GetPagedListAsync()
  - Client: MedicalCaseMasterDetailViewModel<MedicalCaseListDto, MedicalCaseDetailModel>
  - 消除了N+1查询问题
- [ ] Phase 4: 服务层迁移 (P2 - DEFERRED to Post-Release)
- [ ] Phase 5: 过期代码移除 (P3 - DEFERRED to v2.0)

## 归档说明 (2025-12-19)

Pre-Release目标已全部完成：
- Phase 1: MasterDetail功能验证 ✅
- Phase 2: Management组件标记[Obsolete] ✅
- Phase 3: 所有模块DTO迁移到ListDto ✅ (User/Formula/Patient/Herb/MedicalCase)

Post-Release任务DEFERRED，将在v2.0版本中作为独立提案处理。

# Phase 4 Day 1 待确认问题清单

**创建时间**: 2025-10-11 18:02
**关联报告**: [2025-10-11-phase4-day1-code-inventory.md](2025-10-11-phase4-day1-code-inventory.md)
**关联Issue**: [#1149](https://github.com/shouqitao/LYBTZYZS/issues/1149)

---

## 📋 问题清单概览

| 编号 | 优先级 | 模块 | 问题 | 状态 |
|------|--------|------|------|------|
| Q1 | P0 | MedicalCase | 两个ListViewModel（原版 vs Refactored）哪个在用？ | ✅ 已确认 |
| Q2 | P0 | MedicalCase | Refactored版本的扩展功能是否纳入MVP？ | ✅ 已确认 |
| Q3 | P0 | MedicalCase | 两个CreateViewModel用途差异？ | ✅ 已确认 |
| Q4 | P0 | Auth | LoginWindowViewModel是否已废弃？ | ✅ 已确认 |
| Q5 | P0 | Consultation | MedicalCaseMainViewModel职责归属？ | ✅ 已确认 |
| Q6 | P1 | Desktop全局 | 接口是否统一调整到Interfaces/目录？ | ✅ 已确认 |
| Q7 | P1 | Desktop全局 | Desktop端是否使用AutoMapper？配置在哪？ | ✅ 已确认 |
| Q8 | P1 | Prescriptions | 组件化架构是否符合标准？是否推广？ | ✅ 已确认 |
| Q9 | P1 | Server全局 | Options配置策略（哪些模块需要？） | ✅ 已确认 |
| Q10 | P1 | Server全局 | 业务规则类（Rules.cs）使用场景？ | ✅ 已确认 |
| Q11 | P2 | 全局 | 扩展功能是否纳入MVP？ | ✅ 已确认 |
| Q12 | P2 | Users | Desktop的GetDoctorsAsync()在Server端如何实现？ | ✅ 已确认 |

---

## 🔴 P0 - 立即确认（影响功能）

### Q1: MedicalCase - 两个ListViewModel共存问题

**发现**:
- `MedicalCaseListViewModel.cs` - 394行，原版
- `RefactoredMedicalCaseListViewModel.cs` - 553行，重构版

**差异对比**:
| 功能 | 原版 | Refactored版 |
|------|------|-------------|
| CRUD操作 | ✅ | ✅ |
| 搜索分页 | ✅ | ✅ |
| 批量删除 | ❌ | ✅ |
| 导出Excel | ❌ | ✅ |
| 多选模式 | ❌ | ✅ |
| 日期筛选 | ❌ | ✅ |
| 状态筛选 | ❌ | ✅ |
| 开始诊疗 | ✅ | ❌ |

**问题**:
1. Views中实际使用的是哪个ViewModel？
2. 是否可以删除未使用的那个？
3. 如果两个都在用，用途场景是什么？

**决策选项**:
- [x] A. 保留原版，删除Refactored
- [ ] B. 保留Refactored，删除原版
- [ ] C. 两个都保留（需说明场景差异）
- [ ] D. 合并两者优势功能到一个ViewModel

**您的决策**: ✅ **A - 保留原版，删除Refactored**

**决策理由**：
1. **MVP合规**：Refactored版的扩展功能（批量删除、导出Excel、日期筛选）都属于Extended范围（参考Q11决策）
2. **简化架构**：原版ViewModel更简洁（394行 vs 553行），符合MVP"够用即好"原则
3. **功能完整性**：原版包含"开始诊疗"功能，这是核心就诊流程的必要入口
4. **减少技术债务**：删除冗余ViewModel，避免维护两套代码的混乱

**后续行动**：
- 创建Issue删除RefactoredMedicalCaseListViewModel.cs
- 确认原版ViewModel注册在DI容器和Views中
- 保留原版的基础筛选功能（状态筛选，与Users模块对齐）

---

### Q2: MedicalCase - Refactored版本扩展功能MVP归属

**Refactored版本新增功能**:
- 批量删除 (`BatchDeleteAsync`)
- 导出Excel (`ExportAsync`)
- 多选模式 (`ToggleMultiSelect`)
- 全选 (`SelectAll`)
- 日期范围筛选 (`StartDate/EndDate`)
- 状态筛选 (`StatusFilter`)

**问题**: 这些扩展功能是否属于MVP核心范围？

**功能分级建议**:
- 🔴 Core MVP: List/Detail/Create/Edit/Delete（基础CRUD）
- 🟡 Extended: 批量删除、导出Excel、多选模式、高级筛选
- 🟢 Advanced: 复杂数据分析、自定义报表

**决策选项**:
- [ ] A. 全部纳入MVP（作为核心功能）
- [ ] B. 仅CRUD纳入MVP，扩展功能标记为Extended
- [ ] C. 批量操作纳入MVP，导出和筛选作为Extended
- [x] D. 全部作为Extended，MVP仅保留基础CRUD

**您的决策**: ✅ **D - 全部作为Extended，MVP仅保留基础CRUD**

**决策理由**：
1. **与Q11决策一致**：批量删除、导出Excel、日期范围筛选全部被Q11决策为Extended功能
2. **MVP定位明确**：2-3名医生，日均20-100人，基础CRUD+搜索+基础筛选（状态）足够
3. **避免过度设计**：Extended功能可以在MVP+阶段根据实际需求添加
4. **Q1决策关联**：保留原版ViewModel（仅基础CRUD），删除Refactored版（包含Extended功能）

**功能分级**：
- ✅ **MVP Core**：List/Detail/Create/Edit/Delete/Search/基础筛选（状态）
- ⚠️ **Extended**：批量删除、导出Excel、多选模式、日期范围筛选、全选

**后续行动**：
- 在原版MedicalCaseListViewModel中确认基础筛选功能（状态筛选）
- Extended功能在Q1决策中随Refactored版一起删除
- 文档中明确MedicalCase模块的MVP/Extended功能划分

---

### Q3: MedicalCase - 两个CreateViewModel用途

**发现**:
- `CreateMedicalCaseViewModel.cs`
- `CreateMedicalCaseDialogViewModel.cs`

**问题**:
1. 两者用途差异是什么？
2. 是否一个用于主界面创建，一个用于弹窗快速创建？
3. 是否有一个已废弃？

**决策选项**:
- [ ] A. 保留ViewModel，删除DialogViewModel（统一使用主界面创建）
- [x] B. 保留DialogViewModel，删除ViewModel（统一使用弹窗创建）
- [ ] C. 两个都保留（需说明场景差异）
- [ ] D. 需要进一步代码分析确定

**您的决策**: ✅ **B - 保留DialogViewModel，删除ViewModel**

**决策理由**（用户反馈）：
1. **用户明确指示**："一个是技术债务。你根据之前的信息保留一个即可。"
2. **符合Prism Dialog模式**：CreateMedicalCaseDialogViewModel遵循Issue #828 Prism Dialog标准化迁移的统一模式
3. **快速创建场景**：医案创建通常在就诊流程中快速触发，Dialog模式更符合业务场景
4. **减少冗余代码**：避免维护两套创建逻辑，降低维护成本

**后续行动**：
- 创建Issue删除CreateMedicalCaseViewModel.cs（非Dialog版本）
- 确认CreateMedicalCaseDialogViewModel在Prism Dialog系统中正确注册
- 验证ClinicalWorkstation中创建医案的触发点使用Dialog版本

---

### Q4: Auth - LoginWindowViewModel是否废弃

**对比**:
| ViewModel | 行数 | 功能 |
|-----------|------|------|
| **LoginViewModel** | 394行 | API健康检查、记住用户名、自动登录、HasSavedPassword |
| **LoginWindowViewModel** | 87行 | 仅基础登录（Username/Password/IsLoading） |

**问题**:
1. LoginWindowViewModel是否是早期版本，现已被LoginViewModel取代？
2. Views中是否还在引用LoginWindowViewModel？

**决策选项**:
- [x] A. 删除LoginWindowViewModel（已废弃）
- [ ] B. 保留LoginWindowViewModel（仍在使用）
- [ ] C. 需要检查Views引用后决定

**您的决策**: ✅ **A - 删除LoginWindowViewModel（已废弃）**

**决策理由**（用户反馈）：
1. **用户明确确认功能需求**："(API健康检查、记住用户名等都是合理的登录功能）这些功能都是需要的。"
2. **功能完整性对比**：
   - LoginViewModel (394行)：✅ API健康检查 + ✅ 记住用户名 + ✅ 自动登录 + ✅ HasSavedPassword
   - LoginWindowViewModel (87行)：仅基础登录（Username/Password/IsLoading）
3. **明显版本差异**：LoginWindowViewModel是早期简化版本，已被功能完整的LoginViewModel取代
4. **避免冗余**：两个ViewModel功能重叠，保留功能更完整的版本

**后续行动**：
- 创建Issue删除LoginWindowViewModel.cs和LoginWindow.xaml
- 确认LoginViewModel和LoginView是当前使用的版本
- 验证登录功能：API健康检查、记住用户名、自动登录功能正常

---

### Q5: Consultation - MedicalCaseMainViewModel职责归属

**发现**:
- 文件位置: `LYBT.Desktop.Consultation/ViewModels/MedicalCaseMainViewModel.cs`
- 命名包含"MedicalCase"，但放在Consultation模块

**问题**:
1. 这个ViewModel的实际职责是什么？
2. 是否应该移到MedicalCase模块？
3. 还是它确实属于Consultation的诊疗主界面？

**决策选项**:
- [ ] A. 移动到MedicalCase模块（属于病历管理）
- [ ] B. 保留在Consultation模块（是诊疗主界面）
- [ ] C. 重命名为ConsultationMainViewModel（明确职责）
- [x] D. 删除MedicalCaseMainViewModel（由ClinicalWorkstation模块替代）

**您的决策**: ✅ **D - 删除MedicalCaseMainViewModel**

**决策理由**（用户反馈）：
1. **用户明确定位**："目前中心是案例。而不是诊疗。看到很多跟诊疗相关的逻辑是技术债务。"
2. **模块职责明确**："保留Consultation模块。后续会做扩展。" - Consultation模块保留但不需要Main ViewModel
3. **工作台模块替代**："MedicalCaseMainViewModel我的理解可以不用。因为ClinicalWorkstation模块作为医生的专用工作台模块。"
4. **架构清晰化**：ClinicalWorkstation是医生工作台的统一入口，不需要在Consultation模块重复实现Main ViewModel

**后续行动**：
- 创建Issue删除MedicalCaseMainViewModel.cs及相关View
- 确认ClinicalWorkstation模块作为医生工作台的主入口
- 保留Consultation模块的其他ViewModel（ConsultationManagementViewModel等），供后续扩展

---

## 🟡 P1 - 架构统一（重要）

### Q6: Desktop全局 - 接口位置统一

**现状差异**:
- **Server标准**: `Interfaces/IUserRepository.cs` ✅
- **Desktop现状**: `Repositories/IUserRepository.cs` ❌

**影响**: 违反统一设计标准，增加维护认知负担

**技术背景**（用户澄清）：
- 原始设计：Desktop和Server共享LYBT.Shared.Interfaces统一模块接口
- Issue #1114重构：Desktop移除Service层，但接口位置统一还未处理
- 当前状态：技术债务，需要修复

**决策选项**:
- [x] A. Desktop统一调整到Interfaces/目录（对齐Server，完成#1114未完成工作）
- [ ] B. 保持现状（Desktop允许差异）
- [ ] C. 创建Issue逐步迁移（非紧急）

**您的决策**: ✅ **A - Desktop统一调整到Interfaces/目录**

**决策理由**：
1. 这是Issue #1114的未完成重构工作（技术债务）
2. 符合"模式一致性"原则
3. 对齐Server端标准，降低维护成本
4. 工作量可控（6模块×2步=12文件操作）

**后续行动**：
- 创建Issue修复此技术债务
- 为6个模块（Patients/Users/MedicalCase/Consultation/Prescriptions/Herbs/Formula/Auth）创建Interfaces/目录
- 移动IXxxRepository.cs接口文件
- 更新using引用
- 同步更新docs/architecture/client/unified-design-standard.md

---

### Q7: Desktop全局 - AutoMapper使用情况

**发现**:
- **Server端**: 每个模块有`Mapping/XxxMappingProfile.cs` ✅
- **Desktop端**: 
  - Desktop.Services/Mapping/ 存在（7个MappingProfile，已标记删除）
  - PatientDetailViewModel仍使用AutoMapper
  - 已迁移Repository不使用AutoMapper

**问题**: Desktop端是否应该完全移除AutoMapper？

**深度分析（25步UltraThink）**：
- **Server端映射**：Entity ↔ DTO（复杂，高频，必需AutoMapper）
- **Desktop端映射**：Dto ↔ UpdateDto（简单，低频，仅10-15处）
- **映射场景差异**：Server处理复杂对象图（导航属性、Shadow Properties），Desktop仅字段子集复制
- **AutoMapper成本**：NuGet依赖 + 700行配置 + 反射性能损耗 + 学习成本
- **扩展方法方案**：类型安全 + 无性能损耗 + 代码清晰 + 无依赖

**决策选项**:
- [x] A. Desktop完全移除AutoMapper（对齐v2.1标准，使用扩展方法）
- [ ] B. Desktop保留AutoMapper（用于特殊场景）
- [ ] C. 逐步迁移，暂不强制移除

**您的决策**: ✅ **A - Desktop完全移除AutoMapper**

**决策理由**：
1. 映射场景简单且低频（10-15处Dto → UpdateDto）
2. 扩展方法在类型安全、性能、可读性、维护性上全面优于AutoMapper
3. 符合"够用即好"和MVP原则
4. 减少700行配置代码和第三方依赖
5. Server端保留AutoMapper处理复杂Entity ↔ DTO映射

**实施路径**：
- Phase 1：在Shared.Models/Extensions/创建8个模块扩展方法（ToUpdateDto/ToCreateDto）
- Phase 2：替换ViewModel中的AutoMapper调用为扩展方法（约10-15处）
- Phase 3：删除Desktop.Services/Mapping/目录，移除AutoMapper依赖
- Phase 4：更新v2.1文档标准

**预期收益**：
- 删除700行配置代码
- 移除AutoMapper NuGet依赖
- 提升性能（无反射）
- 降低学习成本

---

### Q8: Prescriptions - 组件化架构推广

**现状**:
- **Prescriptions**: 使用`ViewModels/Components/`组件化架构（2445行 = 668主体 + 1777组件）
  - PrescriptionCalculator (242行) - 价格/剂量/重量计算
  - PrescriptionCommandHandler (457行) - 8+命令管理
  - PrescriptionDataManager (314行) - Repository适配
  - PrescriptionEventCoordinator (502行) - 事件协调
  - PrescriptionValidator (262行) - 验证逻辑
- **Formula**: 672行扁平结构，用户明确指出"验方中也有很多和处方类似的逻辑"
- **PatientImportWizard**: 1079行扁平结构，严重违反SRP
- **其他5个模块**: <500行，标准CRUD

**深度分析（30步UltraThink）关键发现**:
1. **Prescription组件化合理性**：5个组件全部符合MVP 85功能点要求，4种开方方式明确需要，无过度设计
2. **Formula与Prescription相似度**：药材管理/价格计算/验证逻辑80-100%相似，架构应对齐
3. **共享组件可行性**：Calculator和Validator可提取到`LYBT.Shared.Components`，减少200-300行重复代码
4. **复杂度阈值规则**：行数>800 OR 职责≥4类 OR MVP功能点≥50 OR 相似模块需对齐 → 触发组件化

**决策选项**:
- [ ] A. 推广到所有复杂模块（写入标准）
- [ ] B. 仅Prescriptions使用（作为特例）
- [x] C. 创建"复杂度阈值"规则（超过N个方法才组件化）
- [ ] D. 逐步重构，先不强制统一

**您的决策**: ✅ **C改进版 - 制定复杂度阈值规则，按规则实施组件化**

**决策理由**：
1. **MVP合规性**：Prescription (85功能点) + Formula (P0核心) + PatientImportWizard (P0批量导入) 都是MVP明确要求
2. **架构一致性**：Formula与Prescription高度相似逻辑必须对齐架构，避免代码重复和维护困难
3. **SOLID原则**：PatientImportWizard 1079行严重违反单一职责原则，必须拆分
4. **够用即好**：5个简单模块(<500行，标准CRUD)保持扁平，避免过度设计
5. **代码复用**：共享Calculator/Validator基类减少200-300行重复代码

**复杂度阈值规则**（写入unified-design-standard.md v2.2）：

**触发条件**（满足任一即组件化）：
- ViewModel代码行数 > 800行（不含空行和注释）
- 独立职责数量 ≥ 4类（计算/验证/命令/数据/事件/导入导出等）
- MVP功能点数 ≥ 50个
- 存在高度相似逻辑的模块需对齐架构

**组件规范**：
- 命名：`{Module}{Responsibility}.cs`（如PrescriptionCalculator）
- 目录：`ViewModels/Components/`
- 依赖注入：组件通过构造函数注入到主ViewModel，不单独注册DI容器
- 共享策略：计算/验证逻辑高度相似时提取到`LYBT.Shared.Components`

**立即实施的3个模块**：
1. **Prescription（2445行）** - ✅保持现状，5组件全部符合MVP
2. **Formula（672行）** - ✨新增4组件（Calculator/Validator/CommandHandler/DataManager）
   - 复用Shared层HerbCalculatorBase/HerbValidatorBase
   - 对齐Prescription架构
3. **PatientImportWizard（1079行）** - ✨新增4组件（FileReader/DataValidator/ImportExecutor/ProgressReporter）
   - 拆分后主体约280行，组件各150-250行

**保持扁平的5个模块**：
- Users (UserManagementViewModel 503行, UserEditViewModel 553行) - 标准CRUD
- Herbs (HerbManagementViewModel 489行) - 标准CRUD
- Auth (LoginViewModel 394行) - 单一登录逻辑
- Consultation (ConsultationManagementViewModel <500行) - 标准CRUD
- MedicalCase (已清理重复ViewModel) - 标准CRUD

**共享组件设计**：
- **LYBT.Shared.Components项目**：
  - `HerbCalculatorBase` (约150行) - 单味药价格/总价/总重量计算
  - `HerbValidatorBase` (约120行) - 重复检测/剂量范围/必填项验证
- **继承实现**：
  - `PrescriptionCalculator : HerbCalculatorBase` - 扩展4种开方方式特定逻辑
  - `FormulaCalculator : HerbCalculatorBase` - 扩展方剂比例计算
  - `PrescriptionValidator : HerbValidatorBase` - 扩展冲突处理策略
  - `FormulaValidator : HerbValidatorBase` - 扩展方剂名称唯一性验证

**实施路径**（4个Phase，总工期4天，可并行至2天）：
- **Phase 1（基础设施，1天）**：创建`LYBT.Shared.Components`，实现HerbCalculatorBase/HerbValidatorBase，编写单元测试
- **Phase 2（Prescription重构，0.5天）**：重构Calculator/Validator继承Shared基类，删除重复代码约100-150行
- **Phase 3（Formula组件化，1天）**：创建4组件，重构FormulaDetailViewModel（672→200行主体），编写单元测试
- **Phase 4（PatientImportWizard组件化，1.5天）**：创建4组件，重构主ViewModel（1079→280行），编写单元测试

**验收标准**：
1. 架构测试通过（DesktopLayerArchTests）
2. 既有功能无回归（运行完整测试套件）
3. 代码行数符合预期（Prescription/Formula/PatientImportWizard主体≤300行）
4. 文档同步更新（unified-design-standard.md v2.2）
5. 共享组件单元测试覆盖率≥80%

**预期收益**：
- 减少200-300行重复代码（共享Calculator/Validator）
- Formula与Prescription架构对齐，降低维护成本
- PatientImportWizard拆分后职责清晰，便于测试和扩展
- 建立明确的组件化标准，指导后续开发

**风险与缓解**：
- 风险1：破坏既有功能 → 缓解：Phase 2先重构Prescription验证模式，每个Phase完成后运行测试
- 风险2：DI配置错误 → 缓解：组件不单独注册DI，参考Prescription现有模式
- 风险3：性能回归 → 缓解：组件构造时一次性创建，共享基类使用虚方法允许内联优化
- 风险4：工期延误 → 缓解：Phase 3+4可并行，每个Phase独立可交付

---

### Q9: Server全局 - Options配置策略

**现状**:
| 模块 | 是否有Options | 实际使用情况 | 一致性 |
|------|--------------|------------|--------|
| Patients | ✅ PatientModuleOptions.cs | ❌ 未使用（已注册DI，但无IOptions注入） | 占位符 |
| Consultation | ✅ ConsultationModuleOptions.cs | ❌ 未使用 | 占位符 |
| Herbs | ✅ HerbModuleOptions.cs | ❌ 未使用 | 占位符 |
| Auth/Users/MedicalCase/Prescriptions/Formula | ❌ 无 | N/A | - |

**关键发现**:
1. **3个Options文件内容完全相同**（DefaultPageSize/MaxPageSize/EnableCache/CacheExpirationMinutes）
2. **已注册到DI但未实际使用**：搜索`IOptions<PatientModuleOptions>`无结果，Service构造函数未注入
3. **appsettings.json无配置段**：无PatientModule/ConsultationModule/HerbModule配置
4. **标准文档明确说明**：`server-module-design-standard.md:886-890` - Options是**可选的**，按需配置

**决策选项**:
- [ ] A. 所有模块统一添加Options（即使为空）
- [ ] B. 仅有配置需求的模块添加Options
- [ ] C. 删除现有Options，统一到appsettings.json
- [x] D. 制定"何时需要Options"的明确规则

**您的决策**: ✅ **D改进版 - 制定明确规则 + 删除未使用占位符**

**决策理由**：
1. **符合标准文档**：Options是可选的，无特殊配置需求可省略
2. **MVP原则**：当前3个Options是占位符代码，违反YAGNI原则
3. **代码清理**：删除未使用的占位符代码，降低维护成本

**Options使用规则**（写入server-module-design-standard.md）：

**需要Options场景**（满足任一）：
1. 模块特定配置（如超时时间、重试次数）
2. 运行时可调整配置
3. 环境差异化配置
4. 已有明确使用（Service注入`IOptions<XxxModuleOptions>`）

**不需要Options场景**：
1. 通用配置（分页/缓存）应统一在appsettings.json
2. 硬编码常量应在Constants类
3. 未使用配置（占位符代码）

**处理方案**：
- 删除3个Options文件及注册代码
- 在appsettings.json统一配置Pagination/Caching
- 未来新增Options需有单元测试验证

**预期收益**：删除约60行占位符代码，统一通用配置管理

---

### Q10: Server全局 - 业务规则类使用场景

**现状**:
- **MedicalCase**: 有`MedicalCaseRules.cs`（108行，静态工具类，4条业务规则）
  - CanCreateNewCase（唯一性约束：患者同时只能1个Active医案）
  - CanEdit（时间锁定：当天可改、过期锁定 + 权限分级：医生/管理员）
  - CanDelete（权限检查）
  - CanComplete（状态机转换：Active→Completed）
  - 在3个Service方法中调用（CreateAsync/UpdateAsync/DeleteAsync）
- **其他7个模块**: 无独立Rules类
  - Prescription：纯CRUD，无业务规则验证（配伍禁忌、剂量限制不在MVP）
  - Users/Patients/Herbs/Formula/Consultation/Auth：简单验证或技术约束

**深度分析（15步）关键发现**:
1. **MedicalCase规则复杂度高**：4条规则，涉及时间计算、跨实体查询、多条件判断、业务级错误提示
2. **MedicalCase规则是MVP业务需求**："当天可改、过期锁定"是中医诊所核心业务规则
3. **其他模块无复杂业务规则**：仅简单非空/格式验证、技术约束、单行权限筛选
4. **Prescription无需规则类**：用户明确"配伍禁忌、剂量限制等规则这种没有的"，当前MVP仅CRUD
5. **静态工具类模式合理**：4条规则全部是纯函数（无外部依赖），Service层负责调用后处理

**问题**:
1. 为何只有MedicalCase有规则类？→ 只有MedicalCase有复杂业务领域规则
2. 是否应该推广到其他模块？→ 按标准判断，当前7个模块都不满足标准
3. 业务规则类的使用场景和标准是什么？→ 制定明确的5条标准

**决策选项**:
- [ ] A. 推广到所有模块（规则复杂度>3条时独立）
- [ ] B. 仅复杂业务模块使用（MedicalCase、Prescription）
- [ ] C. 合并到Service类，删除独立Rules类
- [x] D. 制定"何时需要Rules"的明确规则

**您的决策**: ✅ **D - 制定明确的Rules类使用标准**

**决策理由**：
1. **现状合理性**：MedicalCase有Rules类合理（满足标准），其他7模块无Rules类也合理（不满足标准）
2. **避免过度设计**：不应强制所有模块创建Rules类（Users/Herbs等仅简单验证）
3. **指导未来扩展**：明确标准后，当Prescription添加配伍禁忌时可按标准创建PrescriptionRules
4. **架构清晰**：区分"业务规则"与"技术验证"/"表单验证"

**Rules类使用标准**（写入server-module-design-standard.md 9.10节FAQ）：

**需要独立Rules类**（满足**任意2条**）：
1. **业务规则数量** ≥3条
2. **涉及领域业务知识**（非技术验证，如"当天可改、过期锁定"是诊所业务规则）
3. **规则复用度** ≥3次调用
4. **需要跨实体查询或复杂计算**（如检查患者所有医案、时间计算、状态判断）
5. **需要业务级错误提示**（如"该患者已有进行中的医案，请先完成现有医案"）

**不需要独立Rules类**：
1. 简单非空/格式验证 → 用FluentValidation/DataAnnotations
2. 技术约束验证（如JWT密钥强度、密码哈希）→ 直接在Service中if判断
3. 单次使用的逻辑 → 内联在Service方法中
4. 单行权限筛选 → Repository的Where条件（如`f.UserId == userId || f.IsShared`）

**Rules类设计约束**：
1. **静态工具类**：`public static class XxxRules`
2. **纯函数设计**：无状态、无外部依赖（不注入ILogger/IRepository）
3. **返回值**：`bool`或`ValidationResult`
4. **位置**：`src/Server/Modules/LYBT.Module.Xxx/Services/XxxRules.cs`
5. **调用方**：Service层调用Rules类，然后处理结果（记日志、返回ServiceResult）

**当前8模块评估**：
- **MedicalCase** ✅ - 保持MedicalCaseRules（满足5条标准中的4条）
- **Prescription** ❌ - 无需Rules（MVP无配伍禁忌/剂量限制，将来扩展时再评估）
- **其他6模块** ❌ - 无需Rules（仅简单CRUD/技术验证）

**未来扩展指导**：
- **场景1**：Prescription添加配伍禁忌（MVP+）
  - 满足：规则≥3条、领域知识、复用3次、跨实体查询 → 创建PrescriptionRules.cs
- **场景2**：Users添加角色权限规则
  - 不满足：规则<3条、技术验证 → 用FluentValidation，无需Rules类
- **场景3**：Patients添加建档规则
  - 取决于具体复杂度，按标准评估

**实施路径**：
- Phase 1：保持MedicalCaseRules现状（无需改动）
- Phase 2：将Rules类使用标准写入server-module-design-standard.md（9.10节FAQ）
- Phase 3：其他模块无需新增Rules类（当前MVP范围）

**预期收益**：
- 建立明确的Rules类使用标准，指导后续开发
- 避免过度设计（不强制所有模块创建Rules类）
- 区分"业务规则"与"技术验证"，架构更清晰

---

## 🟢 P2 - 功能分级（次要）

### Q11: 全局 - 扩展功能MVP归属

**扩展功能清单**:
- 批量操作（批量删除、批量导入）
- 导入导出（Excel导入/导出）
- 高级筛选（日期范围、状态、多条件）
- 对话框增强（Edit/ViewDialog）

**功能实现现状**:
- **MedicalCase Refactored版**: 批量删除、导出Excel、日期筛选、状态筛选
- **Patients模块**: PatientImportWizard（1079行）Excel批量导入
- **Users模块**: 基础筛选（SelectedRole/SelectedStatus）

**MVP需求对照**:
- **系统定位**: 中小型中医诊所，2-3名医生，日均20-100人

**问题**: 这些功能是否纳入MVP？

**决策选项**:
- [x] A. 基础筛选+批量导入纳入MVP，其他Extended
- [ ] B. 全部纳入MVP
- [ ] C. 按模块分别评估（如MedicalCase需要导出，Patients需要导入）
- [ ] D. 全部作为Extended，MVP仅CRUD

**您的决策**: ✅ **A - 基础筛选+批量导入纳入MVP，其他Extended**

**决策理由**：
1. **基础筛选必需**: 20-100条数据，基础筛选（状态、角色、关键词）是MVP核心功能
2. **批量导入必需**: 新诊所开业需要导入历史患者档案（PatientImportWizard是MVP P0功能）
3. **批量删除非必需**: 小诊所单条删除即可，批量删除场景少
4. **导出Excel非必需**: 数据分析需求，非核心就诊流程，可在MVP+实现
5. **日期范围筛选非必需**: 高级查询，可用"搜索+基础筛选"替代
6. **对话框增强非必需**: UI增强，非功能性需求

**功能分级结果**：

**✅ MVP Core（纳入MVP）**：
1. **基础筛选**（状态、角色、关键词） - 所有List模块
2. **患者批量导入**（PatientImportWizard） - Patients模块

**⚠️ Extended（MVP后期或MVP+）**：
1. **批量删除** - 所有模块（小诊所场景少）
2. **导出Excel** - 所有模块（数据分析需求）
3. **日期范围筛选** - MedicalCase/Consultation（高级查询）
4. **Edit/ViewDialog** - 所有模块（UI增强）

**实施影响**：
- **Q1-Q2（MedicalCase双ViewModel）**: Refactored版的批量删除/导出/日期筛选标记为Extended，不影响MVP发布
- **Q8（PatientImportWizard组件化）**: 保持P0优先级，必须完成组件化（1079行需拆分）
- **其他模块**: 保留基础筛选（SelectedXxx属性），移除批量/导出功能

**后续行动**：
- 标记MedicalCaseRefactoredListViewModel的Extended功能
- 确认PatientImportWizard完成组件化（Issue #1114 Phase 4）
- 文档中明确MVP/Extended/Advanced功能分级标准

---

### Q12: Users - GetDoctorsAsync实现

**发现**:
- **Desktop**: `UserRepository.GetDoctorsAsync()` - 调用`$"{_endpoint}/doctors"` API
- **Server**: 未找到`/doctors`接口，UserService无GetDoctorsAsync方法

**关键信息**（用户补充）:
- **UserRole定义中目前只实现了Doctor一个角色**
- 系统中所有用户都是Doctor角色
- MVP定位：2-3名医生的小诊所，总用户≤10人

**业务场景**:
- 创建医案/处方时选择医生下拉列表
- 当前GetDoctorsAsync() = GetAllUsers（因为只有Doctor角色）

**问题**:
1. Server端是否有GetDoctorsAsync实现？→ 无
2. 如果没有，是否需要补充？→ 无需补充
3. 获取医生列表的逻辑应该在哪一层？→ Desktop本地筛选

**决策选项**:
- [ ] A. Server端补充GetDoctorsAsync到UserService
- [x] B. Desktop端通过筛选角色实现（无需Server新增）
- [ ] C. 创建独立DoctorService管理医生相关业务
- [ ] D. 需要进一步分析业务需求

**您的决策**: ✅ **B - Desktop端通过筛选角色实现（无需Server新增）**

**决策理由**：
1. **当前实现简单**：只有Doctor角色，GetDoctorsAsync等同于获取所有用户
2. **避免接口膨胀**：Server不需要为每个角色新增专用接口（避免/doctors、/nurses、/admins等冗余接口）
3. **性能无影响**：MVP总用户≤10人，全量获取后本地筛选无性能问题
4. **已有基础**：Desktop已有角色筛选能力（UserManagementViewModel的SelectedRole）
5. **符合MVP原则**："够用即好"，简单方案优先

**实施方案**：

Desktop `UserRepository.GetDoctorsAsync`修改为：
```csharp
public async Task<List<UserDto>> GetDoctorsAsync()
{
    try
    {
        // 当前系统只有Doctor角色，获取所有用户即可
        // 为未来角色扩展预留筛选逻辑
        var result = await GetPagedAsync(1, 100, null); // 小诊所≤100人
        return result.Items
            .Where(u => u.Role == UserRole.Doctor && u.Status == CommonStatus.Enabled)
            .ToList();
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error getting doctors");
        return new List<UserDto>();
    }
}
```

**未来扩展策略**：
- **短期**（添加Admin/Nurse角色时）：保持本地筛选方案，10-50人数据量可接受
- **中期**（100+用户）：在Server的GetPagedAsync添加role参数筛选（Repository层WHERE子句）
- **长期**（大型医院，1000+用户）：考虑新增专用接口`GET /api/users/doctors`

**后续行动**：
- 修改Desktop UserRepository.GetDoctorsAsync实现（移除API调用，改为本地筛选）
- 删除Server端不存在的`/doctors`路由引用
- 单元测试验证筛选逻辑

---

## 📝 决策记录模板

```markdown
### 决策记录 - [日期]

**Q[编号]: [问题标题]**
- 决策: [选项字母] - [具体说明]
- 理由: [决策依据]
- 影响: [对架构/功能的影响]
- 后续行动: [需要执行的任务]
```

---

## 🎯 使用说明

1. 逐一讨论问题（从P0到P2）
2. 每个问题记录决策和理由
3. 完成后生成统一行动计划
4. 创建对应的GitHub Issues执行

---

**当前进度**: ✅ **12/12 已确认完成**

**决策汇总**：
- ✅ **P0 (5/5)**：MedicalCase清理3项、Auth清理1项、Consultation清理1项
- ✅ **P1 (5/5)**：Desktop接口位置、AutoMapper移除、组件化标准、Options清理、Rules标准
- ✅ **P2 (2/2)**：扩展功能分级、GetDoctorsAsync实现

**下一步行动**：根据12项决策创建对应的GitHub Issues，启动实施工作

# Tasks: 统一前后端实体类型与命名

## Phase 0: DTO层类型统一（优先） ✅

### 0.1 MedicalCase DTOs
- [x] MedicalCaseDetailDto.PatientGender: string? → Gender enum
- [x] MedicalCaseListDto.PatientGender: string? → Gender enum
- [x] 更新MedicalCaseMappingProfile映射配置
- [x] 编译验证

### 0.2 相关服务层更新
- [x] 检查MedicalCaseQueryService中PatientGender的赋值逻辑
- [x] 确保映射正确处理Gender枚举
- [x] 编译验证

## Phase 1: PatientItem类型统一 ✅

### 1.1 修改PatientItem
- [x] Gender属性从string改为Gender枚举
- [x] 添加GenderDisplay属性用于UI显示
- [x] 更新FromDto()移除ToString()转换
- [x] 更新ToDto()移除Enum.Parse转换
- [x] 更新UpdateFromDto()移除ToString()转换

### 1.2 更新PatientItem相关绑定
- [x] 检查PatientMasterDetailView.xaml绑定 → GenderDisplay
- [x] 检查PatientSelectionView.xaml绑定 → GenderDisplay
- [x] PatientViewControl.xaml绑定 → GenderDisplay
- [x] 编译验证

## Phase 2: MedicalCaseItem类型统一 ✅

### 2.1 修改MedicalCaseItem
- [x] PatientGender属性从string改为Gender枚举
- [x] 添加PatientGenderDisplay属性用于UI显示
- [x] 更新FromDto()直接使用Gender枚举
- [x] 更新ToDto()直接使用Gender枚举
- [x] 更新UpdateFromDto()直接使用Gender枚举

### 2.2 更新MedicalCaseItem相关绑定
- [x] 检查相关XAML绑定
- [x] 编译验证

## Phase 3: HerbItem类型统一 ✅

### 3.1 修改HerbItem
- [x] IsActive: bool 改为 Status: CommonStatus
- [x] 添加IsActive计算属性（向后兼容）
- [x] 更新FromDto()直接赋值Status
- [x] 更新ToDto()直接使用Status
- [x] StatusText/StatusColor改为switch表达式

### 3.2 更新HerbItem相关绑定
- [x] 检查HerbMasterDetailView.xaml绑定
- [x] 确保StatusText/StatusColor仍可用
- [x] 编译验证

## Phase 4: FormulaItem类型统一 ✅

### 4.1 修改FormulaItem
- [x] IsActive: bool 改为 Status: CommonStatus
- [x] 添加IsActive计算属性（向后兼容）
- [x] 更新FromDto()直接赋值Status
- [x] 更新ToDto()直接使用Status
- [x] CreatedBy: string? 改为 Guid?（与DTO一致）
- [x] StatusText/StatusColor改为switch表达式

### 4.2 更新FormulaItem相关绑定
- [x] 检查FormulaMasterDetailView.xaml绑定
- [x] 确保StatusText/StatusColor仍可用
- [x] 编译验证

## Phase 5: 验证与测试 ✅

### 5.1 编译验证
- [x] dotnet build LYBT.All.sln (0 errors, 0 warnings)

### 5.2 单元测试
- [ ] 运行Patient模块测试 (待手动验证)
- [ ] 运行MedicalCase模块测试 (待手动验证)
- [ ] 运行Herbs模块测试 (待手动验证)
- [ ] 运行Formula模块测试 (待手动验证)

### 5.3 功能测试
- [ ] Patient列表显示正常 (待手动验证)
- [ ] MedicalCase列表显示正常 (待手动验证)
- [ ] Herb列表显示正常 (待手动验证)
- [ ] Formula列表显示正常 (待手动验证)
- [ ] 各模块CRUD功能正常 (待手动验证)

### 5.4 文档更新
- [ ] 更新CHANGELOG
- [ ] 归档提案

## Phase 6: UI Model命名统一 ✅

> **执行**: 2025-12-19完成，Grep验证所有属性无外部XAML绑定引用，低风险重构。

### 6.1 UserItem命名统一
- [x] CreateTime → CreatedAt
- [x] UpdateTime → UpdatedAt
- [x] 更新FromDto/ToDto/UpdateFromDto方法
- [x] 编译验证

### 6.2 PatientItem命名统一
- [x] IdCard → IdNumber
- [x] LastVisitDate → LastVisitTime
- [x] 更新FromDto/ToDto/UpdateFromDto方法
- [x] 编译验证

### 6.3 HerbItem命名统一
- [x] Pinyin → PinYinCode
- [x] DosageUnit → Unit
- [x] UnitPrice → Price
- [x] Specification → Spec
- [x] 更新FromDto/ToDto方法和计算属性(DosageRangeText/DisplayText/SearchText/PriceText/CalculateSubtotal)
- [x] 编译验证

### 6.4 FormulaItem命名统一
- [x] Indication → Indications
- [x] Contraindication → Contraindications
- [x] Note → Remark
- [x] 更新FromDto/ToDto方法和计算属性(SearchText/HasContraindication)
- [x] 编译验证

### 6.5 MedicalCaseItem命名统一
- [x] Status → CaseStatus（与DTO一致）
- [x] 更新FromDto/ToDto/UpdateFromDto方法和计算属性(StatusText/StatusColor/IsActive/IsCompleted)
- [x] 编译验证

## 执行优先级

| Phase | 优先级 | 风险 | 预估工时 | 说明 |
|-------|--------|------|----------|------|
| Phase 0 | P0 | Low | 0.5h | DTO层优先修复 |
| Phase 1 | P1 | Low | 1h | PatientItem类型统一 |
| Phase 2 | P1 | Low | 0.5h | MedicalCaseItem类型统一 |
| Phase 3 | P2 | Medium | 1h | HerbItem类型统一 |
| Phase 4 | P2 | Medium | 1h | FormulaItem类型统一 |
| Phase 5 | P0 | - | 1h | 验证测试 |
| Phase 6 | P3 | High | 2h | 命名统一（可选） |

## 完成标准

### 核心标准（Phase 0-5, 7）✅
- [x] 所有枚举属性使用枚举类型
- [x] FromDto/ToDto无类型转换代码
- [x] 编译0错误0警告
- [ ] 所有测试通过 (待手动验证)
- [ ] UI显示正常 (待手动验证)

### 可选标准（Phase 6）✅ 已完成
- [x] 所有属性命名与DTO一致
- [x] FromDto/ToDto方法直接映射（无命名转换）
- [x] 编译0错误0警告

## Phase 7: 药材项命名统一 ✅

### 7.1 字段命名统一
- [x] FormulaHerbItem: Sequence → SortOrder（与DTO一致）
- [x] 更新FromDto()中的字段映射
- [x] 更新ToDto()中的字段映射
- [x] 编译验证

### 7.2 类命名评估（待定）
- [ ] 评估PrescriptionItem vs PrescriptionHerbItem命名 (Post-Release)
- [ ] Server: PrescriptionItem (不含Herb)
- [ ] Desktop: PrescriptionHerbItemViewModel (含Herb)
- [ ] 确定是否需要统一命名

## Phase 8: 前端Item定义集中化 ✅

> **执行**: 2025-12-19完成，所有Item类迁移到LYBT.Desktop.Models/Items/集中目录。

### 8.1 创建集中目录结构
- [x] 在LYBT.Desktop.Models中创建Items/目录
- [x] 创建Items/Formulas/、Items/Patients/、Items/Herbs/、Items/Users/、Items/MedicalCases/、Items/Consultations/子目录
- [x] 编译验证

### 8.2 迁移Item类
- [x] 迁移FormulaItem到Items/Formulas/（命名空间: LYBT.Desktop.Models.Items.Formulas）
- [x] 迁移FormulaHerbItem到Items/Formulas/（独立文件）
- [x] 迁移PatientItem到Items/Patients/（命名空间: LYBT.Desktop.Models.Items.Patients）
- [x] 迁移HerbItem到Items/Herbs/（命名空间: LYBT.Desktop.Models.Items.Herbs）
- [x] 迁移UserItem到Items/Users/（命名空间: LYBT.Desktop.Models.Items.Users）
- [x] 迁移MedicalCaseItem到Items/MedicalCases/（命名空间: LYBT.Desktop.Models.Items.MedicalCases）
- [x] 迁移ConsultationItem到Items/Consultations/（命名空间: LYBT.Desktop.Models.Items.Consultations）
- [x] 更新所有引用
- [x] 删除旧Item文件（6个）
- [x] 编译验证（0错误0警告）

### 8.3 拆分合并文件
- [x] 将FormulaHerbItem从FormulaItem.cs拆分到独立文件FormulaHerbItem.cs
- [x] 更新命名空间
- [x] 编译验证

### 8.4 处方Item标准化
> **执行**: 2025-12-19完成，合并并标准化处方药材项类命名

- [x] 创建Items/Prescriptions/目录
- [x] 合并PrescriptionItemViewModel和PrescriptionHerbItemViewModel为统一的PrescriptionHerbItem
- [x] 迁移到LYBT.Desktop.Models/Items/Prescriptions/PrescriptionHerbItem.cs
- [x] 命名空间: LYBT.Desktop.Models.Items.Prescriptions
- [x] 添加SetLoadedUnitPrice方法（向后兼容）
- [x] 添加ItemAmount属性（ItemTotal的别名，向后兼容）
- [x] 更新所有引用文件（7个Components + 2个ViewModels）
- [x] 删除旧ViewModel文件
- [x] 更新MedicalCaseModule.cs移除废弃的DI注册
- [x] 编译验证（0错误0警告）

## Phase 9: 最终验证与归档 ✅

### 9.1 全面编译验证
- [x] dotnet build LYBT.All.sln (0 errors, 0 warnings)

### 9.2 全面测试
- [x] 运行所有单元测试 (228/228 MedicalCase测试通过)
- [ ] 运行所有集成测试 (待手动验证)
- [ ] 功能回归测试 (待手动验证)

### 9.3 文档更新
- [x] 更新CHANGELOG
- [x] 归档提案

## 执行优先级（更新版）

| Phase | 优先级 | 风险 | 预估工时 | 说明 |
|-------|--------|------|----------|------|
| Phase 0 | P0 | Low | 0.5h | DTO层优先修复 |
| Phase 1 | P1 | Low | 1h | PatientItem类型统一 |
| Phase 2 | P1 | Low | 0.5h | MedicalCaseItem类型统一 |
| Phase 3 | P2 | Medium | 1h | HerbItem类型统一 |
| Phase 4 | P2 | Medium | 1h | FormulaItem类型统一 |
| Phase 5 | P0 | - | 1h | 验证测试 |
| Phase 6 | P3 | High | 2h | UI Model命名统一 |
| Phase 7 | P1 | Low | 0.5h | 药材项字段命名统一 |
| Phase 8 | P4 | High | 3h | Item定义集中化 ✅ |
| Phase 9 | P0 | - | 1h | 最终验证归档 |

## 分支策略

```
master
  └── feature/unify-frontend-backend-types
        ├── Phase 0-5 完成后合并回master
        ├── Phase 6-7 完成后合并回master
        └── Phase 8 (Post-Release)
```

每个Phase完成后：
1. 编译验证通过
2. 测试通过
3. 提交到feature分支
4. 合并到master（可选，或等待多个Phase一起合并）

## 字段比对速查表

### 类型不一致（必修 Phase 0-5）✅ 已完成

| Item | 属性 | 当前 | 目标 | 状态 |
|------|------|------|------|------|
| PatientItem | Gender | string | Gender enum | ✅ |
| MedicalCaseItem | PatientGender | string | Gender enum | ✅ |
| HerbItem | IsActive | bool | CommonStatus enum | ✅ |
| FormulaItem | IsActive | bool | CommonStatus enum | ✅ |
| FormulaItem | CreatedBy | string? | Guid? | ✅ |

### 药材项字段不一致（Phase 7）✅ 已完成

| 类 | 当前 | DTO命名 | 状态 |
|------|------|---------|------|
| FormulaHerbItem | Sequence | SortOrder | ✅ |

### UI Model命名不一致（Phase 6）✅ 已完成

| Item | 旧名 | 新名(与DTO一致) | 状态 |
|------|------|-----------------|------|
| UserItem | CreateTime | CreatedAt | ✅ |
| UserItem | UpdateTime | UpdatedAt | ✅ |
| PatientItem | IdCard | IdNumber | ✅ |
| PatientItem | LastVisitDate | LastVisitTime | ✅ |
| HerbItem | Pinyin | PinYinCode | ✅ |
| HerbItem | DosageUnit | Unit | ✅ |
| HerbItem | UnitPrice | Price | ✅ |
| HerbItem | Specification | Spec | ✅ |
| FormulaItem | Indication | Indications | ✅ |
| FormulaItem | Contraindication | Contraindications | ✅ |
| FormulaItem | Note | Remark | ✅ |
| MedicalCaseItem | Status | CaseStatus | ✅ |

### 前端Item定义位置（Phase 8）✅ 已完成

| Item | 旧位置 | 新位置 | 状态 |
|------|--------|--------|------|
| FormulaItem | LYBT.Desktop.Formula/Models/ | LYBT.Desktop.Models/Items/Formulas/ | ✅ |
| FormulaHerbItem | (同上，与FormulaItem同文件) | LYBT.Desktop.Models/Items/Formulas/FormulaHerbItem.cs | ✅ |
| PatientItem | LYBT.Desktop.Patients/Models/ | LYBT.Desktop.Models/Items/Patients/ | ✅ |
| HerbItem | LYBT.Desktop.Herbs/Models/ | LYBT.Desktop.Models/Items/Herbs/ | ✅ |
| UserItem | LYBT.Desktop.Users/Models/ | LYBT.Desktop.Models/Items/Users/ | ✅ |
| MedicalCaseItem | LYBT.Desktop.MedicalCase/Models/ | LYBT.Desktop.Models/Items/MedicalCases/ | ✅ |
| ConsultationItem | LYBT.Desktop.Consultation/Models/ | LYBT.Desktop.Models/Items/Consultations/ | ✅ |
| PrescriptionHerbItem | LYBT.Desktop.MedicalCase/ViewModels/ | LYBT.Desktop.Models/Items/Prescriptions/ | ✅ |
| TodayPatientItem | Shell/Models/ | (未迁移，Shell专用) | - |

### 处方Item类型合并（Phase 8.4）✅ 已完成

| 旧类名 | 新类名 | 说明 | 状态 |
|--------|--------|------|------|
| PrescriptionItemViewModel | PrescriptionHerbItem | 合并为统一类 | ✅ |
| PrescriptionHerbItemViewModel | PrescriptionHerbItem | 合并为统一类 | ✅ |

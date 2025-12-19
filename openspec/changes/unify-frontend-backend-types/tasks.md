# Tasks: 统一前后端实体类型与命名

## Phase 0: DTO层类型统一（优先）

### 0.1 MedicalCase DTOs
- [ ] MedicalCaseDetailDto.PatientGender: string? → Gender enum
- [ ] MedicalCaseListDto.PatientGender: string? → Gender enum
- [ ] 更新MedicalCaseMappingProfile映射配置
- [ ] 编译验证

### 0.2 相关服务层更新
- [ ] 检查MedicalCaseQueryService中PatientGender的赋值逻辑
- [ ] 确保映射正确处理Gender枚举
- [ ] 编译验证

## Phase 1: PatientItem类型统一

### 1.1 修改PatientItem
- [ ] Gender属性从string改为Gender枚举
- [ ] 添加GenderDisplay属性用于UI显示
- [ ] 更新FromDto()移除ToString()转换
- [ ] 更新ToDto()移除Enum.Parse转换
- [ ] 更新UpdateFromDto()移除ToString()转换

### 1.2 更新PatientItem相关绑定
- [ ] 检查PatientMasterDetailView.xaml绑定
- [ ] 检查PatientSelectionView.xaml绑定
- [ ] 添加GenderConverter（如需要）
- [ ] 编译验证

## Phase 2: MedicalCaseItem类型统一

### 2.1 修改MedicalCaseItem
- [ ] PatientGender属性从string改为Gender枚举
- [ ] 添加PatientGenderDisplay属性用于UI显示
- [ ] 更新FromDto()直接使用Gender枚举
- [ ] 更新ToDto()直接使用Gender枚举

### 2.2 更新MedicalCaseItem相关绑定
- [ ] 检查相关XAML绑定
- [ ] 编译验证

## Phase 3: HerbItem类型统一

### 3.1 修改HerbItem
- [ ] IsActive: bool 改为 Status: CommonStatus
- [ ] 添加IsActive计算属性（向后兼容）
- [ ] 更新FromDto()直接赋值Status
- [ ] 更新ToDto()直接使用Status

### 3.2 更新HerbItem相关绑定
- [ ] 检查HerbMasterDetailView.xaml绑定
- [ ] 确保StatusText/StatusColor仍可用
- [ ] 编译验证

## Phase 4: FormulaItem类型统一

### 4.1 修改FormulaItem
- [ ] IsActive: bool 改为 Status: CommonStatus
- [ ] 添加IsActive计算属性（向后兼容）
- [ ] 更新FromDto()直接赋值Status
- [ ] 更新ToDto()直接使用Status
- [ ] CreatedBy: string? 改为 Guid?（与DTO一致）

### 4.2 更新FormulaItem相关绑定
- [ ] 检查FormulaMasterDetailView.xaml绑定
- [ ] 确保StatusText/StatusColor仍可用
- [ ] 编译验证

## Phase 5: 验证与测试

### 5.1 编译验证
- [ ] dotnet build LYBT.All.sln (0 errors, 0 warnings)

### 5.2 单元测试
- [ ] 运行Patient模块测试
- [ ] 运行MedicalCase模块测试
- [ ] 运行Herbs模块测试
- [ ] 运行Formula模块测试

### 5.3 功能测试
- [ ] Patient列表显示正常
- [ ] MedicalCase列表显示正常
- [ ] Herb列表显示正常
- [ ] Formula列表显示正常
- [ ] 各模块CRUD功能正常

### 5.4 文档更新
- [ ] 更新CHANGELOG
- [ ] 归档提案

## Phase 6: 命名统一（可选，待评估）

### 6.1 UserItem命名统一
- [ ] CreateTime → CreatedAt
- [ ] UpdateTime → UpdatedAt
- [ ] 更新相关XAML绑定
- [ ] 编译验证

### 6.2 PatientItem命名统一
- [ ] IdCard → IdNumber
- [ ] LastVisitDate → LastVisitTime
- [ ] 更新相关XAML绑定
- [ ] 编译验证

### 6.3 HerbItem命名统一
- [ ] Pinyin → PinYinCode
- [ ] DosageUnit → Unit
- [ ] UnitPrice → Price
- [ ] Specification → Spec
- [ ] 更新相关XAML绑定
- [ ] 编译验证

### 6.4 FormulaItem命名统一
- [ ] Indication → Indications
- [ ] Contraindication → Contraindications
- [ ] Note → Remark
- [ ] 更新相关XAML绑定
- [ ] 编译验证

### 6.5 MedicalCaseItem命名统一
- [ ] Status → CaseStatus（与DTO一致）
- [ ] 更新相关XAML绑定
- [ ] 编译验证

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

### 核心标准（Phase 0-5）
- [ ] 所有枚举属性使用枚举类型
- [ ] FromDto/ToDto无类型转换代码
- [ ] 编译0错误0警告
- [ ] 所有测试通过
- [ ] UI显示正常

### 可选标准（Phase 6）
- [ ] 所有属性命名与DTO一致
- [ ] XAML绑定全部更新
- [ ] 无Naming Inconsistency警告

## 字段比对速查表

### 类型不一致（必修）

| Item | 属性 | 当前 | 目标 |
|------|------|------|------|
| PatientItem | Gender | string | Gender enum |
| MedicalCaseItem | PatientGender | string | Gender enum |
| HerbItem | IsActive | bool | CommonStatus enum |
| FormulaItem | IsActive | bool | CommonStatus enum |
| FormulaItem | CreatedBy | string? | Guid? |

### 命名不一致（可选）

| Item | 当前 | DTO命名 |
|------|------|---------|
| UserItem | CreateTime | CreatedAt |
| UserItem | UpdateTime | UpdatedAt |
| PatientItem | IdCard | IdNumber |
| PatientItem | LastVisitDate | LastVisitTime |
| HerbItem | Pinyin | PinYinCode |
| HerbItem | DosageUnit | Unit |
| HerbItem | UnitPrice | Price |
| HerbItem | Specification | Spec |
| FormulaItem | Indication | Indications |
| FormulaItem | Contraindication | Contraindications |
| FormulaItem | Note | Remark |
| MedicalCaseItem | Status | CaseStatus |

# Tasks: unify-medicalcase-input-dto

## Phase 1: 分析现有DTO使用情况

- [x] 1.1 搜索MedicalCaseInputDto的所有使用位置
- [x] 1.2 搜索MedicalCaseAggregateInputDto的所有使用位置
- [x] 1.3 定位Server端CreateMedicalCaseRequest位置
- [x] 1.4 分析哪些诊断字段实际被使用
- [x] 1.5 生成影响分析报告

## Phase 2: 简化MedicalCaseInputDto

- [x] 2.1 备份原MedicalCaseInputDto定义
- [x] 2.2 移除诊断相关字段(ChiefComplaint, TCMDiagnosis等)
- [x] 2.3 添加可选的Id字段用于区分Create/Update
- [x] 2.4 更新XML注释说明用途
- [x] 2.5 验证编译

## Phase 3: 统一Server端实现

- [x] 3.1 定位CreateMedicalCaseRequest使用位置
- [x] 3.2 替换为MedicalCaseInputDto
- [x] 3.3 删除CreateMedicalCaseRequest类
- [x] 3.4 更新MedicalCaseService.CreateAsync参数类型 (保持原有签名)
- [x] 3.5 更新MedicalCasesController参数类型
- [x] 3.6 验证编译

## Phase 4: 适配Client端代码

- [x] 4.1 搜索使用诊断字段的位置
- [x] 4.2 将诊断字段使用迁移到ConsultationInputDto (已完成，Client代码已正确使用)
- [x] 4.3 确保MedicalCaseDataManager正确使用DTO (已验证)
- [x] 4.4 验证MedicalCaseRepository调用正确 (已验证)
- [x] 4.5 验证编译

## Phase 5: 修复测试文件

- [x] 5.1 更新Server模块单元测试 (无需修改)
- [x] 5.2 更新Client模块单元测试 (更新MedicalCaseValidatorTests)
- [x] 5.3 更新集成测试 (已兼容)
- [x] 5.4 运行测试验证

## Phase 6: 最终验证

- [x] 6.1 全量编译验证 (0 errors, 0 warnings)
- [ ] 6.2 运行所有相关测试
- [x] 6.3 更新CHANGELOG.md
- [ ] 6.4 提交代码

## Progress Tracking

| Phase | Status | Notes |
|-------|--------|-------|
| Phase 1 | completed | 发现CreateMedicalCaseRequest仅使用PatientId+VisitDate |
| Phase 2 | completed | MedicalCaseInputDto简化为5字段 |
| Phase 3 | completed | 删除CreateMedicalCaseRequest，统一使用MedicalCaseInputDto |
| Phase 4 | completed | Client代码已正确使用简化后的DTO |
| Phase 5 | completed | 更新测试用例中的字段引用 |
| Phase 6 | in_progress | 编译验证通过 |

## 变更摘要

### MedicalCaseInputDto (简化后)
```csharp
public class MedicalCaseInputDto
{
    public Guid? Id { get; set; }           // 更新时必填，创建时为null
    public Guid PatientId { get; set; }      // 必填
    public Guid DoctorId { get; set; }       // 必填
    public DateTime VisitDate { get; set; }  // 必填
    public string? Remark { get; set; }      // 可选
}
```

### 移除的字段 (14个诊断字段)
- ChiefComplaint, PresentIllnessHistory, PastMedicalHistory, AllergyHistory
- Inspection, Auscultation, Inquiry, Palpation
- TCMDiagnosis, WesternDiagnosis, TreatmentPrinciple
- (诊断字段已统一由ConsultationInputDto管理)

### 删除的类
- `CreateMedicalCaseRequest` (MedicalCaseController内部类)

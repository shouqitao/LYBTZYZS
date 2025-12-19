# refactor-diagnosis-fields

## Summary

精简诊断字段结构：移除5个不必要的字段（主诉、四诊、治疗原则、医嘱、备注），只保留核心诊断信息。

## Motivation

当前诊断模块字段过多，实际临床使用中：

1. **主诉与现病史重叠**：主诉信息通常已包含在现病史中，无需单独字段
2. **四诊字段刚重构**：刚完成的四诊合并字段实际使用价值不高，舌诊和脉诊已独立
3. **治疗原则冗余**：治疗原则信息通常体现在处方中，不需要单独记录
4. **医嘱可移至处方**：医嘱信息更适合放在处方模块
5. **备注使用率低**：诊断备注字段使用频率极低

## Scope

### 涉及的系统层级

| 层级 | 文件/模块 | 变更类型 |
|------|-----------|----------|
| 数据库 | EF Core迁移 | 删除列 |
| 实体 | `Consultation` | 移除字段 |
| DTO | `ConsultationDto`, `ConsultationInputDto` | 移除字段 |
| 验证器 | `ConsultationInputDtoValidator` | 移除验证规则 |
| 服务 | `MedicalCaseCommandService` | 移除映射 |
| 客户端模型 | `ConsultationItem` | 移除字段 |
| ViewModel | `ConsultationFormViewModel`, `ConsultationPanelViewModel` | 移除字段 |
| DataManager | `ConsultationDataManager`, `MedicalCaseDataManager` | 移除字段处理 |
| 视图 | `ConsultationFormView.xaml`, `ConsultationPanel.xaml`, `MedicalCaseWorkspaceView.xaml` | 移除UI元素 |
| 打印 | `PrescriptionPrintDto`, `PrescriptionPrintService`, `PrescriptionFlowDocumentBuilder` | 移除打印内容 |

### 字段变更

**移除字段**（5个）：
- `ChiefComplaint` (主诉)
- `FourDiagnosis` (四诊)
- `TreatmentPrinciple` (治疗原则)
- `MedicalAdvice` (医嘱)
- `Remark` (备注)

**保留字段**（4个核心诊断字段）：
- `PresentIllness` (现病史) - 病情描述
- `TongueDiagnosis` (舌诊) - 舌象观察
- `PulseDiagnosis` (脉诊) - 脉象观察
- `TCMDiagnosis` (中医诊断) - 诊断结论

**保留字段**（系统字段）：
- `MedicalCaseId`, `PatientId`, `UserId` (关联ID)
- `PatientName`, `DoctorName` (展示用)
- `CreatedAt`, `UpdatedAt` (时间戳)

## Breaking Changes

- **数据库Schema变更**：删除5个列，需要数据备份
- **API契约变更**：DTO字段减少，需要客户端同步更新
- **验证规则变更**：ChiefComplaint不再是必填字段

## Dependencies

无外部依赖。

## Risks

1. **数据丢失风险**：移除的字段中可能有历史数据，需要评估是否需要保留
2. **并发开发冲突**：如有其他涉及Consultation的开发需协调

## Migration Strategy

```sql
-- 数据备份（可选，如需保留历史数据）
SELECT Id, ChiefComplaint, FourDiagnosis, TreatmentPrinciple, MedicalAdvice, Remark
INTO Consultations_Backup_DiagnosisFields
FROM Consultations
WHERE ChiefComplaint IS NOT NULL
   OR FourDiagnosis IS NOT NULL
   OR TreatmentPrinciple IS NOT NULL
   OR MedicalAdvice IS NOT NULL
   OR Remark IS NOT NULL;

-- 删除列（EF迁移自动生成）
ALTER TABLE Consultations DROP COLUMN ChiefComplaint;
ALTER TABLE Consultations DROP COLUMN FourDiagnosis;
ALTER TABLE Consultations DROP COLUMN TreatmentPrinciple;
ALTER TABLE Consultations DROP COLUMN MedicalAdvice;
ALTER TABLE Consultations DROP COLUMN Remark;
```

## Acceptance Criteria

1. [ ] 数据库迁移成功，5个列已删除
2. [ ] 诊断表单只显示4个核心字段（现病史、舌诊、脉诊、中医诊断）
3. [ ] 诊断数据可正常保存和读取
4. [ ] 打印处方时正确显示保留字段内容
5. [ ] 所有相关单元测试通过
6. [ ] 验证规则已更新，不再要求主诉必填

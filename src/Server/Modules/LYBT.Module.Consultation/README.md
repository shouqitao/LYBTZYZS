# LYBT.Module.Consultation

> 中医诊断管理(望闻问切) | 传统三层 | MedicalCase聚合根组成部分

## 项目定位

- **层级**: Server端
- **架构模式**: 传统三层
- **跨模块通信**: 共享主键与MedicalCase一对一关系

## 目录结构

```
LYBT.Module.Consultation/
├── ConsultationModule.cs
├── Interfaces/
│   └── IConsultationRepository.cs
├── Services/
│   └── ConsultationService.cs
├── Repositories/
│   └── ConsultationRepository.cs
├── Validators/
│   ├── ConsultationCreateDtoValidator.cs
│   └── ConsultationUpdateDtoValidator.cs
└── Mapping/
    └── ConsultationMappingProfile.cs
```

## 核心接口

| 接口 | 方法数 | 说明 |
|------|--------|------|
| IConsultationService | 2 | 按ID/医案ID查询诊断 |
| IConsultationRepository | 6 | 患者历史/分页/详情/条件查询 |

## 中医四诊字段

| 字段 | 说明 |
|------|------|
| Inspection | 望诊(面色、舌象、形体) |
| AuscultationOlfaction | 闻诊(声音、气味) |
| Inquiry | 问诊(症状、病史) |
| Palpation | 切诊(脉诊、按诊) |
| TCMDiagnosis | 中医辨证(如:肝郁脾虚证) |
| TreatmentPrinciple | 治疗原则(如:疏肝健脾) |

## 三步工作流

| 步骤 | 字段 | 说明 |
|------|------|------|
| Step1 | Step1CompletedAt | 辩证完成 |
| Step2 | Step2CompletedAt | 施治完成 |
| Step3 | Step3CompletedAt | 总结完成 |
| - | PrescriptionEnabled | 处方开关(控制是否开方) |

## 依赖关系

### 依赖
- LYBT.Infrastructure (BaseRepository, AppDbContext)
- LYBT.Entities (Consultation实体)
- LYBT.Shared.Models (ConsultationDto等)

### 被依赖
- LYBT.Module.MedicalCase (聚合根管理)
- LYBT.Module.Prescriptions (处方使用诊断数据)
- LYBT.WebAPI (ConsultationsController)

## API端点

| 端点 | 方法 | 说明 |
|------|------|------|
| /api/consultations/{id} | GET | 按ID查询诊断详情 |
| /api/consultations/medical-case/{id} | GET | 按医案ID查询诊断 |
| /api/consultations/patient/{id}/history | GET | 查询患者就诊历史 |
| /api/consultations/{id} | PUT | 更新诊断记录(四诊、诊断) |
| /api/consultations/{id}/step1 | PUT | 完成Step1(辩证) |
| /api/consultations/{id}/step2 | PUT | 完成Step2(施治) |
| /api/consultations/{id}/step3 | PUT | 完成Step3(总结) |
| /api/consultations/{id}/prescription-enabled | PUT | 设置处方开关 |

## 更新记录

| 日期 | 变更 |
|------|------|
| 2025-12-04 | 按README规范重写文档 |
| 2025-10-29 | 初始版本 |

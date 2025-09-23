# 模块对接分析报告：看诊系统 (Consultation)

| 统计项 | 数量 |
| :--- | :---: |
| WebApi 总点数 | 13 |
| Desktop 已对接点数 | 5 |
| **对接完成情况** | **38%** |

---

### WebApi 总点数列表 (13 个)

- `GetConsultations` (分页列表)
- `GetById` (获取详情)
- `GetByMedicalCaseId` (按医案查)
- `StartConsultation` (开始看诊)
- `UpdateConsultation` (更新看诊)
- `CompleteConsultation` (完成看诊)
- `CancelConsultation` (取消看诊)
- `GetStatistics` (统计)
- `GetTodayConsultationsByDoctor` (查医生今日看诊)
- `GetPatientHistory` (查患者历史)
- `GetDoctorConsultationCount` (查医生看诊数)
- `UpdateStatus` (更新状态)
- `Delete` (删除)

---

### Desktop 已对接点数列表 (5 个)

- `GetByIdAsync`
- `StartConsultationAsync`
- `UpdateConsultationAsync`
- `UpdateStatusAsync`
- `DeleteAsync`

---

### 未对接点分析

- **查询类**: `GetConsultations`, `GetByMedicalCaseId`, `GetTodayConsultationsByDoctor`, `GetPatientHistory`
- **生命周期类**: `CompleteConsultation`, `CancelConsultation`
- **统计类**: `GetStatistics`, `GetDoctorConsultationCount`

---

### 状态总结

**对接程度较低，仅核心流程开始。**

目前仅实现了“开始看诊”、“更新看诊”和“删除”等最基础的操作。所有围绕看诊的查询、统计，以及“完成”、“取消”等重要的流程操作都尚未与后端对接。

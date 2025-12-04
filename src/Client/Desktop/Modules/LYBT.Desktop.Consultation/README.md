# LYBT.Desktop.Consultation

> 中医四诊模块 | 望闻问切数据采集 | ISaveable集成

## 项目定位

- **层级**: Client Modules层
- **职责**: 提供中医四诊(望、闻、问、切)数据采集界面，作为MedicalCase流程Step1组件

## 目录结构

```
LYBT.Desktop.Consultation/
├── Interfaces/
│   └── IConsultationRepository.cs   # 诊断仓储接口
├── Repositories/
│   └── ConsultationRepository.cs    # 诊断仓储实现
├── ViewModels/
│   └── ConsultationFormViewModel.cs # 四诊表单ViewModel(核心)
├── Views/
│   ├── ConsultationFormView.xaml    # 四诊表单视图
│   └── ConsultationFormView.xaml.cs # CodeBehind
└── ConsultationModule.cs            # Prism模块注册
```

## ConsultationFormViewModel

### 属性(21个)

| 属性类别 | 属性 | 类型 | 说明 |
|----------|------|------|------|
| 望诊 | Complexion | string | 面色 |
| 望诊 | TongueColor | string | 舌色 |
| 望诊 | TongueCoating | string | 舌苔 |
| 望诊 | TongueShape | string | 舌形 |
| 闻诊 | VoiceCondition | string | 声音情况 |
| 闻诊 | BreathOdor | string | 口气 |
| 问诊 | ChiefComplaint | string | 主诉 |
| 问诊 | MedicalHistory | string | 病史 |
| 问诊 | SleepCondition | string | 睡眠 |
| 问诊 | AppetiteCondition | string | 食欲 |
| 问诊 | UrineCondition | string | 小便 |
| 问诊 | StoolCondition | string | 大便 |
| 切诊 | PulseLeft | string | 左脉 |
| 切诊 | PulseRight | string | 右脉 |
| 切诊 | PulseSummary | string | 脉象总结 |
| 诊断 | Diagnosis | string | 诊断结论 |
| 诊断 | SyndromePattern | string | 证型 |
| 诊断 | TreatmentPrinciple | string | 治则 |
| 状态 | HasChanges | bool | 数据变更标记 |
| 状态 | IsReadOnly | bool | 只读模式 |
| 状态 | ConsultationId | Guid? | 诊断记录ID |

### 命令

| 命令 | 说明 |
|------|------|
| SaveCommand | 保存诊断数据(异步) |
| ResetCommand | 重置表单 |
| ValidateCommand | 触发验证 |

### ISaveable接口实现

| 成员 | 说明 |
|------|------|
| SaveAsync() | 异步保存诊断数据到服务器 |
| ValidateAll() | 验证必填字段(主诉、诊断) |
| HasChanges | 数据变更状态 |
| IsReadOnly | 只读状态 |

## IConsultationRepository

| 方法 | 说明 |
|------|------|
| GetByIdAsync | 获取诊断记录 |
| GetByMedicalCaseIdAsync | 按医案ID获取诊断 |
| CreateAsync | 创建诊断记录 |
| UpdateAsync | 更新诊断记录 |
| DeleteAsync | 删除诊断记录 |

## 与MedicalCase集成

| 集成点 | 说明 |
|--------|------|
| Step1组件 | 作为MedicalCaseFlowViewModel的第一步 |
| ISaveable | 实现统一保存接口 |
| IValidatable | 实现统一验证接口 |
| IDataContext | 接收MedicalCaseId上下文 |

## 依赖关系

### 依赖
- LYBT.Desktop.Models (ViewModelBase)
- LYBT.Desktop.Foundation (BaseApiRepository)
- LYBT.Desktop.Contracts (IConsultationApi/ISaveable/IValidatable)
- LYBT.Shared.Models (ConsultationDto)
- Prism.Core/Prism.DryIoc (8.x)

### 被依赖
- LYBT.Desktop.Shell (模块加载)
- LYBT.Desktop.MedicalCase (Step1组件)

## 更新记录

| 日期 | 变更 |
|------|------|
| 2025-12-04 | 按README规范重写文档 |
| 2025-10-29 | 初始版本 |

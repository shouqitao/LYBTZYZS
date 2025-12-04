# LYBT.Module.Prescriptions

> 处方管理 | 传统三层 | 编号生成+状态机

## 项目定位

- **层级**: Server端
- **架构模式**: 传统三层
- **跨模块通信**: 依赖MedicalCase/Consultation/Herbs/Formula模块

## 目录结构

```
LYBT.Module.Prescriptions/
├── PrescriptionsModule.cs
├── Interfaces/
│   ├── IPrescriptionRepository.cs
│   └── IPrescriptionNumberService.cs
├── Services/
│   ├── PrescriptionService.cs
│   └── PrescriptionNumberService.cs
├── Repositories/
│   └── PrescriptionRepository.cs
├── Validators/
│   ├── PrescriptionCreateDtoValidator.cs
│   └── PrescriptionEditDtoValidator.cs
└── Mapping/
    └── PrescriptionMappingProfile.cs
```

## 核心接口

| 接口 | 方法数 | 说明 |
|------|--------|------|
| IPrescriptionService | 5 | 查询/搜索/金额计算 |
| IPrescriptionNumberService | 3 | 编号生成/格式验证/序列号获取 |
| IPrescriptionRepository | 7 | 患者/医案/编号/详情查询 |

## 处方编号格式

| 组成 | 示例 | 说明 |
|------|------|------|
| 前缀 | CF- | 处方标识 |
| 日期 | 20251204 | YYYYMMDD格式 |
| 序号 | 0001 | 4位当日序列号 |
| 完整 | CF-20251204-0001 | 17字符 |

## 状态机

| 状态 | 说明 | 允许操作 |
|------|------|----------|
| Draft | 草稿 | 可编辑、可确认 |
| Confirmed | 已确认 | 不可编辑、可配药 |
| Dispensed | 已配药 | 完成状态 |

## 依赖关系

### 依赖
- LYBT.Infrastructure (BaseRepository, AppDbContext)
- LYBT.Entities (PrescriptionModel, PrescriptionItemModel)
- LYBT.Shared.Models (PrescriptionDto等)
- LYBT.Module.MedicalCase (处方关联医案)
- LYBT.Module.Consultation (处方基于诊断)
- LYBT.Module.Herbs (药材单价计算)
- LYBT.Module.Formula (从验方创建处方)

### 被依赖
- LYBT.WebAPI (PrescriptionsController)

## API端点

| 端点 | 方法 | 说明 |
|------|------|------|
| /api/prescriptions/{id} | GET | 按ID查询处方详情 |
| /api/prescriptions/medical-case/{id} | GET | 按医案ID查询处方列表 |
| /api/prescriptions/patient/{id}/recent | GET | 获取患者近期处方 |
| /api/prescriptions/search | GET | 搜索处方(多条件) |
| /api/prescriptions | POST | 创建处方 |
| /api/prescriptions/from-formula | POST | 从验方创建处方 |
| /api/prescriptions/{id} | PUT | 更新处方(仅草稿) |
| /api/prescriptions/{id}/confirm | PUT | 确认处方 |
| /api/prescriptions/{id}/dispense | PUT | 标记已配药 |
| /api/prescriptions/{id} | DELETE | 删除处方(软删除) |

## 更新记录

| 日期 | 变更 |
|------|------|
| 2025-12-04 | 按README规范重写文档 |
| 2025-10-29 | 初始版本 |

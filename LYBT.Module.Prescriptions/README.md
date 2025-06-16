# LYBT.Module.Prescriptions

处方模块，保存和编辑患者处方信息。

## 主要服务及接口
- `IPrescriptionService` / `PrescriptionService`
- `IPrescriptionApi` 外部接口调用

## 重要模型和DTO
- `PrescriptionModel`、`PrescriptionHerbModel`
- 枚举 `PrescriptionStatus`

## 用法
在 Prism 应用中加载 `PrescriptionsModule`，通过 `IPrescriptionService` 操作处方数据。

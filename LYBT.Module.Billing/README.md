# LYBT.Module.Billing

费用结算模块，处理患者账单的创建、查询及支付状态维护。

## 主要服务及接口
- `IBillingService` / `BillingService`
- `IBillingRepository` / `BillingRepository`

## 重要模型和DTO
- `BillingModel`、`BillingItem`
- `BillingDto`、`BillingCreateDto`、`BillingEditDto`、`BillingDetailDto`

## 用法
在启动时调用 `BillingModule.Register(services)` 注册依赖，随后通过 `IBillingService` 完成账单的增删改查。

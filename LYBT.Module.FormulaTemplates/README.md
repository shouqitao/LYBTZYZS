# LYBT.Module.FormulaTemplates

经验方模板模块，保存常用药方模板并提供增删改查。

## 主要服务及接口
- `IFormulaTemplateService` / `FormulaTemplateService`
- `IFormulaTemplateRepository` / `FormulaTemplateRepository`

## 重要模型和DTO
- `FormulaTemplateModel`
- `FormulaTemplateDto`、`FormulaTemplateCreateDto`、`FormulaTemplateEditDto`、`FormulaTemplateDetailDto`

## 用法
应用启动时执行 `FormulaTemplatesModule.Register(services)`，之后通过 `IFormulaTemplateService` 管理模板数据。

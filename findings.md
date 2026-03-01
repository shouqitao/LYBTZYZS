# Findings: Code Simplifier 审查修复

## 问题清单
- H-01: 4 个 StatusHandler 间 85%+ 代码重复
- H-02: MasterDetailViewModelBase 11 处 DEBUG 日志
- H-03: HerbImportExportHandler 2 处 ex.Message 泄露
- M-01: FormulaModule DI 注册类型参数错误 (FormulaItem vs FormulaDetailModel)
- M-04: StatusHandler 异常捕获过宽 (catch Exception)
- M-06: LoggingRegistrationExtensions 未使用 using
- L-01: StatusOptions 重复定义

## 架构发现
- FormulaModule.AddMasterDetailServices<FormulaListDto, FormulaItem>() 但 ViewModel 实际使用 FormulaDetailModel
- UserStatusHandler.ToggleUserStatusAsync 走 UserService 元组模式，不适合统一到基类
- PatientStatusHandler 仅有 RestoreAsync，无 Toggle 操作

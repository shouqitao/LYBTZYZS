# LYBT.Tests.Unit

> Server 端单元测试 | 实体模型、基础服务、工具类、日志组件

## 覆盖范围

| 被测项目 | 测试类数 | 说明 |
|----------|----------|------|
| LYBT.Entities | 9 | 实体模型构造、属性、计算属性、聚合根行为 |
| LYBT.Infrastructure | 2 | BaseService 权限验证、JSON 序列化 |
| LYBT.Shared.Utilities | 1 | 缓存扩展 (RemoveByPrefix/Clear) |
| LYBT.Shared.Logging | 3 | 日志级别管理、关联ID、敏感数据脱敏 |
| LYBT.Shared.Models | 1 | 密码策略验证 |

## 测试策略

- 框架: xUnit + NSubstitute + FluentAssertions
- 模式: AAA (Arrange-Act-Assert)
- 目标框架: net8.0
- 无外部依赖 (数据库、网络、文件系统)

## 关键测试领域

- MedicalCase 聚合根生命周期 (状态流转、锁定/完成判定)
- Consultation / Prescription 内部实体行为
- BaseService 统一权限验证 (Admin/Doctor 角色区分)
- 密码安全工具链 (哈希、策略验证、角色解析)
- 日志脱敏管道 (SensitiveDataMasker / SensitiveDataJsonConverter)

## 运行方式

```
dotnet test tests/LYBT.Tests.Unit/
```

## 更新记录

| 日期 | 变更 |
|------|------|
| 2026-03-01 | 初始创建 README |

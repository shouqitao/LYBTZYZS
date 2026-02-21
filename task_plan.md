# MedicalCase 模块全面简化

## Goal
减少 ~400 行代码，消除权限双重检查、死代码、重复逻辑

## Phases

| Phase | 描述 | Status |
|-------|------|--------|
| 1 | 重置上下文 + 编译验证 | complete |
| 2 | 清理死代码 (~80行) | complete |
| 3 | 统一权限层 (~40行净减) | complete |
| 4 | 合并创建逻辑 (~80行) | complete |
| 5 | 提取共享 Helper (~80行) | complete |
| 6 | 精简日志 (~60行) | complete |
| 7 | 全量验证 + 文档更新 | complete |

## Decisions
- PermissionService 为唯一权限权威，Rules 仅保留无状态策略检查
- ServiceHelper 吸收公共逻辑 (重试、权限验证 helper、创建上下文验证)
- CreateAsync 委托给 CreateFromInputDtoAsync
- ValidationHelper 合并到 Rules
- PermissionService 添加 bool isAdmin 重载 (最小改动方案)

## Errors Encountered
| 错误 | 解决方案 |
|------|----------|
| UpdateConsultation_WhenStatusNotActive 测试失败 | 覆盖默认 mock 设置 CanEdit 返回 false |
| CreateAsync_WhenDoctorIdIsEmpty 测试失败 | 更新断言匹配新的错误消息格式 |

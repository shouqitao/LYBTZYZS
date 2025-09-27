# 040-数据访问最小规范

## EF 规范
- 读取路径使用 `AsNoTracking()`；写入路径保持跟踪。
- 软删除遵循现有实现；查询默认过滤已删除。
- 连接重试沿用现有配置；开发启用详细错误与敏感日志（生产关闭）。

## 缓存
- 保留 MemoryCache 基线；不引入复杂治理（穿透/细粒度过期等）于本轮。

## 自检结果（✅ 已验证）
- **DbContext注册**：✅ 已在 `UnifiedServiceRegistration.cs` 中配置
- **MemoryCache**：✅ 基础配置已就绪
- **数据库初始化**：✅ `DatabaseInitializationService` 处理迁移和种子数据
- **EF配置**：✅ 现有配置满足最小规范

## 实际状态
- EF Core配置完整，支持迁移和种子数据
- MemoryCache已注册并在控制器中使用
- 数据访问层符合最小规范要求


# 配置与环境

## SDK 与框架
- .NET SDK: 由 `global.json` 锁定为 9.0.305（rollForward: latestMinor）
- 目标框架: `net8.0`（WebAPI/Shared）、`net8.0-windows`（WPF 桌面）

## WebAPI 配置（appsettings.*）
- 路径: `src/Server/Services/LYBT.WebAPI/`
- 常见文件: `appsettings.json`、`appsettings.Development.json`、`appsettings.Production.json`
- 建议:
  - 机密配置通过环境变量或本地未提交的 `appsettings.Development.json` 提供
  - 参考 README 的运行命令与日志位置

## Desktop Shell 配置
- 路径: `src/Client/Desktop/Shell/`
- 示例键（来自 Core/Configuration 模块）:
  - `ApiBaseUrl`: API 基地址（默认 https://localhost:7001）
  - `ConnectionTimeout`: 连接超时（秒）
  - `Cache.DefaultExpirationMinutes`: 默认缓存过期分钟数
  - `Performance.EnableVirtualization`: 是否启用虚拟化优化

## JSON 序列化
- 统一使用 System.Text.Json
- Refit 内容序列化: `SystemTextJsonContentSerializer`（见 Infrastructure 的 RefitConfiguration 与 UnifiedApiClientManager）

## API 版本与路由
- 控制器:
  - `[ApiVersion("1")]`
  - `[Route("api/v{version:apiVersion}/[controller]")]`
- 前端调用固定 `/api/v1/*` 前缀，与上述约定匹配


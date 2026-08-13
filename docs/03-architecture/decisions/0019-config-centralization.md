# ADR-0019: 配置集中管理（Shared 类型集中 + config/ 文件集中 + 双端行为分层）

## 状态

**已实施（2026-08-12）**——Server 端 config/ 目录集中（WebAPI）：appsettings*.json + clinic.config.json 移入 `config/` 子目录（发布产物 = {BaseDir}/config/）；Program 加载链改 config/ 路径（保持 CFG-BATCH2 优先级 + CFGCLOSE 自动生成 + PostProcessor）；AppDbContextFactory 同路径；Desktop 第二阶段待实施
Accepted（设计思路用户确认 2026-08-13）— 待实施（范围确认后派发）

## 上下文

当前配置体系存在「三处分散」问题：
1. **类型分散**：19 个 Options 虽已在 `LYBT.Shared.Configuration`（Shared×7 项目引用），但 Desktop 专属（CommonOptions/FeatureToggleOptions）散在 Desktop.Infrastructure
2. **文件分散**：发布产物中配置文件散在根目录（appsettings.json、appsettings.{env}.json、clinic.config.json），而 `runtime-overrides.json` 设计在 `config/` 子目录——配置分离两处
3. **行为分层**：Server 配置行为（Service/Store/Validator/WritePolicy）在 `Infrastructure.Configuration`，Desktop 本地模式（LocalWebAPI）有独立配置逻辑

Server 与 Desktop **安装在不同位置**（服务器 /home/player/lybt-api vs 诊所电脑），但共享配置**类型定义**（Jwt/Login/ClinicSettings 等）。

## 决策

**三层结构（类型集中 → 文件集中 → 行为分层）**：

### L1 共享契约层 —— `LYBT.Shared.Configuration`（保持，已实现）

- 配置**类型定义**唯一来源（编译期共享）
- Options/ 19 个：Common×2（Jwt/Login 双端共享）、Client×5、Server×12
- Extensions/ 双端注册装配；ConnectionStringResolver 双端共用解析
- 约束：只依赖 Shared.Models，不引用任何端；敏感配置（JWT/密码）只读保护在 Server 行为层实现

### L2 文件集中层 —— `config/` 目录（新增）

- **Server 部署**：`{BaseDir}/config/` 集中 appsettings.json + appsettings.{env}.json + clinic.config.json + runtime-overrides.json
- **Desktop 部署**：`{安装目录}/config/` 集中 Shell 配置 + LocalWebAPI 配置
- 加载链：Program.cs / LocalWebApiProgram 用 `AddJsonFile("config/...")` 指定路径
- 发布脚本（dotnet publish）自动输出 config/ 目录

### L3 双端行为层 —— 各自实现（保持）

- **Server**：Configuration API（SystemConfigurationService / JsonFileConfigurationStore / ProductionConfigurationValidator / ConfigurationWritePolicy）+ 延迟重启端点
- **Desktop**：本地配置读写（LocalWebAPI 内嵌）+ Shell 配置

## 理由

1. **定义唯一**：类型只在 Shared 定义，双端编译复用——消除 Jwt/Login 双轨漂移历史坑
2. **文件集中**：部署后所有配置一个 config/ 目录——运维友好（与 Java Spring config/ 外部化同模式）
3. **值分离**：Server/Desktop 各自 config/ 提供各自值（Server 连远程 SQL，Desktop 连本地 LocalDB）——类型共享但值独立
4. **升级安全**：替换 dll 不覆盖 config/（已定红线「只覆盖 dll 不覆盖 appsettings」的物理体现）
5. **安装位置无关**：Shared.Configuration 是编译期代码共享（类似 BCL），不要求两端同机

## 优先级（配置加载链，已定 CFG-BATCH2）

```
环境变量 > runtime-overrides.json > appsettings.{env}.json > appsettings.json
```

## 配置闭环（已定 CFGCLOSE）

- Development 缺失 appsettings.{env}.json → 自动从模板生成（保证启动）
- 其他环境 → 生产门禁校验器报错
- 占位符/空串 → 视为无效回退下一级

## 实施范围（待用户确认）

1. **appsettings 移入 config/**：Server 端改 Program.cs 加载链（AddJsonFile 路径）+ LocalWebAPI + AppDbContextFactory + 测试基建 + 发布脚本
2. **Desktop config/ 结构**：Shell 与 LocalWebAPI 共用一份 config/ 还是各自子目录（建议各自子目录：config/shell/ + config/localwebapi/ 或合并）
3. **clinic.config.json**：移入 config/ 还是保留根目录

## 影响

- Program.cs（WebAPI + LocalWebAPI 配置加载）
- 发布脚本（dotnet publish 输出 config/）
- 部署 Runbook（01-deployment.md：start.sh 引用路径）
- 测试基建（WebApplicationFactory 配置路径）
- 架构文档（08-shared.md / 04-data-model 如有配置引用）

## 决策记录

| 日期 | 决策 | 说明 |
|------|------|------|
| 2026-08-13 | 设计思路确认 | 三层结构（类型集中+文件集中+行为分层）；用户确认 |

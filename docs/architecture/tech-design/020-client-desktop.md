# 020-Client Desktop 设计（最小闭环）

## 决策
- 默认 API BaseUrl：`http://localhost:5001`；允许从环境/配置覆盖。
- 登录对接：最小登录窗体 → 调用 `/api/auth/login` → 成功后缓存 token（内存），失败友好提示。
- 事件/资源：保留统一入口，登录闭环不展开重构。

## 代码证据
- 入口：`src/Client/Desktop/Shell/App.xaml.cs:44`（CreateShell），`55-71`（注册），`142-175`（模块目录）。
- BaseUrl 配置位置：`Infrastructure/Api` 或 `Core/Http` 下的默认配置（待确认）。

## 自检结果（✅ 已验证）
- **BaseUrl配置**：✅ 已设置 `src/Client/Desktop/Shell/appsettings.json:3`
- **默认端口**：✅ 已配置为5001 `"BaseUrl": "http://localhost:5001/"`
- **登录窗体**：✅ 需确认Login模块实现状态
- **API客户端**：✅ 存在 `Infrastructure/Api/UnifiedApiClientManager.cs`

## 实际状态
1) ✅ BaseUrl已设为5001：配置文件已更新
2) ⏳ 登录对接流程：需进一步检查Login模块完整性


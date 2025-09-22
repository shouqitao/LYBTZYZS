# 运行手册与排障

## 本地运行
```bash
# 还原依赖（解决方案级）
dotnet restore LYBT.All.sln

# 构建（Release）
dotnet build LYBT.All.sln -c Release --no-restore

# 运行 WebAPI
dotnet run --project src/Server/Services/LYBT.WebAPI

# 运行桌面客户端（可选命令行方式）
dotnet run --project src/Client/Desktop/Shell/LYBT.Desktop.Shell.csproj
```

## 常见问题

### 端口配置
- **WebAPI 默认端口**:
  - 开发环境: http://localhost:5001
  - 生产环境: https://localhost:7001
  - 如端口被占用，可通过 `--urls` 参数指定：`dotnet run --urls "http://localhost:8080"`

### 数据库连接
- **SQL Server 配置**:
  - 确保 SQL Server 服务正在运行
  - 连接字符串位于 `appsettings.json`
  - 默认数据库名: LYBTDB
  - 如遇连接失败，检查 Windows 认证或 SQL 认证配置

### Desktop 客户端问题
- **API 连接失败**:
  - 确认 WebAPI 已启动且可访问
  - 检查 Desktop 配置中的 API 地址是否正确
  - 防火墙可能阻止本地连接

- **桌面构建缺少 `Microsoft.Extensions.ObjectPool`**:
  - 现象：编译报错找不到 `ObjectPool<T>`/`IPooledObjectPolicy<T>` 类型
  - 处理：给 `LYBT.Desktop.Core` 添加 `Microsoft.Extensions.ObjectPool` 包

### JSON 序列化
- **JSON 栈混用**:
  - 现象：文档与代码已统一 System.Text.Json，但仍保留 `Refit.Newtonsoft.Json` 依赖
  - 处理：按 PRD "一致性治理"阶段移除该依赖，保持 Refit 使用 `SystemTextJsonContentSerializer`

## 小贴士
- 输出目录统一 `BIN/`；避免提交 `out/`、`obj/`、`TestResults/` 等生成物
- 配置：生产机密仅用环境变量或本地未提交文件；勿提交密钥


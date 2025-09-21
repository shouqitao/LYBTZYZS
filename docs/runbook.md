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
- 桌面构建缺少 `Microsoft.Extensions.ObjectPool`
  - 现象：编译报错找不到 `ObjectPool<T>`/`IPooledObjectPolicy<T>` 类型
  - 处理：给 `LYBT.Desktop.Core` 添加 `Microsoft.Extensions.ObjectPool` 包；避免命名空间与类型同名（可使用 `Microsoft.Extensions.ObjectPool.ObjectPool<T>` 全名或更名命名空间）
- JSON 栈混用
  - 现象：文档与代码已统一 System.Text.Json，但仍保留 `Refit.Newtonsoft.Json` 依赖
  - 处理：按 PRD “一致性治理”阶段移除该依赖，保持 Refit 使用 `SystemTextJsonContentSerializer`
- 端口/证书
  - WebAPI 默认 `https://localhost:7001`，如端口被占用或证书异常，请检查 Kestrel 配置与本机证书

## 小贴士
- 输出目录统一 `BIN/`；避免提交 `out/`、`obj/`、`TestResults/` 等生成物
- 配置：生产机密仅用环境变量或本地未提交文件；勿提交密钥


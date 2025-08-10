# 错误处理和日志系统 - 必需的NuGet包

## 需要安装的NuGet包

请在WPF项目中安装以下NuGet包：

```xml
<!-- 日志相关 -->
<PackageReference Include="Serilog" Version="3.1.1" />
<PackageReference Include="Serilog.Extensions.Logging" Version="8.0.0" />
<PackageReference Include="Serilog.Sinks.Console" Version="5.0.1" />
<PackageReference Include="Serilog.Sinks.File" Version="5.0.0" />
<PackageReference Include="Serilog.Sinks.Debug" Version="2.0.0" />
<PackageReference Include="Serilog.Sinks.EventLog" Version="3.1.0" />
<PackageReference Include="Serilog.Enrichers.Thread" Version="3.1.0" />
<PackageReference Include="Serilog.Enrichers.Process" Version="2.0.2" />
<PackageReference Include="Serilog.Enrichers.Environment" Version="2.3.0" />
<PackageReference Include="Serilog.Formatting.Json" Version="1.1.0" />

<!-- 弹性策略 -->
<PackageReference Include="Polly" Version="8.2.0" />
<PackageReference Include="Polly.Extensions.Http" Version="3.0.0" />

<!-- 依赖注入和日志 -->
<PackageReference Include="Microsoft.Extensions.Logging" Version="8.0.0" />
<PackageReference Include="Microsoft.Extensions.Logging.Abstractions" Version="8.0.0" />
<PackageReference Include="Microsoft.Extensions.DependencyInjection" Version="8.0.0" />
<PackageReference Include="Microsoft.Extensions.Http" Version="8.0.0" />
<PackageReference Include="Microsoft.Extensions.Http.Polly" Version="8.0.0" />
```

## 安装方法

### 方法1：使用Package Manager Console

在Visual Studio中打开Package Manager Console（工具 -> NuGet包管理器 -> 程序包管理器控制台），运行以下命令：

```powershell
# Serilog相关
Install-Package Serilog -Version 3.1.1
Install-Package Serilog.Extensions.Logging -Version 8.0.0
Install-Package Serilog.Sinks.Console -Version 5.0.1
Install-Package Serilog.Sinks.File -Version 5.0.0
Install-Package Serilog.Sinks.Debug -Version 2.0.0
Install-Package Serilog.Sinks.EventLog -Version 3.1.0
Install-Package Serilog.Enrichers.Thread -Version 3.1.0
Install-Package Serilog.Enrichers.Process -Version 2.0.2
Install-Package Serilog.Enrichers.Environment -Version 2.3.0
Install-Package Serilog.Formatting.Json -Version 1.1.0

# Polly相关
Install-Package Polly -Version 8.2.0
Install-Package Polly.Extensions.Http -Version 3.0.0

# Microsoft Extensions
Install-Package Microsoft.Extensions.Logging -Version 8.0.0
Install-Package Microsoft.Extensions.Http.Polly -Version 8.0.0
```

### 方法2：使用.NET CLI

在项目目录下运行：

```bash
# Serilog相关
dotnet add package Serilog --version 3.1.1
dotnet add package Serilog.Extensions.Logging --version 8.0.0
dotnet add package Serilog.Sinks.Console --version 5.0.1
dotnet add package Serilog.Sinks.File --version 5.0.0
dotnet add package Serilog.Sinks.Debug --version 2.0.0
dotnet add package Serilog.Sinks.EventLog --version 3.1.0
dotnet add package Serilog.Enrichers.Thread --version 3.1.0
dotnet add package Serilog.Enrichers.Process --version 2.0.2
dotnet add package Serilog.Enrichers.Environment --version 2.3.0
dotnet add package Serilog.Formatting.Json --version 1.1.0

# Polly相关
dotnet add package Polly --version 8.2.0
dotnet add package Polly.Extensions.Http --version 3.0.0

# Microsoft Extensions
dotnet add package Microsoft.Extensions.Logging --version 8.0.0
dotnet add package Microsoft.Extensions.Http.Polly --version 8.0.0
```

### 方法3：使用NuGet包管理器UI

1. 右键点击项目 -> 管理NuGet包
2. 点击"浏览"标签
3. 搜索并安装上述包

## 验证安装

安装完成后，重新构建项目确保没有编译错误：

```bash
dotnet build
```

## 日志文件位置

日志文件将保存在：
- `%LOCALAPPDATA%\LYBT\Logs\lybt-YYYYMMDD.log`
- 例如：`C:\Users\[用户名]\AppData\Local\LYBT\Logs\lybt-20250110.log`

## 配置说明

日志配置已在`ErrorHandlingServiceExtensions.cs`中完成，包括：
- 控制台输出（开发环境）
- JSON格式文件输出
- Windows事件日志（错误级别）
- 日志文件自动滚动（按天）
- 保留30天的日志文件
- 单个日志文件最大10MB
# 项目根目录文件说明

> 本文档说明项目根目录下每个配置文件/目录的用途和约定。

---

## 核心文件

| 文件 | 用途 | 维护说明 |
|------|------|----------|
| `LYBTZYZS.sln` | Visual Studio 解决方案 | 新增项目时通过 `dotnet sln add` 更新 |
| `version.txt` | 当前版本号 | 发布时手动更新 |
| `README.md` | 项目说明 | 面向新开发者的入口文档 |
| `AGENTS.md` | AI Agent 工作指令 | 定义 AI 工具的行为约定 |

## .NET 构建配置

| 文件 | 用途 | 关键内容 |
|------|------|----------|
| `Directory.Build.props` | 全局 MSBuild 属性 | `LangVersion`、`Nullable`、`ImplicitUsings` |
| `Directory.Packages.props` | NuGet 中央包管理 | 所有包版本集中管理，项目文件不指定版本 |
| `global.json` | .NET SDK 版本锁定 | 指定项目使用的 SDK 版本 |
| `nuget.config` | NuGet 源配置 | 包源、包管理行为 |

### 约定

- **包版本**：统一在 `Directory.Packages.props` 中声明，项目 `.csproj` 使用 `<PackageReference />` 不带版本号
- **新建项目**：继承 `Directory.Build.props` 的属性，无需重复配置
- **SDK 版本**：通过 `global.json` 锁定，确保团队构建一致

## Git 配置

| 文件 | 用途 |
|------|------|
| `.gitignore` | 忽略构建产物、临时文件、本地工具配置 |
| `.gitattributes` | 行尾规范（LF/CRLF）、二进制文件标记 |
| `.gitnexusignore` | GitNexus 索引排除列表（项目特定） |

### 忽略规则约定

```
# 本地工具配置（不入库）
.agents/
.codegraph/
.gitnexus/
.graphify-out/
.envsitter/
.mcp.json

# 构建产物
bin/
obj/
out/
publish/

# 运行时生成
backups/
temp/
```

## 代码风格

| 文件 | 用途 |
|------|------|
| `.editorconfig` | 编辑器/IDE 代码风格规则（命名、格式、分析器） |

### 关键约定

- 中文用于业务文档和注释
- 英文用于技术标识符（类名、变量名、API 路由）
- 命名：`PascalCase`（公共成员）、`_camelCase`（私有字段）

## 环境配置

| 文件 | 用途 |
|------|------|
| `.env.example` | 环境变量模板（不含实际密钥） |
| `.config/dotnet-tools.json` | .NET 本地工具清单 |

### MCP 工具配置

MCP 工具配置（`.mcp.json`）属于**用户级配置**，不放在项目目录中。
每个开发者根据需要在自己的环境中配置 MCP 工具。

## 目录结构

```
LYBTZYZS/
├── src/        # 源代码
├── tests/      # 测试代码
├── docs/       # 项目文档
└── .config/    # .NET 工具配置
```

工具生成的本地目录（已忽略）：
- `.agents/` — AI agent 配置
- `.codegraph/` — CodeGraph 索引
- `.gitnexus/` — GitNexus 索引
- `graphify-out/` — Graphify 分析输出

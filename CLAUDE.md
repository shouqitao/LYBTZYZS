# CLAUDE.md - 凌隐宝堂中医诊所系统开发指南

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## 🔄 项目感知与上下文

### 项目概述

凌隐宝堂中医诊所诊疗系统 (LYBTZYZS) - 基于 .NET 8 的企业级纯中医诊所管理系统，采用 Web API 后端 + WPF 桌面前端架构。

### 开始新对话时必须

- **始终先阅读本文档** 了解项目架构、目标、风格和约束
- **检查 `docs/TODO-Latest-*.md`** 了解当前任务状态
- **查看 `CLAUDE.local.md`** 了解用户特定的开发环境配置
- **使用一致的命名约定、文件结构和架构模式**

## 🚀 Serena MCP Server 初始化

### 关于Serena

Serena是一个MCP (Model Context Protocol) 服务器，提供增强的开发工具支持。为了正确使用Serena的工具，Claude需要读取其指令文本。

### 初始化方法

#### 自动初始化（v1.0.52+）
从v1.0.52版本开始，Claude Code会自动读取MCP服务器的指令，无需手动操作。

#### 手动初始化（旧版本或自动失败时）
如果使用旧版本或自动读取失败，可以通过以下方式手动初始化：

1. **方法一：显式请求**
   ```
   请求："read Serena's initial instructions"
   或中文："读取Serena的初始化指令"
   ```

2. **方法二：运行命令**
   ```
   /mcp__serena__initial_instructions
   ```

### 配置要求

要启用initial_instructions工具，需要在配置文件中添加：

```json
{
  "included_optional_tools": [
    "initial_instructions"
  ]
}
```

### 重要时机

**必须重新读取Serena指令的情况**：
- 🔄 开始新对话时
- 🗜️ 执行压缩操作后（compacting operation）
- ❌ Serena工具使用异常时

### 使用验证

初始化成功后，可以通过以下方式验证：
- 检查Serena工具是否可用
- 尝试调用Serena的基础功能
- 查看工具列表中是否包含Serena相关工具

### 注意事项

> ⚠️ **重要**: 每次新会话开始时，建议主动执行初始化命令，确保Claude正确配置了Serena工具。这是保证Serena功能正常工作的关键步骤。

## 🧱 代码结构与模块化

### 文件大小限制

- **永远不要创建超过500行的文件**，接近限制时进行重构
- **组织代码为清晰分离的模块**，按功能或职责分组

### 模块化原则

- 每个业务模块独立但共享数据上下文 (AppDbContext)
- 严格分离关注点：Model、Service、Repository、Controller
- 使用依赖注入：构造函数注入模式
- 异步优先：所有数据库操作使用 async/await

## 📁 文件组织规范（重要）

### 强制性规则

创建或修改文件时，**必须严格遵守**以下规则：

1. **禁止在根目录创建文档文件** - 所有文档必须放在 `docs/` 相应子目录
2. **必须使用英文文件名** - 避免中文造成的编码和跨平台问题
3. **必须使用kebab-case命名** - 如 `user-guide.md`, `fix-report-20250131.md`
4. **报告文件必须包含日期** - 格式：`name-YYYYMMDD.md`
5. **临时文件放入temp/目录** - 并确保在 `.gitignore` 中忽略

### 目录结构规范

```
docs/
├── architecture/    # 架构设计文档
├── api/            # API文档和规范
├── development/    # 开发指南（包含FILE_ORGANIZATION.md）
├── deployment/     # 部署和运维文档
├── testing/        # 测试相关文档
├── reports/        # 项目报告（带日期）
├── guides/         # 用户指南
├── ultrathink/     # UltraThink方法论文档
├── design/         # UI/UX设计文档
├── fixes/          # 问题修复记录
├── progress/       # 进度跟踪
└── legacy/         # 归档文档

scripts/            # 开发和构建脚本
tools/              # 用户工具和启动器
temp/               # 临时文件（gitignored）
```

### 文件命名示例

❌ **错误示例**:
- `WPF登录问题修复报告.md`
- `系统使用说明.md`
- `创建桌面快捷方式.bat`

✅ **正确示例**:
- `docs/reports/wpf-login-fix-report-20250131.md`
- `docs/guides/system-user-guide.md`
- `tools/create-desktop-shortcut.bat`

### 快速参考

| 文件类型 | 位置 | 命名格式 | 示例 |
|---------|------|---------|------|
| 用户指南 | `docs/guides/` | `feature-guide.md` | `system-user-guide.md` |
| API文档 | `docs/api/` | `api-name.md` | `auth-api.md` |
| 项目报告 | `docs/reports/` | `name-YYYYMMDD.md` | `fix-report-20250131.md` |
| UltraThink | `docs/ultrathink/` | `analysis-name.md` | `ui-design-system-report.md` |
| 开发脚本 | `scripts/` | `action.bat` | `build-all.bat` |
| 用户工具 | `tools/` | `tool-name.bat` | `start-system.bat` |

> 📌 **重要**: 详细规范请查看 [文件组织标准](docs/development/FILE_ORGANIZATION.md)

## 🎨 资源管理规范（重要）

### 强制性资源规则

处理资源文件（图标、图片、字体等）时，**必须严格遵守**：

1. **资源文件统一放置** - 所有资源必须放在 `Assets/` 目录及其子目录
2. **遵循命名规范** - 图标: `icon-{purpose}-{size}.png`，图片: `img-{module}-{description}.jpg`
3. **使用Pack URI引用** - `pack://application:,,,/Assets/Icons/icon-name.png`
4. **Build Action设为Resource** - 所有图片资源的生成操作必须设为Resource
5. **使用ResourcePaths常量** - 通过ResourcePaths.cs类引用资源路径

### 资源目录结构

```
src/Frontend/Desktop/
├── Assets/           # 静态资源（图片、图标等）
│   ├── Images/      # 图片文件
│   ├── Icons/       # 图标文件
│   ├── Fonts/       # 字体文件
│   └── Audio/       # 音频文件
├── Themes/          # XAML样式资源
│   ├── Design/      # 设计系统基础
│   └── Controls/    # 控件模板
└── Resources/       # 资源字典
    └── Dictionaries/# 合并的资源字典
```

### 添加新资源步骤

1. 将文件放入正确的Assets子目录
2. 设置Build Action = Resource
3. 在相应的ResourceDictionary中定义资源
4. 更新ResourcePaths.cs添加路径常量
5. 测试资源加载是否正常

> 📌 **重要**: 详细规范请查看 [资源管理指南](src/Frontend/Desktop/Assets/RESOURCE_MANAGEMENT.md)

## 常用开发命令

### 快速启动

```bash
# 交互式开发管理器（推荐）
scripts\dev-manager.bat

# 快速启动开发服务器
scripts\start-dev.bat

# 手动启动（开发时通常使用 Visual Studio）
dotnet run --project src/Backend/Services/LYBT.WebAPI
```

### 构建命令

```bash
# 构建解决方案
dotnet build LYBT.Backend.sln    # 后端
dotnet build LYBT.Desktop.sln    # 前端
dotnet build LYBT.All.sln        # 完整方案

# 发布生产版本
scripts\publish-production.bat
```

### 数据库管理

```bash
# 交互式数据库管理器
scripts\database-manager.bat

# 添加迁移 - 必须使用 Infrastructure 项目
dotnet ef migrations add [迁移名称] --project src/Backend/Core/LYBT.Infrastructure --startup-project src/Backend/Services/LYBT.WebAPI

# 更新数据库
dotnet ef database update --project src/Backend/Core/LYBT.Infrastructure --startup-project src/Backend/Services/LYBT.WebAPI
```

### 测试

```bash
# 运行所有测试
dotnet test

# API 自动化测试
cd tests/api
python api_test_automation.py
```

## 🧪 测试与可靠性

### 测试要求

- **为新功能创建单元测试**（使用 xUnit）
- **更新逻辑后检查现有测试是否需要更新**
- **测试文件位于 `tests/` 文件夹**，镜像主应用结构
- 每个功能至少包含：
  - 1个正常使用测试
  - 1个边缘情况测试  
  - 1个失败情况测试

### 测试完成状态（2025-08-08）

**Repository层测试**（完成）：
- 97个测试用例全部通过
- UserRepository: 31个测试
- PatientRepository: 38个测试 
- HerbRepository: 28个测试

**Service层测试**（进行中）：
- UserService: 68个测试用例（完成）
- PatientService: 88个测试用例（完成）
- 总计156个Service层测试已完成
- 下一步：HerbService、AuthService单元测试

**代码覆盖率**：从2.30%提升至2.76%，目标60%

### API测试

- 使用 Python 脚本进行 API 自动化测试
- 测试脚本位于 `tests/api/` 目录
- 运行命令：`python api_test_automation.py`

## 高层架构

### 整体技术栈

- **后端**: .NET 8, ASP.NET Core Web API, Entity Framework Core 8.0.17, SQL Server
- **前端**: WPF (.NET 8), Prism.DryIoc 9.0.537, Refit
- **认证**: JWT Bearer Token
- **API文档**: Swagger/Swashbuckle 9.0.1

### 项目结构

```
src/
├── Backend/
│   ├── Core/
│   │   ├── LYBT.Infrastructure/     # 统一 AppDbContext，所有迁移在此
│   │   └── LYBT.Models/            # 领域模型
│   ├── Modules/                    # 15个业务模块
│   └── Services/LYBT.WebAPI/       # Web API 入口
├── Frontend/Desktop/               # WPF 客户端
└── Shared/                        # 前后端共享模型
```

### 关键架构特点

1. **统一数据访问**: 所有模块共享 `AppDbContext`（在 Infrastructure 中）
2. **模块化设计**: 每个业务模块独立但共享数据上下文
3. **整洁架构**: 严格分离关注点
4. **API 响应包装**: 所有响应包装在 `ApiResponse<T>` 中
5. **依赖注入**: 构造函数注入模式
6. **异步优先**: 数据库操作使用 async/await

### 业务模块列表（实际存在的8个核心模块）

1. **Auth** - 身份认证和授权
2. **Users** - 用户管理  
3. **Patients** - 患者档案（包含基础挂号功能）
4. **Consultation** - 看诊管理（核心模块，支持中医四诊）
5. **MedicalCase** - 医疗案例（统一管理整个诊疗流程，包含病历记录）
6. **Prescriptions** - 处方管理
7. **Herbs** - 中药材管理（仅处方用药，不涉及库存管理）
8. **Formula** - 验方管理（经典验方模板，支持处方组合）

**重要说明**：
- **Herbs模块**：只负责管理诊所可用药材信息和单价，供医生开处方时选择使用，不涉及药品库存管理
- **Formula模块**：管理验方模板，支持经典验方库和医生个人验方，可被Prescriptions引用组合
- **MedicalCase模块**：作为诊疗流程的聚合根，包含了原Records（病历档案）的功能
- **Patients模块**：整合了基础的患者接待功能，简化了原Registration（挂号）流程

## 核心工作流

### 诊疗流程（简化版）

```
患者接待(Patients) → 看诊(Consultation) → 开方(Prescriptions)
         ↑                    ↓
      医疗案例(MedicalCase)贯穿全程
```

- **MedicalCase** 贯穿整个流程，统一管理患者的诊疗案例和病历记录
- **Consultation** 是核心，支持中医四诊（望闻问切）
- **Patients** 模块处理患者信息管理和基础接待功能
- **Prescriptions** 结合Formula（验方）和Herbs（中药材）完成处方开具

## ✅ 任务完成

### 任务管理

- **立即在 `docs/TODO-Latest-*.md` 中标记完成的任务**
- **添加发现的新任务到"发现的问题"部分**
- **每个任务完成后创建详细的 commit**

### Git 提交规范

```bash
# 提交格式
<type>: <subject>

# type 类型：
- feat: 新功能
- fix: 修复bug
- docs: 文档更新
- refactor: 重构
- test: 测试相关
- chore: 构建/工具相关
```

## 📎 风格与约定

### C# 编码规范

- **遵循 .NET 编码约定**
- **使用 PascalCase** 用于类名、方法名、属性
- **使用 camelCase** 用于参数、局部变量
- **私有字段使用 _camelCase**
- **接口以 I 开头**

### 代码注释规范

```csharp
/// <summary>
/// 方法功能简述
/// </summary>
/// <param name="参数名">参数说明</param>
/// <returns>返回值说明</returns>
public async Task<Result> MethodName(Type param)
{
    // 关键逻辑注释
    // Reason: 解释为什么这样做
}
```

## 开发约定

### 必须遵循的规则

1. **数据库迁移**: 只能在 `LYBT.Infrastructure` 项目中添加
2. **数据访问**: 使用统一的 `AppDbContext`
3. **API 响应格式**: 遵循 [API响应标准](docs/API响应标准.md)
   - POST 方法返回 `Ok(createdObject)`
   - PUT/DELETE 方法返回 `Ok(new { message = "xxx" })`
   - 错误响应使用 `ProblemDetails`
4. **对象映射**: 使用 AutoMapper
5. **模块模式**: 新模块遵循现有模块结构（Interfaces/Services/Repositories/Mapping）

### 环境配置

- **数据库**: SQL Server (localhost/LYBTDB)
- **API端口**: https://localhost:7001
- **默认登录**: sysadmin / Admin@123456
- **JWT过期**: 8小时（Remember Me: 30天）
- **默认密码配置**:
  - 普通用户: `ChangeMe123` (UserOptions.DefaultUserPassword)
  - 管理员: `Admin@123456` (SysAdminOptions.DefaultPassword)
  - 详见: [默认密码文档](docs/development/DEFAULT_PASSWORDS.md)

### 开发流程

1. 使用 Visual Studio 手动运行项目（根据 CLAUDE.local.md）
2. API 文档访问: https://localhost:7001/swagger
3. 使用 scripts/ 目录的批处理文件执行常见任务
4. 数据库在首次运行时自动初始化

## 📚 文档与可解释性

### 文档更新要求

- **添加新功能时更新 `README.md`**
- **更改依赖时更新 `docs/开发规范.md`**
- **修改设置时更新本文档**
- **注释非显而易见的代码**，确保中级开发者能理解
- **编写复杂逻辑时，添加 `# Reason:` 注释**解释原因

## 🧠 AI 行为规则

### 必须遵守

- **永远不要假设缺失的上下文，不确定时询问**
- **永远不要虚构库或函数**，只使用已验证的包
- **始终确认文件路径和模块名存在**再引用
- **除非明确指示，否则不要删除或覆盖现有代码**

### 中文支持

- **所有显示和回答使用中文**
- **注意处理中文字符编码问题**
- **API 响应消息使用中文**
- **错误提示使用中文**

## 术语说明

- **Prescriptions**: 处方（医生开具的用药指导）
- **Formula**: 验方（经典处方模板）
- **Consultation**: 看诊（中医四诊过程）
- **Herbs**: 中药材（仅用于处方，不含库存管理）
- **TCM**: 中医（Traditional Chinese Medicine）
- **MedicalCase**: 医疗案例（诊疗流程聚合根，包含完整病历）
- **Patients**: 患者（包含档案和基础接待功能）

## 项目特定指令

- 显示和回答都用中文
- 本项目数据库为 SQL Server（不是 LocalDB）
- 开发时手动用 VS 执行运行操作

## 文档管理

- 文档同时保留一份中文版，一份英文版。

## 开发规范文档

- [开发规范](docs/开发规范.md) - 完整的开发规范指南
- [前后端契约规范](docs/前后端契约规范.md) - 前后端接口约定
- [API响应标准](docs/API响应标准.md) - API 响应格式规范

## 🔧 特殊配置说明

### AutoMapper 配置（重要）

- 使用 AutoMapper 15.0.1
- **必须提供 ILoggerFactory 参数**
- 示例配置：
  
  ```csharp
  var mapperConfig = new MapperConfiguration(cfg =>
  {
    cfg.AddProfile(new MappingProfile());
  }, NullLoggerFactory.Instance);  // 关键：需要ILoggerFactory参数
  ```

**测试中的AutoMapper配置**：
```csharp
// 在单元测试中的正确配置方式
var config = new MapperConfiguration(cfg => 
    cfg.AddProfile(new UserMappingProfile()), 
    NullLoggerFactory.Instance);  // 必须的第二参数
var mapper = config.CreateMapper();
```

### 依赖注入配置

- 使用 Prism.DryIoc 9.0.537
- 服务注册在 ServiceCollectionExtensions.cs
- 所有服务使用构造函数注入

## 脚本管理

- 脚本都用Python脚本

## 📋 常见问题快速解决

### 编译错误

1. 检查 NuGet 包版本
2. 清理解决方案：`dotnet clean`
3. 重新生成：`dotnet build`

### 依赖注入错误

1. 检查服务是否已注册
2. 确认接口和实现匹配
3. 检查构造函数参数

### 数据库连接问题

1. 确认 SQL Server 服务运行
2. 检查连接字符串
3. 验证数据库权限

## 🎯 项目质量目标

- **代码覆盖率目标：60%**（当前：2.76%）
- 响应时间 < 2秒
- 零关键bug
- 完整的错误处理
- 用户友好的中文提示

### 质量改进计划

1. **高优先级**：完成核心Service层测试（HerbService、AuthService）
2. **中优先级**：添加Controller层测试，提升覆盖率到60%
3. **低优先级**：实现缓存机制、API版本管理

## 中文编码

- 使用 UTF-8 编码处理所有中文字符
- 建议使用 Unicode 标准处理中文文本
- 在文件头添加 BOM 头以确保正确识别编码
- 注意跨平台兼容性和编码一致性
- windows 10 中文版 开发环境
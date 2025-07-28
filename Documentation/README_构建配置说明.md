# 项目文档自动输出配置说明

## 概述

本项目已配置MSBuild自动构建时将模块文档文件复制到解决方案根目录的`Documentation`文件夹中。这确保了所有模块的文档在构建时都能统一收集和管理。

## 配置详情

### 配置文件：`Directory.Build.props`

该文件包含了以下关键配置：

1. **文档输出路径定义**
   ```xml
   <DocumentationOutputPath>$(SolutionDir)Documentation</DocumentationOutputPath>
   ```

2. **文档复制目标**
   - 构建后自动执行 `CopyDocumentationFiles` 目标
   - 查找每个项目中的 `FUNCTIONALITY.md` 和 `README.md` 文件
   - 复制到统一的文档目录，并添加项目名称前缀

3. **清理目标**
   - 执行 `Clean` 操作时自动清理文档文件

## 文档命名规则

复制到 `Documentation` 文件夹的文档文件按以下格式命名：

- `{项目名}_FUNCTIONALITY.md` - 功能说明文档
- `{项目名}_README.md` - 项目说明文档

例如：
- `LYBT.Module.TreatmentRoom_FUNCTIONALITY.md`
- `LYBT.Module.Pharmacy_README.md`

## 支持的文档类型

系统会自动检测并复制以下类型的文档：

1. **FUNCTIONALITY.md** - 详细的功能说明文档，包含：
   - 模块概述和业务价值
   - 数据模型定义
   - DTO 对象说明
   - 服务层和仓储层方法
   - 权限控制策略
   - 日志审计机制
   - 使用示例

2. **README.md** - 项目基本说明文档，包含：
   - 项目介绍
   - 快速开始指南
   - 基本配置说明

## 构建命令

### 自动文档输出

任何标准的 dotnet build 命令都会触发文档复制：

```bash
# 构建单个项目
dotnet build LYBT.Module.TreatmentRoom

# 构建整个解决方案（如果没有编译错误）
dotnet build

# 构建并查看详细信息
dotnet build --verbosity detailed
```

### 使用便捷脚本

运行根目录下的 `build-with-docs.bat` 脚本：

```bash
build-with-docs.bat
```

该脚本会：
1. 依次构建所有模块项目
2. 自动触发文档复制
3. 显示生成的文档文件列表
4. 提供使用说明

## 文档结构

构建完成后，`Documentation` 文件夹包含所有模块的文档：

```
Documentation/
├── LYBT.Common_FUNCTIONALITY.md
├── LYBT.Common_README.md
├── LYBT.Infrastructure_FUNCTIONALITY.md
├── LYBT.Infrastructure_README.md
├── LYBT.Module.Auth_FUNCTIONALITY.md
├── LYBT.Module.Auth_README.md
├── LYBT.Module.Billing_FUNCTIONALITY.md
├── LYBT.Module.Billing_README.md
├── LYBT.Module.DiagnosisTreatment_FUNCTIONALITY.md
├── LYBT.Module.DiagnosisTreatment_README.md
├── LYBT.Module.FormulaTemplates_FUNCTIONALITY.md
├── LYBT.Module.FormulaTemplates_README.md
├── LYBT.Module.Pharmacy_FUNCTIONALITY.md
├── LYBT.Module.Pharmacy_README.md
├── LYBT.Module.Queueing_FUNCTIONALITY.md
├── LYBT.Module.Queueing_README.md
├── LYBT.Module.Records_FUNCTIONALITY.md
├── LYBT.Module.Records_README.md
├── LYBT.Module.Sync_FUNCTIONALITY.md
├── LYBT.Module.Sync_README.md
├── LYBT.Module.TreatmentRoom_FUNCTIONALITY.md
├── LYBT.Module.TreatmentRoom_README.md
├── LYBT.Module.Users_FUNCTIONALITY.md
└── LYBT.Module.Users_README.md
```

## 技术实现

### MSBuild 目标定义

```xml
<Target Name="CopyDocumentationFiles" AfterTargets="Build">
  <ItemGroup>
    <!-- 查找所有模块的FUNCTIONALITY.md文件 -->
    <FunctionalityDocs Include="$(MSBuildProjectDirectory)\FUNCTIONALITY.md" 
                       Condition="Exists('$(MSBuildProjectDirectory)\FUNCTIONALITY.md')" />
    <!-- 查找所有模块的README.md文件 -->
    <ReadmeDocs Include="$(MSBuildProjectDirectory)\README.md" 
                Condition="Exists('$(MSBuildProjectDirectory)\README.md')" />
  </ItemGroup>
  
  <!-- 创建文档输出目录 -->
  <MakeDir Directories="$(DocumentationOutputPath)" 
           Condition="!Exists('$(DocumentationOutputPath)')" />
  
  <!-- 复制文档文件 -->
  <Copy SourceFiles="@(FunctionalityDocs)" 
        DestinationFiles="$(DocumentationOutputPath)\$(MSBuildProjectName)_FUNCTIONALITY.md"
        Condition="@(FunctionalityDocs) != ''"
        SkipUnchangedFiles="true" />
        
  <Copy SourceFiles="@(ReadmeDocs)" 
        DestinationFiles="$(DocumentationOutputPath)\$(MSBuildProjectName)_README.md"
        Condition="@(ReadmeDocs) != ''"
        SkipUnchangedFiles="true" />
</Target>
```

### 特性说明

1. **条件执行** - 只有存在对应文档文件时才会执行复制
2. **增量更新** - 使用 `SkipUnchangedFiles="true"` 避免不必要的复制
3. **自动创建目录** - 如果文档目录不存在会自动创建
4. **构建信息** - 复制成功时会显示信息消息
5. **清理支持** - Clean 操作时自动清理生成的文档

## 优势

1. **自动化** - 无需手动复制文档，构建时自动完成
2. **统一管理** - 所有模块文档集中在一个位置
3. **版本同步** - 文档与代码构建保持同步
4. **易于分发** - 文档输出到解决方案级别，便于打包和分发
5. **开发友好** - 不影响原有的开发流程，对现有项目结构无侵入

## 注意事项

1. 只有在项目根目录存在对应文档文件时才会进行复制
2. 文档文件命名必须严格按照 `FUNCTIONALITY.md` 或 `README.md`
3. 如果需要添加其他类型的文档，可以修改 `Directory.Build.props` 配置
4. Clean 操作会删除生成的文档文件，如需保留请做好备份

## 扩展说明

如需添加其他类型的文档支持，可以在 `Directory.Build.props` 中添加相应的 ItemGroup 和 Copy 任务。

例如，添加 `API.md` 文档支持：

```xml
<ItemGroup>
  <ApiDocs Include="$(MSBuildProjectDirectory)\API.md" 
           Condition="Exists('$(MSBuildProjectDirectory)\API.md')" />
</ItemGroup>

<Copy SourceFiles="@(ApiDocs)" 
      DestinationFiles="$(DocumentationOutputPath)\$(MSBuildProjectName)_API.md"
      Condition="@(ApiDocs) != ''"
      SkipUnchangedFiles="true" />
```
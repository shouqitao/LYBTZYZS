# 测试覆盖率收集指南

本文档说明如何使用配置好的覆盖率工具链来收集和查看服务端单元测试覆盖率。

## 快速开始

### Windows (PowerShell)
```powershell
# 运行测试并生成覆盖率报告（自动打开报告）
.\tests\RunCoverage.ps1

# 运行但不自动打开报告
.\tests\RunCoverage.ps1 -OpenReport $false

# 使用Debug配置运行
.\tests\RunCoverage.ps1 -Configuration Debug

# 强制覆盖率阈值检查
.\tests\RunCoverage.ps1 -EnforceThresholds $true
```

### Linux/Mac (Bash)
```bash
# 运行测试并生成覆盖率报告
./tests/run-coverage.sh

# 运行但不自动打开报告
./tests/run-coverage.sh Release false

# 强制覆盖率阈值检查
./tests/run-coverage.sh Release true true
```

## 手动运行步骤

如果脚本无法运行，可以手动执行以下步骤：

### 1. 收集覆盖率数据
```bash
dotnet test LYBT.Server.sln \
    -c Release \
    --collect:"XPlat Code Coverage" \
    --results-directory BIN/TestResults
```

### 2. 生成HTML报告
```bash
reportgenerator \
    -reports:BIN/TestResults/**/coverage.cobertura.xml \
    -targetdir:BIN/TestResults/coverage \
    -reporttypes:Html;Cobertura;JsonSummary;Badges
```

### 3. 查看报告
打开 `BIN/TestResults/coverage/index.html` 查看详细的覆盖率报告。

## 工具链组成

- **Coverlet**: 跨平台的代码覆盖率收集工具
  - `coverlet.msbuild`: MSBuild集成
  - `coverlet.collector`: dotnet test集成

- **ReportGenerator**: 覆盖率报告生成工具
  - 支持多种格式：HTML, Cobertura, JSON, Badges
  - 提供详细的行级和分支级覆盖率信息

## 配置文件

### Directory.Packages.props
定义了覆盖率工具的版本：
- coverlet.collector: 6.0.2
- coverlet.msbuild: 6.0.2
- ReportGenerator: 5.3.13

### tests/Directory.Build.targets
为所有测试项目自动配置覆盖率收集：
- 自动添加Coverlet包引用
- 配置排除规则（ObsoleteAttribute, GeneratedCode等）
- 设置输出格式和路径

## 覆盖率目标

根据PRD要求，我们的覆盖率目标是：

| 指标 | 整体目标 | 关键模块目标 |
|------|---------|-------------|
| 行覆盖率 | ≥90% | ≥95% |
| 分支覆盖率 | ≥80% | - |
| 方法覆盖率 | - | - |

关键模块包括：
- Auth（认证）
- Users（用户管理）
- MedicalCase（病历）
- Prescriptions（处方）

## 报告输出位置

所有覆盖率报告将生成在以下位置：
- **HTML报告**: `BIN/TestResults/coverage/index.html`
- **Cobertura XML**: `BIN/TestResults/coverage/Cobertura.xml`
- **JSON摘要**: `BIN/TestResults/coverage/Summary.json`
- **徽章**: `BIN/TestResults/coverage/badge_*.svg`

## CI/CD集成

在CI管道中使用覆盖率检查：

```yaml
# Azure DevOps示例
- task: DotNetCoreCLI@2
  inputs:
    command: 'test'
    projects: 'LYBT.Server.sln'
    arguments: '--configuration Release --collect:"XPlat Code Coverage"'

- task: reportgenerator@5
  inputs:
    reports: '$(Agent.TempDirectory)/**/coverage.cobertura.xml'
    targetdir: '$(Build.ArtifactStagingDirectory)/coverage'
    reporttypes: 'HtmlInline_AzurePipelines;Cobertura'

- task: PublishCodeCoverageResults@1
  inputs:
    codeCoverageTool: 'Cobertura'
    summaryFileLocation: '$(Build.ArtifactStagingDirectory)/coverage/Cobertura.xml'
    reportDirectory: '$(Build.ArtifactStagingDirectory)/coverage'
```

## 故障排除

### 问题：找不到reportgenerator命令
**解决方案**：
```bash
dotnet tool install -g dotnet-reportgenerator-globaltool
```

### 问题：覆盖率文件未生成
**可能原因**：
1. 测试项目未正确引用Coverlet包
2. 测试执行失败
3. 输出目录权限问题

**解决方案**：
1. 检查`tests/Directory.Build.targets`是否存在
2. 确保测试能正常运行
3. 清理输出目录：`rm -rf BIN/TestResults`

### 问题：覆盖率数据不准确
**可能原因**：
1. 包含了自动生成的代码
2. 测试未覆盖所有项目

**解决方案**：
检查并调整排除规则在`tests/Directory.Build.targets`中：
```xml
<ExcludeByAttribute>ObsoleteAttribute,GeneratedCodeAttribute,CompilerGeneratedAttribute</ExcludeByAttribute>
<ExcludeByFile>**/*.Designer.cs,**/*.g.cs,**/*.g.i.cs</ExcludeByFile>
```

## 下一步任务

配置完成后，接下来的任务是：
1. **Task 2-7**: 为各模块补充单元测试
2. **Task 8**: 创建CI覆盖率门禁
3. **Task 9**: 编写团队培训文档
4. **Task 10**: 验收和总结

---

*文档更新日期：2025-09-21*
*负责人：DevOps团队*
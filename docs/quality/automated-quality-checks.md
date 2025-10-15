# 自动化质量检查文档

> **版本**: 1.0
> **创建日期**: 2025-10-15
> **维护者**: 项目质量团队
> **相关文档**: [架构合规检查](architecture-compliance.md) | [测试指南](../development/testing-guide.md) | [CI/CD 流程](../deployment/cicd-pipeline.md)

## 📋 文档概述

本文档提供 LYBT 医疗信息系统自动化质量检查的全面实施指南，涵盖代码质量、架构合规、安全检查、性能监控和测试自动化。旨在通过自动化手段持续提升代码质量，确保系统稳定性和安全性。

## 🎯 质量检查目标

### 核心目标
- **代码质量**: 确保代码符合编码规范和最佳实践
- **架构合规**: 维护系统架构的一致性和完整性
- **安全检查**: 自动发现和修复安全漏洞
- **性能监控**: 持续监控系统性能指标
- **测试覆盖**: 确保充分的测试覆盖率

### 质量指标
- **代码覆盖率**: ≥ 80%
- **代码质量评分**: ≥ 85分
- **安全漏洞**: 0个高危漏洞
- **架构违规**: 0个严重违规
- **性能回归**: < 5%

## 🔧 代码质量检查

### 1. 静态代码分析

#### SonarQube 集成配置
```yaml
# sonar-project.properties
sonar.projectKey=lybt-medical-system
sonar.projectName=LYBT Medical System
sonar.projectVersion=1.0.0

# 项目配置
sonar.sources=src/
sonar.tests=tests/
sonar.exclusions=**/bin/**,**/obj/**,**/packages/**,**/*.Designer.cs

# 代码质量阈值
sonar.qualitygate.wait=true

# C# 特定配置
sonar.cs.analyzeProject=true
sonar.cs.file.suffixes=.cs
sonar.cs.ignoreHeaderComments=true

# 测试覆盖率配置
sonar.cs.coverage.unitTests=tests/**/*UnitTests*.cs
sonar.cs.coverage.integrationTests=tests/**/*IntegrationTests*.cs
sonar.cs.vscoveragexml.reportsPaths=**/*.coveragexml

# 代码复杂度阈值
sonar.complexity.class.threshold=20
sonar.complexity.file.threshold=200
sonar.complexity.function.threshold=10

# 重复代码检测
sonar.cpd.exclusions=**/*Tests.cs,**/Migrations/**/*.cs
sonar.cpd.minimum.tokens=100

# 安全检查
sonar.cs.hotspot.enabled=true
sonar.cs.security.hotspot.enabled=true
```

#### GitHub Actions 工作流
```yaml
# .github/workflows/quality-checks.yml
name: Automated Quality Checks

on:
  push:
    branches: [ main, develop ]
  pull_request:
    branches: [ main ]

jobs:
  code-quality:
    runs-on: ubuntu-latest

    steps:
    - name: Checkout code
      uses: actions/checkout@v3
      with:
        fetch-depth: 0

    - name: Setup .NET
      uses: actions/setup-dotnet@v3
      with:
        dotnet-version: '8.0.x'

    - name: Restore dependencies
      run: dotnet restore LYBT.All.sln

    - name: Build solution
      run: dotnet build LYBT.All.sln --no-restore --configuration Release

    - name: Run unit tests with coverage
      run: |
        dotnet test LYBT.All.sln \
          --no-build \
          --configuration Release \
          --logger "trx;LogFileName=test_results.trx" \
          --results-directory TestResults \
          --collect:"XPlat Code Coverage"

    - name: Convert coverage to SonarQube format
      run: |
        dotnet tool install --global dotnet-coverage
        dotnet tool install --global coverageconverter
        coverageconverter TestResults/coverage.cobertura.xml -o TestResults/sonarqube.xml

    - name: SonarQube Scan
      uses: SonarSource/sonarqube-scan-action@v1.2.0
      env:
        SONAR_TOKEN: ${{ secrets.SONAR_TOKEN }}
        GITHUB_TOKEN: ${{ secrets.GITHUB_TOKEN }}

    - name: CodeQL Analysis
      uses: github/codeql-action/init@v2
      with:
        languages: csharp

    - name: Perform CodeQL Analysis
      uses: github/codeql-action/analyze@v2

    - name: Upload test results
      uses: actions/upload-artifact@v3
      if: always()
      with:
        name: test-results
        path: TestResults/
```

#### 本地质量检查脚本
```powershell
# scripts/quality-check.ps1
param(
    [string]$ProjectPath = ".",
    [switch]$FixIssues,
    [switch]$Verbose
)

Write-Host "开始自动化质量检查..." -ForegroundColor Green

# 1. 代码格式检查
Write-Host "检查代码格式..." -ForegroundColor Yellow
dotnet format --verify-no-changes $ProjectPath/LYBT.All.sln
if ($LASTEXITCODE -ne 0) {
    Write-Host "代码格式检查失败，请运行 'dotnet format' 修复格式问题" -ForegroundColor Red
    exit 1
}

# 2. 编译检查
Write-Host "检查项目编译..." -ForegroundColor Yellow
dotnet build $ProjectPath/LYBT.All.sln --no-restore --configuration Release
if ($LASTEXITCODE -ne 0) {
    Write-Host "项目编译失败" -ForegroundColor Red
    exit 1
}

# 3. 运行测试
Write-Host "运行单元测试..." -ForegroundColor Yellow
dotnet test $ProjectPath/LYBT.All.sln --no-build --configuration Release --logger "console;verbosity=normal"
if ($LASTEXITCODE -ne 0) {
    Write-Host "单元测试失败" -ForegroundColor Red
    exit 1
}

# 4. 代码分析
Write-Host "运行代码分析..." -ForegroundColor Yellow
dotnet tool install --global dotnet-sonarscanner
dotnet sonarscanner begin /k:"sonar-project.properties"
dotnet build $ProjectPath/LYBT.All.sln --no-restore --configuration Release
dotnet test $ProjectPath/LYBT.All.sln --no-build --configuration Release --logger "trx;LogFileName=test_results.trx" --results-directory TestResults --collect:"XPlat Code Coverage"
dotnet sonarscanner end

# 5. 安全扫描
Write-Host "运行安全扫描..." -ForegroundColor Yellow
dotnet tool install --global SecurityCodeScan
dotnet-scanner $ProjectPath/src --format Csv --output-file security-scan-results.csv

# 6. 依赖检查
Write-Host "检查依赖项安全..." -ForegroundColor Yellow
dotnet list package --outdated --include-prerelease
dotnet tool install --global dotnet-outdated-tool
dotnet outdated $ProjectPath/LYBT.All.sln

Write-Host "质量检查完成！" -ForegroundColor Green
```

### 2. 代码规范检查

#### .editorconfig 配置
```ini
# .editorconfig
root = true

# C# 代码规范
[*.cs]
indent_style = space
indent_size = 4
tab_width = 4
end_of_line = crlf
insert_final_newline = true
trim_trailing_whitespace = true

# using 指令排序
dotnet_sort_system_directives_first = true
dotnet_separate_import_directive_groups = true

# 代码风格
dotnet_style_qualification_for_field = false:silent
dotnet_style_qualification_for_property = false:silent
dotnet_style_qualification_for_method = false:silent
dotnet_style_qualification_for_event = false:silent
dotnet_style_readonly_field = true:suggestion

# 表达式级别首选项
dotnet_style_object_initializer = true:suggestion
dotnet_style_collection_initializer = true:suggestion
dotnet_style_explicit_tuple_names = true:suggestion
dotnet_style_null_propagation = true:suggestion
dotnet_style_coalesce_expression = true:suggestion
dotnet_style_prefer_is_null_check_over_reference_equality = true:suggestion
dotnet_style_prefer_inferred_tuple_names = true:suggestion
dotnet_style_prefer_inferred_anonymous_type_member_names = true:suggestion
dotnet_style_prefer_auto_properties = true:silent
dotnet_style_prefer_conditional_expression_over_assignment = true:silent
dotnet_style_prefer_conditional_expression_over_return = true:silent

# 命名约定
dotnet_naming_rule.interface_should_be_prefixed_with_i = true
dotnet_naming_rule.types_should_be_pascal_case = true
dotnet_naming_rule.non_field_members_should_be_pascal_case = true

# 文件头
file_header_template = Copyright (c) {company_name}. All rights reserved.\nLicensed under the {license_name} license.\n
```

#### StyleCop.Analyzers 配置
```xml
<!-- StyleCop.Analyzers.ruleset -->
<?xml version="1.0" encoding="utf-8"?>
<RuleSet Name="LYBT StyleCop Rules" Description="LYBT 项目代码规范">
  <Rules>
    <!-- 文档规则 -->
    <Rule Action="Info" RuleId="SA1633" />
    <Rule Action="Info" RuleId="SA1642" />
    <Rule Action="Warning" RuleId="SA1600" />
    <Rule Action="Warning" RuleId="SA1611" />
    <Rule Action="Warning" RuleId="SA1615" />

    <!-- 可读性规则 -->
    <Rule Action="Warning" RuleId="SA1101" />
    <Rule Action="Warning" RuleId="SA1127" />
    <Rule Action="Warning" RuleId="SA1128" />
    <Rule Action="Info" RuleId="SA1130" />
    <Rule Action="Warning" RuleId="SA1131" />
    <Rule Action="Warning" RuleId="SA1133" />
    <Rule Action="Warning" RuleId="SA1134" />
    <Rule Action="Warning" RuleId="SA1135" />
    <Rule Action="Warning" RuleId="SA1136" />
    <Rule Action="Warning" RuleId="SA1137" />
    <Rule Action="Warning" RuleId="SA1141" />
    <Rule Action="Warning" RuleId="SA1142" />

    <!-- 排序规则 -->
    <Rule Action="Warning" RuleId="SA1200" />
    <Rule Action="Warning" RuleId="SA1201" />
    <Rule Action="Warning" RuleId="SA1202" />
    <Rule Action="Warning" RuleId="SA1204" />
    <Rule Action="Warning" RuleId="SA1206" />
    <Rule Action="Warning" RuleId="SA1207" />
    <Rule Action="Warning" RuleId="SA1208" />
    <Rule Action="Warning" RuleId="SA1209" />
    <Rule Action="Warning" RuleId="SA1210" />
    <Rule Action="Warning" RuleId="SA1211" />
    <Rule Action="Warning" RuleId="SA1212" />
    <Rule Action="Warning" RuleId="SA1213" />
    <Rule Action="Warning" RuleId="SA1214" />
    <Rule Action="Warning" RuleId="SA1215" />
    <Rule Action="Warning" RuleId="SA1216" />
    <Rule Action="Warning" RuleId="SA1217" />

    <!-- 命名规则 -->
    <Rule Action="Warning" RuleId="SA1300" />
    <Rule Action="Warning" RuleId="SA1302" />
    <Rule Action="Warning" RuleId="SA1303" />
    <Rule Action="Warning" RuleId="SA1304" />
    <Rule Action="Warning" RuleId="SA1306" />
    <Rule Action="Warning" RuleId="SA1307" />
    <Rule Action="Warning" RuleId="SA1308" />
    <Rule Action="Warning" RuleId="SA1309" />
    <Rule Action="Warning" RuleId="SA1310" />
    <Rule Action="Warning" RuleId="SA1311" />
    <Rule Action="Warning" RuleId="SA1312" />
    <Rule Action="Warning" RuleId="SA1313" />
    <Rule Action="Warning" RuleId="SA1314" />

    <!-- 维护性规则 -->
    <Rule Action="Warning" RuleId="SA1400" />
    <Rule Action="Warning" RuleId="SA1401" />
    <Rule Action="Warning" RuleId="SA1402" />
    <Rule Action="Warning" RuleId="SA1403" />
    <Rule Action="Warning" RuleId="SA1404" />
    <Rule Action="Warning" RuleId="SA1405" />
    <Rule Action="Warning" RuleId="SA1406" />
    <Rule Action="Warning" RuleId="SA1407" />
    <Rule Action="Warning" RuleId="SA1408" />
    <Rule Action="Warning" RuleId="SA1410" />
    <Rule Action="Warning" RuleId="SA1411" />
    <Rule Action="Warning" RuleId="SA1413" />
    <Rule Action="Warning" RuleId="SA1414" />

    <!-- 布局规则 -->
    <Rule Action="Warning" RuleId="SA1500" />
    <Rule Action="Warning" RuleId="SA1501" />
    <Rule Action="Warning" RuleId="SA1502" />
    <Rule Action="Warning" RuleId="SA1503" />
    <Rule Action="Warning" RuleId="SA1504" />
    <Rule Action="Warning" RuleId="SA1505" />
    <Rule Action="Warning" RuleId="SA1506" />
    <Rule Action="Warning" RuleId="SA1507" />
    <Rule Action="Warning" RuleId="SA1508" />
    <Rule Action="Warning" RuleId="SA1509" />
    <Rule Action="Warning" RuleId="SA1510" />
    <Rule Action="Warning" RuleId="SA1511" />
    <Rule Action="Warning" RuleId="SA1512" />
    <Rule Action="Warning" RuleId="SA1513" />
    <Rule Action="Warning" RuleId="SA1514" />
    <Rule Action="Warning" RuleId="SA1515" />
    <Rule Action="Warning" RuleId="SA1516" />
    <Rule Action="Warning" RuleId="SA1517" />
    <Rule Action="Warning" RuleId="SA1518" />
    <Rule Action="Warning" RuleId="SA1519" />
    <Rule Action="Warning" RuleId="SA1520" />
  </Rules>
</RuleSet>
```

## 🏗️ 架构合规检查

### 1. 架构规则定义

#### 架构规则配置
```json
{
  "architectureRules": {
    "layeringRules": [
      {
        "name": "Controller-Layer-Dependencies",
        "description": "控制器层只能依赖服务层，不能直接依赖数据访问层",
        "sourceLayer": "Controllers",
        "targetLayers": ["Services"],
        "forbiddenTargets": ["Repositories", "Data", "Infrastructure"],
        "severity": "Error"
      },
      {
        "name": "Service-Layer-Dependencies",
        "description": "服务层通过仓储接口访问数据，不能直接依赖数据库实现",
        "sourceLayer": "Services",
        "targetLayers": ["Repositories"],
        "forbiddenTargets": ["DbContext", "EntityFramework"],
        "severity": "Error"
      },
      {
        "name": "No-Circular-Dependencies",
        "description": "禁止模块间的循环依赖",
        "severity": "Error"
      }
    ],
    "namingRules": [
      {
        "pattern": ".*Repository$",
        "description": "仓储类必须以 Repository 结尾",
        "targetType": "Class",
        "severity": "Warning"
      },
      {
        "pattern": ".*Service$",
        "description": "服务类必须以 Service 结尾",
        "targetType": "Class",
        "severity": "Warning"
      },
      {
        "pattern": ".*Controller$",
        "description": "控制器类必须以 Controller 结尾",
        "targetType": "Class",
        "severity": "Error"
      }
    ],
    "dependencyRules": [
      {
        "name": "No-Database-Direct-Access",
        "description": "禁止在服务层直接访问数据库",
        "forbiddenDependencies": ["System.Data.SqlClient", "Microsoft.EntityFrameworkCore"],
        "allowedLayers": ["Repositories", "Infrastructure"],
        "severity": "Error"
      },
      {
        "name": "No-Static-Database-Context",
        "description": "禁止使用静态数据库上下文",
        "forbiddenPattern": "static.*DbContext",
        "severity": "Error"
      }
    ],
    "designRules": [
      {
        "name": "No-God-Classes",
        "description": "单个类不能超过500行",
        "maxLines": 500,
        "severity": "Warning"
      },
      {
        "name": "No-Long-Methods",
        "description": "单个方法不能超过50行",
        "maxMethodLines": 50,
        "severity": "Warning"
      },
      {
        "name": "No-Too-Many-Parameters",
        "description": "方法参数不能超过5个",
        "maxParameters": 5,
        "severity": "Warning"
      },
      {
        "name": "No-Deep-Inheritance",
        "description": "继承层级不能超过3层",
        "maxInheritanceDepth": 3,
        "severity": "Warning"
      }
    ]
  }
}
```

#### 架构合规检查实现
```csharp
// src/Shared/Architecture/ArchitectureComplianceChecker.cs
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace LYBT.Shared.Architecture
{
    public class ArchitectureComplianceChecker
    {
        private readonly ArchitectureRules _rules;
        private readonly List<ArchitectureViolation> _violations;

        public ArchitectureComplianceChecker()
        {
            _rules = LoadArchitectureRules();
            _violations = new List<ArchitectureViolation>();
        }

        public ArchitectureComplianceResult CheckCompliance(string solutionPath)
        {
            var projects = Directory.GetFiles(solutionPath, "*.csproj", SearchOption.AllDirectories);

            foreach (var project in projects)
            {
                CheckProjectCompliance(project);
            }

            return new ArchitectureComplianceResult
            {
                TotalViolations = _violations.Count,
                Violations = _violations,
                CriticalViolations = _violations.Count(v => v.Severity == ViolationSeverity.Error),
                WarningViolations = _violations.Count(v => v.Severity == ViolationSeverity.Warning),
                ComplianceScore = CalculateComplianceScore()
            };
        }

        private void CheckProjectCompliance(string projectPath)
        {
            var syntaxTrees = GetSyntaxTrees(projectPath);

            foreach (var syntaxTree in syntaxTrees)
            {
                CheckSyntaxTreeCompliance(syntaxTree, projectPath);
            }
        }

        private void CheckSyntaxTreeCompliance(SyntaxTree syntaxTree, string projectPath)
        {
            var root = syntaxTree.GetCompilationUnitRoot();

            // 检查命名规则
            CheckNamingRules(root, projectPath);

            // 检查依赖规则
            CheckDependencyRules(root, projectPath);

            // 检查设计规则
            CheckDesignRules(root, projectPath);
        }

        private void CheckNamingRules(CompilationUnitSyntax root, string projectPath)
        {
            var classes = root.DescendantNodes<ClassDeclarationSyntax>();

            foreach (var classDeclaration in classes)
            {
                foreach (var namingRule in _rules.NamingRules)
                {
                    if (!System.Text.RegularExpressions.Regex.IsMatch(classDeclaration.Identifier.Text, namingRule.Pattern))
                    {
                        _violations.Add(new ArchitectureViolation
                        {
                            Type = ViolationType.Naming,
                            Severity = namingRule.Severity,
                            Message = namingRule.Description,
                            Location = GetLocation(classDeclaration),
                            FilePath = projectPath
                        });
                    }
                }
            }
        }

        private void CheckDependencyRules(CompilationUnitSyntax root, string projectPath)
        {
            var usingDirectives = root.DescendantNodes<UsingDirectiveSyntax>();

            foreach (var usingDirective in usingDirectives)
            {
                foreach (var dependencyRule in _rules.DependencyRules)
                {
                    if (dependencyRule.ForbiddenDependencies.Contains(usingDirective.Name.ToString()))
                    {
                        _violations.Add(new ArchitectureViolation
                        {
                            Type = ViolationType.Dependency,
                            Severity = dependencyRule.Severity,
                            Message = dependencyRule.Description,
                            Location = GetLocation(usingDirective),
                            FilePath = projectPath
                        });
                    }
                }
            }
        }

        private void CheckDesignRules(CompilationUnitSyntax root, string projectPath)
        {
            var classes = root.DescendantNodes<ClassDeclarationSyntax>();

            foreach (var classDeclaration in classes)
            {
                // 检查类长度
                var classLines = classDeclaration.GetText().Split('\n').Length;
                var maxLinesRule = _rules.DesignRules.FirstOrDefault(r => r.Name == "No-God-Classes");
                if (maxLinesRule != null && classLines > maxLinesRule.MaxLines)
                {
                    _violations.Add(new ArchitectureViolation
                    {
                        Type = ViolationType.Design,
                        Severity = maxLinesRule.Severity,
                        Message = $"类 '{classDeclaration.Identifier.Text}' 超过最大行数限制 ({maxLinesRule.MaxLines} 行)",
                        Location = GetLocation(classDeclaration),
                        FilePath = projectPath
                    });
                }

                // 检查方法长度
                var methods = classDeclaration.DescendantNodes<MethodDeclarationSyntax>();
                foreach (var method in methods)
                {
                    var methodLines = method.GetText().Split('\n').Length;
                    var maxMethodLinesRule = _rules.DesignRules.FirstOrDefault(r => r.Name == "No-Long-Methods");
                    if (maxMethodLinesRule != null && methodLines > maxMethodLinesRule.MaxMethodLines)
                    {
                        _violations.Add(new ArchitectureViolation
                        {
                            Type = ViolationType.Design,
                            Severity = maxMethodLinesRule.Severity,
                            Message = $"方法 '{method.Identifier.Text}' 超过最大行数限制 ({maxMethodLinesRule.MaxMethodLines} 行)",
                            Location = GetLocation(method),
                            FilePath = projectPath
                        });
                    }
                }
            }
        }

        private double CalculateComplianceScore()
        {
            if (_violations.Count == 0) return 100.0;

            var criticalWeight = 10.0;
            var warningWeight = 5.0;

            var totalDeduction = (_violations.Count(v => v.Severity == ViolationSeverity.Error) * criticalWeight) +
                               (_violations.Count(v => v.Severity == ViolationSeverity.Warning) * warningWeight);

            return Math.Max(0, 100.0 - totalDeduction);
        }

        private Location GetLocation(SyntaxNode node)
        {
            return new Location
            {
                Line = node.GetLocation().GetLineSpan().StartLinePosition.Line + 1,
                Column = node.GetLocation().GetLineSpan().StartLinePosition.Character + 1
            };
        }
    }

    public class ArchitectureComplianceResult
    {
        public int TotalViolations { get; set; }
        public List<ArchitectureViolation> Violations { get; set; }
        public int CriticalViolations { get; set; }
        public int WarningViolations { get; set; }
        public double ComplianceScore { get; set; }
        public bool IsCompliant => CriticalViolations == 0 && ComplianceScore >= 80.0;
    }

    public class ArchitectureViolation
    {
        public ViolationType Type { get; set; }
        public ViolationSeverity Severity { get; set; }
        public string Message { get; set; }
        public Location Location { get; set; }
        public string FilePath { get; set; }
    }

    public enum ViolationType
    {
        Naming,
        Dependency,
        Design,
        Layering,
        Security
    }

    public enum ViolationSeverity
    {
        Warning,
        Error,
        Critical
    }
}
```

### 2. 自动化架构检查

#### MSBuild 集成
```xml
<!-- Directory.Build.targets -->
<Project>
  <Target Name="CheckArchitectureCompliance" Before="Build">
    <Message Text="检查架构合规性..." Importance="high" />

    <Exec Command="dotnet tool install --global LYBT.ArchitectureChecker" ContinueOnError="true" />
    <Exec Command="lybt-architecture-check --project $(MSBuildProjectDirectory) --output $(MSBuildProjectDirectory)/architecture-report.json" ContinueOnError="true" />

    <ReadLinesFromFile File="$(MSBuildProjectDirectory)/architecture-report.json">
      <Output TaskParameter="ArchitectureReport" ItemName="ArchitectureReportLines" />
    </ReadLinesFromFile>

    <PropertyGroup>
      <ArchitectureComplianceScore>$([System.Text.RegularExpressions.Regex]::Match([System.String]::Join(';', @(ArchitectureReportLines)), 'complianceScore\":(\d+\.?\d*)').Groups[1].Value)</ArchitectureComplianceScore>
    </PropertyGroup>

    <Error Text="架构合规检查失败：合规评分 $(ArchitectureComplianceScore)% 低于最低要求 80%"
           Condition="$(ArchitectureComplianceScore) < 80" />
  </Target>
</Project>
```

#### 持续集成检查
```yaml
# .github/workflows/architecture-compliance.yml
name: Architecture Compliance Check

on:
  push:
    branches: [ main, develop ]
  pull_request:
    branches: [ main ]

jobs:
  architecture-compliance:
    runs-on: ubuntu-latest

    steps:
    - name: Checkout code
      uses: actions/checkout@v3

    - name: Setup .NET
      uses: actions/setup-dotnet@v3
      with:
        dotnet-version: '8.0.x'

    - name: Install Architecture Checker
      run: |
        dotnet tool install --global LYBT.ArchitectureChecker

    - name: Run Architecture Compliance Check
      run: |
        lybt-architecture-check --solution . --output architecture-report.json

    - name: Parse Results
      id: parse-results
      run: |
        echo "compliance_score=$(jq -r '.complianceScore' architecture-report.json)" >> $GITHUB_OUTPUT

    - name: Check Compliance Score
      run: |
        if [ "${{ steps.parse-results.outputs.compliance_score }}" -lt 80 ]; then
          echo "架构合规评分 ${{ steps.parse-results.outputs.compliance_score }}% 低于最低要求 80%"
          exit 1
        fi

    - name: Upload Architecture Report
      uses: actions/upload-artifact@v3
      with:
        name: architecture-report
        path: architecture-report.json

    - name: Comment PR
      if: github.event_name == 'pull_request'
      uses: actions/github-script@v6
      with:
        script: |
          const complianceScore = `${{ steps.parse-results.outputs.compliance_score }}`;
          const comment = `
          ## 架构合规检查结果

          📊 合规评分: ${complianceScore}%
          ${complianceScore >= 80 ? '✅ 通过' : '❌ 未通过'}

          ${complianceScore < 80 ? '⚠️ 请修复架构违规问题后重新提交' : ''}
          `;
          github.rest.issues.createComment({
            issue_number: context.issue.number,
            owner: context.repo.owner,
            repo: context.repo.repo,
            body: comment
          });
```

## 🔒 安全检查自动化

### 1. 安全扫描工具集成

#### 安全漏洞扫描
```csharp
// src/Shared/Security/VulnerabilityScanner.cs
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace LYBT.Shared.Security
{
    public class VulnerabilityScanner
    {
        private readonly ILogger<VulnerabilityScanner> _logger;
        private readonly List<IVulnerabilityCheck> _checks;

        public VulnerabilityScanner(ILogger<VulnerabilityScanner> logger)
        {
            _logger = logger;
            _checks = new List<IVulnerabilityCheck>
            {
                new SqlInjectionCheck(),
                new XssCheck(),
                new AuthenticationCheck(),
                new AuthorizationCheck(),
                new DataValidationCheck(),
                new CryptographyCheck(),
                new ConfigurationSecurityCheck()
            };
        }

        public async Task<VulnerabilityScanResult> ScanAsync(string projectPath)
        {
            var result = new VulnerabilityScanResult
            {
                ScanTime = DateTime.UtcNow,
                ProjectPath = projectPath
            };

            _logger.LogInformation("开始安全漏洞扫描: {ProjectPath}", projectPath);

            var files = GetSourceFiles(projectPath);

            foreach (var file in files)
            {
                var fileResult = await ScanFileAsync(file);
                result.FileResults.Add(fileResult);
            }

            // 汇总结果
            result.TotalVulnerabilities = result.FileResults.Sum(r => r.Vulnerabilities.Count);
            result.CriticalVulnerabilities = result.FileResults.Sum(r => r.Vulnerabilities.Count(v => v.Severity == VulnerabilitySeverity.Critical));
            result.HighVulnerabilities = result.FileResults.Sum(r => r.Vulnerabilities.Count(v => v.Severity == VulnerabilitySeverity.High));
            result.MediumVulnerabilities = result.FileResults.Sum(r => r.Vulnerabilities.Count(v => v.Severity == VulnerabilitySeverity.Medium));
            result.LowVulnerabilities = result.FileResults.Sum(r => r.Vulnerabilities.Count(v => v.Severity == VulnerabilitySeverity.Low));

            _logger.LogInformation("安全扫描完成: 发现 {Total} 个漏洞 (严重: {Critical}, 高: {High}, 中: {Medium}, 低: {Low})",
                result.TotalVulnerabilities,
                result.CriticalVulnerabilities,
                result.HighVulnerabilities,
                result.MediumVulnerabilities,
                result.LowVulnerabilities);

            return result;
        }

        private async Task<FileVulnerabilityResult> ScanFileAsync(string filePath)
        {
            var result = new FileVulnerabilityResult
            {
                FilePath = filePath
            };

            try
            {
                var content = await File.ReadAllTextAsync(filePath);
                var syntaxTree = CSharpSyntaxTree.ParseText(content);

                foreach (var check in _checks)
                {
                    var checkResult = await check.CheckAsync(syntaxTree, filePath);
                    result.Vulnerabilities.AddRange(checkResult.Vulnerabilities);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "扫描文件时发生错误: {FilePath}", filePath);
                result.Error = ex.Message;
            }

            return result;
        }

        private List<string> GetSourceFiles(string projectPath)
        {
            return Directory.GetFiles(projectPath, "*.cs", SearchOption.AllDirectories)
                .Where(path => !IsExcludedFile(path))
                .ToList();
        }

        private bool IsExcludedFile(string path)
        {
            var excludedPaths = new[]
            {
                "/bin/",
                "/obj/",
                "/Migrations/",
                "/Generated/",
                ".Designer.cs",
                ".g.cs",
                ".Designer.cs"
            };

            return excludedPaths.Any(excluded => path.Contains(excluded));
        }
    }

    public interface IVulnerabilityCheck
    {
        Task<VulnerabilityCheckResult> CheckAsync(SyntaxTree syntaxTree, string filePath);
    }

    public class VulnerabilityCheckResult
    {
        public List<Vulnerability> Vulnerabilities { get; set; } = new();
    }

    public class Vulnerability
    {
        public string Title { get; set; }
        public string Description { get; set; }
        public VulnerabilitySeverity Severity { get; set; }
        public VulnerabilityCategory Category { get; set; }
        public string Recommendation { get; set; }
        public Location Location { get; set; }
        public string FilePath { get; set; }
    }

    public class SqlInjectionCheck : IVulnerabilityCheck
    {
        public async Task<VulnerabilityCheckResult> CheckAsync(SyntaxTree syntaxTree, string filePath)
        {
            var result = new VulnerabilityCheckResult();

            var stringLiterals = syntaxTree.GetRoot().DescendantNodes<LiteralExpressionSyntax>()
                .Where(le => le.Kind() == SyntaxKind.StringLiteralExpression);

            foreach (var literal in stringLiterals)
            {
                var value = literal.Token.ValueText;

                // 检查 SQL 注入风险模式
                if (ContainsSqlInjectionRisk(value))
                {
                    result.Vulnerabilities.Add(new Vulnerability
                    {
                        Title = "SQL 注入风险",
                        Description = "检测到可能的 SQL 注入漏洞",
                        Severity = VulnerabilitySeverity.Critical,
                        Category = VulnerabilityCategory.SqlInjection,
                        Recommendation = "使用参数化查询或 ORM 来防止 SQL 注入",
                        Location = new Location
                        {
                            Line = literal.GetLocation().GetLineSpan().StartLinePosition.Line + 1,
                            Column = literal.GetLocation().GetLineSpan().StartLinePosition.Character + 1
                        },
                        FilePath = filePath
                    });
                }
            }

            return result;
        }

        private bool ContainsSqlInjectionRisk(string value)
        {
            var sqlKeywords = new[]
            {
                "SELECT", "INSERT", "UPDATE", "DELETE", "DROP", "CREATE", "ALTER",
                "UNION", "EXEC", "EXECUTE", "sp_", "xp_", "fn_"
            };

            return sqlKeywords.Any(keyword =>
                value.Contains(keyword, StringComparison.OrdinalIgnoreCase) &&
                !value.Contains("@", StringComparison.OrdinalIgnoreCase));
        }
    }
}
```

#### 安全配置检查
```csharp
// src/Shared/Security/SecurityConfigurationChecker.cs
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace LYBT.Shared.Security
{
    public class SecurityConfigurationChecker
    {
        private readonly ILogger<SecurityConfigurationChecker> _logger;
        private readonly List<ISecurityConfigurationRule> _rules;

        public SecurityConfigurationChecker(ILogger<SecurityConfigurationChecker> logger)
        {
            _logger = logger;
            _rules = new List<ISecurityConfigurationRule>
            {
                new ConnectionStringSecurityRule(),
                new AuthenticationSecurityRule(),
                new CorsSecurityRule(),
                new HstsSecurityRule(),
                new DataProtectionSecurityRule(),
                new LoggingSecurityRule()
            };
        }

        public async Task<SecurityConfigurationResult> CheckConfigurationAsync(string configPath)
        {
            var result = new SecurityConfigurationResult
            {
                CheckTime = DateTime.UtcNow,
                ConfigurationPath = configPath
            };

            _logger.LogInformation("开始安全配置检查: {ConfigPath}", configPath);

            try
            {
                var configuration = new ConfigurationBuilder()
                    .SetBasePath(Path.GetDirectoryName(configPath))
                    .AddJsonFile(Path.GetFileName(configPath), optional: false)
                    .Build();

                foreach (var rule in _rules)
                {
                    var ruleResult = await rule.CheckAsync(configuration);
                    result.RuleResults.Add(ruleResult);
                }

                // 汇总结果
                result.TotalRules = _rules.Count;
                result.PassedRules = result.RuleResults.Count(r => r.Passed);
                result.FailedRules = result.RuleResults.Count(r => !r.Passed);
                result.SecurityScore = CalculateSecurityScore(result.RuleResults);

                _logger.LogInformation("安全配置检查完成: {Passed}/{Total} 个规则通过，安全评分: {Score}%",
                    result.PassedRules,
                    result.TotalRules,
                    result.SecurityScore);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "检查安全配置时发生错误");
                result.Error = ex.Message;
            }

            return result;
        }

        private double CalculateSecurityScore(List<RuleResult> ruleResults)
        {
            if (ruleResults.Count == 0) return 0.0;

            var passedRules = ruleResults.Count(r => r.Passed);
            return (double)passedRules / ruleResults.Count * 100.0;
        }
    }

    public interface ISecurityConfigurationRule
    {
        Task<RuleResult> CheckAsync(IConfiguration configuration);
    }

    public class RuleResult
    {
        public string RuleName { get; set; }
        public bool Passed { get; set; }
        public string Message { get; set; }
        public string Recommendation { get; set; }
        public SecurityLevel Level { get; set; }
    }

    public class ConnectionStringSecurityRule : ISecurityConfigurationRule
    {
        public async Task<RuleResult> CheckAsync(IConfiguration configuration)
        {
            var result = new RuleResult
            {
                RuleName = "连接字符串安全检查",
                Level = SecurityLevel.Critical
            };

            var connectionString = configuration.GetConnectionString("DefaultConnection");

            if (string.IsNullOrEmpty(connectionString))
            {
                result.Passed = false;
                result.Message = "未找到数据库连接字符串";
                result.Recommendation = "请配置数据库连接字符串";
                return result;
            }

            // 检查是否包含敏感信息
            if (connectionString.Contains("password", StringComparison.OrdinalIgnoreCase) ||
                connectionString.Contains("pwd", StringComparison.OrdinalIgnoreCase))
            {
                result.Passed = false;
                result.Message = "连接字符串中包含明文密码";
                result.Recommendation = "使用加密的连接字符串或环境变量存储敏感信息";
                return result;
            }

            // 检查是否使用加密
            if (!connectionString.Contains("Encrypt=", StringComparison.OrdinalIgnoreCase) ||
                !connectionString.Contains("TrustServerCertificate=", StringComparison.OrdinalIgnoreCase))
            {
                result.Passed = false;
                result.Message = "连接字符串未启用加密或信任服务器证书";
                result.Recommendation = "在连接字符串中添加 Encrypt=True 和 TrustServerCertificate=True";
                return result;
            }

            result.Passed = true;
            result.Message = "连接字符串安全配置正确";
            return result;
        }
    }

    public class SecurityConfigurationResult
    {
        public DateTime CheckTime { get; set; }
        public string ConfigurationPath { get; set; }
        public List<RuleResult> RuleResults { get; set; } = new();
        public int TotalRules { get; set; }
        public int PassedRules { get; set; }
        public int FailedRules { get; set; }
        public double SecurityScore { get; set; }
        public bool IsSecure => FailedRules == 0 && SecurityScore >= 90.0;
        public string Error { get; set; }
    }

    public enum SecurityLevel
    {
        Low,
        Medium,
        High,
        Critical
    }
}
```

### 2. 安全报告生成

#### 安全仪表板
```csharp
// src/Shared/Security/SecurityDashboard.cs
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LYBT.Shared.Security
{
    public class SecurityDashboard
    {
        private readonly IVulnerabilityScanner _vulnerabilityScanner;
        private readonly ISecurityConfigurationChecker _configChecker;
        private readonly ISecurityMetricsService _metricsService;

        public SecurityDashboard(
            IVulnerabilityScanner vulnerabilityScanner,
            ISecurityConfigurationChecker configChecker,
            ISecurityMetricsService metricsService)
        {
            _vulnerabilityScanner = vulnerabilityScanner;
            _configChecker = configChecker;
            _metricsService = metricsService;
        }

        public async Task<SecurityDashboardData> GenerateDashboardAsync(string projectPath, string configPath)
        {
            var dashboard = new SecurityDashboardData
            {
                GeneratedAt = DateTime.UtcNow,
                ProjectPath = projectPath
            };

            // 扫描漏洞
            dashboard.VulnerabilityScanResult = await _vulnerabilityScanner.ScanAsync(projectPath);

            // 检查配置
            dashboard.ConfigurationCheckResult = await _configChecker.CheckConfigurationAsync(configPath);

            // 获取安全指标
            dashboard.SecurityMetrics = await _metricsService.GetSecurityMetricsAsync(
                DateTime.UtcNow.AddDays(-30),
                DateTime.UtcNow);

            // 计算整体安全评分
            dashboard.OverallSecurityScore = CalculateOverallSecurityScore(dashboard);

            // 生成建议
            dashboard.Recommendations = GenerateRecommendations(dashboard);

            return dashboard;
        }

        private double CalculateOverallSecurityScore(SecurityDashboardData dashboard)
        {
            var vulnerabilityScore = CalculateVulnerabilityScore(dashboard.VulnerabilityScanResult);
            var configurationScore = dashboard.ConfigurationCheckResult.SecurityScore;
            var metricsScore = dashboard.SecurityMetrics.OverallScore;

            // 权重分配：漏洞扫描 40%，配置检查 30%，安全指标 30%
            return (vulnerabilityScore * 0.4) + (configurationScore * 0.3) + (metricsScore * 0.3);
        }

        private double CalculateVulnerabilityScore(VulnerabilityScanResult scanResult)
        {
            if (scanResult.TotalVulnerabilities == 0) return 100.0;

            var criticalWeight = 25.0;
            var highWeight = 15.0;
            var mediumWeight = 5.0;
            var lowWeight = 1.0;

            var deduction = (scanResult.CriticalVulnerabilities * criticalWeight) +
                           (scanResult.HighVulnerabilities * highWeight) +
                           (scanResult.MediumVulnerabilities * mediumWeight) +
                           (scanResult.LowVulnerabilities * lowWeight);

            return Math.Max(0, 100.0 - deduction);
        }

        private List<SecurityRecommendation> GenerateRecommendations(SecurityDashboardData dashboard)
        {
            var recommendations = new List<SecurityRecommendation>();

            // 漏洞相关建议
            if (dashboard.VulnerabilityScanResult.CriticalVulnerabilities > 0)
            {
                recommendations.Add(new SecurityRecommendation
                {
                    Type = RecommendationType.Critical,
                    Title = "发现严重安全漏洞",
                    Description = $"发现 {dashboard.VulnerabilityScanResult.CriticalVulnerabilities} 个严重安全漏洞，需要立即修复",
                    Priority = Priority.High,
                    DueDate = DateTime.UtcNow.AddDays(1)
                });
            }

            // 配置相关建议
            if (!dashboard.ConfigurationCheckResult.IsSecure)
            {
                recommendations.Add(new SecurityRecommendation
                {
                    Type = RecommendationType.Configuration,
                    Title = "安全配置需要改进",
                    Description = $"安全配置评分为 {dashboard.ConfigurationCheckResult.SecurityScore:F1}，低于安全标准",
                    Priority = Priority.Medium,
                    DueDate = DateTime.UtcNow.AddDays(7)
                });
            }

            // 指标相关建议
            if (dashboard.SecurityMetrics.SecurityIncidents > 0)
            {
                recommendations.Add(new SecurityRecommendation
                {
                    Type = RecommendationType.Monitoring,
                    Title = "安全事件需要关注",
                    Description = $"过去30天内发生了 {dashboard.SecurityMetrics.SecurityIncidents} 起安全事件",
                    Priority = Priority.High,
                    DueDate = DateTime.UtcNow.AddDays(3)
                });
            }

            return recommendations.OrderByDescending(r => r.Priority).ToList();
        }
    }

    public class SecurityDashboardData
    {
        public DateTime GeneratedAt { get; set; }
        public string ProjectPath { get; set; }
        public VulnerabilityScanResult VulnerabilityScanResult { get; set; }
        public SecurityConfigurationResult ConfigurationCheckResult { get; set; }
        public SecurityMetrics SecurityMetrics { get; set; }
        public double OverallSecurityScore { get; set; }
        public List<SecurityRecommendation> Recommendations { get; set; }
    }

    public class SecurityRecommendation
    {
        public RecommendationType Type { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public Priority Priority { get; set; }
        public DateTime DueDate { get; set; }
        public bool IsResolved { get; set; }
    }

    public enum RecommendationType
    {
        Critical,
        Configuration,
        Monitoring,
        Training
    }

    public enum Priority
    {
        Low,
        Medium,
        High,
        Critical
    }
}
```

## 📊 质量指标与报告

### 1. 质量指标收集

#### 质量指标服务
```csharp
// src/Shared/Quality/QualityMetricsService.cs
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace LYBT.Shared.Quality
{
    public class QualityMetricsService
    {
        private readonly ILogger<QualityMetricsService> _logger;
        private readonly ITestResultRepository _testResultRepository;
        private readonly ICodeAnalysisRepository _codeAnalysisRepository;

        public QualityMetricsService(
            ILogger<QualityMetricsService> logger,
            ITestResultRepository testResultRepository,
            ICodeAnalysisRepository codeAnalysisRepository)
        {
            _logger = logger;
            _testResultRepository = testResultRepository;
            _codeAnalysisRepository = codeAnalysisRepository;
        }

        public async Task<QualityMetrics> GetQualityMetricsAsync(
            DateTime startDate,
            DateTime endDate)
        {
            var metrics = new QualityMetrics
            {
                PeriodStart = startDate,
                PeriodEnd = endDate,
                GeneratedAt = DateTime.UtcNow
            };

            // 获取测试指标
            metrics.TestMetrics = await GetTestMetricsAsync(startDate, endDate);

            // 获取代码分析指标
            metrics.CodeAnalysisMetrics = await GetCodeAnalysisMetricsAsync(startDate, endDate);

            // 获取安全指标
            metrics.SecurityMetrics = await GetSecurityMetricsAsync(startDate, endDate);

            // 获取性能指标
            metrics.PerformanceMetrics = await GetPerformanceMetricsAsync(startDate, endDate);

            // 计算综合质量评分
            metrics.OverallQualityScore = CalculateOverallQualityScore(metrics);

            return metrics;
        }

        private async Task<TestMetrics> GetTestMetricsAsync(DateTime startDate, DateTime endDate)
        {
            var testResults = await _testResultRepository.GetTestResultsAsync(startDate, endDate);

            return new TestMetrics
            {
                TotalTests = testResults.Count,
                PassedTests = testResults.Count(t => t.Status == TestStatus.Passed),
                FailedTests = testResults.Count(t => t.Status == TestStatus.Failed),
                SkippedTests = testResults.Count(t => t.Status == TestStatus.Skipped),
                TotalTestTime = testResults.Sum(t => t.Duration),
                AverageTestTime = testResults.Any() ? testResults.Average(t => t.Duration) : TimeSpan.Zero,
                TestCoverage = await CalculateTestCoverageAsync(startDate, endDate),
                UnitTestCount = testResults.Count(t => t.TestType == TestType.Unit),
                IntegrationTestCount = testResults.Count(t => t.TestType == TestType.Integration),
                E2ETestCount = testResults.Count(t => t.TestType == TestType.E2E)
            };
        }

        private async Task<CodeAnalysisMetrics> GetCodeAnalysisMetricsAsync(DateTime startDate, DateTime endDate)
        {
            var analysisResults = await _codeAnalysisRepository.GetAnalysisResultsAsync(startDate, endDate);

            return new CodeAnalysisMetrics
            {
                TotalFilesAnalyzed = analysisResults.Count,
                FilesWithIssues = analysisResults.Count(r => r.Issues.Any()),
                TotalIssues = analysisResults.Sum(r => r.Issues.Count),
                CodeQualityScore = analysisResults.Any() ? analysisResults.Average(r => r.QualityScore) : 0.0,
                TechnicalDebt = analysisResults.Sum(r => r.TechnicalDebt),
                CodeComplexity = analysisResults.Average(r => r.Complexity),
                DuplicatedCode = analysisResults.Sum(r => r.DuplicatedLines),
                MaintainabilityIndex = analysisResults.Any() ? analysisResults.Average(r => r.MaintainabilityIndex) : 0.0
            };
        }

        private async Task<double> CalculateTestCoverageAsync(DateTime startDate, DateTime endDate)
        {
            // 这里应该从测试覆盖率报告中获取实际数据
            // 为了示例，我们返回一个模拟值
            return 85.5;
        }

        private double CalculateOverallQualityScore(QualityMetrics metrics)
        {
            // 权重分配：测试 40%，代码质量 30%，安全 20%，性能 10%
            var testScore = metrics.TestMetrics != null
                ? CalculateTestScore(metrics.TestMetrics)
                : 0.0;
            var codeScore = metrics.CodeAnalysisMetrics != null
                ? metrics.CodeAnalysisMetrics.CodeQualityScore
                : 0.0;
            var securityScore = metrics.SecurityMetrics != null
                ? metrics.SecurityMetrics.OverallScore
                : 0.0;
            var performanceScore = metrics.PerformanceMetrics != null
                ? metrics.PerformanceMetrics.OverallScore
                : 0.0;

            return (testScore * 0.4) + (codeScore * 0.3) + (securityScore * 0.2) + (performanceScore * 0.1);
        }

        private double CalculateTestScore(TestMetrics testMetrics)
        {
            if (testMetrics.TotalTests == 0) return 0.0;

            var passRate = (double)testMetrics.PassedTests / testMetrics.TotalTests * 100;
            var coverageScore = testMetrics.TestCoverage;

            return (passRate * 0.6) + (coverageScore * 0.4);
        }
    }

    public class QualityMetrics
    {
        public DateTime PeriodStart { get; set; }
        public DateTime PeriodEnd { get; set; }
        public DateTime GeneratedAt { get; set; }
        public TestMetrics TestMetrics { get; set; }
        public CodeAnalysisMetrics CodeAnalysisMetrics { get; set; }
        public SecurityMetrics SecurityMetrics { get; set; }
        public PerformanceMetrics PerformanceMetrics { get; set; }
        public double OverallQualityScore { get; set; }
    }

    public class TestMetrics
    {
        public int TotalTests { get; set; }
        public int PassedTests { get; set; }
        public int FailedTests { get; set; }
        public int SkippedTests { get; set; }
        public TimeSpan TotalTestTime { get; set; }
        public TimeSpan AverageTestTime { get; set; }
        public double TestCoverage { get; set; }
        public int UnitTestCount { get; set; }
        public int IntegrationTestCount { get; set; }
        public int E2ETestCount { get; set; }
    }

    public class CodeAnalysisMetrics
    {
        public int TotalFilesAnalyzed { get; set; }
        public int FilesWithIssues { get; set; }
        public int TotalIssues { get; set; }
        public double CodeQualityScore { get; set; }
        public TimeSpan TechnicalDebt { get; set; }
        public double CodeComplexity { get; set; }
        public int DuplicatedCode { get; set; }
        public double MaintainabilityIndex { get; set; }
    }

    public class SecurityMetrics
    {
        public int SecurityIncidents { get; set; }
        public int VulnerabilitiesFound { get; set; }
        public int VulnerabilitiesFixed { get; set; }
        public int SecurityViolations { get; set; }
        public double OverallScore { get; set; }
    }

    public class PerformanceMetrics
    {
        public double AverageResponseTime { get; set; }
        public double MaxResponseTime { get; set; }
        public double Throughput { get; set; }
        public double ErrorRate { get; set; }
        public double ResourceUtilization { get; set; }
        public double OverallScore { get; set; }
    }
}
```

### 2. 质量报告生成

#### 质量报告服务
```csharp
// src/Shared/Quality/QualityReportService.cs
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace LYBT.Shared.Quality
{
    public class QualityReportService
    {
        private readonly ILogger<QualityReportService> _logger;
        private readonly QualityMetricsService _metricsService;

        public QualityReportService(
            ILogger<QualityReportService> logger,
            QualityMetricsService metricsService)
        {
            _logger = logger;
            _metricsService = metricsService;
        }

        public async Task<QualityReport> GenerateReportAsync(
            string projectPath,
            DateTime startDate,
            DateTime endDate,
            ReportFormat format = ReportFormat.Html)
        {
            var metrics = await _metricsService.GetQualityMetricsAsync(startDate, endDate);

            var report = new QualityReport
            {
                ReportId = Guid.NewGuid(),
                ProjectPath = projectPath,
                ReportPeriod = new DateRange { Start = startDate, End = endDate },
                GeneratedAt = DateTime.UtcNow,
                Format = format,
                Metrics = metrics
            };

            // 根据格式生成报告
            switch (format)
            {
                case ReportFormat.Html:
                    report.HtmlContent = await GenerateHtmlReportAsync(report);
                    break;
                case ReportFormat.Json:
                    report.JsonContent = await GenerateJsonReportAsync(report);
                    break;
                case ReportFormat.Pdf:
                    report.PdfContent = await GeneratePdfReportAsync(report);
                    break;
                case ReportFormat.Xml:
                    report.XmlContent = await GenerateXmlReportAsync(report);
                    break;
            }

            return report;
        }

        private async Task<string> GenerateHtmlReportAsync(QualityReport report)
        {
            var template = await LoadHtmlTemplateAsync();

            var html = template
                .Replace("{{REPORT_ID}}", report.ReportId.ToString())
                .Replace("{{PROJECT_PATH}}", report.ProjectPath)
                .Replace("{{GENERATED_AT}}", report.GeneratedAt.ToString("yyyy-MM-dd HH:mm:ss"))
                .Replace("{{PERIOD_START}}", report.ReportPeriod.Start.ToString("yyyy-MM-dd"))
                .Replace("{{PERIOD_END}}", report.ReportPeriod.End.ToString("yyyy-MM-dd"))
                .Replace("{{OVERALL_SCORE}}", report.Metrics.OverallQualityScore.ToString("F1"))
                .Replace("{{TEST_SCORE}}", CalculateTestScore(report.Metrics.TestMetrics).ToString("F1"))
                .Replace("{{CODE_SCORE}}", report.Metrics.CodeAnalysisMetrics?.CodeQualityScore.ToString("F1") ?? "N/A")
                .Replace("{{SECURITY_SCORE}}", report.Metrics.SecurityMetrics?.OverallScore.ToString("F1") ?? "N/A")
                .Replace("{{PERFORMANCE_SCORE}}", report.Metrics.PerformanceMetrics?.OverallScore.ToString("F1") ?? "N/A")
                .Replace("{{TEST_METRICS_TABLE}}", GenerateTestMetricsTable(report.Metrics.TestMetrics))
                .Replace("{{CODE_ANALYSIS_TABLE}}", GenerateCodeAnalysisTable(report.Metrics.CodeAnalysisMetrics))
                .Replace("{{SECURITY_METRICS_TABLE}}", GenerateSecurityMetricsTable(report.Metrics.SecurityMetrics))
                .Replace("{{PERFORMANCE_METRICS_TABLE}}", GeneratePerformanceMetricsTable(report.Metrics.PerformanceMetrics));

            return html;
        }

        private string GenerateTestMetricsTable(TestMetrics metrics)
        {
            if (metrics == null) return "<p>暂无测试数据</p>";

            return $@"
                <table class='metrics-table'>
                    <thead>
                        <tr>
                            <th>指标</th>
                            <th>数值</th>
                            <th>状态</th>
                        </tr>
                    </thead>
                    <tbody>
                        <tr>
                            <td>总测试数</td>
                            <td>{metrics.TotalTests}</td>
                            <td class='{GetStatusClass((double)metrics.PassedTests / metrics.TotalTests * 100)}'>{GetStatusText((double)metrics.PassedTests / metrics.TotalTests * 100)}</td>
                        </tr>
                        <tr>
                            <td>通过率</td>
                            <td>{((double)metrics.PassedTests / metrics.TotalTests * 100):F1}%</td>
                            <td class='{GetStatusClass((double)metrics.PassedTests / metrics.TotalTests * 100)}'>{GetStatusText((double)metrics.PassedTests / metrics.TotalTests * 100)}</td>
                        </tr>
                        <tr>
                            <td>测试覆盖率</td>
                            <td>{metrics.TestCoverage:F1}%</td>
                            <td class='{GetStatusClass(metrics.TestCoverage)}'>{GetStatusText(metrics.TestCoverage)}</td>
                        </tr>
                        <tr>
                            <td>平均执行时间</td>
                            <td>{metrics.AverageTestTime.TotalMilliseconds:F0}ms</td>
                            <td class='good'>正常</td>
                        </tr>
                    </tbody>
                </table>";
        }

        private string GetStatusClass(double score)
        {
            return score switch
            {
                >= 90 => "excellent",
                >= 80 => "good",
                >= 70 => "fair",
                >= 60 => "poor",
                _ => "critical"
            };
        }

        private string GetStatusText(double score)
        {
            return score switch
            {
                >= 90 => "优秀",
                >= 80 => "良好",
                >= 70 => "一般",
                >= 60 => "较差",
                _ => "严重"
            };
        }

        private async Task<string> LoadHtmlTemplateAsync()
        {
            // 从模板文件加载 HTML 模板
            var templatePath = Path.Combine("templates", "quality-report.html");
            return await File.ReadAllTextAsync(templatePath);
        }
    }

    public class QualityReport
    {
        public Guid ReportId { get; set; }
        public string ProjectPath { get; set; }
        public DateRange ReportPeriod { get; set; }
        public DateTime GeneratedAt { get; set; }
        public ReportFormat Format { get; set; }
        public QualityMetrics Metrics { get; set; }
        public string HtmlContent { get; set; }
        public string JsonContent { get; set; }
        public byte[] PdfContent { get; set; }
        public string XmlContent { get; set; }
    }

    public class DateRange
    {
        public DateTime Start { get; set; }
        public DateTime End { get; set; }
    }

    public enum ReportFormat
    {
        Html,
        Json,
        Pdf,
        Xml
    }
}
```

## 🔄 持续集成集成

### 1. 质量门配置

#### Azure DevOps 质量门
```yaml
# azure-pipelines.yml
trigger:
- main
- develop

stages:
- build
- quality-check
- test
- deploy

variables:
  solution: '**/*.sln'
  buildPlatform: 'Any CPU'
  architecture: x64

pool:
  vmImage: 'windows-latest'

stages:
- stage: Build
  displayName: '构建项目'
  jobs:
  - job: Build
    displayName: '构建解决方案'
    steps:
    - task: NuGetToolInstaller@1
      displayName: '安装 NuGet'
    - task: NuGetCommand@2
      displayName: '恢复依赖包'
      inputs:
        restoreSolution: '$(solution)'
    - task: VSBuild@1
      displayName: '构建项目'
      inputs:
        solution: '$(solution)'
        platform: '$(buildPlatform)'
        configuration: 'Release'
    - task: PublishBuildArtifacts@1
      displayName: '发布构建产物'
      inputs:
        PathtoPublish: '$(Build.ArtifactStagingDirectory)'

- stage: Quality_Check
  displayName: '质量检查'
  dependsOn: Build
  jobs:
  - job: Code_Quality
    displayName: '代码质量检查'
    steps:
    - task: DownloadBuildArtifacts@0
      displayName: '下载构建产物'
      inputs:
        buildType: 'current'
        downloadType: 'single'
        artifactName: 'drop'
        downloadPath: '$(System.ArtifactsDirectory)'
    - task: SonarQubePrepare@5
      displayName: '准备 SonarQube 分析'
      inputs:
        SonarQube: 'sonarqube'
        scannerMode: 'MSBuild'
        projectKey: 'lybt-medical-system'
        projectName: 'LYBT Medical System'
    - task: SonarQubeAnalyze@5
      displayName: '运行 SonarQube 分析'
      inputs:
        projectKey: 'lybt-medical-system'
    - task: SonarQubePublish@5
      displayName: '发布 SonarQube 结果'
      inputs:
        pollingTimeoutSec: '300'
    - task: SonarQubeBreak@5
      displayName: '质量门检查'
      inputs:
        condition: 'quality_gate'
        pollTimeoutSec: '300'

  - job: Security_Scan
    displayName: '安全扫描'
    steps:
    - task: DownloadBuildArtifacts@0
      displayName: '下载构建产物'
      inputs:
        buildType: 'current'
        downloadType: 'single'
        artifactName: 'drop'
        downloadPath: '$(System.ArtifactsDirectory)'
    - task: CredScan@3
      displayName: '凭证扫描'
      inputs:
        scanPath: '$(System.ArtifactsDirectory)'
        tool: 'CredentialScanner'
    - task: SASTscan@3
      displayName: '静态代码分析'
      inputs:
        CredScan: true
        tool: 'VSTest'
        ruleSet: 'SDL Basic'
        warningsAsErrors: true

  - job: Architecture_Check
    displayName: '架构合规检查'
    steps:
    - task: DownloadBuildArtifacts@0
      displayName: '下载构建产物'
      inputs:
        buildType: 'current'
        downloadType: 'single'
        artifactName: 'drop'
        downloadPath: '$(System.ArtifactsDirectory)'
    - task: PowerShell@2
      displayName: '运行架构合规检查'
      inputs:
        targetType: 'inline'
        script: |
          dotnet tool install --global LYBT.ArchitectureChecker
          lybt-architecture-check --solution $(System.ArtifactsDirectory) --output $(System.ArtifactsDirectory)/architecture-report.json
    - task: PublishBuildArtifacts@1
      displayName: '发布架构报告'
      inputs:
        PathtoPublish: '$(System.ArtifactsDirectory)'
        ArtifactName: 'architecture-report'
```

### 2. 自动化报告部署

#### 报告部署服务
```csharp
// src/Shared/Quality/ReportDeploymentService.cs
using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace LYBT.Shared.Quality
{
    public class ReportDeploymentService
    {
        private readonly ILogger<ReportDeploymentService> _logger;
        private readonly IReportStorageService _storageService;

        public ReportDeploymentService(
            ILogger<ReportDeploymentService> logger,
            IReportStorageService storageService)
        {
            _logger = logger;
            _storageService = storageService;
        }

        public async Task<DeploymentResult> DeployReportAsync(
            QualityReport report,
            List<ReportDeploymentTarget> targets)
        {
            var result = new DeploymentResult
            {
                ReportId = report.ReportId,
                DeployedAt = DateTime.UtcNow,
                Targets = new List<ReportDeploymentTarget>()
            };

            foreach (var target in targets)
            {
                try
                {
                    var deploymentResult = await DeployToTargetAsync(report, target);
                    result.Targets.Add(deploymentResult);

                    _logger.LogInformation("报告已成功部署到 {TargetType}: {TargetLocation}",
                        target.Type,
                        target.Location);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "部署报告到 {TargetType} 失败: {TargetLocation}",
                        target.Type,
                        target.Location);

                    result.Targets.Add(new ReportDeploymentTarget
                    {
                        Type = target.Type,
                        Location = target.Location,
                        Success = false,
                        Error = ex.Message
                    });
                }
            }

            // 发送部署通知
            await SendDeploymentNotificationsAsync(result);

            return result;
        }

        private async Task<ReportDeploymentTarget> DeployToTargetAsync(
            QualityReport report,
            ReportDeploymentTarget target)
        {
            switch (target.Type)
            {
                case ReportDeploymentType.FileSystem:
                    return await DeployToFileSystemAsync(report, target);
                case ReportDeploymentType.WebServer:
                    return await DeployToWebServerAsync(report, target);
                case ReportDeploymentType.SharePoint:
                    return await DeployToSharePointAsync(report, target);
                case ReportDeploymentType.Email:
                    return await DeployViaEmailAsync(report, target);
                case ReportDeploymentType.S3:
                    return await DeployToS3Async(report, target);
                default:
                    throw new ArgumentException($"不支持的部署目标类型: {target.Type}");
            }
        }

        private async Task<ReportDeploymentTarget> DeployToFileSystemAsync(
            QualityReport report,
            ReportDeploymentTarget target)
        {
            var fileName = GetReportFileName(report);
            var filePath = Path.Combine(target.Location, fileName);

            // 确保目录存在
            Directory.CreateDirectory(Path.GetDirectoryName(filePath));

            // 写入报告文件
            await File.WriteAllTextAsync(filePath, report.HtmlContent);

            return new ReportDeploymentTarget
            {
                Type = target.Type,
                Location = filePath,
                Success = true,
                Url = filePath
            };
        }

        private string GetReportFileName(QualityReport report)
        {
            var timestamp = report.GeneratedAt.ToString("yyyyMMdd_HHmmss");
            return $"quality-report-{timestamp}.{GetFileExtension(report.Format)}";
        }

        private string GetFileExtension(ReportFormat format)
        {
            return format switch
            {
                ReportFormat.Html => "html",
                ReportFormat.Json => "json",
                ReportFormat.Pdf => "pdf",
                ReportFormat.Xml => "xml",
                _ => "html"
            };
        }
    }

    public class ReportDeploymentResult
    {
        public Guid ReportId { get; set; }
        public DateTime DeployedAt { get; set; }
        public List<ReportDeploymentTarget> Targets { get; set; } = new();
    }

    public class ReportDeploymentTarget
    {
        public ReportDeploymentType Type { get; set; }
        public string Location { get; set; }
        public bool Success { get; set; }
        public string Url { get; set; }
        public string Error { get; set; }
    }

    public enum ReportDeploymentType
    {
        FileSystem,
        WebServer,
        SharePoint,
        Email,
        S3,
        AzureBlob
    }

    public interface IReportStorageService
    {
        Task<bool> StoreAsync(string path, byte[] content);
        Task<byte[]> RetrieveAsync(string path);
        Task<bool> ExistsAsync(string path);
    }
}
```

## 📚 检查清单与最佳实践

### 1. 质量检查清单

#### 日常质量检查清单
```markdown
# 自动化质量检查日常清单

## 代码质量检查
- [ ] 每次提交前运行代码格式检查
- [ ] 每日自动运行静态代码分析
- [ ] 每周生成代码质量报告
- [ ] 代码覆盖率保持 ≥ 80%
- [ ] 技术债务控制在可接受范围内
- [ ] 复杂度指标符合标准

## 安全检查
- [ ] 每日自动扫描安全漏洞
- [ ] 依赖项安全检查
- [ ] 配置安全审查
- [ ] 权限审计日志检查
- [ ] 安全事件监控
- [ ] 定期渗透测试

## 架构合规检查
- [ ] 架构规则检查通过
- [ ] 依赖关系图更新
- [ ] 模块接口文档同步
- [ ] 设计模式使用规范
- [ ] 代码分层正确
- [ ] 无循环依赖

## 测试质量检查
- [ ] 单元测试覆盖率 ≥ 80%
- [ ] 集成测试覆盖核心功能
- [ ] API 测试覆盖所有端点
- [ ] 性能测试基线建立
- [ ] 测试环境与生产环境一致性
- [ ] 测试数据清理机制

## 性能检查
- [ ] 响应时间符合 SLA
- [ ] 系统吞吐量达标
- [ ] 资源利用率正常
- [ ] 数据库性能优化
- [ ] 缓存命中率达标
- [ ] 内存泄漏检查
```

### 2. 最佳实践指南

#### 质量检查最佳实践
```csharp
public class QualityCheckBestPractices
{
    // 1. 渐进式质量门
    public class ProgressiveQualityGates
    {
        /// <summary>
        /// 实施渐进式质量门的最佳实践
        /// </summary>
        public static async Task<bool> ImplementProgressiveGatesAsync()
        {
            // 1. 开发阶段质量门
            await ImplementDevelopmentGatesAsync();

            // 2. 集成阶段质量门
            await ImplementIntegrationGatesAsync();

            // 3. 发布阶段质量门
            await ImplementReleaseGatesAsync();

            // 4. 生产阶段质量门
            await ImplementProductionGatesAsync();

            return true;
        }
    }

    // 2. 质量指标趋势分析
    public class QualityTrendAnalysis
    {
        /// <summary>
        /// 分析质量指标趋势的最佳实践
        /// </summary>
        public static async Task<QualityTrendReport> AnalyzeQualityTrendsAsync(
            DateTime startDate,
            DateTime endDate)
        {
            var report = new QualityTrendReport
            {
                Period = new DateRange { Start = startDate, End = endDate }
            };

            // 收集历史数据
            var historicalData = await CollectHistoricalQualityDataAsync(startDate, endDate);

            // 分析趋势
            report.TrendAnalysis = await AnalyzeTrendsAsync(historicalData);

            // 预测未来趋势
            report.Predictions = await PredictFutureTrendsAsync(historicalData);

            // 生成建议
            report.Recommendations = GenerateTrendRecommendations(report);

            return report;
        }
    }

    // 3. 质量改进措施
    public class QualityImprovementMeasures
    {
        /// <summary>
        /// 实施质量改进措施的最佳实践
        /// </summary>
        public static async Task<bool> ImplementQualityImprovementsAsync()
        {
            // 1. 识别质量瓶颈
            var bottlenecks = await IdentifyQualityBottlenecksAsync();

            // 2. 制定改进计划
            var improvementPlan = CreateImprovementPlan(bottlenecks);

            // 3. 实施改进措施
            await ImplementImprovementPlanAsync(improvementPlan);

            // 4. 监控改进效果
            await MonitorImprovementEffectivenessAsync();

            return true;
        }
    }
}
```

## 🔄 版本历史

| 版本 | 日期 | 更新内容 | 作者 |
|------|------|----------|------|
| 1.0 | 2025-10-15 | 初始版本 | 项目质量团队 |

## 📞 联系方式

- **维护者**: 项目质量团队
- **质量负责人**: quality@lybt.com
- **技术支持**: devops@lybt.com
- **反馈渠道**: GitHub Issues 或内部反馈系统

---

*本文档遵循项目质量标准编写，如有疑问请参考相关文档或联系质量团队。*
# Tasks: coverage-driven-cleanup

## Phase 1: 配置覆盖率收集

### 1.1 更新测试项目引用
- [ ] 为Server模块测试项目添加coverlet.collector引用
  - LYBT.Module.Auth.Tests
  - LYBT.Module.Users.Tests
  - LYBT.Module.Patients.Tests
  - LYBT.Module.Herbs.Tests
  - LYBT.Module.Formula.Tests
  - LYBT.Module.Prescriptions.Tests
  - LYBT.Module.MedicalCase.Tests
  - LYBT.Module.Consultation.Tests
- [ ] 为Desktop模块测试项目添加coverlet.collector引用
  - LYBT.Desktop.Auth.Tests
  - LYBT.Desktop.Users.Tests
  - LYBT.Desktop.Patients.Tests
  - LYBT.Desktop.Foundation.Tests
  - LYBT.Desktop.Infrastructure.Tests
  - LYBT.Desktop.Models.Tests
  - LYBT.Desktop.Prescriptions.Tests
  - LYBT.Desktop.MedicalCase.Tests
  - LYBT.Desktop.Consultation.Tests
  - LYBT.Desktop.Shell.Tests
  - LYBT.Desktop.Herbs.Tests
  - LYBT.Desktop.Formula.Tests
- [ ] 为Shared层测试项目添加coverlet.collector引用
  - LYBT.Shared.Models.Tests
  - LYBT.Shared.Validators.Tests
  - LYBT.Shared.Utilities.Tests
  - LYBT.Shared.ExceptionHandling.Tests
- [ ] 为Core层测试项目添加coverlet.collector引用
  - LYBT.WebAPI.Tests
  - LYBT.Infrastructure.Tests
  - LYBT.Entities.Tests
- [ ] 为集成测试项目添加coverlet.collector引用
  - LYBT.Desktop.Foundation.IntegrationTests
  - WebAPI.IntegrationTests
  - LYBT.Module.Formula.IntegrationTests

### 1.2 优化runsettings配置
- [ ] 更新排除规则添加Migrations、Generated等
- [ ] 配置Include规则仅覆盖LYBT.*程序集
- [ ] 设置输出目录为BIN/TestResults/Coverage

### 1.3 创建覆盖率收集脚本
- [ ] 创建scripts/collect-coverage.ps1脚本
- [ ] 脚本支持增量运行（仅特定项目）
- [ ] 脚本支持HTML报告生成

## Phase 2: 运行覆盖率分析

### 2.1 执行测试收集覆盖率
- [ ] 运行Server模块单元测试
- [ ] 运行Desktop模块单元测试
- [ ] 运行Shared层单元测试
- [ ] 运行集成测试

### 2.2 合并覆盖率报告
- [ ] 安装ReportGenerator工具（dotnet tool）
- [ ] 合并所有coverage.cobertura.xml
- [ ] 生成统一的Cobertura报告

### 2.3 生成HTML报告
- [ ] 使用ReportGenerator生成HTML报告
- [ ] 报告输出到BIN/TestResults/CoverageReport
- [ ] 验证报告可正常查看

## Phase 3: 识别零覆盖代码

### 3.1 解析覆盖率报告
- [ ] 创建Python/PowerShell脚本解析Cobertura XML
- [ ] 提取所有line-rate="0"的类
- [ ] 按模块分组输出

### 3.2 分类整理候选列表
- [ ] Server模块零覆盖类列表
- [ ] Desktop模块零覆盖类列表
- [ ] Shared模块零覆盖类列表
- [ ] 标注预期零覆盖（接口、DTO、配置类）

### 3.3 输出分析报告
- [ ] 创建docs/analysis/coverage-zero-classes.md
- [ ] 记录每个候选类的文件路径和行数
- [ ] 标注清理优先级（高/中/低）

## Phase 4: 验证并清理

### 4.1 引用分析验证
- [ ] 对Server模块候选类执行grep引用分析
- [ ] 对Desktop模块候选类执行grep引用分析
- [ ] 排除有外部引用的类

### 4.2 执行清理（Server模块）
- [ ] 清理Module.Auth模块死代码
- [ ] 清理Module.Users模块死代码
- [ ] 清理Module.Patients模块死代码
- [ ] 清理其他Server模块死代码
- [ ] 更新DI注册
- [ ] 验证Server构建通过

### 4.3 执行清理（Desktop模块）
- [ ] 清理Desktop.Foundation死代码
- [ ] 清理Desktop.Infrastructure死代码
- [ ] 清理Desktop业务模块死代码
- [ ] 更新Prism模块注册
- [ ] 验证Desktop构建通过

### 4.4 执行清理（Shared模块）
- [ ] 清理Shared.Models死代码
- [ ] 清理Shared.Utilities死代码
- [ ] 验证Shared构建通过

### 4.5 全量验证
- [ ] 执行dotnet build LYBT.All.sln
- [ ] 执行dotnet test（全部通过）
- [ ] 记录删除的文件清单

## Phase 5: 记录基准

### 5.1 收集最终覆盖率数据
- [ ] 重新运行覆盖率收集
- [ ] 记录各模块覆盖率指标
- [ ] 对比清理前后的变化

### 5.2 更新文档
- [ ] 更新docs/analysis/coverage-baseline.md
- [ ] 记录覆盖率基准线
- [ ] 说明覆盖率收集方法

### 5.3 保存分析脚本
- [ ] 提交scripts/collect-coverage.ps1
- [ ] 提交scripts/analyze-coverage.ps1
- [ ] 更新README说明如何运行覆盖率分析

## 最终验证

- [ ] 所有单元测试通过
- [ ] 所有集成测试通过
- [ ] 应用程序正常启动
- [ ] 覆盖率报告可正常生成
- [ ] 分析脚本可重复使用

# P3本地覆盖率验证工具

Phase 3 本地覆盖率验证工具，用于在提交代码前验证是否达到 70% 覆盖率硬门槛。

## 🚀 快速使用

### 方法1: 批处理文件（推荐）
```bash
# 在项目根目录执行
scripts\test-coverage.bat
```

### 方法2: PowerShell脚本
```powershell
# 在项目根目录执行
powershell -ExecutionPolicy Bypass -File scripts\test-coverage-local.ps1
```

## 📋 命令参数

### PowerShell脚本参数
```powershell
scripts\test-coverage-local.ps1 [选项]

参数说明:
  -CoverageThreshold <数值>   # 覆盖率阈值 (默认: 70)
  -SkipBuild                  # 跳过构建步骤  
  -OpenReport                 # 自动打开HTML报告
```

### 使用示例
```powershell
# 基础验证 (70%阈值)
scripts\test-coverage-local.ps1

# 设置更高阈值
scripts\test-coverage-local.ps1 -CoverageThreshold 80

# 跳过构建并打开报告
scripts\test-coverage-local.ps1 -SkipBuild -OpenReport
```

## 📊 输出说明

### 成功示例
```
🎯 P3本地覆盖率验证结果
==================================================

🧪 测试执行结果:
  ✓ Users: PASS
  ✓ Patients: PASS
  ✓ Prescriptions: PASS
  ✓ Consultation: PASS
  ✓ Herbs: PASS
  ✓ Formula: PASS
  ✓ MedicalCase: PASS
  ✓ Auth: PASS

📈 覆盖率指标:
  行覆盖率 (Line): 75.2%
  分支覆盖率 (Branch): 68.5%
  目标阈值: 70%

🎯 P3硬门槛检查:
  ✅ 覆盖率达标: 75.2% ≥ 70%
  🚀 CI门禁预期: 通过
```

### 失败示例
```
🎯 P3硬门槛检查:
  ❌ 覆盖率不达标: 65.8% < 70%
  🚫 CI门禁预期: 失败
  💡 建议: 增加测试用例提升覆盖率
```

## 🎯 CI门禁关系

此本地验证工具与 CI 门禁使用相同的覆盖率计算逻辑：

| 本地验证 | CI门禁 | 说明 |
|----------|--------|------|
| ✅ 通过 | ✅ 通过 | 覆盖率 ≥ 70% |
| ❌ 失败 | ❌ 失败 | 覆盖率 < 70% |

**推荐工作流程**:
1. 本地开发完成后运行 `scripts\test-coverage.bat`
2. 确保显示 "✅ 覆盖率达标"
3. 提交代码，CI 门禁将自动通过

## 📁 报告文件

验证完成后会生成以下文件：

```
TestResults/LocalCoverage/
├── CoverageReport/
│   ├── index.html          # HTML报告 (可用浏览器打开)
│   ├── Summary.json        # JSON摘要
│   ├── Summary.txt         # 文本摘要
│   └── badge_linecoverage.svg  # 覆盖率徽章
└── [各模块测试结果]/
```

使用 `-OpenReport` 参数可自动打开 HTML 报告查看详细信息。

## 🛠️ 故障排除

### 常见问题

**问题**: "❌ 未找到覆盖率数据文件"
```
解决: 确保所有测试项目都编译成功
1. 检查测试项目引用路径
2. 运行 dotnet build 确认无编译错误
```

**问题**: "❌ ReportGenerator安装失败"  
```
解决: 手动安装全局工具
dotnet tool install -g dotnet-reportgenerator-globaltool
```

**问题**: PowerShell执行策略错误
```
解决: 使用批处理文件或临时修改策略
scripts\test-coverage.bat
# 或
Set-ExecutionPolicy -ExecutionPolicy Bypass -Scope Process
```

## 🔧 自定义配置

### 修改覆盖率阈值
编辑脚本第4行修改默认阈值：
```powershell
[int]$CoverageThreshold = 75,  # 修改为75%
```

### 排除特定程序集
编辑reportgenerator参数：
```powershell
"-assemblyfilters:-*Tests*;-*TestUtilities*;-*YourAssembly*" `
```

### 排除特定类
编辑reportgenerator参数：
```powershell  
"-classfilters:-*Tests*;-*Mock*;-*Stub*;-*YourClass*" `
```

---

**Phase 3 目标**: 通过本地验证确保提交的代码始终满足 70% 覆盖率要求，避免 CI 门禁失败。
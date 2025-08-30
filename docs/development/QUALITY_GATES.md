# UltraThink代码质量门禁规则

基于UltraThink重构经验制定的自动化代码质量检查标准，防止代码质量倒退。

## 🎯 质量标准

### 1. 文件行数限制

| 文件类型 | 最大行数 | 严重性 | 依据 |
|---------|---------|--------|------|
| Helper类 | 500行 | 🚨 HIGH | 基于重构前问题（User:534行, Patient:521行, Prescription:649行）|
| Service类 | 300行 | ⚠️ MEDIUM | 合理的单一职责原则范围 |
| Controller类 | 200行 | ⚠️ MEDIUM | API层应保持轻量 |

### 2. AutoMapper使用规范

**强制要求**：
- ✅ 所有DTO映射必须使用AutoMapper
- ❌ 禁止手动字段映射 (`model.Field = dto.Field`)
- ❌ 禁止手动null检查模式 (`if (!string.IsNullOrWhiteSpace(dto.Field))`)

**检测模式**：
```csharp
// ❌ 危险模式 - 容易遗漏字段
if (!string.IsNullOrWhiteSpace(dto.Name)) { model.Name = dto.Name; }
if (!string.IsNullOrWhiteSpace(dto.Status)) { model.Status = status; }

// ✅ 正确模式 - 使用AutoMapper
_mapper.Map(dto, model);
```

### 3. 重构架构完整性

**必须存在的重构文件**：
- `UserBusinessHelper.Refactored.cs`
- `PatientBusinessHelper.Refactored.cs` 
- `PrescriptionBusinessHelper.Refactored.cs`

**重构模式验证**：
- 每个重构模块包含5个专业服务 + 1个协调器
- 服务接口清晰分离职责
- 依赖注入正确配置

## 🔧 使用方法

### 手动执行检查
```batch
# 标准检查
scripts\quality-check.bat

# 详细报告
powershell scripts\quality-check.ps1 -Detailed

# 自定义限制
powershell scripts\quality-check.ps1 -MaxHelperLines 400
```

### Git自动检查
```batch
# 安装pre-commit钩子
scripts\install-git-hooks.bat

# 正常提交（自动检查）
git commit -m "提交消息"

# 跳过检查（紧急情况）
git commit --no-verify -m "紧急修复"
```

## 📊 检查项目详情

### Helper类行数检查
- **目标**: 防止单一类承担过多职责
- **检查范围**: `*Helper.cs`文件
- **排除文件**: `*Refactored.cs`, `*Base.cs`
- **处理建议**: 
  - 500行+ → 使用UltraThink服务分离模式重构
  - 参考User/Patient/Prescription重构案例

### Service类行数检查
- **目标**: 保持服务类的单一职责
- **检查范围**: `*Service.cs`文件
- **排除文件**: `*Interface.cs`, `*Base.cs`
- **处理建议**: 
  - 300行+ → 分离为多个专业服务
  - 按功能域拆分（CRUD、Business、Import等）

### AutoMapper规范检查
- **目标**: 确保字段映射完整性
- **检查模式**: 正则表达式匹配危险模式
- **处理建议**:
  - 发现手动映射 → 重构为AutoMapper方式
  - 参考MedicalCase修复案例

### 重构架构检查
- **目标**: 确保重构后架构完整
- **检查项**: Refactored文件存在性
- **处理建议**: 
  - 缺失文件 → 按UltraThink模式补充
  - 确保服务分离完整

## 🚦 质量门禁策略

### 阻止提交（HIGH严重性）
- Helper类超过500行
- 发现严重架构违规

### 警告提示（MEDIUM严重性）
- Service/Controller类超行数限制
- 检测到手动映射模式

### 信息提示（LOW严重性）
- 缺少重构文件
- 轻微规范偏差

## 📈 质量改进建议

### 立即处理
1. **分析超限文件**：使用`-Detailed`参数获取详细报告
2. **应用重构模式**：参考现有重构案例
3. **更新映射配置**：替换手动映射为AutoMapper

### 预防措施
1. **开发前检查**：提交前运行质量检查
2. **Code Review**：使用质量门禁报告指导审查
3. **持续监控**：定期执行质量检查，趋势分析

### 工具集成
1. **CI/CD集成**：在构建管道中执行检查
2. **IDE集成**：配置文件保存时自动检查
3. **团队规范**：建立质量检查SOP

## 📋 相关文档

- [UltraThink重构方法论](../ultrathink/)
- [AutoMapper使用规范](../development/AUTOMAPPER_GUIDELINES.md)
- [服务分离最佳实践](../architecture/SERVICE_SEPARATION.md)
- [代码审查清单](../development/CODE_REVIEW_CHECKLIST.md)

---

**最后更新**: 2025-08-28  
**版本**: v1.0  
**维护者**: UltraThink重构团队
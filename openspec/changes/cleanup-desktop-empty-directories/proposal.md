# cleanup-desktop-empty-directories

## Summary

清理Desktop层文件结构：删除空目录 + 将接口文件移动到Interfaces文件夹。

## Motivation

项目经过多次重构后存在以下问题：

**1. 空目录遗留：**
- `LYBT.Desktop.Admin` - 完全空的模块目录（无.csproj文件）
- `LYBT.Desktop.Services` - 空的Core目录
- `LYBT.Desktop.Infrastructure/Enums` - 空的Enums目录

**2. 接口文件位置不一致：**
- Prescriptions模块: `IPrescriptionPrintService.cs` 在 Services/ 而非 Interfaces/
- Auth模块: `IConnectionSettingsService.cs` 在 Services/ 而非 Interfaces/

其他模块（Consultation, Formula, Herbs, MedicalCase, Patients, Users）已正确组织。

## Scope

**In Scope:**
- 删除Desktop层中完全空的目录
- 创建缺失的Interfaces文件夹
- 移动接口文件到对应的Interfaces文件夹
- 更新文件的namespace（如需要）
- 验证编译不受影响

**Out of Scope:**
- Server层文件结构优化（单独提案）
- 测试文件清理
- obj/bin目录清理（构建产物）

## Risk Assessment

**Risk Level: Low-Medium**

- 删除空目录：无风险
- 移动接口文件：需要更新namespace和引用，但IDE支持自动重构

## Related Specs

- `client-layer-architecture` - Desktop层架构规范
- `project-architecture` - 项目整体架构

## Stakeholders

- 开发团队（代码整洁性和一致性）

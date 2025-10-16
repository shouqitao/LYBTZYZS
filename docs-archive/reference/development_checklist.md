# 开发检查清单

**更新时间**: 2025-10-15 18:11:07
**条目数量**: 12 个
**使用说明**: 快速查找常用解决方案，点击目录直接跳转

## 📋 快速目录

1. [## #### 3. WPF / Prism MVVM 规范（🟡 建议问题）](#1-##-####-3.-wpf-/-prism-mvvm-规范（🟡-建议问题）)
2. [## ## 📚 参考文档](#2-##-##-📚-参考文档)
3. [## ### Phase 2: 核心重构阶段 (Core Refactoring - 短期)](#3-##-###-phase-2:-核心重构阶段-(core-refactoring---短期))
4. [## ### 药材组成验证规则](#4-##-###-药材组成验证规则)
5. [## #### 4. PR提交前最终检查](#5-##-####-4.-pr提交前最终检查)
6. [## #### 4. 代码质量（🟢 优化建议）](#6-##-####-4.-代码质量（🟢-优化建议）)
7. [## ### 审查清单](#7-##-###-审查清单)
8. [## ## 🔗 相关资源](#8-##-##-🔗-相关资源)
9. [## ### 适用场景](#9-##-###-适用场景)
10. [## ### 主要目标](#10-##-###-主要目标)
11. [1.  **深化后端服务层重构**：遵循“单一职责”原则，对臃肿的服务进行拆分；引入`FluentV...](#11-1.--**深化后端服务层重构**：遵循“单一职责”原则，对臃肿的服务进行拆分；引入`fluentv...)
12. [## ## 📊 检查清单](#12-##-##-📊-检查清单)

---

## 1. ## #### 3. WPF / Prism MVVM 规范（🟡 建议问题）

**解决方案**:
- ViewModel:
- 导航:
- 对话框:

**来源**: `code-review-guidelines.md`

**重要程度**: ⭐⭐⭐⭐⭐ (1.0/1.0)

---

## 2. ## ## 📚 参考文档

**解决方案**:
- **Prism 官方文档**：https://prismlibrary.com/docs/
- **架构设计**：`docs/architecture/desktop/viewmodel-base-architecture.md`（待创建）
- **标准规范**：`docs/development/standards.md`
- **Phase 4B 报告**：`docs/reports/phase2-step2-skeleton-generation-report.md`

**来源**: `architecture-unification-issue-897-2025-10-04.md`

**重要程度**: ⭐⭐⭐⭐⭐ (1.0/1.0)

---

## 3. ## ### Phase 2: 核心重构阶段 (Core Refactoring - 短期)

**解决方案**:
- **深化后端服务层重构**：遵循“单一职责”原则，对臃肿的服务进行拆分；引入`FluentValidation`统一验证逻辑。
- **完成桌面端ViewModel重构**：全面推广使用`ModernManagementViewModel`等现代化基类，统一界面交互逻辑。
- **统一UI资源与清理**：将所有零散的转换器（Converters）、样式（Styles）集中到`UnifiedDesignSystem.xaml`，为未来可能的UI升级做好准备。

**来源**: `ADR-002-technology-roadmap-suggestion.md`

**重要程度**: ⭐⭐⭐⭐ (0.9/1.0)

---

## 4. ## ### 药材组成验证规则

**解决方案**:
- **药材选择**：必须从药材库中选择有效药材
- **剂量设置**：必须为正整数，范围1-1000
- **单位规范**：默认"g"，支持"钱"、"两"等中医单位
- **炮制方法**：可选，最多100字符
- **用法说明**：可选，最多200字符

**来源**: `formula-module.md`

**重要程度**: ⭐⭐⭐⭐ (0.9/1.0)

---

## 5. ## #### 4. PR提交前最终检查

**解决方案**:
- [ ] PR标题符合规范: `{type}({scope}): {description}`
- [ ] 关联了正确的Issue (使用 Closes/Fixes/Resolves #xxx)
- [ ] 如果是Epic Task，添加了 `epic:{epic-name}` 标签
- [ ] 所有验收标准已勾选
- [ ] 编译和测试结果已粘贴
- [ ] 技术合规检查已完成
- [ ] 文档已同步更新
- [ ] 代码已格式化 (`dotnet format`)

**来源**: `ai-collaboration-guide.md`

**重要程度**: ⭐⭐⭐⭐ (0.9/1.0)

---

## 6. ## #### 4. 代码质量（🟢 优化建议）

**解决方案**:
- 单个文件 ≤500 行
- 方法圈复杂度 ≤10
- 避免重复代码（DRY 原则）
- 清晰的注释（复杂逻辑必须注释）
- 魔法数字提取为常量

**来源**: `code-review-guidelines.md`

**重要程度**: ⭐⭐⭐⭐ (0.9/1.0)

---

## 7. ## ### 审查清单

**解决方案**:
- [ ] 业务逻辑正确
- [ ] 符合项目架构标准（Record-Only 系统）
- [ ] 命名规范清晰
- [ ] 注释充分
- [ ] 测试覆盖充分
- [ ] 文档已同步更新

**来源**: `code-review-guidelines.md`

**重要程度**: ⭐⭐⭐⭐ (0.9/1.0)

---

## 8. ## ## 🔗 相关资源

**解决方案**:
- **Claude Code Review Workflow**: `.github/workflows/claude-code-review.yml`
- **PR 模板**: `.github/pull_request_template.md`
- **CODEOWNERS 配置**: `.github/CODEOWNERS`
- **分支保护配置**: `docs/development/branch-protection-setup.md`
- **开发规范**: `docs/development/standards.md`

**来源**: `code-review-guidelines.md`

**重要程度**: ⭐⭐⭐⭐ (0.9/1.0)

---

## 9. ## ### 适用场景

**解决方案**:
- **新模块开发**: 创建全新的业务功能模块
- **模块重构**: 基于模板重构现有模块
- **团队培训**: 帮助新团队成员了解项目规范
- **代码审查**: 检查模块是否符合模板标准

**来源**: `module-template-guide.md`

**重要程度**: ⭐⭐⭐⭐ (0.9/1.0)

---

## 10. ## ### 主要目标

**解决方案**:
- **提升开发效率**: 新模块开发时间减少 30% 以上
- **保证代码质量**: 自动化质量检查，代码符合项目标准
- **降低学习成本**: 新开发人员能快速上手项目开发
- **减少技术争论**: 明确的开发规范和最佳实践指导

**来源**: `rapid-development-guide.md`

**重要程度**: ⭐⭐⭐⭐ (0.9/1.0)

---

## 11. 1.  **深化后端服务层重构**：遵循“单一职责”原则，对臃肿的服务进行拆分；引入`FluentValidation`统一验证逻辑。

**解决方案**:
- **深化后端服务层重构**：遵循“单一职责”原则，对臃肿的服务进行拆分；引入`FluentValidation`统一验证逻辑。
- **完成桌面端ViewModel重构**：全面推广使用`ModernManagementViewModel`等现代化基类，统一界面交互逻辑。
- **统一UI资源与清理**：将所有零散的转换器（Converters）、样式（Styles）集中到`UnifiedDesignSystem.xaml`，为未来可能的UI升级做好准备。

**来源**: `ADR-002-technology-roadmap-suggestion.md`

**重要程度**: ⭐⭐⭐⭐ (0.8/1.0)

---

## 12. ## ## 📊 检查清单

**解决方案**:
- [ ] DTO 是否有**明确的单一使用场景** (展示/创建/更新/详情)?
- [ ] 命名是否符合规范 (`*Dto` / `*CreateDto` / `*UpdateDto` / `*DetailDto`)?
- [ ] 是否避免了包含不必要的字段 (如 CreateDto 中的 Id)?
- [ ] 字段类型是否正确 (必需字段非 nullable,可选字段 nullable)?
- [ ] 是否使用了合理的默认值 (字符串 `= string.Empty`,集合 `= new()`)?
- [ ] 是否添加了必要的验证特性 (Data Annotations 或 FluentValidation)?

**来源**: `dto-design-principles.md`

**重要程度**: ⭐⭐⭐⭐ (0.8/1.0)

---

## 💡 使用建议

- **快速查找**: 使用目录快速定位到具体问题
- **代码示例**: 所有代码示例都可以直接复制使用
- **相关问题**: 查看条目的来源文档获取更多详细信息
- **反馈建议**: 发现问题或有改进建议请及时反馈


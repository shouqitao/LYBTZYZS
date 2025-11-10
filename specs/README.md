# 规格文档 (Specifications)

## 📋 目录概览

本目录包含 LYBTZYZS 项目的所有功能规格文档（需求契约）。每个模块都有独立的 `spec.md` 文件，定义了系统**必须做什么**。

## 🎯 规格文档的作用

### 与其他文档的区别

| 文档类型 | 职责 | 示例 |
|---------|------|------|
| **specs/** | 需求契约 (WHAT) | "系统 MUST 在用户登录时生成 JWT" |
| **docs/** | 解释文档 (WHY & HOW) | "三层架构说明"、"开发指南" |
| **changes/** | 变更追踪 (DELTA) | "添加2FA功能的变更记录" |

### 核心原则

1. **单一事实来源**: 所有需求必须在 specs/ 中定义
2. **强制关键词**: 使用 **MUST** / **SHALL** 表达强制需求
3. **可验证性**: 每个需求必须有明确的验收场景
4. **持续更新**: 代码变更必须同步更新相关 spec

## 📁 目录结构

```
specs/
├── README.md                    # 本文件
├── auth/
│   └── spec.md                 # 认证模块规格
├── patients/
│   └── spec.md                 # 患者管理模块规格
├── prescriptions/
│   └── spec.md                 # 处方管理模块规格
└── ...
```

## 📝 规格索引

### Server 端模块

| 模块 | 路径 | 状态 | 最后更新 |
|-----|------|------|---------|
| 认证 (Auth) | [auth/spec.md](auth/spec.md) | ⏸️ 待创建 | - |
| 用户管理 (Users) | [users/spec.md](users/spec.md) | ⏸️ 待创建 | - |
| 患者管理 (Patients) | [patients/spec.md](patients/spec.md) | ⏸️ 待创建 | - |
| 病例管理 (MedicalCase) | [medical-case/spec.md](medical-case/spec.md) | ⏸️ 待创建 | - |
| 诊疗管理 (Consultation) | [consultation/spec.md](consultation/spec.md) | ⏸️ 待创建 | - |
| 处方管理 (Prescriptions) | [prescriptions/spec.md](prescriptions/spec.md) | ⏸️ 待创建 | - |
| 草药管理 (Herbs) | [herbs/spec.md](herbs/spec.md) | ⏸️ 待创建 | - |
| 方剂管理 (Formula) | [formula/spec.md](formula/spec.md) | ⏸️ 待创建 | - |

### Client 端模块

| 模块 | 路径 | 状态 | 最后更新 |
|-----|------|------|---------|
| Desktop 认证 | [desktop-auth/spec.md](desktop-auth/spec.md) | ⏸️ 待创建 | - |
| Desktop 患者 | [desktop-patients/spec.md](desktop-patients/spec.md) | ⏸️ 待创建 | - |

## 🚀 快速开始

### 创建新规格

使用模板创建新的规格文件：

```bash
# 复制模板
cp .claude/specs/template.md specs/module-name/spec.md

# 编辑内容
# 填写：目的、需求、场景、技术约束
```

### 查看规格

```bash
# 使用任何 Markdown 编辑器查看
code specs/auth/spec.md
```

### 验证规格

```bash
# 使用 lybtzyzs-spec-validator skill（开发中）
# 验证格式、关键词、场景完整性
```

## 📖 编写规范

### 必需章节

每个 `spec.md` 必须包含以下章节：

1. **目的 (Purpose)**: 最少20字符，描述模块职责
2. **需求 (Requirements)**: 使用 MUST/SHALL 关键词
3. **场景 (Scenarios)**: Given-When-Then 格式
4. **技术约束 (Constraints)**: 数据库、API、依赖

### 需求格式

```markdown
### 需求：用户认证
系统 **必须 (MUST)** 在用户提交有效凭证后生成 JWT 令牌。

**业务规则**：
- JWT 有效期为24小时
- 失败3次后账户锁定15分钟
```

### 场景格式

```markdown
#### 场景：成功登录
- **前提条件 (Given)**: 用户已注册且账户未锁定
- **操作 (When)**: 用户提交正确的用户名和密码
- **预期结果 (Then)**: 系统返回 JWT 令牌并重定向到首页
- **验证方式**: 检查 HTTP 响应包含 token 字段且状态码为 200
```

## 🔄 变更管理

### 小变更（直接修改）

如果变更简单（<5个需求），直接修改 spec.md：

1. 编辑 specs/module/spec.md
2. 提交 Git commit
3. 更新相关 docs/

### 大变更（Delta 追踪）

如果变更复杂（>5个需求），使用 changes/ 目录：

1. 创建 changes/change-name/
2. 编写 Delta spec（ADDED/MODIFIED/REMOVED/RENAMED）
3. 完成后归档并合并到主 spec

详见 [changes/README.md](../changes/README.md)

## 🛠️ 相关工具

### Skills

- **lybtzyzs-spec-manager**: 规格文件的 CRUD 操作
- **lybtzyzs-spec-validator**: 格式和语义验证
- **lybtzyzs-change-manager**: 变更流程管理

### 命令

- `/create-spec [module]`: 创建新规格（开发中）
- `/validate-spec [module]`: 验证规格（开发中）
- `/list-specs`: 列出所有规格（开发中）

## 📚 扩展阅读

- [规格模板](.claude/specs/template.md)
- [验证规则](.claude/specs/validation-rules.md)
- [变更流程](../changes/README.md)
- [编码规范](.claude/reference/coding-standards.md)

---

**最后更新**: 2025-01-10  
**维护者**: Claude Code  
**版本**: v1.0.0

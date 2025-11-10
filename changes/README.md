# 变更管理 (Changes)

## 📋 目录概览

本目录管理 LYBTZYZS 项目的所有**进行中**和**已完成**的规格变更。使用 Delta 追踪机制记录需求的演进历史。

## 🎯 变更管理的作用

### 为什么需要变更目录？

当需求变更涉及**多个模块**或**复杂的需求重构**时，直接修改 specs/ 会导致：
- ❌ 变更历史难以追踪
- ❌ 变更原因不明确
- ❌ 多人协作冲突
- ❌ 回滚困难

使用 changes/ 目录可以：
- ✅ 清晰记录变更原因（Why）和内容（What）
- ✅ 使用 Delta 格式追踪增量变化
- ✅ 验证变更一致性
- ✅ 归档后自动合并到主 specs/

### 何时使用变更目录？

| 场景 | 使用方式 |
|-----|---------|
| 简单变更（<5个需求） | 直接修改 specs/ |
| 复杂变更（>5个需求） | 创建 changes/xxx/ |
| 跨模块变更 | 创建 changes/xxx/ |
| 重大重构 | 创建 changes/xxx/ |

## 📁 目录结构

```
changes/
├── README.md                    # 本文件
├── add-2fa/                    # 进行中的变更
│   ├── proposal.md             # 变更提案（Why + What + Impact）
│   ├── tasks.md                # 任务清单（checkbox 格式）
│   ├── design.md               # 技术设计（可选）
│   └── specs/                  # Delta 规格
│       └── auth/
│           └── spec.md         # ADDED/MODIFIED/REMOVED
└── archive/                    # 已完成变更归档
    └── 2025-01-15-add-2fa/     # 归档时自动添加时间戳
```

## 🚀 变更流程

### 完整流程图

```
1. 创建变更目录
   ↓
2. 编写 proposal.md（Why + What + Impact）
   ↓
3. 编写 tasks.md（任务清单）
   ↓
4. 编写 Delta specs（ADDED/MODIFIED/REMOVED/RENAMED）
   ↓
5. 验证 Delta 一致性
   ↓
6. 实施代码变更
   ↓
7. 归档变更（合并到主 specs/）
```

### 步骤详解

#### 步骤 1: 创建变更目录

```bash
# 手动创建
mkdir -p changes/add-2fa/specs/auth

# 或使用 Skill（开发中）
# lybtzyzs-change-manager create add-2fa
```

#### 步骤 2: 编写 proposal.md

定义变更的**为什么**和**是什么**：

```markdown
# 添加两因素认证

## 为什么 (Why)
增强安全性，满足合规要求...

## 变更内容 (What Changes)
- 添加 OTP 生成和验证功能
- 修改登录流程集成2FA

## 影响范围 (Impact)
- 影响的模块: LYBT.Server.Modules.Auth
- 数据库迁移: 需要
```

#### 步骤 3: 编写 tasks.md

使用标准 checkbox 格式：

```markdown
## 1. 数据层实现
- [ ] 1.1 创建 TwoFactorSecret 字段
- [ ] 1.2 创建 OTPVerificationLog 表

## 2. 业务层实现
- [ ] 2.1 实现 IOTPService 接口
```

#### 步骤 4: 编写 Delta specs

在 `changes/xxx/specs/module/spec.md` 中定义增量变化：

```markdown
# 认证模块规格变更

## ADDED 需求
### 需求：两因素认证
系统 **必须 (MUST)** 在用户提交有效凭证后要求提供第二因素验证。

## MODIFIED 需求
### 需求：用户认证
**原有内容**：...
**修改为**：...
**变更原因**：增强安全性

## REMOVED 需求
- `### 需求：记住我功能`
**删除原因**：与2FA安全策略冲突

## RENAMED 需求
- FROM: `### 需求：登录功能`
- TO: `### 需求：用户认证`
```

#### 步骤 5: 验证 Delta

```bash
# 使用 Skill 验证（开发中）
# lybtzyzs-spec-validator validate changes/add-2fa --strict
```

#### 步骤 6: 实施代码变更

按照 tasks.md 执行开发任务，完成后勾选 checkbox：

```markdown
- [x] 1.1 创建 TwoFactorSecret 字段
- [x] 1.2 创建 OTPVerificationLog 表
```

#### 步骤 7: 归档变更

所有任务完成后，归档变更：

```bash
# 使用 Skill 归档（开发中）
# lybtzyzs-change-manager archive add-2fa

# 归档过程：
# 1. 验证所有任务完成
# 2. 解析 Delta specs
# 3. 应用变更到主 specs/
# 4. 移动到 archive/2025-01-15-add-2fa/
```

## 📝 Delta 格式规范

### ADDED（添加新需求）

```markdown
## ADDED 需求

### 需求：两因素认证
系统 **必须 (MUST)** 在用户提交有效凭证后要求提供第二因素验证。

**业务规则**：
- OTP 有效期为5分钟

#### 场景：OTP 验证成功
- **前提条件 (Given)**: 用户已启用2FA且完成密码验证
- **操作 (When)**: 用户提交正确的 OTP 代码
- **预期结果 (Then)**: 系统完成登录流程
- **验证方式**: 检查 Session 包含 TwoFactorVerified=true
```

### MODIFIED（修改现有需求）

```markdown
## MODIFIED 需求

### 需求：用户认证

**原有内容**：
> 系统 **必须 (MUST)** 在用户提交有效凭证后生成 JWT 令牌。

**修改为**：
> 系统 **必须 (MUST)** 在用户提交有效凭证并通过2FA验证后生成 JWT 令牌。

**变更原因**：
增强安全性，满足合规要求。

**影响的场景**：
- ✏️ 修改"成功登录"场景：增加2FA验证步骤
```

### REMOVED（删除需求）

```markdown
## REMOVED 需求

- `### 需求：记住我功能`

**删除原因**：
与2FA安全策略冲突，改为使用长期 Refresh Token。

**影响评估**：
- 需要更新 LYBT.Desktop.Auth ViewModel
- 需要迁移现有"记住我"用户数据
```

### RENAMED（重命名需求）

```markdown
## RENAMED 需求

- **FROM**: `### 需求：登录功能`
- **TO**: `### 需求：用户认证`

**重命名原因**：
更准确地反映功能范围（包含登录+2FA+会话管理）。
```

## 📊 变更索引

### 进行中的变更

| 变更名称 | 创建日期 | 任务进度 | 负责人 | 状态 |
|---------|---------|---------|--------|------|
| _暂无进行中的变更_ | - | - | - | - |

### 已完成变更（最近10条）

| 变更名称 | 完成日期 | 影响模块 | 归档路径 |
|---------|---------|---------|---------|
| _暂无已完成变更_ | - | - | - |

## 🛠️ 相关工具

### Skills

- **lybtzyzs-change-manager**: 变更流程全生命周期管理
  - `create`: 创建新变更
  - `validate`: 验证 Delta 一致性
  - `archive`: 归档并合并到主 specs/
  
- **lybtzyzs-spec-validator**: 验证 Delta 格式和语义

### 命令（开发中）

```bash
# 创建新变更
/create-change add-2fa

# 查看变更状态
/list-changes

# 验证变更
/validate-change add-2fa

# 归档变更
/archive-change add-2fa
```

## 📚 扩展阅读

- [变更模板](.claude/specs/change-template/)
- [规格文档](../specs/README.md)
- [验证规则](.claude/specs/validation-rules.md)
- [工作流指南](.claude/core/WORKFLOW.md)

---

**最后更新**: 2025-01-10  
**维护者**: Claude Code  
**版本**: v1.0.0

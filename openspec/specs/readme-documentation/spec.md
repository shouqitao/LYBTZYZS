# Spec: readme-documentation

## Purpose

定义项目README.md文档的统一标准和格式规范，确保各项目README简洁实用、反映真实情况、易于维护。

## Requirements

### Requirement: DOC-001 README基本原则

README SHALL 遵循以下原则:

1. **简洁实用** - 控制在100-300行，重点信息一目了然
2. **反映真实** - 内容必须与代码一致，禁止复制粘贴示例代码
3. **易于维护** - 结构统一，更新方便
4. **按需详细** - 复杂内容链接到专门文档

#### Scenario: README长度控制
- **WHEN** 编写README
- **THEN** Server模块 SHALL 控制在100-200行
- **AND** Client模块 SHALL 控制在150-250行
- **AND** Shared项目 SHALL 控制在100-200行

#### Scenario: 禁止代码复制
- **WHEN** 需要展示代码示例
- **THEN** SHALL NOT 复制大段示例代码到README
- **AND** MAY 展示核心接口签名(不超过20行)
- **AND** SHALL 链接到实际代码文件

---

### Requirement: DOC-002 Server模块README格式

Server模块README SHALL 采用以下结构:

```markdown
# LYBT.Module.{Domain}

> {一句话描述} | {架构模式} | {依赖方式}

## 项目定位

- **层级**: Server端
- **架构模式**: {传统三层/CQRS}
- **跨模块通信**: {ICrossModuleQueryService/IXxxService/无}

## 目录结构

```
LYBT.Module.{Domain}/
├── {Domain}Module.cs
├── Repositories/
├── Services/
└── ...
```

## 核心接口

| 接口 | 方法数 | 说明 |
|------|--------|------|
| I{Entity}Service | X | {说明} |
| I{Entity}Repository | X | {说明} |

## 依赖关系

### 依赖
- LYBT.Infrastructure
- LYBT.Entities
- LYBT.Shared.Models

### 被依赖
- LYBT.WebAPI

## API端点

| 端点 | 方法 | 说明 |
|------|------|------|
| /api/{entity} | GET | 分页查询 |
| /api/{entity}/{id} | GET | 按ID查询 |
| ... | ... | ... |

## 更新记录

| 日期 | 变更 |
|------|------|
| YYYY-MM-DD | {变更说明} |
```

#### Scenario: Server模块核心接口
- **WHEN** 描述核心接口
- **THEN** SHALL 使用表格列出接口名、方法数、说明
- **AND** SHALL NOT 展示完整方法签名

---

### Requirement: DOC-003 Client模块README格式

Client模块README SHALL 采用以下结构:

```markdown
# LYBT.Desktop.{Domain}

> {一句话描述} | ViewModel数: {N} | View数: {M}

## 项目定位

- **层级**: Client端
- **模块类型**: 业务模块
- **主要功能**: {功能列表}

## 目录结构

```
LYBT.Desktop.{Domain}/
├── {Domain}Module.cs
├── Views/
├── ViewModels/
└── ...
```

## Views/ViewModels

| View | ViewModel | 说明 |
|------|-----------|------|
| {Feature}View | {Feature}ViewModel | {说明} |
| ... | ... | ... |

## 依赖关系

### 依赖
- LYBT.Desktop.Presentation
- LYBT.Desktop.Foundation
- LYBT.Shared.Models

### 被依赖
- LYBT.Desktop.{Role}

## 事件通信

| 事件 | 发布/订阅 | 说明 |
|------|-----------|------|
| {Event}Event | 发布 | {说明} |
| ... | ... | ... |

## 更新记录

| 日期 | 变更 |
|------|------|
| YYYY-MM-DD | {变更说明} |
```

#### Scenario: Client模块View列表
- **WHEN** 描述Views
- **THEN** SHALL 使用表格列出View、ViewModel、说明
- **AND** 复杂模块 MAY 分组展示(主视图、弹窗等)

---

### Requirement: DOC-004 Shared项目README格式

Shared项目README SHALL 采用以下结构:

```markdown
# LYBT.Shared.{Purpose}

> {一句话描述} | 文件数: {N}

## 项目定位

- **层级**: Shared层
- **职责**: {职责描述}

## 目录结构

```
LYBT.Shared.{Purpose}/
├── {Category}/
│   └── ...
└── ...
```

## 主要内容

| 分类 | 文件数 | 说明 |
|------|--------|------|
| {Category} | X | {说明} |
| ... | ... | ... |

## 关键类型

| 类型 | 说明 |
|------|------|
| {ClassName} | {说明} |
| ... | ... |

## 依赖关系

### 依赖
- {无/其他Shared项目}

### 被依赖
- Server层所有模块
- Client层所有模块

## 更新记录

| 日期 | 变更 |
|------|------|
| YYYY-MM-DD | {变更说明} |
```

---

### Requirement: DOC-005 Core项目README格式

Core项目(Infrastructure、Entities等) SHALL 采用以下结构:

```markdown
# LYBT.{Project}

> {一句话描述} | 核心基础设施

## 项目定位

- **层级**: {Server/Client} Core层
- **职责**: {职责描述}

## 目录结构

```
LYBT.{Project}/
├── ...
```

## 核心组件

| 组件 | 说明 |
|------|------|
| {Component} | {说明} |
| ... | ... |

## 扩展点

| 扩展点 | 用途 |
|--------|------|
| {ExtensionPoint} | {说明} |

## 依赖关系

### 依赖
- {列表}

### 被依赖
- {列表}

## 更新记录

| 日期 | 变更 |
|------|------|
| YYYY-MM-DD | {变更说明} |
```

---

### Requirement: DOC-006 README维护规范

README SHALL 保持与代码同步更新。

#### Scenario: 接口变更
- **WHEN** 添加/删除/修改接口
- **THEN** SHALL 更新核心接口表
- **AND** SHALL 更新API端点表(Server模块)
- **AND** SHALL 记录到更新记录

#### Scenario: 文件结构变更
- **WHEN** 添加/删除目录或重要文件
- **THEN** SHALL 更新目录结构
- **AND** SHALL 记录到更新记录

#### Scenario: 依赖变更
- **WHEN** 添加/删除项目依赖
- **THEN** SHALL 更新依赖关系章节
- **AND** SHALL 记录到更新记录

#### Scenario: 更新记录
- **WHEN** README内容变更
- **THEN** SHALL 在更新记录表添加条目
- **AND** SHALL 使用日期和简要变更说明

---

### Requirement: DOC-007 禁止内容

README SHALL NOT 包含以下内容:

1. **大段示例代码** - 超过20行的代码块
2. **重复的使用示例** - 与其他README重复的代码
3. **详细实现说明** - 应放到专门的设计文档
4. **过时的Bug记录** - 已修复的Bug应移除或归档
5. **待创建的文档链接** - 不存在的链接应删除

#### Scenario: 发现禁止内容
- **WHEN** README包含禁止内容
- **THEN** SHALL 删除或迁移该内容
- **AND** MAY 添加链接指向正确位置

---

## Cross-Reference

| 相关规范 | 关联说明 |
|----------|----------|
| project-architecture | 项目结构定义 |
| server-layer-architecture | Server层详细架构 |
| client-layer-architecture | Client层详细架构 |
| shared-layer-architecture | Shared层详细架构 |

---

## Changelog

| 日期 | 版本 | 变更 |
|------|------|------|
| 2025-12-04 | 1.0 | 初始版本，定义README文档规范 |

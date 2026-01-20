# relocate-cardreader-to-core

## Why

### 架构一致性问题

CardReader模块当前位于`Modules/`目录，但其职责定位与`Core/`目录下的Printing模块相同：

| 模块 | 职责 | 当前位置 | 建议位置 |
|------|------|----------|----------|
| Printing | 硬件抽象（打印输出） | Core/ | Core/ |
| CardReader | 硬件抽象（读卡输入） | Modules/ | **Core/** |

### 发现的问题

| 位置 | 问题类型 | 当前状态 | 期望状态 |
|------|----------|----------|----------|
| `Modules/LYBT.Desktop.CardReader/` | 架构定位不一致 | 与业务模块混放 | 移至Core/与Printing并列 |

### 影响分析

- CardReader是**基础设施服务**，不含业务逻辑
- 被多个模块依赖（Patients, Clinical）
- 应遵循分层架构：Core被Modules依赖

## What Changes

### Phase 1: 目录迁移

1. 将`src/Client/Desktop/Modules/LYBT.Desktop.CardReader/`移动到`src/Client/Desktop/Core/LYBT.Desktop.CardReader/`
2. 更新解决方案文件(.sln)中的项目路径
3. 更新所有项目引用路径

**注意**: 仅目录变更，代码内容不变

### Phase 2: 验证

1. 编译验证
2. 确认模块加载正常

## Architecture

### 变更前

```
src/Client/Desktop/
├── Core/
│   ├── LYBT.Desktop.Infrastructure/
│   ├── LYBT.Desktop.Contracts/
│   └── LYBT.Desktop.Printing/
├── Modules/
│   ├── LYBT.Desktop.CardReader/     ← 当前位置
│   ├── LYBT.Desktop.Patients/
│   └── ...
```

### 变更后

```
src/Client/Desktop/
├── Core/
│   ├── LYBT.Desktop.Infrastructure/
│   ├── LYBT.Desktop.Contracts/
│   ├── LYBT.Desktop.Printing/
│   └── LYBT.Desktop.CardReader/     ← 新位置
├── Modules/
│   ├── LYBT.Desktop.Patients/
│   └── ...
```

## Impact

- **文件变更**: ~5个文件（.sln + .csproj引用）
- **风险等级**: Low
- **测试要求**: 编译验证 + 模块加载测试

## Risks

| 风险 | 缓解措施 |
|------|----------|
| 项目引用路径错误 | 使用IDE重构工具或仔细检查相对路径 |
| Git历史丢失 | 使用git mv保留历史 |

## References

- 用户需求: 将CardReader模块移至Core目录，与Printing保持一致
- 相关提案: integrate-cardreader-module（已完成）

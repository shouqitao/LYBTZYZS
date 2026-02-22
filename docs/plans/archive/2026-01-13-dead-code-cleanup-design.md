# Dead Code Cleanup 设计方案

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** 系统性清理 LYBTZYZS 项目中的死代码，包括 Desktop 客户端和 Server 后端

**Architecture:** 采用静态分析（Grep + LSP）从大到小分层清理，每批编译验证后提交

**Tech Stack:** Grep, LSP Find References, dotnet build, git

---

## 1. 清理范围与层次

```
清理顺序（从大到小）：
┌─────────────────────────────────────────────────────┐
│ Phase 1: 未使用的完整文件/类                         │
│   - 未引用的 Service/Repository 类                  │
│   - 未引用的 ViewModel 类                           │
│   - 未引用的 DTO/Model 类                           │
│   - 空壳模块文件                                    │
├─────────────────────────────────────────────────────┤
│ Phase 2: 未使用的接口                               │
│   - 已定义但无实现的接口                            │
│   - 有实现但从未被注入/引用的接口                    │
├─────────────────────────────────────────────────────┤
│ Phase 3: 未调用的公共成员                           │
│   - 未调用的 public 方法                            │
│   - 未读取的 public 属性                            │
│   - 废弃的兼容代码（OpenSpec 标记）                  │
├─────────────────────────────────────────────────────┤
│ Phase 4: DI 注册清理                                │
│   - 未使用的服务注册                                │
│   - Logger 注册清理                                 │
└─────────────────────────────────────────────────────┘
```

## 2. 分析工具组合

| 工具 | 用途 |
|------|------|
| Grep | 搜索类型/方法名引用数量 |
| LSP (Find References) | 精确查找符号引用 |
| dotnet build | 每批次编译验证 |

## 3. Phase 1 分析流程：未使用的完整文件/类

### 步骤 1.1：识别候选文件

```bash
# 扫描所有 C# 类文件
find src -name "*.cs" -type f

# 对每个类名执行引用搜索
grep -r "ClassName" --include="*.cs" | wc -l
```

### 判定规则

- 引用数 = 1（仅自身）→ 死代码候选
- 引用数 = 2（自身 + DI注册）→ 检查 DI 是否被使用
- 引用数 ≥ 3 → 活跃代码，跳过

### 步骤 1.2：LSP 二次确认

对候选文件使用 LSP "Find All References"，确认零外部引用后标记为待删除。

### 步骤 1.3：分类整理

| 分类 | 目录模式 | 预期数量 |
|------|----------|----------|
| 死 ViewModel | `ViewModels/*.cs` | 中 |
| 死 Service | `Services/*.cs` | 中 |
| 死 DTO/Model | `Models/*.cs`, `Contracts/*.cs` | 高 |
| 死 Repository | `Repositories/*.cs` | 低 |
| 死接口 | `I*.cs` | 中 |

### 步骤 1.4：批次删除与验证

```bash
# 每 5-10 个文件为一批
rm <files>
dotnet build LYBT.All.sln -c Release
# 通过 → git commit
# 失败 → 回滚分析原因
```

## 4. Phase 2：未使用的接口清理

### 识别方法

```bash
# 搜索接口定义
grep -r "interface I" --include="*.cs" -l

# 对每个接口检查：
# 1. 实现类数量
grep -r ": IInterfaceName" --include="*.cs"
# 2. 注入/引用数量
grep -r "IInterfaceName" --include="*.cs" | grep -v "interface IInterfaceName"
```

### 判定规则

- 无实现类 → 删除接口
- 有实现但零注入 → 删除接口 + 实现类

## 5. Phase 3：未调用的公共成员

### 5.1 废弃兼容代码（优先）

```bash
# 搜索 OpenSpec 兼容标记
grep -rn "// OpenSpec:.*兼容" --include="*.cs"
grep -rn "// OpenSpec:.*待.*移除" --include="*.cs"
```

这类代码已明确标记为临时，可直接清理。

### 5.2 未调用的 public 方法

使用 LSP "Find References" 检查每个 public 方法，引用数 = 0 → 候选删除。

### 排除规则（不删除）

- `override` 方法
- 接口显式实现
- 带 `[RelayCommand]` 等特性的方法
- 生命周期方法（`OnNavigatedTo` 等）

## 6. Phase 4：DI 注册清理

### 检查位置

```
src/Client/Desktop/Shell/Extensions/ServiceCollectionExtensions.cs
src/Client/Desktop/Core/*/DependencyInjection/*.cs
src/Server/Services/LYBT.WebAPI/Extensions/*.cs
```

### 清理规则

- 被删除类的注册语句 → 同步删除
- `RegisterSingleton<T>` 中 T 已删除 → 删除该行
- Logger 注册已无对应类 → 删除

## 7. 执行工作流

```
┌─────────────────────────────────────────────────────────────┐
│                     死代码清理工作流                          │
└─────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────┐
│ Step 1: 全量扫描生成候选列表                                 │
│   - 按 Phase 1→4 顺序扫描                                   │
│   - 输出: dead-code-candidates.md                           │
└─────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────┐
│ Step 2: 用户确认候选列表                                     │
│   - 审查高风险项（ViewModel、Service）                       │
│   - 标记保留项（如预留扩展点）                               │
└─────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────┐
│ Step 3: 分批清理（每批 5-10 文件）                           │
│   ┌─────────────────────────────────────────────────────┐   │
│   │ 删除文件 → dotnet build → 通过? → git commit        │   │
│   │                            ↓ 失败                    │   │
│   │                     回滚 + 分析原因                  │   │
│   └─────────────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────┐
│ Step 4: 最终验证                                            │
│   - dotnet build LYBT.All.sln                              │
│   - dotnet test（确保测试通过）                              │
│   - 生成清理报告                                            │
└─────────────────────────────────────────────────────────────┘
```

## 8. 安全措施

| 措施 | 说明 |
|------|------|
| **编译验证** | 每批删除后必须 `dotnet build` 通过 |
| **小批量提交** | 每批 5-10 文件，便于回滚 |
| **保留清单** | 明确标记不删除的预留代码 |
| **测试验证** | 最终运行单元测试确保无回归 |

## 9. 提交信息规范

```
chore(cleanup): 删除未使用的 {模块} 代码

删除文件:
- path/to/File1.cs
- path/to/File2.cs

原因: 零引用，经 Grep + LSP 确认
```

## 10. 预期清理范围

| 层级 | 预估死代码量 | 说明 |
|------|-------------|------|
| Desktop.Models | 中 | 重构后遗留的旧 ViewModel 基类 |
| Desktop.Infrastructure | 中 | 已废弃的 Service 接口 |
| Desktop.Modules | 低-中 | 各模块中的废弃 DTO |
| Server.Modules | 低 | 相对稳定 |
| Shared | 低 | 公共代码使用率高 |

## 11. 已知高风险区域

根据最近重构历史，以下区域可能有较多死代码：

1. **ViewModel 基类** - `refactor-viewmodel-architecture` 后的旧基类
2. **导航服务** - `unify-navigation-architecture` 后的旧服务
3. **Mapping 服务** - `standardize-api-architecture` 删除 MappingService 后的残留
4. **兼容代码** - 带 `// OpenSpec:` 标记的临时适配

## 12. 风险与缓解

| 风险 | 缓解措施 |
|------|----------|
| 误删活跃代码 | LSP 二次确认 + 小批量提交 |
| 反射调用漏检 | 搜索字符串形式的类名引用 |
| 测试代码引用 | 同步检查 tests 目录 |
| DI 运行时失败 | 删除后运行应用验证 |

## 13. 成功标准

- [ ] 编译通过（0 错误 0 警告）
- [ ] 单元测试全部通过
- [ ] 清理报告记录所有删除项
- [ ] 无运行时 DI 注入失败

---

**设计者**: Claude Code (brainstorming skill)
**日期**: 2026-01-13
**状态**: 已确认，待实施

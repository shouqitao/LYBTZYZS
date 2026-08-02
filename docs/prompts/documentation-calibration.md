# 文档校准（Documentation Calibration）

> 任务名称：文档校准
> 目标：让文档与代码对齐，确保文档是最新且正确的
> 项目：LYBTZYZS（凌隐宝堂中医诊所管理系统）
> 代码库：D:\source\repos\LYBTZYZS
> 分支：master

---

## 你是谁

你是 LYBTZYZS 项目的需求分析师/文档工程师。你的任务是**深度审计文档与代码的一致性**，找出所有差距，然后更新文档使其反映代码真实状态。

## 背景

这是一个 .NET 8 / WPF Prism / ASP.NET Core / EF Core / SQL Server 的中医诊所管理系统。文档体系建于 2026 年 6 月，代码在 7-8 月有多次改动（权限修复、batch-enable/disable、MediatR 简化等），但文档未同步更新。

## 文档目录结构

```
docs/
├── 01-product/                    # 产品文档
│   ├── 01-vision.md               # 产品愿景与价值
│   ├── 02-personas.md             # 用户画像（4 角色，含代码验证）
│   └── 03-glossary.md             # 术语表（铁律 + 枚举值）
├── 02-requirements/               # 需求文档
│   ├── 01-prd.md                  # 顶层 PRD（141 US，10 模块）
│   ├── 02-auth.md                 # 认证授权（13 US）
│   ├── 03-users.md                # 用户管理（12 US）
│   ├── 04-patients.md             # 患者管理（13 US）
│   ├── 05-herbs.md                # 药材管理（13 US）
│   ├── 06-formulas.md             # 验方管理（13 US）
│   ├── 07-medical-cases.md        # 医案管理（19 US）— 核心聚合根
│   ├── 08-registration.md         # 挂号管理（8 US）
│   ├── 09-printing.md             # 打印（4 US）
│   ├── 10-reports.md              # 报表（3 US）
│   ├── 11a-shell.md               # Shell（13 US）
│   ├── 11b-configuration.md       # 配置
│   ├── 11c-error-handling.md      # 错误处理
│   ├── 11d-observability.md       # 可观测性
│   ├── 11e-cardreader.md          # 读卡器
│   ├── 12-nfr.md                  # 非功能需求
│   ├── 13-traceability-matrix.md  # 需求追溯矩阵
│   └── README.md                  # 需求文档索引 + v1.0 范围决策
├── 03-architecture/               # 架构文档
│   ├── 12-permissions-matrix.md   # 权限矩阵
│   ├── 13-project-master-plan.md  # 项目总账
│   ├── decisions/                 # ADR 架构决策记录
│   └── modules/                   # 模块架构规格
└── AGENTS.md                      # 开发规范
```

## 你的任务（按顺序执行）

### Phase 1：代码现状扫描

对每个模块，读取以下文件并记录实际状态：

1. **Controller** — 读 `src/Server/Services/LYBT.WebAPI/controllers/{Module}Controller.cs`，记录：
   - 实际端点列表
   - 实际授权策略（`[Authorize(Policy = ...)]`）
   - 实际调用链（MediatR vs 直接注入）

2. **Service/Handler** — 读 `src/Server/Modules/LYBT.Module.{Module}/Services/` 和 `Application/`，记录：
   - 哪些 Handler 是 trivial（纯透传）
   - 哪些 Handler 有实际业务逻辑

3. **Entity** — 读 `src/Shared/LYBT.Entities/{Module}/`，记录：
   - 实际字段
   - 实际状态枚举值

4. **Desktop View** — 读 `src/Client/Desktop/` 相关 View.xaml 和 ViewModel.cs，记录：
   - 实际 UI 功能
   - 是否有 TODO/NotSupportedException

5. **Test** — 运行 `dotnet test tests/LYBT.Tests.Desktop/ --no-build`，记录：
   - 失败测试数量和原因

### Phase 2：差距分析

对每个模块，对比：

| 维度 | 文档说的 | 代码实际 | 差距类型 |
|------|---------|---------|---------|
| US 状态 | ✅ 已实现 | 实际实现情况 | 状态不准确 |
| 权限策略 | 策略名称 | 实际 `[Authorize]` | 策略不一致 |
| 字段/枚举 | 文档定义 | 代码定义 | 定义不一致 |
| 功能完整性 | US 验收标准 | 实际功能 | 功能缺失 |

差距类型分类：
- **A. 代码已修复，文档未更新** — 更新文档
- **B. 文档正确，代码未实现** — 保持文档，标记代码问题
- **C. 文档和代码都有问题** — 需要用户确认
- **D. 文档描述过时** — 重写文档段落

### Phase 3：生成校准报告

输出一份报告，格式：

```markdown
# 文档校准报告

## 校准摘要
- 扫描模块数：X
- 发现差距数：X
- A 类（代码已修，文档未更新）：X 项
- B 类（文档正确，代码未实现）：X 项
- C 类（两者都有问题）：X 项
- D 类（文档过时）：X 项

## 逐模块差距清单

### 模块名
| # | 文档位置 | 文档说的 | 代码实际 | 差距类型 | 建议操作 |
|---|---------|---------|---------|---------|---------|
| 1 | 02-xxx.md:行号 | ... | ... | A/B/C/D | 更新/保持/确认/重写 |
```

### Phase 4：更新文档

根据校准报告，逐项更新文档：

1. **A 类** — 直接更新文档（代码已修复，文档需同步）
2. **B 类** — 在文档中标注"⚠️ 代码待实现"
3. **C 类** — 输出问题列表，等待用户确认
4. **D 类** — 重写过时段落

### Phase 5：提交

```bash
git add docs/
git commit -m "docs: documentation calibration — align docs with code reality"
```

## 关键检查点

### 权限矩阵（最容易过时）

检查每个 Controller 的 `[Authorize]` 属性，与文档中的权限矩阵对比：
- `src/Server/Services/LYBT.WebAPI/controllers/PatientsController.cs`
- `src/Server/Services/LYBT.WebAPI/controllers/RegistrationsController.cs`
- `src/Server/Services/LYBT.WebAPI/controllers/HerbsController.cs`
- `src/Server/Services/LYBT.WebAPI/controllers/FormulasController.cs`
- `src/Server/Services/LYBT.WebAPI/controllers/MedicalCasesController.cs`
- `src/Server/Services/LYBT.WebAPI/controllers/UsersController.cs`

### US 状态（最容易标错）

对每个标"✅ 已实现"的 US，验证：
1. 对应的 Controller 端点存在
2. 对应的 Service/Handler 存在且有实际逻辑（非空方法体）
3. 对应的 Desktop View 存在且功能完整
4. 无已知 Bug 阻塞

### 术语一致性

检查代码中的枚举值是否与 `03-glossary.md` 一致：
- `MedicalCaseStatus` 枚举值
- `RegistrationStatus` 枚举值
- `UserRole` 枚举值
- `FormulaValidationStatus` 枚举值

## 输出要求

1. 校准报告（markdown 格式）
2. 更新后的文档（原地修改，不新建文件）
3. Git commit

## 注意事项

- **不要新建文档** — 在原文档中更新
- **不要修改代码** — 只改文档
- **不确定的标记为 C 类** — 等用户确认
- **保留文档中的设计意图** — 即使代码未实现，也要保留设计说明，标注"⚠️ 待实现"

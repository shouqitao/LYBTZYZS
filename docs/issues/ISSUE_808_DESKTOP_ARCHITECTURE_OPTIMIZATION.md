# Issue #815: Desktop架构适度优化 - 降低复杂度提升可维护性

**GitHub Issue**: https://github.com/shouqitao/LYBTZYZS/issues/815

## 1. 问题背景

### 1.1 当前架构问题
基于深度代码分析和四份核心文档评估，Desktop项目存在以下关键问题：

**架构复杂度过高**：
- Core文件夹包含27个子文件夹，职责混杂违反单一职责原则
- 架构层次深度达5-6层，超出推荐的3-4层标准
- 缺少清晰的领域边界，业务逻辑散落各处

**代码质量问题**：
- 代码重复率高达40%，主要集中在HTTP客户端调用、ViewModel基类、数据验证逻辑
- 未充分利用Shared.Models.Contracts作为统一数据契约
- 缺少统一的API客户端封装，HTTP调用逻辑分散

**维护困难**：
- 依赖关系复杂，单元测试困难
- 缺少统一的状态管理和缓存策略
- 模块间通信机制不清晰

### 1.2 影响评估
- **开发效率**：新功能开发需要在多个文件夹间寻找相关代码
- **维护成本**：代码重复导致bug修复需要多处修改
- **团队协作**：复杂的项目结构增加新成员学习成本
- **架构健康度**：当前90/100，有提升空间

## 2. 解决方案

### 2.1 设计原则
基于项目**适度设计、拒绝过度工程**的核心理念：

✅ **允许的技术栈**：
- MVVM + Prism.DryIoc (保持现有)
- ReactiveUI (响应式编程)
- Refit + Polly (统一HTTP客户端)
- Microsoft.Extensions.* (DI、日志、配置)

❌ **禁止的技术栈**：
- CQRS/MediatR (过度工程)
- Redis (外部依赖复杂化)
- Docker/K8s (部署复杂化)
- GraphQL (API复杂化)

### 2.2 目标架构
采用**业务导向的四层清晰架构**：

```
src/Client/Desktop/
├── Core/                           # 基础设施层 (3个项目)
│   ├── LYBT.Desktop.Infrastructure   # 基础设施(主题、控件、工具)
│   ├── LYBT.Desktop.Services         # 服务层(API客户端、缓存、安全)
│   └── LYBT.Desktop.Models           # 模型层(ViewModels基类、映射器)
├── Modules/                        # 业务模块层 (8个业务模块)
│   ├── LYBT.Desktop.Auth
│   ├── LYBT.Desktop.Patients
│   ├── LYBT.Desktop.Prescriptions
│   ├── LYBT.Desktop.Herbs
│   ├── LYBT.Desktop.Formula
│   ├── LYBT.Desktop.Users
│   ├── LYBT.Desktop.Consultation
│   └── LYBT.Desktop.MedicalCase
├── Workstations/                   # 工作台层 (聚合层)
│   ├── LYBT.Desktop.ClinicalWorkstation
│   └── LYBT.Desktop.AdminWorkstation
└── Shell/                          # 启动层
    └── LYBT.Desktop.Shell
```

### 2.3 关键改进点

**统一数据契约**：
```csharp
// 正确使用Shared层DTOs
using LYBT.Shared.Models.Contracts.Patients;

public class PatientViewModel : ReactiveViewModel
{
    private PatientDto _patient;  // 使用Shared DTO
    public bool IsSelected { get; set; }  // 添加UI特定属性
    public ReactiveCommand SaveCommand { get; }
}
```

**统一API客户端**：
```csharp
// 基础HTTP客户端使用Refit+Polly
public interface IPatientApiClient
{
    [Get("/api/patients/{id}")]
    Task<PatientDto> GetPatientAsync(int id);

    [Post("/api/patients")]
    Task<PatientDto> CreatePatientAsync([Body] CreatePatientRequest request);
}
```

## 3. 实施计划

### 3.1 分阶段执行

**Phase 1: 基础设施重组** (2周)
- 创建Core层3个项目结构
- 迁移基础设施代码(主题、控件、工具类)
- 建立统一的API客户端基类
- **验收标准**：编译通过，基础功能正常

**Phase 2: 业务模块标准化** (3周)
- 重组8个业务模块，对齐Server端结构
- 统一使用Shared.Models.Contracts
- 标准化ViewModel模式，减少代码重复
- **验收标准**：代码重复率<20%，层次深度≤4

**Phase 3: 工作台层实现** (2周)
- 创建独立的Workstations层
- 实现模块聚合逻辑
- 优化启动和模块加载机制
- **验收标准**：功能完整，性能不下降

### 3.2 量化目标

| 指标 | 当前状态 | 目标状态 |
|------|----------|----------|
| 项目数量 | 27个文件夹(Core) | 14个项目(4层) |
| 代码重复率 | 40% | <20% |
| 架构层次深度 | 5-6层 | ≤4层 |
| 架构健康度 | 90/100 | 95/100 |
| 编译时间 | 基准 | 不超过基准+20% |
| 启动时间 | 基准 | 不超过基准+10% |

## 4. 验收标准

### 4.1 技术验收
- [ ] `dotnet build LYBT.All.sln -c Release` 编译通过
- [ ] 所有现有功能正常运行，无回归
- [ ] 单元测试覆盖率不低于当前水平
- [ ] 性能指标在可接受范围内

### 4.2 架构验收
- [ ] 项目依赖关系清晰，无循环依赖
- [ ] 代码重复率低于20%
- [ ] 层次深度不超过4层
- [ ] 统一使用Shared.Models.Contracts作为数据契约

### 4.3 文档验收
- [ ] 更新`docs/architecture/desktop-architecture.md`
- [ ] 创建迁移指南`docs/development/desktop-migration-guide.md`
- [ ] 更新`README.md`项目结构说明
- [ ] 在`docs/reports/INDEX.md`登记重构报告

## 5. 风险控制

### 5.1 技术风险
- **风险**：大规模代码迁移可能导致功能回归
- **控制**：使用Git feature分支，每个Phase单独验证

### 5.2 性能风险
- **风险**：重构过程中性能可能下降
- **控制**：设置性能基准线，每个Phase都进行性能测试

### 5.3 回滚方案
- 保留原始分支作为备份
- 每个Phase完成后创建milestone标签
- 如发现严重问题可快速回滚到上一个稳定状态

## 6. AI辅助自动化流程

### 6.1 开发流程
1. **Issue创建**：Claude使用sequential-thinking生成模块化清单
2. **实施阶段**：按清单逐项实现，实时更新状态
3. **代码审查**：Claude初审 + Serena二审
4. **文档同步**：自动更新相关文档和报告索引

### 6.2 质量门禁
- 每个Phase必须通过编译、测试、架构指标检查
- PR自动生成包含编译结果和验收状态
- 使用`Fixes #808`自动关联和关闭Issue

## 7. 分工与时间表

| 阶段 | 时间 | 负责人 | 主要任务 |
|------|------|---------|----------|
| Phase 1 | Week 1-2 | Claude Code | 基础设施重组 |
| Phase 2 | Week 3-5 | Claude Code | 业务模块标准化 |
| Phase 3 | Week 6-7 | Claude Code | 工作台层实现 |
| 总结 | Week 8 | Claude Code | 文档完善和报告 |

## 8. 相关文档

**分析依据**：
- `docs/reports/documentation-system-analysis.md` - 文档系统健康度84%
- `docs/optimization/desktop-architecture-optimization-plan.md` - 架构优化方案
- `docs/optimization/desktop-structure-optimization-plan.md` - 结构重组方案
- `CLAUDE.md` - 项目约束和工作流程

**输出文档**：
- `docs/architecture/desktop-architecture.md` - 新架构文档
- `docs/development/desktop-migration-guide.md` - 迁移指南
- `docs/reports/desktop-refactoring-summary.md` - 重构总结报告

---

**创建时间**：2025-09-29
**预计完成**：2025-11-17 (7周)
**优先级**：High
**标签**：`architecture`, `refactoring`, `desktop`, `phase-plan`
**里程碑**：Desktop架构优化完成
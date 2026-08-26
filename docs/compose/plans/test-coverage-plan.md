# 测试补全方案：需求驱动的全覆盖测试策略

> 目标：确保每个需求都有对应测试，未实现的功能能在测试阶段被发现。
> 版本: v1.0 | 日期: 2026-08-25

## 一、核心问题

当前测试体系的问题：**测试验证代码正确性，不验证需求完整性。**

| 维度 | 现状 | 应有 |
|------|------|------|
| API 端点 | 40+ 端点 | 每端点至少 1 个集成测试 |
| ViewModel | 27 个 | 每个核心 ViewModel 至少 1 个行为测试 |
| 业务场景 | 0 个端到端测试 | 每个核心流程至少 1 个场景测试 |
| 权限矩阵 | 文档定义 | 每个角色×操作至少 1 个授权测试 |

## 二、测试分层策略

### 2.1 三层金字塔（调整后）

```
        ╱ 业务场景测试  ╲         ← 验证「用户能完成任务」
       ╱─────────────────╲
      ╱   API 集成测试    ╲       ← 验证「端点行为正确」
     ╱─────────────────────╲
    ╱   ViewModel 行为测试   ╲     ← 验证「组件交互正确」
   ╱───────────────────────────╲
  ╱      现有单元测试（保持）     ╲   ← 验证「零件能转」
 ╱─────────────────────────────────╲
```

### 2.2 每层职责

| 层 | 验证什么 | 发现什么问题 | 工具 |
|----|---------|-------------|------|
| 单元测试 | 方法返回值、属性变更 | 逻辑错误、空引用 | xUnit + NSubstitute |
| ViewModel 行为测试 | 命令执行、状态流转、错误处理 | 交互遗漏、状态不一致 | xUnit + NSubstitute + WebApplicationFactory |
| API 集成测试 | 端点请求→响应→数据库 | 端点缺失、权限漏洞、数据错误 | WebApplicationFactory + InMemoryDB |
| 业务场景测试 | 完整用户故事 | 流程断裂、需求未实现 | WebApplicationFactory + InMemoryDB |

## 三、需求→测试映射矩阵

### 3.1 API 端点覆盖（40+ 端点）

| 模块 | 端点数 | 现有测试 | 缺口 | 优先级 |
|------|--------|---------|------|--------|
| Auth | 5 | 有 | 需补 auto-login、refresh | P0 |
| Users | 12 | 部分 | 需补 batch ops、reset-password | P0 |
| Patients | 11 | 部分 | 需补 batch-import、check-reference | P0 |
| Herbs | 11 | 部分 | 需补 batch ops、check-reference | P1 |
| Formulas | 9 | 部分 | 需补 validation status | P1 |
| MedicalCases | 10+ | 部分 | 需补 status transitions | P0 |
| Registrations | 8+ | 部分 | 需补 cancel、start-visit | P0 |
| Reports | 3 | 有 | 需补跨角色权限 | P1 |
| Configuration | 5 | 有 | 需补 sysadmin-only 验证 | P2 |

### 3.2 ViewModel 行为覆盖（27 个）

| 模块 | ViewModel | 现有测试 | 缺口 |
|------|-----------|---------|------|
| Auth | LoginViewModel | 有 | 需补自动登录流程 |
| Auth | ServerConfigViewModel | 有 | — |
| Users | UserMasterDetailViewModel | 有 | 需补角色变更限制 |
| Patients | PatientMasterDetailViewModel | 有 | 需补批量导入 |
| Patients | PatientEditorViewModel | 有 | 需补身份证校验 |
| MedicalCase | MedicalCaseMasterDetailViewModel | 有 | 需补状态机流转 |
| MedicalCase | ConsultationEditorViewModel | 无 | **需新建** |
| MedicalCase | PrescriptionEditorViewModel | 无 | **需新建** |
| MedicalCase | MedicalCaseCommandsViewModel | 无 | **需新建** |
| Catalog | HerbMasterDetailViewModel | 有 | 需补引用检查 |
| Catalog | FormulaMasterDetailViewModel | 有 | 需补验证状态 |
| Registrations | RegistrationListViewModel | 有 | 需补取消流程 |
| Registrations | RegistrationCreateDialogViewModel | 无 | **需新建** |

### 3.3 业务场景覆盖（核心流程）

| # | 场景 | 角色 | 涉及端点 | 现有测试 | 优先级 |
|---|------|------|---------|---------|--------|
| S01 | 患者首次就诊全流程 | Receptionist→Doctor | Patients→Registrations→MedicalCases→Prescriptions | 无 | P0 |
| S02 | 医案状态机流转 | Doctor | MedicalCases (Active→Suspended→Completed) | 无 | P0 |
| S03 | 处方开具与打印 | Doctor | MedicalCases→Prescriptions→Print | 无 | P0 |
| S04 | 用户权限隔离 | All | 各模块端点 | 无 | P0 |
| S05 | 药材引用检查 | Admin | Herbs→Formulas→MedicalCases | 无 | P1 |
| S06 | 验方创建与共享 | Doctor/Admin | Formulas | 无 | P1 |
| S07 | 批量操作 | Admin | batch-delete/enable/disable | 无 | P1 |
| S08 | 双模式切换 | SysAdmin | Configuration→ServerConfig | 无 | P2 |

## 四、实施计划

### Phase 1：API 集成测试补全（P0 端点）
- 目标：每个 P0 端点至少 1 个测试
- 工具：WebApplicationFactory + InMemoryDB
- 预估：40 个测试，2 天

### Phase 2：权限授权测试
- 目标：每个角色×操作组合至少 1 个测试
- 工具：WebApplicationFactory + 真实 JWT
- 预估：30 个测试，1.5 天

### Phase 3：ViewModel 行为测试补全
- 目标：每个无测试的 ViewModel 新建测试
- 工具：xUnit + NSubstitute
- 预估：15 个测试，1 天

### Phase 4：业务场景测试
- 目标：8 个核心场景端到端测试
- 工具：WebApplicationFactory + InMemoryDB
- 预估：8 个测试，2 天

### Phase 5：需求追溯矩阵自动化
- 目标：生成 requirements-traceability.md，自动检测未覆盖需求
- 工具：脚本扫描 Controller/ViewModel/Tests
- 预估：1 天

## 五、验收标准

1. **API 覆盖率**：100% 端点有至少 1 个集成测试
2. **权限覆盖率**：100% 角色×操作组合有授权测试
3. **场景覆盖率**：8 个核心业务场景全部通过
4. **需求追溯**：每个文档定义的需求都有对应测试
5. **构建质量**：全量测试 0 失败

## 六、文件结构

```
tests/
├── LYBT.Tests.Architecture/          # 保持不变（97 项）
├── LYBT.Tests.Integration/           # 新建：API 集成测试
│   ├── Auth/                         # 认证端点测试
│   ├── Users/                        # 用户管理端点测试
│   ├── Patients/                     # 患者管理端点测试
│   ├── Herbs/                        # 药材管理端点测试
│   ├── Formulas/                     # 验方管理端点测试
│   ├── MedicalCases/                 # 医案管理端点测试
│   ├── Registrations/                # 挂号管理端点测试
│   └── Permissions/                  # 权限授权测试
├── LYBT.Tests.Desktop/               # 保持并补全
│   └── Unit/
│       └── ViewModels/               # ViewModel 行为测试
└── LYBT.Tests.E2E/                   # 新建：业务场景测试
    └── Scenarios/                    # 核心业务流程测试
```

---

*本方案由 coder 设计，待用户评审后派发 pi 执行。*

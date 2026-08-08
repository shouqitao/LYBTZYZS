# 任务 A-17：P0 本地模式 CRUD 主干修复（补全 override）

> 依据：`structure-audit-crosscheck-2026-08-08.md` 交叉验证结论 C1（P0）
> 产品负责人已拍板：方案 A——补全本地 override，复用 Server Service，双轨真正对称

## 背景

本地模式（LocalWebAPI，端口 5300）下，Patients/Herbs/Formulas Controller 继承 `BaseCrudController`（virtual 方法默认 `throw new NotSupportedException`）但**未 override CRUD 主干** → 本地列表/创建/更新/删除返回 500/405。MedicalCases 缺列表/search/打印审计端点。

业务逻辑双端**真共享**（同一 `ISender` + `I*Service`，LocalWebAPI 引用 8 个 Server 模块），修复 = 对照远程 Controller 在本地补 override，复用同一 Service 调用。

## 修复清单（精确到端点）

### 1. LocalWebAPI/Controllers/PatientsController.cs
对照 `src/Server/Services/LYBT.WebAPI/Controllers/PatientsController.cs` 补：
- `override GetList`（[HttpGet]，`_patientService.GetPagedAsync(...)` 或等价查询）
- `override Create`（[HttpPost]，`ISender.Send(new CreatePatientCommand(...))` 或 Service 调用——**与远程实现方式一致**）
- `override Update`（[HttpPut("{id:guid}")]，同上）
- 已存在：GetById/Delete/ToggleStatus/Restore/GetByIdNumber/CheckReference/BatchCheckReference

### 2. LocalWebAPI/Controllers/HerbsController.cs
对照远程补：
- `override GetList` / `override Create` / `override Update` / `override Delete` / `override ToggleStatus`
- 已存在：GetById/BatchImport/CheckReference/BatchCheckReference/BatchEnable/BatchDisable
- 注：远程有 `POST {id}/restore`，本地缺——**核实本地是否有恢复需求**（Herbs 实体支持 Restore？远程有即补）

### 3. LocalWebAPI/Controllers/FormulasController.cs
对照远程补：
- `override GetList` / `override Create` / `override Update` / `override Delete` / `override ToggleStatus`
- 已存在：GetById/Restore/Clone/BatchImport/GetPendingValidation/ValidateHerb/BatchEnable/BatchDisable

### 4. LocalWebAPI/Controllers/MedicalCasesController.cs
对照远程 `BaseMedicalCasesController`（13+ virtual）补本地缺口：
- `override GetList`（列表分页）
- search / prescription-flag / print-completed / audit-logs / permissions / batch-delete（**逐一对照远程 BaseMedicalCasesController 端点清单**，本地客户端 `MedicalCasesHttpApiClient` 调了哪些就补哪些）
- 已存在：GetById/Query/GetByStatus/GetPending/Create/CloseCase/SuspendCase/CancelCase/UpdateStatus

### 5. URL 对齐（Mimo 发现 A11）
- `PatientsHttpApiClient.cs:39` 调 `POST /patients/import`，远程端点是 `POST /patients/batch-import` —— **两端对齐**（改客户端或改端点，选择与远程一致 `batch-import`）

## 硬性约束

1. **复用 Server Service/Handler**：本地 override 实现必须与远程 Controller 相同的调用链（`ISender.Send(...)` 或 `I*Service` 方法），**禁止**在本地重写业务逻辑
2. 路由特性与远程一致（`api/v1/[controller]` 前缀已由控制器路由模板保证）
3. 权限策略与远程一致（`[Authorize(...)]`，参照远程）
4. **不引入新机制**：不改基类、不加中间层
5. 每处修改对照远程实现「抄」——先读远程 Controller 对应方法，再写本地

## 验证

1. `dotnet build LYBTZYZS.sln --no-incremental`：0 错误 0 警告
2. `dotnet test tests/LYBT.Tests.Architecture/`：83/83
3. 本地模式冒烟（如环境允许）：启动 LocalWebAPI → 患者列表/新建/更新/删除 → 药材列表/新建 → 验方列表/新建 → 医案列表——验证不再 500/405
4. `git add` 具体文件 + `git commit`（英文 `fix(localapi): ...`）+ `git push origin master`

## 不做（防发散）

- ❌ 不改远程 Controller（除非 URL 对齐需要）
- ❌ 不做契约双套统一/映射收敛（那是 A-18 批次）
- ❌ 不重构本地 Controller 结构

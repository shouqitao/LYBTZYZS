# 2025-09-24 server-phase1-query-layer-refactor-task

## 任务背景
- Serena 架构分析指出多个 QueryService 直接注入 `AppDbContext`（Consultation、Prescription、Users 等），违反 Server 层分层原则，也导致后续测试难以 Mock。
- 在开始大规模测试和 CQRS 架构改造前，需要先完成查询层的基本重构，恢复“Controller → Service → Repository”的路径，并收敛仓储接口重复问题。

## 任务目标
1. 为各业务模块建立只读仓储接口/实现（命名约定：`I<Module>ReadRepository`）。
2. 调整所有 QueryService 通过只读仓储访问数据；禁止直接注入 `AppDbContext`。
3. 收敛仓储接口：保留唯一的 `IRepository<T>` 或 `IBaseRepository<T>`，移除重复定义，保证所有仓储实现继承同一接口体系。
4. 更新依赖注入注册，确保 Controller / Service / Repository 依赖链正确。
5. 同步修正 / 新增单元测试：可通过 Mock 仓储验证 QueryService 行为。

## 工作项
1. **只读仓储接口/实现**
   - Consultation、Prescription、Users、MedicalCase、Patients 等模块逐一创建 ReadRepository（可按模块分 PR/commit）。
   - 复用 AutoMapper `ProjectTo<>`；按原 QueryService 搜索条件实现分页、筛选、排序。

2. **QueryService 改造**
   - 移除对 `AppDbContext` 的注入（包括构造函数、字段、EF 查询代码）。
   - 注入对应的 ReadRepository；保留原返回 DTO 与业务逻辑。

3. **仓储接口统一**
   - 与架构组确认统一接口命名；建议保留 `IRepository<T>`（包含基础 CRUD、`SaveChangesAsync` 等），其他接口迁移后删除。
   - 调整依赖注入（`services.AddScoped<IRepository<User>, UserRepository>()` 等），确保所有写仓储沿用统一接口。

4. **测试调整与补充**
   - 将现有 QueryService 单元测试改为 Mock ReadRepository；新增必要的正向/筛选/分页测试。
   - 受影响模块的 API 集成测试（若有）需确认仍通过。

## 验收标准
- [ ] `src/Server/Modules` 内无 QueryService 直接注入 `AppDbContext`。
- [ ] 所有 QueryService 通过 ReadRepository 获取数据；ReadRepository 内部负责 EF 查询与映射。
- [ ] 仓储接口实现统一；`IRepository<T>`/`IBaseRepository<T>` 重复问题解决。
- [ ] 单元测试更新覆盖重构后的 QueryService。
- [ ] `dotnet build LYBT.Server.sln -c Release` 与相关测试全部通过。

## 风险与提示
- 分阶段提交（模块拆分）有助于降低冲突；建议每个模块独立分支/PR。
- 注意 QueryService 的业务逻辑（排序、筛选条件），确保迁移到 ReadRepository 后结果一致。
- 若某模块暂未编写 QueryService，可先记录 TODO，不影响当前阶段验收。

---
文件：docs/tasks/pending/2025-09-24-server-phase1-query-layer-refactor-task.md
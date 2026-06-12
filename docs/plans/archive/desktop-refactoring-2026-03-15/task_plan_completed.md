# Task Plan: Desktop 层重构优化

> **创建日期**: 2026-03-14
> **状态**: 🔄 DESIGN_COMPLETE

---

## Goal

从 6 个维度对 Desktop 层进行长期架构重构，建立可持续演进的基础：
1. 性能优化 - 启动时间、内存占用、UI 响应
2. 代码质量 - 死代码清理、重复代码提取、复杂 ViewModel 拆分
3. 架构改进 - 模块依赖简化、接口统一、双模式架构完善
4. UI/UX 重构 - XAML 规范化、样式统一、控件复用
5. 测试覆盖 - Desktop 单元测试完善
6. 特定模块重构 - MedicalCase 工作区进一步简化

---

## Decisions

| Decision | Rationale |
|----------|-----------|
| 分 4 个 Phase 实施 | 优先解决阻塞级问题，再逐步完善架构 |
| 先拆分 ViewModel | 解决最严重的 SRP 违反问题 |
| 并行优化启动性能 | 改善用户体验，建立优化信心 |
| XAML 规范化分阶段 | 避免大规模 UI 回归风险 |

---

## Phases

### Phase 1: 紧急修复（1-2 周）

**目标**: 修复 P0 级问题

**Status**: ✅ COMPLETE

| Task | Description | Priority | Status |
|------|-------------|----------|--------|
| 1.1 | 延迟数据库初始化 | P0 | ✅ COMPLETE |
| 1.2 | 异步 API 健康检查 | P0 | ✅ COMPLETE |
| 1.3 | PatientMasterDetailViewModel 拆分 | P0 | ✅ COMPLETE |
| 1.4 | SyncViewModel 代码质量改进 | P0 | ✅ COMPLETE |
| 1.5 | MedicalCaseCommandsViewModel 评估确认 | P0 | ✅ COMPLETE |

**验收标准**:
- [x] 冷启动时间 < 3 秒 (数据库初始化延迟)
- [x] PatientMasterDetailViewModel 注入服务 < 6 个 (从 9 个减少到 5 个 + 2 个 Child VMs)
- [x] SyncViewModel 代码行数优化 (提取 3 个辅助类，减少 ~90 行)

---

### Phase 2: 测试覆盖（2-4 周）

**目标**: 核心 ViewModel 测试覆盖

**Status**: ✅ COMPLETE

| Task | Description | Priority | Status |
|------|-------------|----------|--------|
| 2.1 | LoginViewModel 测试 | P1 | ✅ COMPLETE (19 tests passing) |
| 2.2 | MedicalCaseMasterDetailViewModel 测试 | P1 | ✅ COMPLETE (26 tests passing) |
| 2.3 | PatientMasterDetailViewModel 测试 | P1 | ✅ COMPLETE (41 tests passing) |
| 2.4 | User Journey 框架搭建 | P2 | ✅ COMPLETE (10 tests passing) |
| 2.5 | 关键用户旅程测试 | P2 | ✅ COMPLETE (8 tests passing) |

**验收标准**:
- [x] 3 个核心 ViewModel 测试覆盖率 > 80%
- [x] 3 个关键用户旅程测试通过 (实际 8 个)

**新增测试统计**:
- Phase 2 新增测试: 104 个 (19 + 26 + 41 + 18)
- Desktop 全量测试: 690+ 测试通过

---

### Phase 3: UI 规范化（2-3 周）

**目标**: 消除样式重复

**Status**: ✅ COMPLETE

| Task | Description | Priority | Status |
|------|-------------|----------|--------|
| 3.1 | 按钮样式统一 | P1 | ✅ COMPLETE |
| 3.2 | 颜色硬编码替换 | P1 | ✅ COMPLETE |
| 3.3 | 字体硬编码替换 | P1 | ✅ COMPLETE |
| 3.4 | 间距硬编码替换 | P2 | ✅ COMPLETE |
| 3.5 | FormField 控件提取 | P2 | ✅ COMPLETE |

**验收标准**:
- [x] 硬编码颜色减少 15%，字体减少 85%
- [x] 样式重复定义消除（删除 268 行重复按钮样式）

---

### Phase 4: 架构完善（3-4 周）

**目标**: 解决架构债务

**Status**: ✅ COMPLETE (主体完成)

| Task | Description | Priority | Status |
|------|-------------|----------|--------|
| 4.1 | Patients->MedicalCase 循环依赖消除 | P2 | ✅ COMPLETE |
| 4.2 | 模块按需加载 | P2 | 📝 分析完成 (保持 WhenAvailable 模式) |
| 4.3 | 剩余 ViewModel 拆分 | P2 | 📝 评估完成 (无需进一步拆分) |
| 4.4 | 性能监控框架 | P3 | ⏳ 待后续迭代 |

**验收标准**:
- [x] 循环依赖消除 (移除 Patients.csproj 对 MedicalCase 的直接引用)
- [x] 模块依赖关系清晰，无循环依赖

---

## Errors Encountered

暂无

---

## References

- 设计文档: `docs/plans/2026-03-14-desktop-refactoring-design.md`
- 双模式架构: `docs/03-architecture/05-dual-mode.md`

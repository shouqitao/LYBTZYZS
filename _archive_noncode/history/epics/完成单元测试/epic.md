# Epic: 完成单元测试覆盖率提升到60%

github: 482
url: https://github.com/shouqitao/LYBTZYZS/issues/482
updated: 2025-09-03T22:01:12Z
progress: 15

## Overview

系统化提升LYBTZYZS项目的单元测试覆盖率，从当前2.76%提升到60%，建立完整的测试体系。

## Objectives

- [ ] 完成8个核心业务模块的Service层单元测试
- [ ] 完成Repository层的数据访问测试
- [ ] 建立集成测试框架
- [ ] 配置CI/CD自动化测试

## Tasks

### Phase 1: Service层测试 (Priority: High)

- [ ] Auth模块Service层测试
- [ ] Users模块Service层测试
- [ ] Patients模块Service层测试
- [ ] MedicalCase模块Service层测试
- [ ] Consultation模块Service层测试
- [ ] Prescriptions模块Service层测试
- [ ] Herbs模块Service层测试
- [ ] Formula模块Service层测试

### Phase 2: Repository层测试 (Priority: High)

- [ ] UserRepository测试完善
- [ ] PatientRepository测试完善
- [ ] HerbRepository测试完善
- [ ] 其他Repository测试

### Phase 3: 集成测试 (Priority: Medium)

- [ ] API端到端测试
- [ ] 业务流程集成测试
- [ ] 数据库集成测试

### Phase 4: 测试基础设施 (Priority: Medium)

- [ ] 测试数据工厂建立
- [ ] Mock服务配置
- [ ] 测试辅助工具类
- [ ] CI/CD集成

## Acceptance Criteria

- 测试覆盖率达到60%
- 所有测试稳定通过率>99%
- CI/CD管道自动运行测试
- 测试执行时间<5分钟

## Technical Approach

- xUnit测试框架
- Moq模拟框架
- Bogus测试数据生成
- EF Core InMemory数据库
- 测试金字塔原则 (70%单元测试, 20%集成测试, 10%E2E测试)

## Dependencies

- 现有测试项目结构
- 业务逻辑稳定性
- CI/CD环境配置

## Risks

- 复杂业务逻辑测试难度高
- 测试数据准备耗时
- 维护成本增加

## Timeline

- 预计完成时间: 4周
- Phase 1: 2周
- Phase 2: 1周
- Phase 3: 0.5周
- Phase 4: 0.5周

## 执行策略（本 Epic 专属）

- Epic 目标：整体覆盖率 ≥60%，Auth/Users 等关键模块 ≥80%。
- 自动展开：当对本 Epic 执行 `/pm:issue-start 482` 时，请自动启动全部未完成的子任务（#483、#484、#485…），遵循 `CLAUDE.md` 的并行与 DoD 规则。
- 工具优先级：
  1) Serena MCP 用于代码符号级检索/重构/定位；
  2) CCPM `/pm:*` 命令用于任务编排与进度同步；
  3) 如需生成/修改文档，写入 `.claude/epics/完成单元测试/updates/`。
- 提交约定：所有提交以 `feat(test): ... (#<issue-id>)`、`fix(test): ... (#<issue-id>)` 格式引用 Issue。
- 关闭前检查清单：
  - [ ] 任务描述对应的功能/测试均实现并通过
  - [ ] 覆盖率阈值满足（总≥60%，关键模块≥80%）
  - [ ] 重要路径/异常用例具备
  - [ ] CI/本地测试通过，Issue 已 `/pm:issue-sync`

## Status

**Current**: Planning
**Progress**: 0%
**Blocked**: No
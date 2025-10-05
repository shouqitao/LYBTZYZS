# Workflow 文件分析报告 - Issue #933

**分析日期**: 2025-10-05
**分析目标**: 将现有 22 个 workflow 文件合并为 ≤8 个高效workflow

---

## 1. 现有 Workflow 文件清单与分类

### 1.1 CI 相关（8个）
| 文件名 | 主要功能 | 触发条件 | 问题/重叠 |
|--------|---------|---------|-----------|
| **ci.yml** | Pass 7 治理基线强制门禁 | push/PR (main/master/develop) | ✅ 核心 CI，4级门禁完整 |
| **backend-ci.yml** | 后端持续集成 | push/PR，路径过滤 | ⚠️ 与 ci.yml 重复：构建+测试 |
| **ci-backend.yml** | Backend CI Pipeline | push (main/develop/release/*) | ❌ **严重违规**：包含 Docker 构建（181-244行）<br>⚠️ 与其他 CI 重复 |
| **ci-frontend.yml** | Frontend CI Pipeline | push/PR，路径过滤 (Frontend) | ✅ WPF 专用，可保留 |
| **test.yml** | 测试套件 | push/PR，路径过滤 | ⚠️ 与 ci.yml 重复：单元测试执行 |
| **coverage-check.yml** | 测试覆盖率检查 | push/PR | ⚠️ 与 ci.yml Level 2.5 重复<br>⚠️ 阈值冲突：90%/80%/95% vs ci.yml 70% |
| **test-coverage.yml** | P3 测试优化与覆盖率 | push/PR | ⚠️ 与上述2个覆盖率检查重复<br>⚠️ 阈值：70% |
| **all-solution-build.yml** | （未读取） | - | - |

### 1.2 安全相关（1个）
| 文件名 | 主要功能 | 触发条件 | 问题/重叠 |
|--------|---------|---------|-----------|
| **security-scan.yml** | 安全扫描流水线 | cron/push/PR/manual | ❌ **严重违规**：包含 Docker 容器扫描（97-133行）<br>✅ 功能完整：代码/依赖/容器/基础设施/SAST/许可证 |

### 1.3 部署相关（1个）
| 文件名 | 主要功能 | 触发条件 | 问题/重叠 |
|--------|---------|---------|-----------|
| **cd-deploy.yml** | CD 部署流水线 | workflow_dispatch/tag push | ❌ **严重违规**：整个文件基于 Kubernetes + Docker<br>⚠️ 包含：开发/测试/生产环境部署、蓝绿部署、健康检查 |

### 1.4 治理相关（1个）
| 文件名 | 主要功能 | 触发条件 | 问题/重叠 |
|--------|---------|---------|-----------|
| **governance.yml** | Pass 3 - Governance & Architectural Compliance | push/PR/manual | ✅ 核心治理检查<br>⚠️ 与 ci.yml 部分重复（格式检查、架构测试） |

### 1.5 文档相关（1个）
| 文件名 | 主要功能 | 触发条件 | 问题/重叠 |
|--------|---------|---------|-----------|
| **docs-normalization.yml** | 文档规范化检查 | PR (路径过滤 *.md, *.ps1) | ✅ 独立功能，可保留 |

### 1.6 Issue/PR 自动化（10个 - 未全部读取）
| 文件名 | 推测功能 | 问题/重叠 |
|--------|---------|-----------|
| **auto-checklist.yml** | Issue 清单自动化 | ⚠️ 可能可合并 |
| **auto-close-linked-issues.yml** | 关联 Issue 自动关闭 | ⚠️ 可能可合并 |
| **issue-autocomplete.yml** | Issue 自动补全 | ⚠️ 可能可合并 |
| **issue-triage.yml** | Issue 分类 | ⚠️ 可能可合并 |
| **pr-docs-naming-check.yml** | PR 文档命名检查 | ⚠️ 可能可合并到 docs |
| **pr-issue-sync.yml** | PR/Issue 同步 | ⚠️ 可能可合并 |
| **validate-and-track.yml** | 验证与跟踪 | ⚠️ 可能可合并 |
| **claude.yml** | Claude 相关 | - |
| **claude-code-review.yml** | Claude 代码审查 | - |
| **release.yml** | 版本发布 | - |

---

## 2. 重大问题与违规识别

### 2.1 ❌ 严重违反项目技术标准（根据 docs/PROJECT-STATUS-2025-09-27.md）

**违规文件**：
1. **ci-backend.yml** (line 181-244)
   - 包含完整 Docker 镜像构建流程
   - 使用 docker/setup-buildx-action、docker/login-action、docker/build-push-action
   - 推送到 Docker Hub

2. **security-scan.yml** (line 97-133)
   - 容器镜像安全扫描 job
   - 使用 Docker 构建镜像
   - 使用 Trivy 和 Snyk 扫描 Docker 镜像

3. **cd-deploy.yml** (整个文件)
   - 所有部署基于 Kubernetes + Docker
   - development 环境：kubectl 部署到 Kubernetes (line 133-157)
   - staging 环境：Azure Container Instances (line 175-223)
   - production 环境：Kubernetes 蓝绿部署 (line 281-340)

**项目黑名单**（来自 docs/PROJECT-STATUS-2025-09-27.md）：
- ❌ Docker/容器化
- ❌ Kubernetes
- ❌ 微服务
- ❌ Redis

**结论**：这些 workflow 文件必须完全重写或删除，不能保留任何 Docker/Kubernetes 相关内容。

### 2.2 ⚠️ 重复功能矩阵

#### 构建与编译
| 功能 | ci.yml | backend-ci.yml | ci-backend.yml | test.yml | governance.yml |
|------|--------|---------------|---------------|----------|---------------|
| dotnet restore | ✅ | ✅ | ✅ | ✅ | ✅ |
| dotnet build | ✅ | ✅ | ✅ | ✅ | ✅ |
| 编译检查（零错误零警告） | ✅ | ❌ | ✅ | ❌ | ✅ (兼容CCPM) |

**重复度**: 5个 workflow 都执行相同的 restore + build 操作

#### 单元测试
| 功能 | ci.yml | backend-ci.yml | ci-backend.yml | test.yml |
|------|--------|---------------|---------------|----------|
| 模块级单元测试 | ✅ (8个模块) | ✅ (部分模块) | ✅ (矩阵策略) | ✅ (矩阵策略) |
| 架构测试 | ✅ | ❌ | ❌ | ❌ |
| 并行测试 | ❌ (禁用) | ❌ (禁用) | ❌ (禁用) | ❌ (禁用) |

**重复度**: 4个 workflow 都执行单元测试，且都禁用并行（因为已知的并行问题）

#### 覆盖率检查
| 功能 | ci.yml | coverage-check.yml | test-coverage.yml |
|------|--------|-------------------|------------------|
| 覆盖率收集 | ✅ (Coverlet) | ✅ (Coverlet) | ✅ (Coverlet) |
| ReportGenerator | ✅ | ✅ | ✅ |
| 行覆盖率阈值 | 70% | 90% | 70% |
| 分支覆盖率阈值 | - | 80% | 70% |
| 关键模块阈值 | - | 95% | - |
| 门禁检查 | ✅ 阻塞 | ✅ 阻塞 | ✅ 阻塞 |

**重复度**: 3个 workflow 都执行覆盖率检查，且阈值不一致！
**问题**: 实际覆盖率 0.5%，但所有门禁都要求 ≥70%，必然失败

#### 代码格式检查
| 功能 | ci.yml | backend-ci.yml | ci-backend.yml | ci-frontend.yml | governance.yml |
|------|--------|---------------|---------------|----------------|---------------|
| dotnet format | ✅ | ✅ (continue-on-error) | ✅ | ✅ | ✅ (阻塞) |
| --verify-no-changes | ✅ | ✅ | ✅ | ✅ | ✅ |

**重复度**: 5个 workflow 都执行 dotnet format

#### 安全扫描
| 功能 | ci-backend.yml | security-scan.yml |
|------|---------------|------------------|
| GitLeaks 扫描 | ❌ | ✅ |
| Security Code Scan | ❌ | ✅ |
| NuGet 包漏洞扫描 | ✅ (简单) | ✅ (详细) |
| OWASP 依赖检查 | ❌ | ✅ |
| CodeQL 分析 | ❌ | ✅ |
| Semgrep 扫描 | ❌ | ✅ |

**重复度**: 低，但 security-scan.yml 更全面

---

## 3. 覆盖率阈值冲突问题

### 3.1 现状
| Workflow | 行覆盖率阈值 | 分支覆盖率阈值 | 关键模块阈值 |
|----------|------------|--------------|------------|
| ci.yml | 70% | - | - |
| coverage-check.yml | 90% | 80% | 95% |
| test-coverage.yml | 70% | 70% | - |
| **实际覆盖率** | **0.5%** | **0.5%** | **0.5%** |

### 3.2 问题分析
- **3个不同的覆盖率门禁**，阈值不一致（70% vs 90%）
- **所有门禁都会失败**（实际 0.5% vs 要求 70%+）
- **CI 必然无法通过**

### 3.3 Issue #933 的渐进式策略
根据 Issue #933 的规划，应采用渐进式覆盖率策略：
- Week 1-2: 30% (Phase 1: 核心模块)
- Week 3-4: 50% (Phase 2: 所有模块)
- Week 5-6: 70% (Phase 3: 完整覆盖)
- Week 7+: 新代码 80%

---

## 4. 推荐的新 Workflow 结构（≤8个）

### 4.1 核心 CI/CD Workflows (5个)

#### 1️⃣ **ci-main.yml** - 主 CI 流程（合并: ci.yml + backend-ci.yml 部分）
**触发**: push/PR to main/master/develop
**职责**:
- ✅ 代码格式检查 (dotnet format --verify-no-changes)
- ✅ 编译检查 (零错误零警告)
- ✅ 单元测试 (8个模块 + 架构测试)
- ✅ 基本覆盖率收集（不阻塞，仅报告）

**来源合并**:
- ci.yml (Level 1, Level 2)
- backend-ci.yml (build-and-test job)
- governance.yml (format + build 部分)

#### 2️⃣ **ci-integration.yml** - 集成测试流程（合并: test.yml 部分）
**触发**: push/PR to main/master/develop
**职责**:
- ✅ WebAPI 集成测试 (InMemory DB)
- ✅ 跨模块集成测试
- ✅ E2E 测试（如果存在）

**来源合并**:
- test.yml (integration-tests job)

#### 3️⃣ **ci-coverage.yml** - 覆盖率检查（合并: ci.yml Level 2.5 + coverage-check.yml + test-coverage.yml）
**触发**: push/PR to main/master/develop
**职责**:
- ✅ 收集所有测试覆盖率
- ✅ 生成覆盖率报告（HTML + JSON + Badges）
- ✅ **渐进式阈值门禁**（Phase 1: 30%, Phase 2: 50%, Phase 3: 70%）
- ✅ PR 覆盖率评论
- ✅ Codecov 集成

**特性**:
- 使用环境变量或 workflow_dispatch 输入控制当前阶段阈值
- 关键模块优先检查

#### 4️⃣ **ci-quality.yml** - 代码质量门禁（合并: ci.yml Level 3 + governance.yml）
**触发**: push/PR to main/master/develop
**职责**:
- ✅ 架构合规检查 (ArchTests)
- ✅ 层间依赖验证
- ✅ API 版本控制合规
- ✅ 命名规范检查
- ✅ 禁止框架检查 (CQRS/MediatR/Redis 等)
- ✅ Record-Only 功能模式验证
- ✅ 治理规则文件验证 (.ai/rules.json, _governance/architecture.md)

**来源合并**:
- ci.yml (Level 3: architecture-compliance-gate)
- governance.yml (所有 jobs)

#### 5️⃣ **ci-security.yml** - 安全扫描（重写: security-scan.yml，移除违规内容）
**触发**: cron (每天凌晨2点) / push (main/develop) / PR (main) / manual
**职责**:
- ✅ GitLeaks 密钥泄露扫描
- ✅ Security Code Scan
- ✅ NuGet 包漏洞扫描
- ✅ OWASP 依赖检查
- ✅ CodeQL 静态分析
- ✅ Semgrep 扫描
- ❌ **移除**: Docker 容器扫描（违规）
- ❌ **移除**: Kubernetes 配置扫描（违规）
- ❌ **移除**: Docker Compose 检查（违规）
- ✅ 许可证合规检查

**修改**:
- 删除 container-scan job (97-133行)
- 删除 infrastructure-scan job 中 Docker Compose 部分 (158-178行)

### 4.2 专用 Workflows (3个)

#### 6️⃣ **ci-frontend.yml** - 前端 CI（保留，轻度优化）
**触发**: push/PR，路径过滤 (src/Frontend/**)
**职责**:
- ✅ WPF 客户端构建
- ✅ XAML 语法检查
- ✅ WPF 单元测试
- ✅ 代码签名（生产环境）
- ✅ 安装包制作 (Inno Setup)
- ✅ GitHub Release 创建

**优化**:
- 添加路径过滤，仅在前端代码变更时触发
- 优化构建缓存

#### 7️⃣ **docs-sync.yml** - 文档同步与规范化（保留: docs-normalization.yml）
**触发**: PR (路径过滤 **/*.md, scripts/*.ps1)
**职责**:
- ✅ 中文标题/术语规范化
- ✅ 模块 README 结构检查
- ✅ API/Refs 自动填充
- ✅ 差异检测与失败

**当前名称**: docs-normalization.yml
**建议**: 重命名为 docs-sync.yml 保持一致性

#### 8️⃣ **governance-automation.yml** - 治理自动化（合并: 10个 Issue/PR 自动化 workflows）
**触发**: issues/PR 事件
**职责**:
- ✅ Issue 清单自动生成
- ✅ Issue 分类与标签
- ✅ PR/Issue 同步
- ✅ 关联 Issue 自动关闭
- ✅ PR 文档/命名检查
- ✅ 验证与跟踪

**来源合并**:
- auto-checklist.yml
- auto-close-linked-issues.yml
- issue-autocomplete.yml
- issue-triage.yml
- pr-docs-naming-check.yml
- pr-issue-sync.yml
- validate-and-track.yml

**优化**:
- 使用条件 jobs 根据事件类型执行不同操作
- 统一使用 actions/github-script 处理 API 调用

### 4.3 待重写/删除

#### ❌ **cd-deploy.yml** - 完全重写或暂时删除
**原因**: 整个文件基于 Kubernetes + Docker，严重违反项目标准

**选项**:
1. **暂时删除**: 当前项目没有生产部署需求，可以完全删除
2. **重写为简单部署**:
   - 使用 `dotnet publish` 生成独立可执行文件
   - 使用 FTP/SFTP 上传到 Windows Server
   - 使用 Windows Service/IIS 托管
   - 使用 PowerShell Remoting 执行部署脚本

**推荐**: 暂时删除，等待实际部署需求明确后再根据 MVP 约束重新设计

#### ❌ **ci-backend.yml** - 删除
**原因**: 包含 Docker 构建，且功能与 ci-main.yml 完全重复

#### ❌ **test.yml** - 删除
**原因**: 功能被 ci-main.yml 和 ci-integration.yml 完全覆盖

#### ❌ **coverage-check.yml** - 删除
**原因**: 功能被新的 ci-coverage.yml 覆盖，且阈值设置不合理（90%/80%/95%）

#### ❌ **test-coverage.yml** - 删除
**原因**: 功能被新的 ci-coverage.yml 覆盖

#### ❌ **backend-ci.yml** - 删除
**原因**: 功能被 ci-main.yml 覆盖

---

## 5. 最终 Workflow 结构总结

### ✅ 保留并合并为 8 个 Workflows

| # | 新文件名 | 来源 | 行动 | 优先级 |
|---|---------|------|------|--------|
| 1 | **ci-main.yml** | ci.yml + backend-ci.yml 部分 + governance.yml 部分 | 合并 | P0 |
| 2 | **ci-integration.yml** | test.yml (integration 部分) | 提取 | P1 |
| 3 | **ci-coverage.yml** | ci.yml L2.5 + coverage-check.yml + test-coverage.yml | 合并+重写 | P0 |
| 4 | **ci-quality.yml** | ci.yml L3 + governance.yml | 合并 | P0 |
| 5 | **ci-security.yml** | security-scan.yml | 重写（移除违规） | P1 |
| 6 | **ci-frontend.yml** | ci-frontend.yml | 保留+优化 | P2 |
| 7 | **docs-sync.yml** | docs-normalization.yml | 重命名 | P2 |
| 8 | **governance-automation.yml** | 10个 Issue/PR 自动化 workflows | 合并 | P2 |

### ❌ 删除的 Workflows (14个)

| 文件名 | 原因 |
|--------|------|
| backend-ci.yml | 功能重复 |
| ci-backend.yml | 包含 Docker 违规 + 功能重复 |
| test.yml | 功能重复 |
| coverage-check.yml | 功能重复 + 阈值不合理 |
| test-coverage.yml | 功能重复 |
| cd-deploy.yml | 整个文件基于 K8s/Docker 违规 |
| ci.yml | 合并到 ci-main.yml |
| governance.yml | 合并到 ci-quality.yml |
| auto-checklist.yml | 合并到 governance-automation.yml |
| auto-close-linked-issues.yml | 合并到 governance-automation.yml |
| issue-autocomplete.yml | 合并到 governance-automation.yml |
| issue-triage.yml | 合并到 governance-automation.yml |
| pr-docs-naming-check.yml | 合并到 governance-automation.yml |
| pr-issue-sync.yml | 合并到 governance-automation.yml |

**待分析**: validate-and-track.yml, claude.yml, claude-code-review.yml, release.yml, all-solution-build.yml (可能保留或合并)

---

## 6. 实施计划

### Phase 1: 核心 CI 合并（P0）
**目标**: 22 workflows → 8 workflows
**文件**: ci-main.yml, ci-coverage.yml, ci-quality.yml

**步骤**:
1. 创建 ci-main.yml（合并格式检查、编译、单元测试）
2. 创建 ci-coverage.yml（渐进式覆盖率门禁，初始阈值 30%）
3. 创建 ci-quality.yml（架构合规 + 治理检查）
4. 删除被合并的旧文件（ci.yml, backend-ci.yml, test.yml, coverage-check.yml, test-coverage.yml, governance.yml）
5. 测试新 CI 流程

### Phase 2: 安全与专用流程（P1）
**文件**: ci-integration.yml, ci-security.yml

**步骤**:
1. 提取 test.yml 的集成测试部分创建 ci-integration.yml
2. 重写 security-scan.yml 移除违规内容创建 ci-security.yml
3. 删除旧文件

### Phase 3: 前端与自动化（P2）
**文件**: ci-frontend.yml, docs-sync.yml, governance-automation.yml

**步骤**:
1. 优化 ci-frontend.yml
2. 重命名 docs-normalization.yml 为 docs-sync.yml
3. 合并 10个 Issue/PR 自动化 workflows 为 governance-automation.yml
4. 删除旧文件

### Phase 4: 部署流程（待定）
**文件**: cd-deploy.yml（重写或删除）

**决策点**:
- 如果近期无生产部署需求 → 删除
- 如果有部署需求 → 重写为符合 MVP 约束的简单部署（Windows Server + IIS/Service）

---

## 7. 风险与注意事项

### 7.1 ⚠️ 覆盖率门禁过渡
- **当前**: ci.yml 强制 70% 覆盖率，**必然失败**
- **过渡方案**:
  - Week 1-2: 降低到 30%（ci-coverage.yml 初始阈值）
  - Week 3-4: 提升到 50%
  - Week 5-6: 达到 70%

### 7.2 ⚠️ 违规内容清理
- 必须删除所有 Docker/Kubernetes/容器化相关代码
- 必须更新任何引用这些技术的文档
- 必须确保部署流程符合 MVP 约束

### 7.3 ⚠️ Issue/PR 自动化合并
- 需要仔细测试合并后的 governance-automation.yml
- 确保所有现有自动化功能不丢失
- 可能需要分阶段合并（先合并5个，再合并剩余5个）

### 7.4 ⚠️ 前端 CI 独立性
- ci-frontend.yml 应保持独立，仅在前端代码变更时触发
- 不要与后端 CI 合并，避免不必要的构建

---

## 8. 成功标准

✅ **最终结构**: 22 workflows → ≤8 workflows
✅ **无违规内容**: 所有 Docker/Kubernetes/容器化代码已移除
✅ **无功能丢失**: 所有必要的检查和测试都保留
✅ **CI 可通过**: 覆盖率阈值调整为现实可达的 30%（Phase 1）
✅ **执行效率**: 减少重复执行，优化触发条件
✅ **可维护性**: 清晰的职责划分，易于理解和修改
✅ **符合 MVP**: 所有流程符合项目技术决策和 MVP 约束

---

**报告生成**: 2025-10-05
**分析者**: Claude Code (Issue #933 - Testing & CI Refactoring Epic)
**相关文档**:
- Issue #933: https://github.com/shouqitao/LYBTZYZS/issues/933
- docs/PROJECT-STATUS-2025-09-27.md
- docs/development/minimal-practice.md

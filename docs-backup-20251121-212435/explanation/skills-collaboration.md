# LYBTZYZS Skills 协同关系图

## 整体协同架构

```mermaid
graph TB
    subgraph "用户交互"
        User[用户需求]
    end

    subgraph "Workflow编排层"
        WO[lybtzyzs-workflow-orchestrator<br/>工作流总控]
    end

    subgraph "需求与设计层"
        RG[lybtzyzs-requirements-generator<br/>需求文档生成器]
        DG[lybtzyzs-design-generator<br/>设计文档生成器]
        DAV[lybtzyzs-design-arch-validator<br/>设计架构验证器]
        RAG[lybtzyzs-requirements-arch-guard<br/>需求架构守卫]
    end

    subgraph "任务管理层"
        TB[lybtzyzs-task-breakdown<br/>任务分解器]
        IT[lybtzyzs-issue-template<br/>Issue批量生成器]
        TE[lybtzyzs-task-executor<br/>任务自动执行引擎]
        TT[lybtzyzs-task-tracker<br/>任务状态追踪器]
        TR[lybtzyzs-task-reflector<br/>任务反思改进器]
    end

    subgraph "质量保障层"
        MVP[lybtzyzs-mvp-compliance<br/>MVP合规检查]
        ARCH[lybtzyzs-arch-compliance<br/>架构合规检查]
        DS[lybtzyzs-doc-sync<br/>文档同步检查]
        QR[lybtzyzs-quality-reporter<br/>质量报告生成器]
    end

    subgraph "辅助工具层"
        RA[lybtzyzs-research-assistant<br/>技术研究助手]
        CB[lybtzyzs-context-builder<br/>上下文构建器]
        DA[lybtzyzs-dependency-analyzer<br/>依赖关系分析器]
        WE[lybtzyzs-workload-estimator<br/>工作量估算器]
    end

    subgraph "MCP工具层"
        ST[sequential-thinking]
        CTX[context7]
        SERENA[serena]
        GH[github]
        FS[filesystem]
        MSDOCS[microsoft_docs_mcp]
    end

    User --> WO

    WO --> RG
    WO --> DG
    WO --> TB
    WO --> IT
    WO --> TE
    WO --> QR
    WO --> TR

    RG --> RAG
    RG --> DS
    RG --> ST
    RG --> CTX
    RG --> MVP

    DG --> DAV
    DG --> MVP
    DG --> ARCH

    TB --> WE
    TB --> DA

    TE --> CB
    TE --> MVP
    TE --> ARCH
    TE --> FS

    IT --> GH

    TT --> GH

    TR --> SERENA

    CB --> DS
    CB --> SERENA

    RA --> CTX
    RA --> MSDOCS
    RA --> ST

    QR --> MVP
    QR --> ARCH

    style WO fill:#FFE4B5,stroke:#333,stroke-width:3px
    style RG fill:#90EE90,stroke:#333,stroke-width:2px
    style TE fill:#87CEEB,stroke:#333,stroke-width:2px
    style MVP fill:#FFB6C1,stroke:#333,stroke-width:2px
    style ARCH fill:#FFB6C1,stroke:#333,stroke-width:2px
```

---

## 详细协同关系

### 1. Workflow Orchestrator协同

**作为总控中枢**，orchestrator协调所有Skills按14个状态顺序执行：

```mermaid
sequenceDiagram
    participant User
    participant WO as Workflow Orchestrator
    participant RG as Requirements Generator
    participant DG as Design Generator
    participant TB as Task Breakdown
    participant IT as Issue Template
    participant TE as Task Executor
    participant QR as Quality Reporter
    participant TR as Task Reflector

    User->>WO: 开始新需求：XXX

    activate WO
    WO->>RG: 状态1: 生成需求文档
    activate RG
    RG-->>WO: 需求文档路径
    deactivate RG

    WO->>User: 状态2: 请确认需求（🔴确认点1）
    User-->>WO: 确认

    WO->>DG: 状态3: 生成设计文档
    activate DG
    DG-->>WO: 设计文档路径
    deactivate DG

    WO->>User: 状态4: 请确认设计（🔴确认点2）
    User-->>WO: 确认

    WO->>TB: 状态5: 拆分任务
    activate TB
    TB-->>WO: 任务清单路径
    deactivate TB

    Note over WO: 状态6: Task确认（🟡auto跳过）

    WO->>IT: 状态7: 批量创建Issues
    activate IT
    IT-->>WO: Issue编号列表
    deactivate IT

    loop 每个Issue
        WO->>TE: 状态8: 执行Issue #N
        activate TE
        TE-->>WO: 执行完成
        deactivate TE
    end

    WO->>QR: 状态9-10: 创建PR+质量检查
    activate QR
    QR-->>WO: 质量报告
    deactivate QR

    WO->>User: 状态10: 质量门禁（🔴确认点3）
    User-->>WO: 批准合并

    Note over WO: 状态11: 合并PR

    WO->>TR: 状态12: 生成反思报告
    activate TR
    TR-->>WO: 反思报告路径
    deactivate TR

    Note over WO: 状态13: 反思审查（🟡auto跳过）
    Note over WO: 状态14: 归档知识

    WO-->>User: 工作流完成✅
    deactivate WO
```

---

### 2. Requirements Generator协同

**核心职责**：从用户需求生成结构化需求文档

```mermaid
graph LR
    RG[Requirements Generator]

    subgraph "内部调用"
        ST[sequential-thinking<br/>深度推理5轮]
        DS[doc-sync<br/>检索现有文档]
        CTX[context7<br/>查询技术方案]
        MVP[mvp-compliance<br/>技术栈验证]
    end

    subgraph "前置检查"
        RAG[requirements-arch-guard<br/>强制读取文档体系]
    end

    subgraph "输出"
        DOC[需求讨论文档<br/>discussion.md]
    end

    RAG -->|检查完成| RG
    RG --> ST
    RG --> DS
    RG --> CTX
    RG --> MVP
    ST --> DOC
    DS --> DOC
    CTX --> DOC
    MVP --> DOC

    style RG fill:#90EE90,stroke:#333,stroke-width:2px
```

**协同示例**：
```
用户: 实现病案草稿功能

→ requirements-arch-guard: 强制读取docs/index.md等5个文档
→ requirements-generator:
  ├─ sequential-thinking: 5轮推理分析需求
  ├─ doc-sync: 查找medicalcase-requirements.md
  ├─ context7: 查询草稿管理最佳实践
  └─ mvp-compliance: 验证技术栈（本地存储方案）
→ 生成: medicalcase-draft-discussion.md
```

---

### 3. Task Executor协同

**核心职责**：自动执行GitHub Issue任务

```mermaid
graph TD
    TE[Task Executor]

    subgraph "上下文构建"
        CB[context-builder<br/>聚合上下文]
        CB1[需求文档]
        CB2[设计文档]
        CB3[相关代码]
        CB4[Constitution]
    end

    subgraph "代码生成"
        SERENA[serena<br/>代码编辑]
    end

    subgraph "自动验证"
        BUILD[dotnet build]
        TEST[dotnet test]
        MVP[mvp-compliance]
        ARCH[arch-compliance]
    end

    subgraph "提交代码"
        GIT[git commit]
        GH[github更新Issue]
    end

    TE --> CB
    CB --> CB1
    CB --> CB2
    CB --> CB3
    CB --> CB4

    CB --> SERENA
    SERENA --> BUILD
    BUILD --> TEST
    TEST --> MVP
    MVP --> ARCH
    ARCH --> GIT
    GIT --> GH

    style TE fill:#87CEEB,stroke:#333,stroke-width:2px
```

**协同示例**：
```
Issue #1501: 创建MedicalCaseDraft Entity

→ context-builder: 聚合上下文
  ├─ 需求文档: medicalcase-draft-discussion.md
  ├─ 设计文档: medicalcase-draft-design.md
  ├─ 相关代码: MedicalCase.cs, BaseEntity.cs
  └─ Constitution: MVP约束
→ serena: 生成Entity代码（80行）
→ dotnet build: ✅ 编译通过
→ dotnet test: ✅ 测试通过（无需测试Entity）
→ mvp-compliance: ✅ 无违规
→ arch-compliance: ✅ 符合架构
→ git commit: feat(medicalcase): Issue #1501 创建Entity
→ github: 更新Issue状态 → Completed
```

---

### 4. 质量保障层协同

**核心职责**：多层次质量检查

```mermaid
graph TD
    subgraph "质量检查触发点"
        T1[需求阶段]
        T2[设计阶段]
        T3[代码生成后]
        T4[PR创建前]
    end

    subgraph "质量Skills"
        MVP[mvp-compliance<br/>MVP合规检查]
        ARCH[arch-compliance<br/>架构合规检查]
        DS[doc-sync<br/>文档同步检查]
        QR[quality-reporter<br/>质量报告生成]
    end

    T1 --> MVP
    T1 --> DS

    T2 --> MVP
    T2 --> ARCH
    T2 --> DS

    T3 --> MVP
    T3 --> ARCH

    T4 --> QR
    QR --> MVP
    QR --> ARCH
    QR --> DS

    style MVP fill:#FFB6C1,stroke:#333,stroke-width:2px
    style ARCH fill:#FFB6C1,stroke:#333,stroke-width:2px
    style QR fill:#FFB6C1,stroke:#333,stroke-width:2px
```

**协同示例**：
```
质量门禁阶段:

→ quality-reporter: 生成质量报告
  ├─ mvp-compliance: 检查技术黑名单（Redis/CQRS等）
  ├─ arch-compliance: 检查依赖方向（Controller→Service→Repository）
  ├─ doc-sync: 检查文档是否更新
  ├─ dotnet build: 编译检查
  ├─ dotnet test: 测试覆盖率
  └─ 技术债务分析
→ 生成评分: 88/100
→ 判断: 满足自动合并条件（≥85）
```

---

### 5. 辅助工具层协同

**核心职责**：为其他Skills提供支持服务

```mermaid
graph LR
    subgraph "辅助Skills"
        RA[research-assistant<br/>技术调研]
        CB[context-builder<br/>上下文聚合]
        DA[dependency-analyzer<br/>依赖分析]
        WE[workload-estimator<br/>工作量估算]
    end

    subgraph "被调用者"
        RG[requirements-generator]
        DG[design-generator]
        TE[task-executor]
        TB[task-breakdown]
    end

    RA -->|技术方案| RG
    RA -->|技术方案| DG

    CB -->|上下文| TE

    DA -->|依赖关系| DG
    DA -->|影响范围| TB

    WE -->|工时估算| TB

    style RA fill:#DDA0DD,stroke:#333,stroke-width:1px
    style CB fill:#DDA0DD,stroke:#333,stroke-width:1px
    style DA fill:#DDA0DD,stroke:#333,stroke-width:1px
    style WE fill:#DDA0DD,stroke:#333,stroke-width:1px
```

---

## Skills调用频率统计（完整Epic）

| Skill | 调用次数 | 触发阶段 | 平均耗时 |
|-------|---------|---------|---------|
| requirements-generator | 1 | 需求讨论 | 3-5分钟 |
| design-generator | 1 | 设计生成 | 5-8分钟 |
| task-breakdown | 1 | 任务分解 | 2-3分钟 |
| issue-template | 1 | Issue创建 | 1-2分钟 |
| **task-executor** | **8** | 代码实现（循环） | 10-15分钟/Issue |
| **task-tracker** | **10** | 初始化+更新+查询 | 10秒/次 |
| context-builder | 8 | 每个Issue前 | 30秒/次 |
| mvp-compliance | 3 | 需求/设计/代码 | 1分钟/次 |
| arch-compliance | 2 | 设计/代码 | 2分钟/次 |
| doc-sync | 3 | 需求/设计/质量 | 1分钟/次 |
| pr-generator | 1 | PR创建 | 2-3分钟 |
| quality-reporter | 1 | 质量门禁 | 3-5分钟 |
| task-reflector | 1 | 反思总结 | 2-3分钟 |
| research-assistant | 0-2 | 按需调用 | 5-10分钟/次 |
| dependency-analyzer | 0-1 | 按需调用 | 2-3分钟 |
| workload-estimator | 1 | 任务分解 | 1分钟 |
| **总计** | **38-42** | - | **2-3小时** |

---

## 数据流向图

```mermaid
graph TB
    subgraph "输入"
        INPUT[用户需求<br/>自然语言]
    end

    subgraph "第1层：需求文档"
        REQ[需求讨论文档<br/>discussion.md]
    end

    subgraph "第2层：设计文档"
        DESIGN[设计文档<br/>design.md]
    end

    subgraph "第3层：任务清单"
        TASK[任务清单<br/>tasks.md]
    end

    subgraph "第4层：GitHub Issues"
        ISSUES[Issue #1501-#1508<br/>8个任务]
    end

    subgraph "第5层：代码实现"
        CODE[8个Commits<br/>代码文件]
    end

    subgraph "第6层：Pull Request"
        PR[PR #150<br/>完整实现]
    end

    subgraph "第7层：知识归档"
        RETRO[反思报告<br/>retrospectives/]
        MEMORY[Memory更新<br/>最佳实践]
        ADR[候选ADR<br/>架构决策]
    end

    INPUT -->|requirements-generator| REQ
    REQ -->|design-generator| DESIGN
    DESIGN -->|task-breakdown| TASK
    TASK -->|issue-template| ISSUES
    ISSUES -->|task-executor x8| CODE
    CODE -->|pr-generator| PR
    PR -->|task-reflector| RETRO
    RETRO --> MEMORY
    RETRO --> ADR

    style INPUT fill:#FFFACD,stroke:#333,stroke-width:2px
    style REQ fill:#90EE90,stroke:#333,stroke-width:2px
    style DESIGN fill:#87CEEB,stroke:#333,stroke-width:2px
    style TASK fill:#DDA0DD,stroke:#333,stroke-width:2px
    style ISSUES fill:#FFB6C1,stroke:#333,stroke-width:2px
    style CODE fill:#F0E68C,stroke:#333,stroke-width:2px
    style PR fill:#98FB98,stroke:#333,stroke-width:2px
    style RETRO fill:#FFE4B5,stroke:#333,stroke-width:2px
```

---

## 错误处理与恢复

```mermaid
stateDiagram-v2
    [*] --> RequirementsDiscussion

    RequirementsDiscussion --> RequirementsApproval: 成功
    RequirementsDiscussion --> RequirementsDiscussion: 错误重试

    RequirementsApproval --> DesignGeneration: 用户确认
    RequirementsApproval --> RequirementsDiscussion: 用户拒绝

    DesignGeneration --> DesignApproval: 成功
    DesignGeneration --> DesignGeneration: 错误重试

    DesignApproval --> TaskBreakdown: 用户确认
    DesignApproval --> DesignGeneration: 用户拒绝

    TaskBreakdown --> IssueCreation: 成功
    TaskBreakdown --> TaskBreakdown: 错误重试

    IssueCreation --> CodeImplementation: 成功

    CodeImplementation --> CodeImplementation: 下一个Issue
    CodeImplementation --> CodeImplementation: 错误自动修复
    CodeImplementation --> PRCreation: 所有Issue完成

    PRCreation --> QualityGate: 成功

    QualityGate --> Merge: 质量通过
    QualityGate --> CodeImplementation: 质量不通过（修复）

    Merge --> Reflection: 成功

    Reflection --> Archive: 成功

    Archive --> [*]: 完成

    note right of CodeImplementation
        自动重试最多2次
        简单错误自动修复
        复杂错误等待用户
    end note
```

---

## Skills版本兼容性

| Skill | 版本 | 依赖Skills | 依赖MCP工具 |
|-------|------|-----------|------------|
| workflow-orchestrator | v1.0 | 所有11个Skills | - |
| requirements-generator | v1.0 | requirements-arch-guard, doc-sync, mvp-compliance | sequential-thinking, context7 |
| design-generator | v1.0 | design-arch-validator, mvp-compliance, arch-compliance | sequential-thinking, context7 |
| task-breakdown | v1.0 | workload-estimator, dependency-analyzer | serena |
| issue-template | v1.2 | - | github |
| task-executor | v1.3 | context-builder, mvp-compliance, arch-compliance | serena, filesystem |
| task-tracker | v1.3 | - | github |
| task-reflector | v1.3 | - | serena |
| research-assistant | v1.3 | - | context7, microsoft_docs_mcp, sequential-thinking |
| context-builder | v1.3 | doc-sync | serena |
| dependency-analyzer | v1.3 | - | serena |
| workload-estimator | v1.3 | - | - |
| mvp-compliance | v1.0 | - | grep, serena, sequential-thinking |
| arch-compliance | v1.0 | - | serena |
| doc-sync | v1.0 | - | git, grep |
| quality-reporter | v1.0 | mvp-compliance, arch-compliance, doc-sync | - |

---

**最后更新**: 2025-11-07
**总Skills数**: 16个（11个核心 + 5个辅助）

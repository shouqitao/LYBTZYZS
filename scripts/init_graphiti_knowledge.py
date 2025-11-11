#!/usr/bin/env python3
"""
Graphiti 知识库初始化脚本

功能：
1. 批量导入项目知识到 Graphiti Knowledge Graph
2. 包含 Preference、Procedure、Requirement、Fact 四类实体
3. 总计约 75 条知识点

使用方法：
    python scripts/init_graphiti_knowledge.py

前置条件：
    - Neo4j 数据库运行中（bolt://localhost:7687）
    - OpenAI API Key 配置在环境变量
    - graphiti_core 库已安装

作者：Claude Code
创建日期：2025-11-11
版本：v1.0
"""

import asyncio
import json
from datetime import datetime, timezone
from typing import List, Dict, Any
from pydantic import BaseModel, Field

# 需要安装：pip install graphiti-core python-dotenv
try:
    from graphiti_core import Graphiti
    from graphiti_core.utils.bulk_utils import RawEpisode
    from graphiti_core.nodes import EpisodeType
    from dotenv import load_dotenv
except ImportError as e:
    print(f"❌ 缺少依赖库: {e}")
    print("请运行: pip install graphiti-core python-dotenv")
    exit(1)

# 加载环境变量
load_dotenv()

# 设置控制台UTF-8编码（Windows兼容性）
import sys
import io
if sys.platform == 'win32':
    sys.stdout = io.TextIOWrapper(sys.stdout.buffer, encoding='utf-8')
    sys.stderr = io.TextIOWrapper(sys.stderr.buffer, encoding='utf-8')

# ==================== 实体类型定义 ====================

class ProjectPreference(BaseModel):
    """项目偏好"""
    name: str
    category: str
    description: str
    priority: int = Field(ge=1, le=10)
    applies_to: List[str]
    examples: str = None

class ProjectProcedure(BaseModel):
    """流程规范"""
    name: str
    category: str
    steps: List[str]
    triggers: List[str]
    checkpoints: List[str]
    related_docs: str = None

class ProjectRequirement(BaseModel):
    """需求约束"""
    name: str
    category: str
    constraint_type: str
    description: str
    trigger_conditions: List[str] = None
    exceptions: str = None

class ProjectFact(BaseModel):
    """事实关系"""
    subject: str
    predicate: str
    object: str
    category: str
    source: str = None

# ==================== 知识数据定义 ====================

def get_preferences() -> List[Dict[str, Any]]:
    """批次1：Preference（约20条）"""
    return [
        # 编码规范（5条）
        {
            "name": "语言规范",
            "category": "coding_style",
            "description": "所有注释、输出、提交信息必须使用中文（简体）",
            "priority": 10,
            "applies_to": ["Server", "Client", "Shared"],
            "examples": "// 正确：这是用户服务类\n// 错误：This is user service class"
        },
        {
            "name": "编码格式",
            "category": "coding_style",
            "description": "所有文件必须使用 UTF-8 with BOM 编码",
            "priority": 10,
            "applies_to": ["Server", "Client", "Shared"],
            "examples": "Visual Studio: 文件 → 高级保存选项 → UTF-8 with BOM"
        },
        {
            "name": "命名规范",
            "category": "naming",
            "description": "PascalCase用于类型、方法、属性，_camelCase用于私有字段",
            "priority": 9,
            "applies_to": ["Server", "Client", "Shared"],
            "examples": "public class UserService { private readonly IUserRepository _repository; }"
        },
        {
            "name": "依赖注入规范",
            "category": "coding_style",
            "description": "仅允许构造函数注入，禁止属性注入和方法注入",
            "priority": 9,
            "applies_to": ["Server", "Client"],
            "examples": "public UserService(IUserRepository repository) { _repository = repository; }"
        },
        {
            "name": "异步规范",
            "category": "coding_style",
            "description": "所有I/O操作必须使用async/await，禁止同步阻塞",
            "priority": 10,
            "applies_to": ["Server", "Client"],
            "examples": "public async Task<User> GetUserAsync(Guid id) { return await _repository.GetByIdAsync(id); }"
        },

        # 技术栈偏好（8条）
        {
            "name": "后端框架",
            "category": "tech_stack",
            "description": ".NET 8, ASP.NET Core Web API",
            "priority": 10,
            "applies_to": ["Server"],
            "examples": "TargetFramework: net8.0"
        },
        {
            "name": "ORM框架",
            "category": "tech_stack",
            "description": "Entity Framework Core 8.0",
            "priority": 10,
            "applies_to": ["Server"],
            "examples": "使用LINQ + EF Core，禁止原始SQL"
        },
        {
            "name": "前端框架",
            "category": "tech_stack",
            "description": "WPF (.NET 8), Prism.DryIoc 9.0",
            "priority": 10,
            "applies_to": ["Client"],
            "examples": "MVVM架构，使用Prism区域导航"
        },
        {
            "name": "HTTP客户端",
            "category": "tech_stack",
            "description": "Refit（类型安全的HTTP客户端）",
            "priority": 8,
            "applies_to": ["Client"],
            "examples": "[Get(\"/api/users/{id}\")] Task<UserDto> GetUserAsync(Guid id);"
        },
        {
            "name": "数据库",
            "category": "tech_stack",
            "description": "SQL Server 2022",
            "priority": 10,
            "applies_to": ["Server"],
            "examples": "ConnectionString: Server=.;Database=LYBTZYZS;..."
        },
        {
            "name": "测试框架",
            "category": "tech_stack",
            "description": "xUnit（单元测试框架）",
            "priority": 9,
            "applies_to": ["Server", "Client"],
            "examples": "[Fact] public async Task GetUser_ShouldReturnUser() { ... }"
        },
        {
            "name": "Mock工具",
            "category": "tech_stack",
            "description": "NSubstitute（Mock工具）",
            "priority": 8,
            "applies_to": ["Server", "Client"],
            "examples": "var mockRepo = Substitute.For<IUserRepository>();"
        },
        {
            "name": "版本控制",
            "category": "tech_stack",
            "description": "Git，主分支为master",
            "priority": 10,
            "applies_to": ["Server", "Client", "Shared"],
            "examples": "git checkout master"
        },

        # 版本策略（3条）
        {
            "name": "MVP版本策略",
            "category": "version",
            "description": "MVP阶段保持1.x.x.x系列，避免大版本跳跃",
            "priority": 9,
            "applies_to": ["Server", "Client"],
            "examples": "当前版本：1.0.0.0 → 下一版本：1.1.0.0"
        },
        {
            "name": "版本升级触发条件",
            "category": "version",
            "description": "重大架构重构、破坏性API变更、技术栈重大升级、MVP发布后里程碑",
            "priority": 8,
            "applies_to": ["Server", "Client"],
            "examples": ".NET 8 → .NET 9 升级才考虑 2.0.0.0"
        },
        {
            "name": "版本升级禁止行为",
            "category": "version",
            "description": "避免大版本频繁跳跃，通过功能扩展而非版本升级",
            "priority": 8,
            "applies_to": ["Server", "Client"],
            "examples": "错误：1.0 → 2.0 → 3.0（3个月内）"
        },

        # 架构偏好（4条）
        {
            "name": "Server端架构",
            "category": "architecture",
            "description": "三层架构：Repository层 → Service层 → Controller层",
            "priority": 10,
            "applies_to": ["Server"],
            "examples": "Repository负责数据访问，Service负责业务逻辑，Controller负责API路由"
        },
        {
            "name": "Client端架构",
            "category": "architecture",
            "description": "MVVM五层：View → ViewModel → Module → QueryService/BusinessService → Infrastructure",
            "priority": 10,
            "applies_to": ["Client"],
            "examples": "ViewModel通过Module委托调用QueryService查询数据"
        },
        {
            "name": "共享层架构",
            "category": "architecture",
            "description": "Shared.Models：DTO、接口定义、枚举、常量",
            "priority": 9,
            "applies_to": ["Shared"],
            "examples": "UserDto、IUserService定义在Shared.Models中"
        },
        {
            "name": "Repository可见性",
            "category": "architecture",
            "description": "Repository实现类为internal，仅Service层可访问（Epic #1600 Phase 3）",
            "priority": 9,
            "applies_to": ["Server"],
            "examples": "internal class UserRepository : BaseRepository<User>, IUserRepository"
        }
    ]

def get_procedures() -> List[Dict[str, Any]]:
    """批次2：Procedure（约15条）"""
    return [
        {
            "name": "Issue工作流",
            "category": "issue_workflow",
            "steps": [
                "1. 创建GitHub Issue描述问题",
                "2. 实施代码修改",
                "3. 验证（编译 + 运行 + 数据库检查）",
                "4. 提交代码到master分支"
            ],
            "triggers": ["所有代码变更必须有Issue"],
            "checkpoints": ["验证完整性（运行时验证强制）"],
            "related_docs": ".claude/guides/issue-workflow.md"
        },
        {
            "name": "PR工作流",
            "category": "pr_workflow",
            "steps": [
                "1. 创建分支（可选）",
                "2. 提交代码",
                "3. git diff分析变更",
                "4. 生成PR描述",
                "5. 推送到远程"
            ],
            "triggers": ["需要代码审查", "跨团队协作"],
            "checkpoints": ["PR描述包含Issue关联、测试计划、Claude Code标记"],
            "related_docs": ".claude/guides/issue-workflow.md"
        },
        {
            "name": "验证流程",
            "category": "testing",
            "steps": [
                "1. 编译（dotnet build，0 errors, 0 warnings）",
                "2. 启动应用（Client + Server）",
                "3. 执行真实操作场景",
                "4. 验证数据库状态",
                "5. 从用户视角确认功能完整可用"
            ],
            "triggers": ["任何代码变更"],
            "checkpoints": ["禁止只编译通过就认为完成"],
            "related_docs": ".claude/guides/testing.md"
        },
        {
            "name": "文档同步流程",
            "category": "documentation",
            "steps": [
                "1. 实施前：列出文档更新清单",
                "2. 开发中：代码变更后立即更新文档",
                "3. 完成前：确认所有文档已更新"
            ],
            "triggers": ["架构变更", "API变更", "新功能开发"],
            "checkpoints": ["架构文档、开发指南、API文档、快速参考、导航索引全部更新"],
            "related_docs": ".claude/guides/documentation.md"
        },
        {
            "name": "代码审查流程",
            "category": "code_review",
            "steps": [
                "1. 规范检查（命名、注释、编码格式）",
                "2. 架构验证（三层对齐、依赖方向）",
                "3. 安全扫描（OWASP Top 10）",
                "4. 性能分析（N+1查询、内存泄漏）"
            ],
            "triggers": ["PR提交", "手动触发/code-review命令"],
            "checkpoints": ["使用lybtzyzs-code-review skill"],
            "related_docs": ".claude/modes/code-review.md"
        },
        {
            "name": "测试流程",
            "category": "testing",
            "steps": [
                "1. 单元测试（AAA模式：Arrange-Act-Assert）",
                "2. 集成测试（数据库、HTTP客户端）",
                "3. 运行时验证（真实场景）"
            ],
            "triggers": ["新功能开发", "Bug修复"],
            "checkpoints": ["关键路径100%覆盖"],
            "related_docs": ".claude/guides/testing.md"
        },
        {
            "name": "提交规范",
            "category": "git",
            "steps": [
                "1. 格式：type(module): 描述",
                "2. 正文：Fixes #issue + 具体改动",
                "3. 签名：Claude Code标记 + Co-Authored-By"
            ],
            "triggers": ["git commit"],
            "checkpoints": ["类型：fix, feat, refactor, docs, test, chore"],
            "related_docs": ".claude/guides/issue-workflow.md"
        },
        {
            "name": "小需求工作流",
            "category": "workflow",
            "steps": [
                "1. 创建GitHub Issue",
                "2. 从Graphiti检索规则",
                "3. 直接修改代码（<5文件, <200行）",
                "4. 验证",
                "5. 提交"
            ],
            "triggers": ["<5文件", "<200行", "<2小时"],
            "checkpoints": ["90%的需求使用此流程"],
            "related_docs": "CLAUDE.md"
        },
        {
            "name": "大需求工作流",
            "category": "workflow",
            "steps": [
                "1. 创建GitHub Issue",
                "2. 调用lybtzyzs-workflow-orchestrator skill",
                "3. 14状态自动化流程（需求→设计→任务→实施→质量→归档）",
                "4. 5个人工确认点（需求确认、设计审查、任务审查、质量把关、反思审查）"
            ],
            "triggers": ["跨模块", ">200行", ">2小时", "关键词：复杂需求、新功能、Epic任务"],
            "checkpoints": ["10%的需求使用此流程，自动化率85%"],
            "related_docs": ".claude/skills/AUTOMATION-SYSTEM-SUMMARY.md"
        },
        {
            "name": "环境清理流程",
            "category": "testing",
            "steps": [
                "1. 终止临时进程",
                "2. 释放资源缓存",
                "3. 还原配置",
                "4. 关闭外部连接",
                "5. 归档证据",
                "6. 端口检查"
            ],
            "triggers": ["验证完成后"],
            "checkpoints": ["确保环境干净"],
            "related_docs": ".claude/guides/testing.md"
        }
    ]

def get_requirements() -> List[Dict[str, Any]]:
    """批次3：Requirement（约10条）"""
    return [
        {
            "name": "MVP技术黑名单-分布式",
            "category": "mvp_constraint",
            "constraint_type": "forbidden",
            "description": "禁止使用：Redis, RabbitMQ/Kafka, Docker（开发阶段）, 微服务",
            "trigger_conditions": ["MVP阶段"],
            "exceptions": "Docker仅用于生产部署，开发阶段禁止"
        },
        {
            "name": "MVP技术黑名单-过度设计",
            "category": "mvp_constraint",
            "constraint_type": "forbidden",
            "description": "禁止使用：CQRS, MediatR, Event Sourcing, DDD富领域模型",
            "trigger_conditions": ["MVP阶段"],
            "exceptions": "架构触发指标达标后可调整Constitution"
        },
        {
            "name": "MVP技术黑名单-过度抽象",
            "category": "mvp_constraint",
            "constraint_type": "forbidden",
            "description": "禁止使用：多层抽象接口, 过度工厂/策略模式",
            "trigger_conditions": ["MVP阶段"],
            "exceptions": "简单直接优先，避免过度工程"
        },
        {
            "name": "MVP技术黑名单-前端框架",
            "category": "mvp_constraint",
            "constraint_type": "forbidden",
            "description": "禁止使用：GraphQL, React/Vue（Desktop）, Blazor（Desktop）",
            "trigger_conditions": ["MVP阶段"],
            "exceptions": "WPF + Prism已满足需求"
        },
        {
            "name": "架构触发指标1",
            "category": "architecture_rule",
            "constraint_type": "conditional",
            "description": "业务规则 >20条 → 富领域模型",
            "trigger_conditions": ["当前业务规则约14条"],
            "exceptions": "当前MVP阶段无需"
        },
        {
            "name": "架构触发指标2",
            "category": "architecture_rule",
            "constraint_type": "conditional",
            "description": "Service方法 >200行 → 领域服务拆分",
            "trigger_conditions": ["当前Service方法<100行"],
            "exceptions": "当前MVP阶段无需"
        },
        {
            "name": "架构触发指标3",
            "category": "architecture_rule",
            "constraint_type": "conditional",
            "description": "聚合根关系 >3层 → 重新设计边界",
            "trigger_conditions": ["当前最多2层"],
            "exceptions": "当前MVP阶段无需"
        },
        {
            "name": "质量标准-编译",
            "category": "quality_standard",
            "constraint_type": "required",
            "description": "编译必须0 errors, 0 warnings",
            "trigger_conditions": ["任何代码提交"],
            "exceptions": "无例外"
        },
        {
            "name": "质量标准-运行时验证",
            "category": "quality_standard",
            "constraint_type": "required",
            "description": "启动应用 + 真实操作 + 数据库验证",
            "trigger_conditions": ["任何代码提交"],
            "exceptions": "禁止只编译通过就认为完成"
        },
        {
            "name": "Server/Client职责划分",
            "category": "architecture_rule",
            "constraint_type": "required",
            "description": "Server端：数据持久化、核心业务规则、数据校验、实体关系维护；Client端：工作流编排、UI逻辑、用户交互、业务流程控制",
            "trigger_conditions": ["所有新功能开发"],
            "exceptions": "数据一致性→Server，多步骤流程→Client"
        }
    ]

def get_facts() -> List[Dict[str, Any]]:
    """批次4：Fact（约30条）"""
    return [
        # 8大业务模块
        {"subject": "Auth模块", "predicate": "依赖", "object": "Users模块", "category": "dependency", "source": "项目架构"},
        {"subject": "Auth模块", "predicate": "功能", "object": "身份验证与授权（JWT + RBAC）", "category": "function", "source": "项目架构"},
        {"subject": "Users模块", "predicate": "类型", "object": "聚合根实体", "category": "type", "source": "项目架构"},
        {"subject": "Users模块", "predicate": "功能", "object": "用户管理（Doctor/Admin角色）", "category": "function", "source": "项目架构"},
        {"subject": "Patients模块", "predicate": "类型", "object": "聚合根实体", "category": "type", "source": "项目架构"},
        {"subject": "Patients模块", "predicate": "功能", "object": "患者档案管理", "category": "function", "source": "项目架构"},
        {"subject": "MedicalCase模块", "predicate": "类型", "object": "聚合根实体", "category": "type", "source": "项目架构"},
        {"subject": "MedicalCase模块", "predicate": "功能", "object": "病历管理（管理Prescription/Consultation）", "category": "function", "source": "项目架构"},
        {"subject": "MedicalCase模块", "predicate": "管理", "object": "Prescription模块", "category": "hierarchy", "source": "项目架构"},
        {"subject": "MedicalCase模块", "predicate": "管理", "object": "Consultation模块", "category": "hierarchy", "source": "项目架构"},
        {"subject": "Consultation模块", "predicate": "类型", "object": "从属实体", "category": "type", "source": "项目架构"},
        {"subject": "Consultation模块", "predicate": "功能", "object": "中医诊断（望闻问切）", "category": "function", "source": "项目架构"},
        {"subject": "Prescriptions模块", "predicate": "类型", "object": "从属实体", "category": "type", "source": "项目架构"},
        {"subject": "Prescriptions模块", "predicate": "功能", "object": "处方管理", "category": "function", "source": "项目架构"},
        {"subject": "Herbs模块", "predicate": "类型", "object": "聚合根实体", "category": "type", "source": "项目架构"},
        {"subject": "Herbs模块", "predicate": "功能", "object": "中药管理", "category": "function", "source": "项目架构"},
        {"subject": "Formula模块", "predicate": "类型", "object": "聚合根实体", "category": "type", "source": "项目架构"},
        {"subject": "Formula模块", "predicate": "功能", "object": "方剂管理", "category": "function", "source": "项目架构"},

        # 三层架构层次
        {"subject": "Repository层", "predicate": "调用", "object": "Service层", "category": "hierarchy", "source": "Server端三层架构"},
        {"subject": "Service层", "predicate": "调用", "object": "Controller层", "category": "hierarchy", "source": "Server端三层架构"},
        {"subject": "View层", "predicate": "调用", "object": "ViewModel层", "category": "hierarchy", "source": "Client端MVVM架构"},
        {"subject": "ViewModel层", "predicate": "调用", "object": "Module层", "category": "hierarchy", "source": "Client端MVVM架构"},
        {"subject": "Module层", "predicate": "调用", "object": "QueryService/BusinessService层", "category": "hierarchy", "source": "Client端MVVM架构"},

        # Repository三层接口
        {"subject": "IReadRepository<T>", "predicate": "继承", "object": "IRepository<T>", "category": "hierarchy", "source": "Epic #2016 Phase 3"},
        {"subject": "IRepository<T>", "predicate": "继承", "object": "IXxxRepository", "category": "hierarchy", "source": "Epic #2016 Phase 3"},
        {"subject": "IReadRepository<T>", "predicate": "方法数", "object": "5个只读方法", "category": "specification", "source": "Epic #2016 Phase 3"},
        {"subject": "IRepository<T>", "predicate": "方法数", "object": "14个方法（5只读+9写入）", "category": "specification", "source": "Epic #2016 Phase 3"},

        # 文档位置映射
        {"subject": "架构文档", "predicate": "位于", "object": "docs/explanation/architecture/", "category": "location", "source": "文档索引"},
        {"subject": "开发指南", "predicate": "位于", "object": ".claude/guides/", "category": "location", "source": "文档索引"},
        {"subject": "工作模式", "predicate": "位于", "object": ".claude/modes/", "category": "location", "source": "文档索引"}
    ]

# ==================== 主函数 ====================

async def main():
    """主函数"""
    print("=" * 60)
    print("🚀 Graphiti 知识库初始化脚本")
    print("=" * 60)
    print()

    # 1. 初始化 Graphiti
    print("📡 连接 Graphiti...")
    graphiti = Graphiti(
        uri="bolt://localhost:7687",
        user="neo4j",
        password="demodemo"
    )

    try:
        # 2. 构建索引
        print("🔨 构建索引和约束...")
        await graphiti.build_indices_and_constraints()
        print("✅ 索引构建完成")
        print()

        # 3. 准备知识数据
        print("📚 准备知识数据...")
        preferences = get_preferences()
        procedures = get_procedures()
        requirements = get_requirements()
        facts = get_facts()

        total_count = len(preferences) + len(procedures) + len(requirements) + len(facts)
        print(f"📊 总计 {total_count} 条知识")
        print(f"   - Preference: {len(preferences)}条")
        print(f"   - Procedure: {len(procedures)}条")
        print(f"   - Requirement: {len(requirements)}条")
        print(f"   - Fact: {len(facts)}条")
        print()

        # 4. 批量导入 Preference
        print("⬆️  导入 Preference...")
        preference_episodes = [
            RawEpisode(
                name=f"Preference: {pref['name']}",
                content=json.dumps(pref, ensure_ascii=False),
                source=EpisodeType.json,
                source_description=f"项目偏好：{pref['category']}",
                reference_time=datetime.now(timezone.utc)
            )
            for pref in preferences
        ]
        result_pref = await graphiti.add_episode_bulk(
            bulk_episodes=preference_episodes,
            group_id="lybtzyzs_project"
        )
        print(f"✅ Preference 导入完成：{len(result_pref.episodes)}条")

        # 5. 批量导入 Procedure
        print("⬆️  导入 Procedure...")
        procedure_episodes = [
            RawEpisode(
                name=f"Procedure: {proc['name']}",
                content=json.dumps(proc, ensure_ascii=False),
                source=EpisodeType.json,
                source_description=f"流程规范：{proc['category']}",
                reference_time=datetime.now(timezone.utc)
            )
            for proc in procedures
        ]
        result_proc = await graphiti.add_episode_bulk(
            bulk_episodes=procedure_episodes,
            group_id="lybtzyzs_project"
        )
        print(f"✅ Procedure 导入完成：{len(result_proc.episodes)}条")

        # 6. 批量导入 Requirement
        print("⬆️  导入 Requirement...")
        requirement_episodes = [
            RawEpisode(
                name=f"Requirement: {req['name']}",
                content=json.dumps(req, ensure_ascii=False),
                source=EpisodeType.json,
                source_description=f"需求约束：{req['category']}",
                reference_time=datetime.now(timezone.utc)
            )
            for req in requirements
        ]
        result_req = await graphiti.add_episode_bulk(
            bulk_episodes=requirement_episodes,
            group_id="lybtzyzs_project"
        )
        print(f"✅ Requirement 导入完成：{len(result_req.episodes)}条")

        # 7. 批量导入 Fact
        print("⬆️  导入 Fact...")
        fact_episodes = [
            RawEpisode(
                name=f"Fact: {fact['subject']}-{fact['predicate']}-{fact['object']}",
                content=json.dumps(fact, ensure_ascii=False),
                source=EpisodeType.json,
                source_description=f"事实关系：{fact['category']}",
                reference_time=datetime.now(timezone.utc)
            )
            for fact in facts
        ]
        result_fact = await graphiti.add_episode_bulk(
            bulk_episodes=fact_episodes,
            group_id="lybtzyzs_project"
        )
        print(f"✅ Fact 导入完成：{len(result_fact.episodes)}条")

        # 8. 汇总统计
        print()
        print("=" * 60)
        print("🎉 导入完成！")
        print("=" * 60)
        print(f"📊 总计导入：{len(result_pref.episodes) + len(result_proc.episodes) + len(result_req.episodes) + len(result_fact.episodes)}条知识")
        print(f"📊 实体节点：{len(result_pref.nodes) + len(result_proc.nodes) + len(result_req.nodes) + len(result_fact.nodes)}个")
        print(f"📊 关系边：{len(result_pref.edges) + len(result_proc.edges) + len(result_req.edges) + len(result_fact.edges)}条")
        print()
        print("💡 下一步：")
        print("   1. 运行验证脚本：python scripts/verify_graphiti_knowledge.py")
        print("   2. 测试检索功能：search_nodes(query='编码规范', max_nodes=5)")
        print("   3. 部署新版CLAUDE.md：cp docs/proposals/CLAUDE.md.v7.0 CLAUDE.md")
        print()

    except Exception as e:
        print(f"❌ 错误：{e}")
        import traceback
        traceback.print_exc()
    finally:
        await graphiti.close()
        print("🔌 连接已关闭")

if __name__ == "__main__":
    asyncio.run(main())

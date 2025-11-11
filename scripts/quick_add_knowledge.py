#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""快速添加Graphiti知识的辅助脚本"""

import asyncio
import json
import sys
import io

# 设置控制台UTF-8编码
if sys.platform == 'win32':
    sys.stdout = io.TextIOWrapper(sys.stdout.buffer, encoding='utf-8')
    sys.stderr = io.TextIOWrapper(sys.stderr.buffer, encoding='utf-8')

# 核心知识列表
CORE_KNOWLEDGE = [
    {
        "name": "Preference: 编码格式规范",
        "episode_body": {
            "name": "编码格式规范",
            "category": "coding_style",
            "description": "所有文件必须使用UTF-8 with BOM编码",
            "priority": 10,
            "applies_to": ["Server", "Client"],
            "examples": "*.cs文件统一UTF-8 with BOM"
        },
        "source": "json",
        "source_description": "项目偏好：编码规范"
    },
    {
        "name": "Preference: 命名规范",
        "episode_body": {
            "name": "命名规范",
            "category": "naming",
            "description": "类型PascalCase，私有字段_camelCase，公有属性/方法PascalCase",
            "priority": 10,
            "applies_to": ["Server", "Client"],
            "examples": "class Patient, private string _name, public string Name"
        },
        "source": "json",
        "source_description": "项目偏好：命名规范"
    },
    {
        "name": "Preference: 异步规范",
        "episode_body": {
            "name": "异步规范",
            "category": "coding_style",
            "description": "所有I/O操作必须使用async/await，禁止.Result或.Wait()",
            "priority": 10,
            "applies_to": ["Server", "Client"],
            "examples": "await repository.GetByIdAsync(id)"
        },
        "source": "json",
        "source_description": "项目偏好：异步编程规范"
    },
    {
        "name": "Preference: 依赖注入规范",
        "episode_body": {
            "name": "依赖注入规范",
            "category": "coding_style",
            "description": "仅使用构造函数注入，禁止属性注入或方法注入",
            "priority": 10,
            "applies_to": ["Server", "Client"],
            "examples": "public PatientService(IPatientRepository repository)"
        },
        "source": "json",
        "source_description": "项目偏好：依赖注入规范"
    },
    {
        "name": "Requirement: MVP技术黑名单",
        "episode_body": {
            "name": "MVP技术黑名单",
            "category": "mvp_constraint",
            "description": "MVP阶段严格禁止：Redis、RabbitMQ/Kafka、Docker、微服务、CQRS、MediatR、Event Sourcing、DDD富领域模型、GraphQL",
            "priority": 10,
            "rationale": "过度设计，MVP需要快速交付",
            "allowed_alternatives": ["EF Core内存缓存", "直接方法调用", "简单三层架构"]
        },
        "source": "json",
        "source_description": "项目约束：MVP技术黑名单"
    },
    {
        "name": "Procedure: Issue工作流",
        "episode_body": {
            "name": "Issue工作流",
            "category": "issue_workflow",
            "description": "创建Issue → 实施代码 → 编译通过 → 运行时验证(强制) → 提交代码 → 关闭Issue",
            "priority": 10,
            "steps": [
                "1. GitHub创建Issue描述问题",
                "2. 修改代码",
                "3. 编译验证(0 error, 0 warning)",
                "4. 运行时验证（启动应用，执行操作，验证数据库）",
                "5. 提交到master分支",
                "6. 关闭Issue"
            ],
            "mandatory": "运行时验证不可跳过"
        },
        "source": "json",
        "source_description": "项目流程：Issue工作流"
    },
    {
        "name": "Procedure: 验证流程",
        "episode_body": {
            "name": "验证流程",
            "category": "testing",
            "description": "运行时验证是强制要求，必须启动应用执行真实操作",
            "priority": 10,
            "steps": [
                "1. 启动Server端和Client端",
                "2. 执行真实操作场景",
                "3. 检查数据库状态",
                "4. 从用户视角确认功能完整可用"
            ],
            "prohibited": ["只编译通过就认为完成", "部分功能可用就关闭Issue", "未测试边界条件"]
        },
        "source": "json",
        "source_description": "项目流程：运行时验证"
    },
    {
        "name": "Requirement: 三层架构规范",
        "episode_body": {
            "name": "三层架构规范",
            "category": "architecture_rule",
            "description": "Server端严格三层：Repository层（数据访问） → Service层（业务逻辑） → Controller层（API接口）",
            "priority": 10,
            "rules": [
                "Repository仅数据操作，禁止业务逻辑",
                "Service协调业务流程，可调用多个Repository",
                "Controller薄层，仅参数校验和调用Service"
            ],
            "prohibited": ["Controller直接调用Repository", "Repository包含业务规则", "Service包含SQL逻辑"]
        },
        "source": "json",
        "source_description": "项目约束：三层架构"
    },
    {
        "name": "Preference: LINQ优先原则",
        "episode_body": {
            "name": "LINQ优先原则",
            "category": "coding_style",
            "description": "所有数据库操作使用LINQ + EF Core，严格禁止原始SQL",
            "priority": 10,
            "applies_to": ["Server"],
            "examples": "await _context.Patients.Where(p => !p.IsDeleted).ToListAsync()",
            "exceptions": ["性能优化确有必要时可使用，但需ADR文档说明"]
        },
        "source": "json",
        "source_description": "项目偏好：数据访问规范"
    },
    {
        "name": "Requirement: 质量标准",
        "episode_body": {
            "name": "质量标准",
            "category": "quality_standard",
            "description": "编译0 error 0 warning，运行时验证通过，功能完整可用",
            "priority": 10,
            "criteria": [
                "编译通过",
                "无编译警告",
                "启动应用成功",
                "执行真实操作",
                "数据库状态正确",
                "用户视角功能完整"
            ],
            "prohibited": ["部分功能可用就关闭Issue", "只编译不验证"]
        },
        "source": "json",
        "source_description": "项目约束：质量标准"
    }
]

async def main():
    """通过MCP工具添加知识"""
    print("=" * 60)
    print("🚀 快速添加Graphiti核心知识")
    print("=" * 60)
    print()

    print(f"📦 准备添加 {len(CORE_KNOWLEDGE)} 条核心知识...")
    print()

    # 输出MCP工具调用指令
    print("请使用以下MCP工具调用添加知识：")
    print()

    for i, item in enumerate(CORE_KNOWLEDGE, 1):
        episode_body_str = json.dumps(item["episode_body"], ensure_ascii=False)
        print(f"{i}. {item['name']}")
        print(f"   mcp__graphiti-memory__add_memory(")
        print(f"      name='{item['name']}',")
        print(f"      episode_body='{episode_body_str}',")
        print(f"      source='{item['source']}',")
        print(f"      source_description='{item['source_description']}',")
        print(f"      group_id='lybtzyzs_project'")
        print(f"   )")
        print()

    print("=" * 60)
    print("💡 提示：由于MCP工具调用限制，建议手动执行上述调用")
    print("=" * 60)

if __name__ == "__main__":
    asyncio.run(main())

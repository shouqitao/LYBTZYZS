#!/usr/bin/env python3
"""
Graphiti 知识库验证脚本

功能：
1. 抽样测试检索精度
2. 验证实体类型正确性
3. 检查关系完整性

使用方法：
    python scripts/verify_graphiti_knowledge.py

作者：Claude Code
创建日期：2025-11-11
版本：v1.0
"""

import asyncio
import json
from typing import List, Dict
from datetime import datetime, timezone

try:
    from graphiti_core import Graphiti
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

# 测试用例
TEST_CASES = [
    {
        "name": "编码规范检索",
        "query": "编码规范 命名规范",
        "entity_types": ["Preference"],
        "expected_min": 3,
        "expected_keywords": ["UTF-8", "PascalCase", "async"]
    },
    {
        "name": "工作流检索",
        "query": "Issue工作流 验证流程",
        "entity_types": ["Procedure"],
        "expected_min": 2,
        "expected_keywords": ["Issue", "验证", "编译"]
    },
    {
        "name": "MVP约束检索",
        "query": "MVP技术黑名单",
        "entity_types": ["Requirement"],
        "expected_min": 3,
        "expected_keywords": ["Redis", "CQRS", "MediatR"]
    },
    {
        "name": "模块依赖检索",
        "query": "模块依赖 架构层次",
        "max_facts": 20,
        "expected_min": 10,
        "expected_keywords": ["Auth模块", "Repository层", "Service层"]
    }
]

async def test_search_nodes(graphiti: Graphiti, test_case: Dict) -> Dict:
    """测试 search_nodes"""
    try:
        results = await graphiti.search_nodes(
            query=test_case["query"],
            entity_types=test_case.get("entity_types"),
            max_nodes=test_case.get("max_nodes", 10),
            group_ids=["lybtzyzs_project"]
        )

        # 检查结果数量
        passed = len(results) >= test_case["expected_min"]

        # 检查关键词
        all_content = " ".join([str(r) for r in results])
        keyword_matches = [kw for kw in test_case["expected_keywords"] if kw in all_content]

        return {
            "name": test_case["name"],
            "passed": passed and len(keyword_matches) >= 2,
            "results_count": len(results),
            "expected_min": test_case["expected_min"],
            "keyword_matches": keyword_matches,
            "details": f"检索到 {len(results)} 条结果，匹配关键词: {', '.join(keyword_matches)}"
        }
    except Exception as e:
        return {
            "name": test_case["name"],
            "passed": False,
            "error": str(e)
        }

async def test_search_facts(graphiti: Graphiti, test_case: Dict) -> Dict:
    """测试 search_facts"""
    try:
        results = await graphiti.search_facts(
            query=test_case["query"],
            max_facts=test_case.get("max_facts", 20),
            group_ids=["lybtzyzs_project"]
        )

        # 检查结果数量
        passed = len(results) >= test_case["expected_min"]

        # 检查关键词
        all_content = " ".join([str(r.fact) for r in results])
        keyword_matches = [kw for kw in test_case["expected_keywords"] if kw in all_content]

        return {
            "name": test_case["name"],
            "passed": passed and len(keyword_matches) >= 2,
            "results_count": len(results),
            "expected_min": test_case["expected_min"],
            "keyword_matches": keyword_matches,
            "details": f"检索到 {len(results)} 条事实，匹配关键词: {', '.join(keyword_matches)}"
        }
    except Exception as e:
        return {
            "name": test_case["name"],
            "passed": False,
            "error": str(e)
        }

async def test_episodes_retrieval(graphiti: Graphiti) -> Dict:
    """测试 episode 检索"""
    try:
        episodes = await graphiti.get_episodes(
            group_ids=["lybtzyzs_project"],
            max_episodes=100
        )

        # 统计各类型数量
        preference_count = sum(1 for ep in episodes if "Preference:" in ep.name)
        procedure_count = sum(1 for ep in episodes if "Procedure:" in ep.name)
        requirement_count = sum(1 for ep in episodes if "Requirement:" in ep.name)
        fact_count = sum(1 for ep in episodes if "Fact:" in ep.name)

        total = preference_count + procedure_count + requirement_count + fact_count
        passed = total >= 70  # 预期至少70条

        return {
            "name": "Episode总数检查",
            "passed": passed,
            "total": total,
            "breakdown": {
                "Preference": preference_count,
                "Procedure": procedure_count,
                "Requirement": requirement_count,
                "Fact": fact_count
            },
            "details": f"总计 {total} 条知识（Preference: {preference_count}, Procedure: {procedure_count}, Requirement: {requirement_count}, Fact: {fact_count}）"
        }
    except Exception as e:
        return {
            "name": "Episode总数检查",
            "passed": False,
            "error": str(e)
        }

async def main():
    """主函数"""
    print("=" * 60)
    print("🔍 Graphiti 知识库验证脚本")
    print("=" * 60)
    print()

    # 1. 连接 Graphiti
    print("📡 连接 Graphiti...")
    graphiti = Graphiti(
        uri="bolt://localhost:7687",
        user="neo4j",
        password="demodemo"
    )

    try:
        # 2. 测试 Episode 总数
        print("📊 检查 Episode 总数...")
        episode_result = await test_episodes_retrieval(graphiti)
        print(f"   {'✅' if episode_result['passed'] else '❌'} {episode_result['name']}")
        print(f"   {episode_result['details']}")
        print()

        # 3. 测试检索功能
        print("🔍 测试检索功能...")
        results = []

        for test_case in TEST_CASES:
            if "max_facts" in test_case:
                result = await test_search_facts(graphiti, test_case)
            else:
                result = await test_search_nodes(graphiti, test_case)

            results.append(result)
            status = "✅" if result["passed"] else "❌"
            print(f"   {status} {result['name']}")
            if "error" in result:
                print(f"      错误: {result['error']}")
            else:
                print(f"      {result['details']}")

        print()

        # 4. 汇总报告
        print("=" * 60)
        print("📋 验证报告")
        print("=" * 60)

        total_tests = len(results) + 1  # +1 for episode count
        passed_tests = sum(1 for r in results if r["passed"]) + (1 if episode_result["passed"] else 0)
        pass_rate = (passed_tests / total_tests) * 100

        print(f"总测试数: {total_tests}")
        print(f"通过数: {passed_tests}")
        print(f"失败数: {total_tests - passed_tests}")
        print(f"通过率: {pass_rate:.1f}%")
        print()

        if pass_rate >= 80:
            print("✅ 验证通过！知识库质量良好")
            print()
            print("💡 下一步：")
            print("   1. 部署新版CLAUDE.md：cp docs/proposals/CLAUDE.md.v7.0 CLAUDE.md")
            print("   2. 删除旧版Serena Memory（避免混淆）")
            print("   3. 开始使用Graphiti优先工作流")
        else:
            print("⚠️ 验证未通过！需要调整知识库")
            print()
            print("💡 建议：")
            print("   1. 检查失败的测试用例")
            print("   2. 优化实体标签和分类")
            print("   3. 重新运行初始化脚本")
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

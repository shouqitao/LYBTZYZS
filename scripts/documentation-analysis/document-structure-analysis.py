#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
文档结构分析脚本 - Document Structure Analysis Script

用途：扫描docs/和spec-workflow/目录，建立完整的文档清单和分类映射
目标：为统一文档导航门户提供准确的数据基础

作者：Claude Code
创建时间：2025-10-15
版本：1.0
"""

import os
import json
import argparse
from pathlib import Path
from typing import Dict, List, Any, Optional
from dataclasses import dataclass, asdict
from datetime import datetime


@dataclass
class DocumentInfo:
    """文档信息数据类"""
    path: str                    # 相对路径
    full_path: str              # 完整路径
    name: str                   # 文档名称（不含扩展名）
    title: str                  # 文档标题（从内容提取）
    system: str                 # 所属体系 (docs/spec-workflow)
    category: str               # 分类（architecture/development等）
    file_type: str              # 文件类型 (md/txt等)
    size: int                   # 文件大小（字节）
    modified_time: str          # 最后修改时间
    roles: List[str]            # 相关角色（developer/architect/pm/tester）
    tasks: List[str]            # 相关任务类型（development/bugfix/architecture/maintenance）
    keywords: List[str]         # 关键词
    description: str            # 文档描述
    cross_references: List[str] # 交叉引用的文档路径
    
    def __post_init__(self):
        """后处理，确保数据类型正确"""
        if isinstance(self.roles, str):
            self.roles = [self.roles]
        if isinstance(self.tasks, str):
            self.tasks = [self.tasks]
        if isinstance(self.keywords, str):
            self.keywords = [self.keywords]
        if isinstance(self.cross_references, str):
            self.cross_references = [self.cross_references]


class DocumentAnalyzer:
    """文档分析器"""
    
    def __init__(self, project_root: str):
        self.project_root = Path(project_root)
        self.docs_dir = self.project_root / "docs"
        self.spec_workflow_dir = self.project_root / ".spec-workflow"
        
        # 角色和任务的映射规则
        self.role_patterns = {
            "developer": [
                "开发", "development", "code", "coding", "programming",
                "server", "client", "api", "interface", "module",
                "架构标准", "设计标准", "编码规范", "技术规范"
            ],
            "architect": [
                "架构", "architecture", "design", "pattern", "decision",
                "adr", "structure", "technical", "system",
                "设计文档", "技术决策", "架构设计", "系统设计"
            ],
            "pm": [
                "project", "management", "requirement", "plan", "roadmap",
                "milestone", "delivery", "status", "progress",
                "项目管理", "需求", "计划", "里程碑", "交付", "状态"
            ],
            "tester": [
                "test", "testing", "quality", "qa", "coverage", "automation",
                "validation", "verification", "benchmark", "performance",
                "测试", "质量", "覆盖率", "自动化", "验证", "性能"
            ]
        }
        
        self.task_patterns = {
            "development": [
                "开发", "development", "feature", "function", "implement",
                "新功能", "功能开发", "实现", "新增"
            ],
            "bugfix": [
                "bug", "issue", "fix", "repair", "debug", "troubleshoot",
                "缺陷", "问题", "修复", "调试", "故障排除"
            ],
            "architecture": [
                "architecture", "design", "structure", "pattern", "decision",
                "架构", "设计", "结构", "模式", "决策"
            ],
            "maintenance": [
                "maintenance", "update", "upgrade", "refactor", "optimize",
                "维护", "更新", "升级", "重构", "优化"
            ]
        }
    
    def analyze_documents(self) -> Dict[str, Any]:
        """分析所有文档并返回结构化数据"""
        print("[INFO] 开始分析文档结构...")
        
        # 扫描两个体系的文档
        docs_documents = self._scan_directory(self.docs_dir, "docs")
        spec_documents = self._scan_directory(self.spec_workflow_dir, "spec-workflow")
        
        all_documents = docs_documents + spec_documents
        
        # 分析文档内容和分类
        analyzed_documents = []
        for doc in all_documents:
            analyzed_doc = self._analyze_document(doc)
            if analyzed_doc:
                analyzed_documents.append(analyzed_doc)
        
        # 生成统计信息
        stats = self._generate_statistics(analyzed_documents)
        
        # 生成导航映射
        navigation_mapping = self._generate_navigation_mapping(analyzed_documents)
        
        result = {
            "metadata": {
                "analysis_time": datetime.now().isoformat(),
                "total_documents": len(analyzed_documents),
                "project_root": str(self.project_root)
            },
            "statistics": stats,
            "documents": [asdict(doc) for doc in analyzed_documents],
            "navigation_mapping": navigation_mapping
        }
        
        print(f"[SUCCESS] 分析完成！共处理 {len(analyzed_documents)} 个文档")
        return result
    
    def _scan_directory(self, directory: Path, system: str) -> List[Dict[str, Any]]:
        """扫描目录获取基础文档信息"""
        documents = []
        
        if not directory.exists():
            print(f"[WARN] 目录不存在: {directory}")
            return documents
        
        for root, dirs, files in os.walk(directory):
            # 跳过隐藏目录和特殊目录
            dirs[:] = [d for d in dirs if not d.startswith('.') and d not in ['node_modules', '__pycache__', '.git']]
            
            for file in files:
                if file.endswith(('.md', '.txt', '.rst', '.adoc')):
                    file_path = Path(root) / file
                    relative_path = file_path.relative_to(self.project_root)
                    
                    # 获取分类信息
                    category = self._get_category_from_path(relative_path, system)
                    
                    doc_info = {
                        "path": str(relative_path),
                        "full_path": str(file_path),
                        "name": file_path.stem,
                        "system": system,
                        "category": category,
                        "file_type": file_path.suffix.lower(),
                        "size": file_path.stat().st_size,
                        "modified_time": datetime.fromtimestamp(file_path.stat().st_mtime).isoformat()
                    }
                    
                    documents.append(doc_info)
        
        return documents
    
    def _get_category_from_path(self, path: Path, system: str) -> str:
        """从路径获取文档分类"""
        parts = path.parts
        
        if system == "docs":
            if len(parts) > 1:
                return parts[1]  # docs/下的第一个子目录作为分类
            else:
                return "root"
        elif system == "spec-workflow":
            if len(parts) > 2:
                return parts[2]  # .spec-workflow/下的第二层作为分类
            elif len(parts) > 1:
                return parts[1]
            else:
                return "root"
        
        return "unknown"
    
    def _analyze_document(self, doc_info: Dict[str, Any]) -> Optional[DocumentInfo]:
        """分析单个文档的内容"""
        try:
            file_path = Path(doc_info["full_path"])
            
            # 读取文件内容
            with open(file_path, 'r', encoding='utf-8') as f:
                content = f.read()
            
            # 提取标题（第一行或第一个#标题）
            title = self._extract_title(content)
            
            # 提取描述（前几段文字）
            description = self._extract_description(content)
            
            # 分析关键词
            keywords = self._extract_keywords(content)
            
            # 分析相关角色
            roles = self._analyze_roles(content)
            
            # 分析相关任务类型
            tasks = self._analyze_tasks(content)
            
            # 提取交叉引用
            cross_references = self._extract_cross_references(content)
            
            return DocumentInfo(
                path=doc_info["path"],
                full_path=doc_info["full_path"],
                name=doc_info["name"],
                title=title,
                system=doc_info["system"],
                category=doc_info["category"],
                file_type=doc_info["file_type"],
                size=doc_info["size"],
                modified_time=doc_info["modified_time"],
                roles=roles,
                tasks=tasks,
                keywords=keywords,
                description=description,
                cross_references=cross_references
            )
            
        except Exception as e:
            print(f"[ERROR] 分析文档失败 {doc_info['path']}: {e}")
            return None
    
    def _extract_title(self, content: str) -> str:
        """提取文档标题"""
        lines = content.strip().split('\n')
        
        for line in lines:
            line = line.strip()
            if line.startswith('# '):
                return line[2:].strip()
            elif line and not line.startswith('#') and not line.startswith('```'):
                return line
        
        return "未命名文档"
    
    def _extract_description(self, content: str) -> str:
        """提取文档描述"""
        lines = content.strip().split('\n')
        description_parts = []
        
        skip_until_blank = True
        for line in lines:
            line = line.strip()
            
            # 跳过标题和空行
            if line.startswith('#') or not line:
                if line.startswith('#'):
                    skip_until_blank = True
                continue
            
            # 跳过第一个非标题行后的空行
            if skip_until_blank and not line:
                skip_until_blank = False
                continue
            
            if not skip_until_blank:
                if line.startswith('```'):
                    break  # 遇到代码块停止
                description_parts.append(line)
                if len(description_parts) >= 3:  # 最多取3行作为描述
                    break
        
        description = ' '.join(description_parts)
        return description[:200] + "..." if len(description) > 200 else description
    
    def _extract_keywords(self, content: str) -> List[str]:
        """提取关键词"""
        keywords = set()
        
        # 从标题中提取
        lines = content.split('\n')
        for line in lines[:10]:  # 只检查前10行
            if line.startswith('# '):
                words = line[2:].strip().split()
                keywords.update([w.lower() for w in words if len(w) > 2])
        
        # 从特殊标记中提取
        if '**' in content:
            import re
            bold_words = re.findall(r'\*\*(.*?)\*\*', content)
            keywords.update([w.lower() for w in bold_words if len(w) > 2])
        
        return list(keywords)[:20]  # 最多返回20个关键词
    
    def _analyze_roles(self, content: str) -> List[str]:
        """分析文档相关的角色"""
        content_lower = content.lower()
        roles = []
        
        for role, patterns in self.role_patterns.items():
            for pattern in patterns:
                if pattern in content_lower:
                    roles.append(role)
                    break
        
        return roles
    
    def _analyze_tasks(self, content: str) -> List[str]:
        """分析文档相关的任务类型"""
        content_lower = content.lower()
        tasks = []
        
        for task, patterns in self.task_patterns.items():
            for pattern in patterns:
                if pattern in content_lower:
                    tasks.append(task)
                    break
        
        return tasks
    
    def _extract_cross_references(self, content: str) -> List[str]:
        """提取交叉引用"""
        import re
        
        # 提取Markdown链接
        markdown_links = re.findall(r'\[([^\]]+)\]\(([^)]+)\)', content)
        references = [ref for _, ref in markdown_links if ref.endswith('.md')]
        
        # 提取相对路径引用
        relative_refs = re.findall(r'\.\.\/([^)]+\.md)', content)
        references.extend([f"../{ref}" for ref in relative_refs])
        
        return list(set(references))
    
    def _generate_statistics(self, documents: List[DocumentInfo]) -> Dict[str, Any]:
        """生成统计信息"""
        stats = {
            "by_system": {},
            "by_category": {},
            "by_role": {},
            "by_task": {},
            "file_types": {},
            "total_size": 0
        }
        
        for doc in documents:
            # 按体系统计
            stats["by_system"][doc.system] = stats["by_system"].get(doc.system, 0) + 1
            
            # 按分类统计
            stats["by_category"][doc.category] = stats["by_category"].get(doc.category, 0) + 1
            
            # 按角色统计
            for role in doc.roles:
                stats["by_role"][role] = stats["by_role"].get(role, 0) + 1
            
            # 按任务类型统计
            for task in doc.tasks:
                stats["by_task"][task] = stats["by_task"].get(task, 0) + 1
            
            # 按文件类型统计
            stats["file_types"][doc.file_type] = stats["file_types"].get(doc.file_type, 0) + 1
            
            # 总大小
            stats["total_size"] += doc.size
        
        return stats
    
    def _generate_navigation_mapping(self, documents: List[DocumentInfo]) -> Dict[str, Any]:
        """生成导航映射"""
        mapping = {
            "role_based": {},
            "task_based": {},
            "category_based": {},
            "quick_access": []
        }
        
        # 角色导航映射
        for role in ["developer", "architect", "pm", "tester"]:
            mapping["role_based"][role] = [
                {
                    "path": doc.path,
                    "title": doc.title,
                    "description": doc.description,
                    "category": doc.category,
                    "keywords": doc.keywords[:5]  # 只取前5个关键词
                }
                for doc in documents if role in doc.roles
            ]
        
        # 任务导航映射
        for task in ["development", "bugfix", "architecture", "maintenance"]:
            mapping["task_based"][task] = [
                {
                    "path": doc.path,
                    "title": doc.title,
                    "description": doc.description,
                    "category": doc.category,
                    "keywords": doc.keywords[:5]
                }
                for doc in documents if task in doc.tasks
            ]
        
        # 分类导航映射
        categories = set(doc.category for doc in documents)
        for category in sorted(categories):
            mapping["category_based"][category] = [
                {
                    "path": doc.path,
                    "title": doc.title,
                    "description": doc.description,
                    "system": doc.system,
                    "keywords": doc.keywords[:5]
                }
                for doc in documents if doc.category == category
            ]
        
        # 快速访问（重要文档）
        scored_docs = []
        for doc in documents:
            score = 0
            # 索引文档
            if doc.path.endswith("index.md"):
                score += 10
            # 重要的指南
            if doc.name.lower() in ["readme", "guide"]:
                score += 8
            # 核心分类
            if doc.category in ["architecture", "development"]:
                score += 6
            # 多角色相关
            if len(doc.roles) > 1:
                score += 4
            # 关键词丰富
            if len(doc.keywords) > 5:
                score += 2
            scored_docs.append((doc, score))
        
        # 按分数排序，取前20个作为快速访问
        scored_docs.sort(key=lambda x: x[1], reverse=True)
        mapping["quick_access"] = [
            {
                "path": doc.path,
                "title": doc.title,
                "description": doc.description,
                "category": doc.category,
                "system": doc.system,
                "score": score
            }
            for doc, score in scored_docs[:20]
        ]
        
        return mapping
    
    def save_results(self, results: Dict[str, Any], output_file: str):
        """保存分析结果"""
        output_path = Path(output_file)
        output_path.parent.mkdir(parents=True, exist_ok=True)
        
        with open(output_path, 'w', encoding='utf-8') as f:
            json.dump(results, f, ensure_ascii=False, indent=2)
        
        print(f"[INFO] 分析结果已保存到: {output_path}")
    
    def print_summary(self, results: Dict[str, Any]):
        """打印分析摘要"""
        stats = results["statistics"]
        
        print("\n" + "="*60)
        print("[REPORT] 文档结构分析摘要")
        print("="*60)
        
        print(f"[STATS] 总文档数: {results['metadata']['total_documents']}")
        print(f"[STATS] 文档体系分布:")
        for system, count in stats["by_system"].items():
            print(f"   - {system}: {count} 个文档")
        
        print(f"\n[STATS] 分类分布:")
        for category, count in sorted(stats["by_category"].items()):
            print(f"   - {category}: {count} 个文档")
        
        print(f"\n[STATS] 角色相关性:")
        for role, count in stats["by_role"].items():
            print(f"   - {role}: {count} 个文档")
        
        print(f"\n[STATS] 任务类型相关性:")
        for task, count in stats["by_task"].items():
            print(f"   - {task}: {count} 个文档")
        
        print(f"\n[STATS] 总大小: {stats['total_size'] / 1024:.1f} KB")
        print("="*60)


def main():
    """主函数"""
    parser = argparse.ArgumentParser(description="文档结构分析脚本")
    parser.add_argument("--project-root", default=".", help="项目根目录路径")
    parser.add_argument("--output", default="document-analysis-results.json", help="输出文件路径")
    parser.add_argument("--verbose", action="store_true", help="详细输出")
    
    args = parser.parse_args()
    
    # 创建分析器
    analyzer = DocumentAnalyzer(args.project_root)
    
    # 执行分析
    results = analyzer.analyze_documents()
    
    # 打印摘要
    analyzer.print_summary(results)
    
    # 保存结果
    analyzer.save_results(results, args.output)
    
    if args.verbose:
        print(f"\n[INFO] 详细分析结果已保存到: {args.output}")


if __name__ == "__main__":
    main()
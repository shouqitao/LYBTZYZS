#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
文档交叉引用系统构建脚本

自动扫描文档内容，识别相关文档并建立交叉引用链接
支持docs/和spec-workflow/双体系文档的智能关联
"""

import os
import re
import json
from pathlib import Path
from typing import Dict, List, Tuple, Set
from dataclasses import dataclass, asdict
from collections import defaultdict, Counter

@dataclass
class DocumentReference:
    """文档引用信息"""
    source_file: str
    target_file: str
    reference_type: str  # 'explicit_link', 'content_mention', 'semantic_relation'
    context: str  # 引用上下文
    confidence: float  # 置信度 0-1

@dataclass
class DocumentMetadata:
    """文档元数据"""
    file_path: str
    title: str
    content_hash: str
    keywords: List[str]
    roles: List[str]  # 相关角色
    tasks: List[str]  # 相关任务类型
    category: str  # 文档分类

class CrossReferenceBuilder:
    """交叉引用构建器"""

    def __init__(self, project_root: str):
        self.project_root = Path(project_root)
        self.docs_dir = self.project_root / "docs"
        self.spec_dir = self.project_root / ".spec-workflow"

        # 定义角色和任务关键词
        self.role_keywords = {
            "开发者": ["developer", "development", "code", "programming", "编码", "开发", "代码"],
            "架构师": ["architect", "architecture", "design", "pattern", "架构", "设计", "模式"],
            "项目经理": ["project", "manager", "planning", "schedule", "项目", "管理", "计划"],
            "测试工程师": ["test", "qa", "quality", "testing", "测试", "质量", "保证"]
        }

        self.task_keywords = {
            "开发新功能": ["feature", "new", "development", "功能", "新开发", "特性"],
            "修复Bug": ["bug", "fix", "issue", "problem", "修复", "问题", "缺陷"],
            "架构设计": ["architecture", "design", "structure", "架构", "设计", "结构"],
            "维护文档": ["documentation", "maintain", "update", "文档", "维护", "更新"]
        }

        # 文档分类映射
        self.category_patterns = {
            "architecture": [r"architecture", r"设计", r"架构", r"ADR"],
            "development": [r"development", r"开发", r"coding", r"programming"],
            "testing": [r"test", r"testing", r"测试", r"qa", r"quality"],
            "api": [r"api", r"interface", r"接口"],
            "deployment": [r"deploy", r"deployment", r"部署"],
            "security": [r"security", r"安全"],
            "project": [r"project", r"项目", r"管理", r"planning"]
        }

        self.references: List[DocumentReference] = []
        self.documents: Dict[str, DocumentMetadata] = {}

    def scan_documents(self) -> None:
        """扫描所有文档并提取元数据"""
        print("[INFO] 扫描文档结构...")

        # 扫描docs/目录
        self._scan_directory(self.docs_dir, "docs")

        # 扫描spec-workflow/目录
        if self.spec_dir.exists():
            self._scan_directory(self.spec_dir / "specs", "spec")
            self._scan_directory(self.spec_dir / "archive", "archive")
            self._scan_directory(self.spec_dir / "steering", "steering")

        print(f"[INFO] 共扫描到 {len(self.documents)} 个文档")

    def _scan_directory(self, directory: Path, doc_type: str) -> None:
        """扫描指定目录下的文档"""
        if not directory.exists():
            return

        for file_path in directory.rglob("*.md"):
            if file_path.is_file():
                metadata = self._extract_metadata(file_path, doc_type)
                if metadata:
                    relative_path = str(file_path.relative_to(self.project_root))
                    self.documents[relative_path] = metadata

    def _extract_metadata(self, file_path: Path, doc_type: str) -> DocumentMetadata:
        """提取文档元数据"""
        try:
            with open(file_path, 'r', encoding='utf-8') as f:
                content = f.read()
        except Exception as e:
            print(f"[WARNING] 读取文件失败 {file_path}: {e}")
            return None

        # 提取标题
        title = self._extract_title(content)

        # 生成内容哈希
        import hashlib
        content_hash = hashlib.md5(content.encode()).hexdigest()

        # 提取关键词
        keywords = self._extract_keywords(content)

        # 识别相关角色
        roles = self._identify_roles(content, keywords)

        # 识别相关任务类型
        tasks = self._identify_tasks(content, keywords)

        # 确定文档分类
        category = self._categorize_document(file_path, content)

        return DocumentMetadata(
            file_path=str(file_path.relative_to(self.project_root)),
            title=title,
            content_hash=content_hash,
            keywords=keywords,
            roles=roles,
            tasks=tasks,
            category=category
        )

    def _extract_title(self, content: str) -> str:
        """提取文档标题"""
        # 尝试从第一个# 标题提取
        match = re.search(r'^#\s+(.+)$', content, re.MULTILINE)
        if match:
            return match.group(1).strip()

        # 尝试从文件名提取
        return "未命名文档"

    def _extract_keywords(self, content: str) -> List[str]:
        """提取关键词"""
        # 提取英文关键词
        words = re.findall(r'\b[a-zA-Z]{3,}\b', content.lower())

        # 过滤常见词汇
        stop_words = {'the', 'and', 'for', 'are', 'with', 'this', 'that', 'from', 'they', 'have', 'been', 'has', 'had', 'was', 'were', 'will', 'would', 'could', 'should'}
        words = [word for word in words if word not in stop_words and len(word) > 3]

        # 统计词频并返回前20个
        word_freq = Counter(words)
        return [word for word, _ in word_freq.most_common(20)]

    def _identify_roles(self, content: str, keywords: List[str]) -> List[str]:
        """识别文档相关角色"""
        content_lower = content.lower()
        roles = []

        for role, role_keywords in self.role_keywords.items():
            for keyword in role_keywords:
                if keyword in content_lower:
                    roles.append(role)
                    break

        return roles

    def _identify_tasks(self, content: str, keywords: List[str]) -> List[str]:
        """识别文档相关任务类型"""
        content_lower = content.lower()
        tasks = []

        for task, task_keywords in self.task_keywords.items():
            for keyword in task_keywords:
                if keyword in content_lower:
                    tasks.append(task)
                    break

        return tasks

    def _categorize_document(self, file_path: Path, content: str) -> str:
        """确定文档分类"""
        path_str = str(file_path).lower()
        content_lower = content.lower()

        for category, patterns in self.category_patterns.items():
            for pattern in patterns:
                if re.search(pattern, path_str, re.IGNORECASE) or re.search(pattern, content_lower, re.IGNORECASE):
                    return category

        return "general"

    def build_explicit_references(self) -> None:
        """构建显式链接引用"""
        print("[INFO] 构建显式链接引用...")

        link_pattern = r'\[([^\]]+)\]\(([^)]+)\)'

        for doc_path, metadata in self.documents.items():
            try:
                full_path = self.project_root / doc_path
                with open(full_path, 'r', encoding='utf-8') as f:
                    content = f.read()

                # 查找所有Markdown链接
                matches = re.finditer(link_pattern, content)

                for match in matches:
                    link_text = match.group(1)
                    target_path = match.group(2)

                    # 处理相对路径链接
                    if not target_path.startswith(('http://', 'https://', 'mailto:', '#')):
                        target_path = self._resolve_relative_path(doc_path, target_path)

                        if target_path in self.documents:
                            reference = DocumentReference(
                                source_file=doc_path,
                                target_file=target_path,
                                reference_type="explicit_link",
                                context=link_text,
                                confidence=1.0
                            )
                            self.references.append(reference)

            except Exception as e:
                print(f"[WARNING] 处理显式链接失败 {doc_path}: {e}")

    def _resolve_relative_path(self, source_path: str, relative_path: str) -> str:
        """解析相对路径"""
        source_dir = Path(source_path).parent
        target_path = (source_dir / relative_path).resolve()

        # 尝试不同的文件扩展名
        for ext in ['', '.md', '.txt']:
            candidate = target_path.with_suffix(ext) if ext else target_path
            try:
                relative = candidate.relative_to(self.project_root)
                if str(relative) in self.documents:
                    return str(relative)
            except (ValueError, FileNotFoundError):
                continue

        return None

    def build_semantic_references(self) -> None:
        """构建语义关联引用"""
        print("🧠 构建语义关联引用...")

        # 计算文档相似度
        for doc1_path, meta1 in self.documents.items():
            for doc2_path, meta2 in self.documents.items():
                if doc1_path >= doc2_path:  # 避免重复和自引用
                    continue

                similarity = self._calculate_similarity(meta1, meta2)

                if similarity > 0.3:  # 相似度阈值
                    reference = DocumentReference(
                        source_file=doc1_path,
                        target_file=doc2_path,
                        reference_type="semantic_relation",
                        context=f"相似度: {similarity:.2f}",
                        confidence=similarity
                    )
                    self.references.append(reference)

    def _calculate_similarity(self, meta1: DocumentMetadata, meta2: DocumentMetadata) -> float:
        """计算文档相似度"""
        score = 0.0

        # 角色匹配 (权重: 0.3)
        common_roles = set(meta1.roles) & set(meta2.roles)
        if common_roles:
            score += 0.3 * (len(common_roles) / len(set(meta1.roles) | set(meta2.roles)))

        # 任务匹配 (权重: 0.3)
        common_tasks = set(meta1.tasks) & set(meta2.tasks)
        if common_tasks:
            score += 0.3 * (len(common_tasks) / len(set(meta1.tasks) | set(meta2.tasks)))

        # 分类匹配 (权重: 0.2)
        if meta1.category == meta2.category:
            score += 0.2

        # 关键词匹配 (权重: 0.2)
        common_keywords = set(meta1.keywords) & set(meta2.keywords)
        if common_keywords:
            score += 0.2 * (len(common_keywords) / max(len(meta1.keywords), len(meta2.keywords)))

        return min(score, 1.0)

    def build_content_mentions(self) -> None:
        """构建内容提及引用"""
        print("📝 构建内容提及引用...")

        for doc1_path, meta1 in self.documents.items():
            for doc2_path, meta2 in self.documents.items():
                if doc1_path == doc2_path:
                    continue

                try:
                    # 检查doc1是否提及doc2的标题
                    full_path1 = self.project_root / doc1_path
                    with open(full_path1, 'r', encoding='utf-8') as f:
                        content1 = f.read()

                    if meta2.title.lower() in content1.lower():
                        reference = DocumentReference(
                            source_file=doc1_path,
                            target_file=doc2_path,
                            reference_type="content_mention",
                            context=f"提及: {meta2.title}",
                            confidence=0.8
                        )
                        self.references.append(reference)

                except Exception as e:
                    print(f"⚠️  处理内容提及失败 {doc1_path}: {e}")

    def generate_cross_reference_data(self) -> Dict:
        """生成交叉引用数据"""
        print("📊 生成交叉引用数据...")

        # 按源文档分组引用
        references_by_source = defaultdict(list)
        for ref in self.references:
            references_by_source[ref.source_file].append(ref)

        # 生成推荐文档
        recommendations = {}
        for doc_path, metadata in self.documents.items():
            recommendations[doc_path] = self._generate_recommendations(doc_path)

        return {
            "documents": {path: asdict(meta) for path, meta in self.documents.items()},
            "references": [asdict(ref) for ref in self.references],
            "references_by_source": dict(references_by_source),
            "recommendations": recommendations,
            "statistics": {
                "total_documents": len(self.documents),
                "total_references": len(self.references),
                "references_by_type": dict(Counter([ref.reference_type for ref in self.references])),
                "documents_by_category": dict(Counter([meta.category for meta in self.documents.values()])),
                "documents_by_role": self._count_by_role()
            }
        }

    def _generate_recommendations(self, doc_path: str) -> List[Dict]:
        """为文档生成推荐"""
        if doc_path not in self.documents:
            return []

        metadata = self.documents[doc_path]

        # 获取所有相关引用
        related_refs = [ref for ref in self.references if ref.source_file == doc_path]

        # 按置信度排序
        related_refs.sort(key=lambda x: x.confidence, reverse=True)

        # 生成推荐列表
        recommendations = []
        for ref in related_refs[:10]:  # 最多10个推荐
            if ref.target_file in self.documents:
                target_meta = self.documents[ref.target_file]
                recommendations.append({
                    "document_path": ref.target_file,
                    "title": target_meta.title,
                    "category": target_meta.category,
                    "relevance": ref.confidence,
                    "reason": ref.context,
                    "type": ref.reference_type
                })

        return recommendations

    def _count_by_role(self) -> Dict[str, int]:
        """按角色统计文档数量"""
        role_count = defaultdict(int)
        for metadata in self.documents.values():
            for role in metadata.roles:
                role_count[role] += 1
        return dict(role_count)

    def save_results(self, output_path: str) -> None:
        """保存交叉引用结果"""
        cross_ref_data = self.generate_cross_reference_data()

        output_file = self.project_root / output_path
        with open(output_file, 'w', encoding='utf-8') as f:
            json.dump(cross_ref_data, f, ensure_ascii=False, indent=2)

        print(f"💾 交叉引用数据已保存到: {output_file}")

        # 生成人类可读的报告
        self._generate_human_readable_report(cross_ref_data, output_file.with_suffix('.md'))

    def _generate_human_readable_report(self, data: Dict, output_path: Path) -> None:
        """生成人类可读的报告"""
        report = f"""# 文档交叉引用分析报告

## 📊 统计概览

- **总文档数**: {data['statistics']['total_documents']}
- **总引用数**: {data['statistics']['total_references']}
- **按类型分类**:
"""

        for ref_type, count in data['statistics']['references_by_type'].items():
            type_name = {
                'explicit_link': '显式链接',
                'content_mention': '内容提及',
                'semantic_relation': '语义关联'
            }.get(ref_type, ref_type)
            report += f"  - {type_name}: {count}\n"

        report += f"\n- **按分类统计**:\n"
        for category, count in data['statistics']['documents_by_category'].items():
            report += f"  - {category}: {count}\n"

        report += f"\n- **按角色统计**:\n"
        for role, count in data['statistics']['documents_by_role'].items():
            report += f"  - {role}: {count}\n"

        report += "\n## 📋 推荐文档映射\n\n"

        # 按分类显示推荐
        category_docs = defaultdict(list)
        for doc_path, metadata in data['documents'].items():
            category_docs[metadata['category']].append((doc_path, metadata))

        for category, docs in category_docs.items():
            report += f"### {category.upper()} 类别\n\n"

            for doc_path, metadata in docs[:5]:  # 每个类别最多显示5个
                report += f"#### {metadata['title']}\n"
                report += f"路径: `{doc_path}`\n"

                if doc_path in data['recommendations'] and data['recommendations'][doc_path]:
                    report += "\n**推荐相关文档**:\n"
                    for rec in data['recommendations'][doc_path][:3]:  # 最多显示3个推荐
                        report += f"- [{rec['title']}]({rec['document_path']}) ({rec['relevance']:.2f}) - {rec['reason']}\n"

                report += "\n---\n\n"

        with open(output_path, 'w', encoding='utf-8') as f:
            f.write(report)

        print(f"📄 交叉引用报告已保存到: {output_path}")

def main():
    """主函数"""
    project_root = Path(__file__).parent.parent.parent

    print("🚀 开始构建文档交叉引用系统...")

    builder = CrossReferenceBuilder(str(project_root))

    # 扫描文档
    builder.scan_documents()

    if not builder.documents:
        print("❌ 未找到任何文档，退出")
        return

    # 构建引用关系
    builder.build_explicit_references()
    builder.build_content_mentions()
    builder.build_semantic_references()

    # 保存结果
    builder.save_results("scripts/documentation-maintenance/cross-references.json")

    print("✅ 文档交叉引用系统构建完成!")

if __name__ == "__main__":
    main()
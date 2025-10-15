#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
智能内容价值分析和文档整合工具
基于内容评估算法，识别技术债务文档，提取有价值内容，生成精简的文档体系

核心策略：
1. 评估每个文档的内容价值（时效性、唯一性、完整性、权威性）
2. 识别技术债务和重复内容
3. 智能内容提取和整合
4. 生成精简的高质量文档体系
"""

import os
import json
import re
import hashlib
from datetime import datetime, timedelta
from pathlib import Path
from dataclasses import dataclass, asdict
from typing import Dict, List, Set, Tuple, Optional
from collections import defaultdict, Counter
import logging

# 配置日志
logging.basicConfig(
    level=logging.INFO,
    format='%(asctime)s - %(levelname)s - %(message)s',
    handlers=[
        logging.FileHandler('content-consolidation.log', encoding='utf-8'),
        logging.StreamHandler()
    ]
)
logger = logging.getLogger(__name__)

@dataclass
class ContentMetrics:
    """内容价值评估指标"""
    file_path: str
    file_size: int
    last_modified: datetime

    # 时效性指标
    freshness_score: float  # 0-1, 越新越高
    relevance_score: float  # 0-1, 是否仍适用

    # 唯一性指标
    uniqueness_score: float  # 0-1, 内容的独特性
    duplicate_ratio: float   # 0-1, 与其他文档的重复度

    # 完整性指标
    completeness_score: float  # 0-1, 内容的完整程度
    info_density: float        # 0-1, 信息密度

    # 权威性指标
    authority_score: float  # 0-1, 是否是决策性/标准性文档
    reference_count: int    # 被其他文档引用的次数

    # 综合评分
    overall_value: float    # 0-1, 综合内容价值评分

    # 分类标签
    content_type: str       # 'standard', 'decision', 'guide', 'report', 'temporary'
    debt_level: str         # 'low', 'medium', 'high', 'critical'

    # 处理建议
    recommended_action: str # 'keep', 'extract', 'merge', 'delete'

class ContentAnalyzer:
    """内容价值分析器"""

    def __init__(self, project_root: str):
        self.project_root = Path(project_root)
        self.docs_dir = self.project_root / 'docs'
        self.spec_dir = self.project_root / '.spec-workflow'

        # 加载已有的交叉引用数据
        self.cross_refs = self._load_cross_references()

        # 核心文档清单（高价值，不可删除）
        self.core_documents = {
            'docs/index.md',
            'docs/architecture/server-module-design-standard.md',
            'docs/architecture/client/unified-design-standard.md',
            'docs/architecture/ADR-003-server-module-unified-design.md',
            'docs/architecture/testing/architecture-testing-guide.md',
            'docs/development/test-architecture-standard.md',
            'docs/development/testing-guide.md',
            'docs/development/repository-dependency-injection-guide.md',
            'CLAUDE.md',
            'README.md'
        }

        # 内容关键词权重
        self.authority_keywords = {
            'standard', 'specification', 'architecture', 'decision', 'ADR',
            'requirement', 'principle', 'guideline', 'policy'
        }
        self.temporary_keywords = {
            'draft', 'temp', 'backup', 'old', 'deprecated', 'todo',
            'wip', 'scratch', 'note', 'memo', 'log'
        }

    def _load_cross_references(self) -> Dict:
        """加载交叉引用数据"""
        ref_file = self.project_root / 'scripts/documentation-maintenance/cross-references.json'
        if ref_file.exists():
            with open(ref_file, 'r', encoding='utf-8') as f:
                return json.load(f)
        return {}

    def _calculate_freshness(self, file_path: Path, content: str) -> float:
        """计算时效性评分"""
        try:
            mtime = datetime.fromtimestamp(file_path.stat().st_mtime)
            days_old = (datetime.now() - mtime).days

            # 基础时效性评分
            if days_old <= 30:
                base_score = 1.0
            elif days_old <= 90:
                base_score = 0.8
            elif days_old <= 180:
                base_score = 0.6
            elif days_old <= 365:
                base_score = 0.4
            else:
                base_score = 0.2

            # 内容中日期线索调整
            date_patterns = [
                r'(\d{4}-\d{2}-\d{2})',  # 2025-01-01
                r'(\d{4}/\d{2}/\d{2})',  # 2025/01/01
                r'(\d{1,2}/\d{1,2}/202\d)',  # 1/1/2025
            ]

            latest_mentioned = None
            for pattern in date_patterns:
                matches = re.findall(pattern, content)
                if matches:
                    dates = []
                    for match in matches:
                        try:
                            if '-' in match:
                                date = datetime.strptime(match, '%Y-%m-%d')
                            elif '/' in match and len(match.split('/')[0]) == 4:
                                date = datetime.strptime(match, '%Y/%m/%d')
                            else:
                                date = datetime.strptime(match, '%m/%d/%Y')
                            dates.append(date)
                        except ValueError:
                            continue
                    if dates:
                        latest_mentioned = max(dates)

            if latest_mentioned:
                content_days_old = (datetime.now() - latest_mentioned).days
                if content_days_old <= 30:
                    return min(1.0, base_score + 0.2)
                elif content_days_old <= 90:
                    return base_score
                else:
                    return max(0.1, base_score - 0.3)

            return base_score
        except Exception as e:
            logger.warning(f"计算时效性失败 {file_path}: {e}")
            return 0.5

    def _calculate_uniqueness(self, file_path: Path, content: str) -> Tuple[float, float]:
        """计算唯一性评分和重复度"""
        # 简化的重复度检测，基于内容相似性
        content_hash = hashlib.md5(content.encode()).hexdigest()

        # 检查与其他文档的相似性
        similarity_scores = []
        content_signature = self._extract_content_signature(content)

        # 使用交叉引用数据计算重复度
        for other_file, other_content_hash in self.cross_refs.items():
            if str(file_path) in other_file:
                continue

            # 简单的相似性检测
            other_path = Path(other_file)
            if other_path.exists():
                try:
                    with open(other_path, 'r', encoding='utf-8') as f:
                        other_content = f.read()
                    other_signature = self._extract_content_signature(other_content)

                    # 计算签名相似度
                    intersection = len(content_signature & other_signature)
                    union = len(content_signature | other_signature)
                    if union > 0:
                        similarity = intersection / union
                        similarity_scores.append(similarity)
                except Exception as e:
                    continue

        if similarity_scores:
            avg_similarity = sum(similarity_scores) / len(similarity_scores)
            uniqueness_score = 1.0 - avg_similarity
            duplicate_ratio = avg_similarity
        else:
            uniqueness_score = 1.0
            duplicate_ratio = 0.0

        return uniqueness_score, duplicate_ratio

    def _extract_content_signature(self, content: str) -> Set[str]:
        """提取内容签名用于相似性检测"""
        # 提取关键词和短语
        words = re.findall(r'\b[a-zA-Z_]{3,}\b', content.lower())
        important_words = {w for w in words if len(w) > 4}

        # 提取技术术语
        tech_terms = set()
        tech_patterns = [
            r'\b[A-Z]{2,}\b',  # 缩写
            r'\b[a-z]+[A-Z][a-zA-Z]*\b',  # 驼峰命名
            r'\b\w*Repository\b',  # Repository模式
            r'\b\w*Service\b',    # Service模式
            r'\b\w*Controller\b', # Controller模式
        ]

        for pattern in tech_patterns:
            terms = re.findall(pattern, content)
            tech_terms.update(terms)

        return important_words | tech_terms

    def _calculate_completeness(self, content: str) -> Tuple[float, float]:
        """计算完整性和信息密度"""
        if not content:
            return 0.0, 0.0

        # 文档长度评分
        length_score = min(1.0, len(content) / 2000)  # 2000字符为满分

        # 结构完整性评分
        structure_indicators = [
            r'^# ',           # 主标题
            r'^## ',          # 二级标题
            r'^### ',         # 三级标题
            r'^\* ',          # 列表
            r'^- ',           # 列表
            r'^1\. ',         # 有序列表
            r'```',           # 代码块
            r'\[.*\]\(.*\)',  # 链接
        ]

        structure_score = 0
        for pattern in structure_indicators:
            if re.search(pattern, content, re.MULTILINE):
                structure_score += 1
        structure_score = min(1.0, structure_score / len(structure_indicators))

        # 信息密度（技术术语密度）
        tech_density = len(self._extract_content_signature(content)) / len(content.split())
        density_score = min(1.0, tech_density / 0.1)  # 10%为满分

        completeness = (length_score + structure_score) / 2
        info_density = density_score

        return completeness, info_density

    def _calculate_authority(self, file_path: str, content: str) -> Tuple[float, int]:
        """计算权威性评分和引用次数"""
        authority_score = 0.0

        # 文件路径权重
        path_lower = file_path.lower()
        if any(keyword in path_lower for keyword in ['architecture', 'standard', 'adr']):
            authority_score += 0.3
        elif any(keyword in path_lower for keyword in ['guide', 'development']):
            authority_score += 0.2
        elif any(keyword in path_lower for keyword in ['report', 'temp', 'log']):
            authority_score -= 0.2

        # 内容关键词权重
        content_lower = content.lower()
        authority_count = sum(1 for keyword in self.authority_keywords if keyword in content_lower)
        temporary_count = sum(1 for keyword in self.temporary_keywords if keyword in content_lower)

        authority_score += min(0.5, authority_count * 0.1)
        authority_score -= min(0.3, temporary_count * 0.1)

        authority_score = max(0.0, min(1.0, authority_score))

        # 引用次数（从交叉引用数据获取）
        reference_count = len(self.cross_refs.get(str(file_path), {}).get('referenced_by', []))

        return authority_score, reference_count

    def _classify_content(self, file_path: str, content: str, scores: dict) -> Tuple[str, str]:
        """分类内容类型和债务级别"""
        path_lower = file_path.lower()
        content_lower = content.lower()

        # 内容类型分类
        if any(keyword in path_lower for keyword in ['architecture', 'standard', 'specification']):
            content_type = 'standard'
        elif any(keyword in path_lower for keyword in ['adr', 'decision']):
            content_type = 'decision'
        elif any(keyword in path_lower for keyword in ['guide', 'tutorial', 'readme']):
            content_type = 'guide'
        elif any(keyword in path_lower for keyword in ['report', 'analysis', 'summary']):
            content_type = 'report'
        else:
            content_type = 'other'

        # 技术债务级别评估
        debt_score = 0

        # 时效性问题
        if scores['freshness'] < 0.4:
            debt_score += 1
        # 唯一性问题
        if scores['uniqueness'] < 0.6:
            debt_score += 1
        # 完整性问题
        if scores['completeness'] < 0.5:
            debt_score += 1
        # 权威性问题
        if scores['authority'] < 0.3:
            debt_score += 1
        # 临时性指标
        if any(keyword in path_lower for keyword in ['temp', 'draft', 'old', 'backup']):
            debt_score += 2

        if debt_score >= 4:
            debt_level = 'critical'
        elif debt_score >= 3:
            debt_level = 'high'
        elif debt_score >= 1:
            debt_level = 'medium'
        else:
            debt_level = 'low'

        return content_type, debt_level

    def _recommend_action(self, file_path: str, scores: dict, content_type: str, debt_level: str) -> str:
        """推荐处理动作"""
        # 核心文档必须保留
        if file_path in self.core_documents:
            return 'keep'

        # 综合评分低的优先删除
        if scores['overall'] < 0.3:
            return 'delete'
        elif scores['overall'] < 0.5:
            if debt_level in ['critical', 'high']:
                return 'delete'
            else:
                return 'extract'
        elif scores['overall'] < 0.7:
            if debt_level in ['medium', 'high']:
                return 'merge'
            else:
                return 'extract'
        else:
            return 'keep'

    def analyze_document(self, file_path: Path) -> ContentMetrics:
        """分析单个文档"""
        try:
            with open(file_path, 'r', encoding='utf-8') as f:
                content = f.read()

            # 计算各项指标
            freshness_score = self._calculate_freshness(file_path, content)
            uniqueness_score, duplicate_ratio = self._calculate_uniqueness(file_path, content)
            completeness_score, info_density = self._calculate_completeness(content)
            authority_score, reference_count = self._calculate_authority(str(file_path), content)

            # 时效性调整（基于内容判断）
            relevance_score = freshness_score  # 简化处理

            # 综合评分计算
            weights = {
                'freshness': 0.2,
                'uniqueness': 0.25,
                'completeness': 0.2,
                'authority': 0.25,
                'references': 0.1
            }

            reference_score = min(1.0, reference_count / 10)  # 10次引用为满分

            overall_value = (
                freshness_score * weights['freshness'] +
                uniqueness_score * weights['uniqueness'] +
                completeness_score * weights['completeness'] +
                authority_score * weights['authority'] +
                reference_score * weights['references']
            )

            # 内容分类
            scores = {
                'freshness': freshness_score,
                'uniqueness': uniqueness_score,
                'completeness': completeness_score,
                'authority': authority_score,
                'overall': overall_value
            }

            content_type, debt_level = self._classify_content(str(file_path), content, scores)
            recommended_action = self._recommend_action(str(file_path), scores, content_type, debt_level)

            return ContentMetrics(
                file_path=str(file_path),
                file_size=len(content),
                last_modified=datetime.fromtimestamp(file_path.stat().st_mtime),
                freshness_score=freshness_score,
                relevance_score=relevance_score,
                uniqueness_score=uniqueness_score,
                duplicate_ratio=duplicate_ratio,
                completeness_score=completeness_score,
                info_density=info_density,
                authority_score=authority_score,
                reference_count=reference_count,
                overall_value=overall_value,
                content_type=content_type,
                debt_level=debt_level,
                recommended_action=recommended_action
            )

        except Exception as e:
            logger.error(f"分析文档失败 {file_path}: {e}")
            return None

    def analyze_all_documents(self) -> List[ContentMetrics]:
        """分析所有文档"""
        logger.info("开始分析所有文档...")

        all_metrics = []

        # 扫描docs目录
        for root, dirs, files in os.walk(self.docs_dir):
            for file in files:
                if file.endswith(('.md', '.txt')):
                    file_path = Path(root) / file
                    metrics = self.analyze_document(file_path)
                    if metrics:
                        all_metrics.append(metrics)

        # 扫描spec-workflow目录
        for root, dirs, files in os.walk(self.spec_dir):
            for file in files:
                if file.endswith(('.md', '.txt')):
                    file_path = Path(root) / file
                    metrics = self.analyze_document(file_path)
                    if metrics:
                        all_metrics.append(metrics)

        # 添加核心文档
        core_paths = [self.project_root / p for p in ['CLAUDE.md', 'README.md']]
        for core_path in core_paths:
            if core_path.exists():
                metrics = self.analyze_document(core_path)
                if metrics:
                    all_metrics.append(metrics)

        logger.info(f"完成分析，共处理 {len(all_metrics)} 个文档")
        print(f"[DEBUG] 找到 {len(all_metrics)} 个文档")
        return all_metrics

    def generate_consolidation_plan(self, metrics: List[ContentMetrics]) -> Dict:
        """生成文档整合方案"""
        logger.info("生成文档整合方案...")

        # 按推荐动作分组
        actions = defaultdict(list)
        for metric in metrics:
            actions[metric.recommended_action].append(metric)

        # 统计信息
        stats = {
            'total_documents': len(metrics),
            'by_action': {action: len(docs) for action, docs in actions.items()},
            'by_type': Counter(m.content_type for m in metrics),
            'by_debt_level': Counter(m.debt_level for m in metrics),
            'average_value': sum(m.overall_value for m in metrics) / len(metrics) if metrics else 0
        }

        # 详细处理计划
        consolidation_plan = {
            'statistics': stats,
            'recommendations': {
                'keep': self._plan_keep_documents(actions['keep']),
                'extract': self._plan_extract_documents(actions['extract']),
                'merge': self._plan_merge_documents(actions['merge']),
                'delete': self._plan_delete_documents(actions['delete'])
            }
        }

        return consolidation_plan

    def _plan_keep_documents(self, docs: List[ContentMetrics]) -> Dict:
        """规划保留文档"""
        return {
            'count': len(docs),
            'documents': [asdict(doc) for doc in sorted(docs, key=lambda x: x.overall_value, reverse=True)],
            'total_size': sum(doc.file_size for doc in docs)
        }

    def _plan_extract_documents(self, docs: List[ContentMetrics]) -> Dict:
        """规划内容提取文档"""
        # 按主题分组
        topic_groups = defaultdict(list)
        for doc in docs:
            # 简化的主题分类
            if 'architecture' in doc.file_path.lower():
                topic_groups['architecture'].append(doc)
            elif 'development' in doc.file_path.lower():
                topic_groups['development'].append(doc)
            elif 'testing' in doc.file_path.lower():
                topic_groups['testing'].append(doc)
            elif 'report' in doc.file_path.lower():
                topic_groups['reports'].append(doc)
            else:
                topic_groups['other'].append(doc)

        return {
            'count': len(docs),
            'topic_groups': {topic: [asdict(doc) for doc in group_docs] for topic, group_docs in topic_groups.items()},
            'proposed_new_documents': self._propose_consolidated_docs(topic_groups)
        }

    def _plan_merge_documents(self, docs: List[ContentMetrics]) -> Dict:
        """规划合并文档"""
        # 识别相似文档进行合并
        similar_groups = []
        used_docs = set()

        for i, doc1 in enumerate(docs):
            if doc1.file_path in used_docs:
                continue
            similar_docs = [doc1]
            used_docs.add(doc1.file_path)

            for j, doc2 in enumerate(docs[i+1:], i+1):
                if doc2.file_path in used_docs:
                    continue
                # 简化的相似性检测
                if (doc1.content_type == doc2.content_type and
                    abs(doc1.overall_value - doc2.overall_value) < 0.2):
                    similar_docs.append(doc2)
                    used_docs.add(doc2.file_path)

            if len(similar_docs) > 1:
                similar_groups.append(similar_docs)

        return {
            'count': len(docs),
            'merge_groups': [[asdict(doc) for doc in group] for group in similar_groups]
        }

    def _plan_delete_documents(self, docs: List[ContentMetrics]) -> Dict:
        """规划删除文档"""
        dangerous_deletes = [doc for doc in docs if doc.content_type == 'standard' and doc.overall_value > 0.4]
        safe_deletes = [doc for doc in docs if doc not in dangerous_deletes]

        return {
            'count': len(docs),
            'safe_to_delete': [asdict(doc) for doc in safe_deletes],
            'requires_review': [asdict(doc) for doc in dangerous_deletes],
            'total_reclaimable_size': sum(doc.file_size for doc in safe_deletes)
        }

    def _propose_consolidated_docs(self, topic_groups: Dict) -> List[Dict]:
        """提出整合后的新文档结构"""
        proposals = []

        for topic, docs in topic_groups.items():
            if len(docs) > 1:  # 只有多个文档才需要整合
                proposal = {
                    'title': f'{topic.title()} - 精要总结',
                    'source_documents': [doc.file_path for doc in docs],
                    'estimated_size': sum(doc.file_size for doc in docs) // 2,  # 压缩50%
                    'content_highlights': self._extract_highlights(docs)
                }
                proposals.append(proposal)

        return proposals

    def _extract_highlights(self, docs: List[ContentMetrics]) -> List[str]:
        """提取文档亮点（简化实现）"""
        highlights = []
        for doc in docs:
            if doc.overall_value > 0.6:
                highlights.append(f"高价值内容: {doc.file_path}")
        return highlights

def main():
    """主执行函数"""
    print("[INFO] 启动智能内容价值分析...")

    # 检测项目根目录
    current_path = Path.cwd()
    if (current_path / 'docs').exists() and (current_path / '.spec-workflow').exists():
        project_root = current_path
        print(f"[DEBUG] 检测到项目根目录: {project_root}")
    else:
        parent_path = current_path.parent
        if (parent_path / 'docs').exists() and (parent_path / '.spec-workflow').exists():
            project_root = parent_path
            print(f"[DEBUG] 检测到项目根目录: {project_root}")
        else:
            print(f"[ERROR] 无法找到项目根目录，当前目录: {current_path}")
            return

    analyzer = ContentAnalyzer(project_root)

    # 分析所有文档
    metrics = analyzer.analyze_all_documents()

    # 生成整合方案
    plan = analyzer.generate_consolidation_plan(metrics)

    # 保存结果
    output_dir = project_root / 'docs' / 'reports'
    output_dir.mkdir(exist_ok=True)

    # 保存详细分析结果
    with open(output_dir / 'content-analysis-results.json', 'w', encoding='utf-8') as f:
        json.dump({
            'analysis_timestamp': datetime.now().isoformat(),
            'total_documents_analyzed': len(metrics),
            'detailed_metrics': [asdict(m) for m in metrics],
            'consolidation_plan': plan
        }, f, ensure_ascii=False, indent=2, default=str)

    # 生成人类可读的报告
    with open(output_dir / 'content-consolidation-plan.md', 'w', encoding='utf-8') as f:
        f.write("# 文档内容整合方案\n\n")
        f.write(f"**分析时间**: {datetime.now().strftime('%Y-%m-%d %H:%M:%S')}\n\n")

        stats = plan['statistics']
        f.write("## 统计概览\n\n")
        f.write(f"- **总文档数**: {stats['total_documents']}\n")
        f.write(f"- **平均内容价值**: {stats['average_value']:.2f}\n")
        f.write(f"- **内容类型分布**: {dict(stats['by_type'])}\n")
        f.write(f"- **技术债务分布**: {dict(stats['by_debt_level'])}\n\n")

        f.write("## 处理建议\n\n")
        for action, count in stats['by_action'].items():
            action_map = {'keep': '保留', 'extract': '提取', 'merge': '合并', 'delete': '删除'}
            f.write(f"- **{action_map.get(action, action)}**: {count} 个文档\n")

        f.write("\n## 预期效果\n\n")
        total_reclaimable = plan['recommendations']['delete']['total_reclaimable_size']
        delete_count = stats['by_action'].get('delete', 0)
        merge_count = stats['by_action'].get('merge', 0)
        total_reducible = delete_count + merge_count

        f.write(f"- **可删除文档大小**: {total_reclaimable:,} 字节\n")
        f.write(f"- **可减少文档数量**: {total_reducible} 个\n")
        if stats['total_documents'] > 0:
            f.write(f"- **内容精简率**: {(total_reducible / stats['total_documents'] * 100):.1f}%\n")

    print(f"[SUCCESS] 分析完成！")
    print(f"[DATA] 分析了 {len(metrics)} 个文档")
    print(f"[OUTPUT] 结果保存到: docs/reports/content-consolidation-plan.md")
    print(f"[OUTPUT] 详细数据: docs/reports/content-analysis-results.json")

    # 显示关键统计
    print(f"\n[KEY FINDINGS] 关键发现:")
    print(f"   - 可删除文档: {stats['by_action'].get('delete', 0)} 个")
    print(f"   - 需要内容提取: {stats['by_action'].get('extract', 0)} 个")
    print(f"   - 建议合并: {stats['by_action'].get('merge', 0)} 个")
    print(f"   - 保留核心文档: {stats['by_action'].get('keep', 0)} 个")
    print(f"   - 平均内容价值: {stats['average_value']:.2f}/1.0")

if __name__ == "__main__":
    main()
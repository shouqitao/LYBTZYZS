#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
智能内容提取和文档整合工具
基于内容价值分析结果，从技术债务文档中提取有价值内容，创建高信息密度的精要文档

核心功能：
1. 从低价值文档中提取有价值内容片段
2. 按主题智能聚类和去重
3. 生成结构化的精要文档
4. 保留来源引用和可追溯性
"""

import os
import json
import re
import hashlib
from datetime import datetime
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
        logging.FileHandler('content-extraction.log', encoding='utf-8'),
        logging.StreamHandler()
    ]
)
logger = logging.getLogger(__name__)

@dataclass
class ContentFragment:
    """内容片段数据结构"""
    source_file: str
    fragment_id: str
    content: str
    context_before: str
    context_after: str
    importance_score: float
    topics: List[str]
    extraction_method: str  # 'heading', 'code_block', 'list', 'paragraph'

@dataclass
class ConsolidatedDocument:
    """整合后的文档结构"""
    title: str
    output_path: str
    theme: str
    source_files: List[str]
    fragments: List[ContentFragment]
    metadata: Dict

class ContentExtractor:
    """内容提取器"""

    def __init__(self, project_root: str):
        self.project_root = Path(project_root)

        # 加载内容分析结果
        self.analysis_results = self._load_analysis_results()

        # 识别需要合并的文档
        self.merge_candidates = self._identify_merge_candidates()

        # 内容主题映射
        self.topic_keywords = {
            'architecture': [
                'architecture', 'design', 'pattern', '三层架构', 'mvvm', '模块化',
                'dependency injection', '依赖注入', '接口设计', '系统设计'
            ],
            'development': [
                'development', 'coding', 'programming', '规范', '标准',
                'best practice', '最佳实践', '代码规范', '编码标准', '开发指南'
            ],
            'testing': [
                'testing', 'test', 'unit test', 'integration test', '测试',
                '覆盖率', 'quality', '质量', '验证', '测试标准'
            ],
            'project_management': [
                'project', 'management', '进度', '里程碑', 'planning',
                'status', '报告', '总结', 'review', '评审'
            ],
            'technical_decisions': [
                'decision', 'ADR', '技术决策', '选型', 'evaluation',
                'comparison', '对比', '技术选型', '方案', '架构决策'
            ],
            'troubleshooting': [
                'problem', 'issue', 'error', '故障', '解决',
                'solution', 'fix', '修复', '问题排查', 'debug'
            ]
        }

    def _load_analysis_results(self) -> Dict:
        """加载内容分析结果"""
        analysis_file = self.project_root / 'docs/reports/content-analysis-results.json'
        if analysis_file.exists():
            with open(analysis_file, 'r', encoding='utf-8') as f:
                return json.load(f)
        return {}

    def _identify_merge_candidates(self) -> List[Dict]:
        """识别需要合并的文档"""
        if not self.analysis_results:
            return []

        candidates = []
        for metric in self.analysis_results.get('detailed_metrics', []):
            if metric.get('recommended_action') in ['merge', 'extract']:
                candidates.append(metric)

        return candidates

    def _classify_document_theme(self, file_path: str, content: str) -> List[str]:
        """分类文档主题"""
        themes = []
        path_lower = file_path.lower()
        content_lower = content.lower()

        for theme, keywords in self.topic_keywords.items():
            score = 0
            for keyword in keywords:
                if keyword in path_lower:
                    score += 2  # 路径权重更高
                if keyword in content_lower:
                    score += 1

            if score >= 2:  # 至少2分才算匹配
                themes.append(theme)

        return themes if themes else ['other']

    def _extract_content_fragments(self, file_path: str, content: str) -> List[ContentFragment]:
        """提取内容片段"""
        fragments = []

        # 提取标题和章节
        fragments.extend(self._extract_headings(file_path, content))

        # 提取代码块
        fragments.extend(self._extract_code_blocks(file_path, content))

        # 提取列表
        fragments.extend(self._extract_lists(file_path, content))

        # 提取重要段落
        fragments.extend(self._extract_important_paragraphs(file_path, content))

        return fragments

    def _extract_headings(self, file_path: str, content: str) -> List[ContentFragment]:
        """提取标题内容"""
        fragments = []
        lines = content.split('\n')

        for i, line in enumerate(lines):
            if re.match(r'^#{1,4}\s+', line):  # h1-h4
                heading_content = line.strip()

                # 获取标题下的内容
                context_lines = []
                j = i + 1
                while j < len(lines) and j < i + 10:  # 最多获取10行内容
                    if lines[j].strip() and not lines[j].startswith('#'):
                        context_lines.append(lines[j])
                    elif lines[j].startswith('#'):
                        break
                    j += 1

                if context_lines:
                    fragment_id = hashlib.md5(f"{file_path}_{heading_content}".encode()).hexdigest()[:8]
                    context = '\n'.join(context_lines)

                    importance = self._calculate_importance(heading_content + '\n' + context)
                    topics = self._classify_document_theme(file_path, heading_content + '\n' + context)

                    fragments.append(ContentFragment(
                        source_file=file_path,
                        fragment_id=fragment_id,
                        content=f"## {heading_content}\n\n{context}",
                        context_before=lines[max(0, i-2):i],
                        context_after=lines[j:min(j+3, len(lines))],
                        importance_score=importance,
                        topics=topics,
                        extraction_method='heading'
                    ))

        return fragments

    def _extract_code_blocks(self, file_path: str, content: str) -> List[ContentFragment]:
        """提取代码块"""
        fragments = []

        # 匹配代码块
        code_pattern = r'```(\w+)?\n(.*?)\n```'
        matches = re.finditer(code_pattern, content, re.DOTALL)

        for match in matches:
            language = match.group(1) or 'text'
            code_content = match.group(2)

            # 只提取有意义的代码块（长度>20字符）
            if len(code_content.strip()) > 20:
                fragment_id = hashlib.md5(f"{file_path}_{code_content[:50]}".encode()).hexdigest()[:8]

                # 检查代码块的价值
                importance = self._calculate_code_importance(code_content, language)
                if importance > 0.3:  # 只保留有价值的代码块
                    topics = self._classify_document_theme(file_path, code_content)

                    fragments.append(ContentFragment(
                        source_file=file_path,
                        fragment_id=fragment_id,
                        content=f"```{language}\n{code_content}\n```",
                        context_before="",
                        context_after="",
                        importance_score=importance,
                        topics=topics,
                        extraction_method='code_block'
                    ))

        return fragments

    def _extract_lists(self, file_path: str, content: str) -> List[ContentFragment]:
        """提取列表内容"""
        fragments = []
        lines = content.split('\n')

        i = 0
        while i < len(lines):
            line = lines[i].strip()

            # 检查是否是列表项
            if re.match(r'^[-*+]\s+', line) or re.match(r'^\d+\.\s+', line):
                list_items = [line]
                j = i + 1

                # 收集连续的列表项
                while j < len(lines):
                    next_line = lines[j].strip()
                    if re.match(r'^[-*+]\s+', next_line) or re.match(r'^\d+\.\s+', next_line):
                        list_items.append(next_line)
                        j += 1
                    elif next_line == '':
                        j += 1
                        continue
                    else:
                        break

                # 只保留有意义的列表（至少3个项目）
                if len(list_items) >= 3:
                    list_content = '\n'.join(list_items)
                    fragment_id = hashlib.md5(f"{file_path}_{list_content[:50]}".encode()).hexdigest()[:8]

                    importance = self._calculate_list_importance(list_content)
                    if importance > 0.3:
                        topics = self._classify_document_theme(file_path, list_content)

                        fragments.append(ContentFragment(
                            source_file=file_path,
                            fragment_id=fragment_id,
                            content=list_content,
                            context_before=lines[max(0, i-2):i],
                            context_after=lines[j:min(j+3, len(lines))],
                            importance_score=importance,
                            topics=topics,
                            extraction_method='list'
                        ))

                i = j
            else:
                i += 1

        return fragments

    def _extract_important_paragraphs(self, file_path: str, content: str) -> List[ContentFragment]:
        """提取重要段落"""
        fragments = []
        lines = content.split('\n')

        for i, line in enumerate(lines):
            line = line.strip()

            # 识别重要段落的特征
            importance_indicators = [
                r'重要', r'关键', r'核心', r'必须', r'注意', r'警告',
                r'best practice', r'important', r'key', r'critical',
                r'principle', r'rule', r'standard', r'guideline'
            ]

            is_important = any(re.search(pattern, line, re.IGNORECASE) for pattern in importance_indicators)

            # 检查是否是长度适中的段落（非标题，非代码，长度>50字符）
            if (is_important and len(line) > 50 and
                not line.startswith('#') and not line.startswith('```') and
                not line.startswith('-') and not line.startswith('*')):

                fragment_id = hashlib.md5(f"{file_path}_{line[:50]}".encode()).hexdigest()[:8]
                importance = self._calculate_paragraph_importance(line)

                if importance > 0.4:
                    topics = self._classify_document_theme(file_path, line)

                    fragments.append(ContentFragment(
                        source_file=file_path,
                        fragment_id=fragment_id,
                        content=line,
                        context_before=lines[max(0, i-2):i],
                        context_after=lines[i+1:min(i+4, len(lines))],
                        importance_score=importance,
                        topics=topics,
                        extraction_method='paragraph'
                    ))

        return fragments

    def _calculate_importance(self, content: str) -> float:
        """计算内容重要性"""
        importance = 0.5  # 基础分数

        # 关键词加权
        important_keywords = [
            '原则', '标准', '规范', '必须', '禁止', '要求',
            'principle', 'standard', 'requirement', 'must', 'should',
            '关键', '重要', '核心', 'critical', 'key', 'important'
        ]

        content_lower = content.lower()
        for keyword in important_keywords:
            if keyword in content_lower:
                importance += 0.1

        # 长度加权（适中长度得分更高）
        length = len(content)
        if 100 <= length <= 1000:
            importance += 0.2
        elif length > 1000:
            importance += 0.1

        return min(1.0, importance)

    def _calculate_code_importance(self, code: str, language: str) -> float:
        """计算代码块重要性"""
        importance = 0.3  # 基础分数

        # 配置类代码得分更高
        if any(keyword in code.lower() for keyword in ['config', 'setting', 'connection']):
            importance += 0.3

        # 架构相关代码得分更高
        if any(keyword in code.lower() for keyword in ['interface', 'abstract', 'repository', 'service']):
            importance += 0.3

        # 有注释的代码得分更高
        if re.search(r'//.*|#.*|/\*.*\*/', code):
            importance += 0.2

        return min(1.0, importance)

    def _calculate_list_importance(self, list_content: str) -> float:
        """计算列表重要性"""
        importance = 0.4  # 基础分数

        # 检查是否包含步骤或原则
        if re.search(r'\d+\.', list_content):  # 有序列表
            importance += 0.2

        # 检查关键词
        if any(keyword in list_content.lower() for keyword in ['步骤', '原则', '要点', 'step', 'principle']):
            importance += 0.2

        return min(1.0, importance)

    def _calculate_paragraph_importance(self, paragraph: str) -> float:
        """计算段落重要性"""
        importance = 0.3  # 基础分数

        # 检查是否包含指导性内容
        guidance_keywords = ['应该', '必须', '建议', '推荐', 'should', 'must', 'recommend']
        if any(keyword in paragraph.lower() for keyword in guidance_keywords):
            importance += 0.3

        # 检查是否包含解释性内容
        explanation_keywords = ['因为', '所以', '原因', '目的', 'because', 'therefore', 'reason', 'purpose']
        if any(keyword in paragraph.lower() for keyword in explanation_keywords):
            importance += 0.2

        return min(1.0, importance)

    def _deduplicate_fragments(self, fragments: List[ContentFragment]) -> List[ContentFragment]:
        """去重内容片段"""
        unique_fragments = []
        seen_content = set()

        for fragment in fragments:
            # 使用内容哈希去重
            content_hash = hashlib.md5(fragment.content.encode()).hexdigest()
            if content_hash not in seen_content:
                seen_content.add(content_hash)
                unique_fragments.append(fragment)

        return unique_fragments

    def _consolidate_by_theme(self, fragments: List[ContentFragment]) -> Dict[str, List[ContentFragment]]:
        """按主题整合片段"""
        theme_fragments = defaultdict(list)

        for fragment in fragments:
            for theme in fragment.topics:
                theme_fragments[theme].append(fragment)

        return dict(theme_fragments)

    def _generate_consolidated_document(self, theme: str, fragments: List[ContentFragment]) -> ConsolidatedDocument:
        """生成整合文档"""
        # 按重要性排序
        fragments.sort(key=lambda x: x.importance_score, reverse=True)

        # 生成文档标题
        theme_titles = {
            'architecture': '架构设计精要与演进历程',
            'development': '开发实践与规范演进',
            'testing': '测试质量与最佳实践',
            'project_management': '项目管理经验总结',
            'technical_decisions': '技术决策历史与评估',
            'troubleshooting': '问题解决方案与故障排查',
            'other': '技术文档精要'
        }

        title = theme_titles.get(theme, f'{theme.title()} 相关内容精要')
        output_path = f'docs/consolidated/{theme}-essentials.md'

        # 收集来源文件
        source_files = list(set(f.source_file for f in fragments))

        # 生成元数据
        metadata = {
            'theme': theme,
            'total_fragments': len(fragments),
            'source_files_count': len(source_files),
            'average_importance': sum(f.importance_score for f in fragments) / len(fragments),
            'extraction_date': datetime.now().isoformat(),
            'highest_importance': max(f.importance_score for f in fragments),
            'lowest_importance': min(f.importance_score for f in fragments)
        }

        return ConsolidatedDocument(
            title=title,
            output_path=output_path,
            theme=theme,
            source_files=source_files,
            fragments=fragments,
            metadata=metadata
        )

    def _write_consolidated_document(self, doc: ConsolidatedDocument):
        """写入整合文档"""
        # 确保输出目录存在
        output_path = self.project_root / doc.output_path
        output_path.parent.mkdir(parents=True, exist_ok=True)

        content = []
        content.append(f"# {doc.title}\n")
        content.append(f"**生成时间**: {datetime.now().strftime('%Y-%m-%d %H:%M:%S')}\n")
        content.append(f"**整合主题**: {doc.theme}\n")
        content.append(f"**来源文档**: {len(doc.source_files)} 个\n")
        content.append(f"**内容片段**: {doc.fragments} 个\n")
        content.append(f"**平均重要性**: {doc.metadata['average_importance']:.2f}/1.0\n\n")

        content.append("## 📋 概述\n\n")
        content.append(f"本文档整合了来自 {len(doc.source_files)} 个原始文档的有价值内容，")
        content.append(f"提取了 {len(doc.fragments)} 个高质量内容片段，旨在提供 {doc.theme} 相关的精要知识。\n\n")

        # 添加来源文档列表
        content.append("## 📚 来源文档\n\n")
        for source_file in doc.source_files:
            rel_path = Path(source_file).relative_to(self.project_root)
            content.append(f"- `{rel_path}`\n")
        content.append("\n")

        # 按重要性分组内容
        high_importance = [f for f in doc.fragments if f.importance_score >= 0.8]
        medium_importance = [f for f in doc.fragments if 0.5 <= f.importance_score < 0.8]
        low_importance = [f for f in doc.fragments if f.importance_score < 0.5]

        if high_importance:
            content.append("## 🔥 核心要点\n\n")
            for fragment in high_importance:
                source_rel = Path(fragment.source_file).relative_to(self.project_root)
                content.append(f"### 来自: `{source_rel}`\n\n")
                content.append(f"{fragment.content}\n\n")
                if fragment.context_before:
                    content.append(f"**上下文**: {' | '.join(fragment.context_before[:2])}\n\n")
                content.append("---\n\n")

        if medium_importance:
            content.append("## 📖 重要内容\n\n")
            for fragment in medium_importance:
                source_rel = Path(fragment.source_file).relative_to(self.project_root)
                content.append(f"#### 来自: `{source_rel}`\n\n")
                content.append(f"{fragment.content}\n\n")
                content.append("---\n\n")

        if low_importance:
            content.append("## 📝 补充信息\n\n")
            for fragment in low_importance:
                source_rel = Path(fragment.source_file).relative_to(self.project_root)
                content.append(f"**来源**: `{source_rel}`\n\n")
                content.append(f"{fragment.content}\n\n")
                content.append("---\n\n")

        # 添加总结
        content.append("## 💡 总结\n\n")
        content.append(f"本次整合从 {len(doc.source_files)} 个文档中提取了 {len(doc.fragments)} 个有价值的内容片段，")
        content.append(f"涵盖了 {doc.theme} 领域的核心知识和实践经验。")
        content.append(f"通过智能去重和重要性排序，确保了内容的高信息密度和实用性。\n\n")

        # 写入文件
        with open(output_path, 'w', encoding='utf-8') as f:
            f.writelines(content)

        logger.info(f"已生成整合文档: {output_path}")

    def extract_and_consolidate(self):
        """执行内容提取和整合"""
        logger.info("开始内容提取和整合...")

        all_fragments = []

        # 从所有候选文档中提取内容
        for doc_info in self.merge_candidates:
            file_path = Path(doc_info['file_path'])

            if not file_path.exists():
                logger.warning(f"文件不存在: {file_path}")
                continue

            try:
                with open(file_path, 'r', encoding='utf-8') as f:
                    content = f.read()

                fragments = self._extract_content_fragments(str(file_path), content)
                all_fragments.extend(fragments)

                logger.info(f"从 {file_path} 提取了 {len(fragments)} 个片段")

            except Exception as e:
                logger.error(f"处理文件失败 {file_path}: {e}")

        # 去重
        unique_fragments = self._deduplicate_fragments(all_fragments)
        logger.info(f"去重后剩余 {len(unique_fragments)} 个片段")

        # 按主题分组
        theme_fragments = self._consolidate_by_theme(unique_fragments)
        logger.info(f"识别出 {len(theme_fragments)} 个主题")

        # 生成整合文档
        consolidated_docs = []
        for theme, fragments in theme_fragments.items():
            if len(fragments) >= 3:  # 只生成有足够内容的文档
                doc = self._generate_consolidated_document(theme, fragments)
                self._write_consolidated_document(doc)
                consolidated_docs.append(doc)
                logger.info(f"生成了 {theme} 主题的整合文档")

        # 生成报告
        self._generate_extraction_report(consolidated_docs, unique_fragments)

        logger.info(f"内容提取和整合完成！生成了 {len(consolidated_docs)} 个精要文档")

    def _generate_extraction_report(self, consolidated_docs: List[ConsolidatedDocument], all_fragments: List[ContentFragment]):
        """生成提取报告"""
        report_dir = self.project_root / 'docs/reports'
        report_dir.mkdir(exist_ok=True)

        # 统计信息
        stats = {
            'extraction_timestamp': datetime.now().isoformat(),
            'total_source_documents': len(self.merge_candidates),
            'total_fragments_extracted': len(all_fragments),
            'unique_fragments_after_dedup': len(all_fragments),
            'themes_identified': len(set(f.topics[0] for f in all_fragments if f.topics)),
            'consolidated_documents_generated': len(consolidated_docs),
            'average_fragments_per_document': len(all_fragments) / len(consolidated_docs) if consolidated_docs else 0
        }

        # 保存详细数据
        with open(report_dir / 'content-extraction-results.json', 'w', encoding='utf-8') as f:
            json.dump({
                'statistics': stats,
                'consolidated_documents': [asdict(doc) for doc in consolidated_docs],
                'all_fragments': [asdict(frag) for frag in all_fragments]
            }, f, ensure_ascii=False, indent=2, default=str)

        # 生成人类可读报告
        with open(report_dir / 'content-extraction-summary.md', 'w', encoding='utf-8') as f:
            f.write("# 内容提取和整合报告\n\n")
            f.write(f"**生成时间**: {datetime.now().strftime('%Y-%m-%d %H:%M:%S')}\n\n")

            f.write("## 📊 统计概览\n\n")
            f.write(f"- **处理源文档**: {stats['total_source_documents']} 个\n")
            f.write(f"- **提取内容片段**: {stats['total_fragments_extracted']} 个\n")
            f.write(f"- **去重后片段**: {stats['unique_fragments_after_dedup']} 个\n")
            f.write(f"- **识别主题数**: {stats['themes_identified']} 个\n")
            f.write(f"- **生成精要文档**: {stats['consolidated_documents_generated']} 个\n\n")

            f.write("## 📚 生成的精要文档\n\n")
            for doc in consolidated_docs:
                f.write(f"### {doc.title}\n")
                f.write(f"- **文件路径**: `{doc.output_path}`\n")
                f.write(f"- **来源文档**: {len(doc.source_files)} 个\n")
                f.write(f"- **内容片段**: {len(doc.fragments)} 个\n")
                f.write(f"- **平均重要性**: {doc.metadata['average_importance']:.2f}/1.0\n\n")

            f.write("## 🎯 预期效果\n\n")
            f.write(f"- 通过智能内容提取，将 {stats['total_source_documents']} 个源文档整合为 {stats['consolidated_documents_generated']} 个精要文档\n")
            f.write(f"- 内容精简率: {(1 - stats['consolidated_documents_generated'] / stats['total_source_documents']) * 100:.1f}%\n")
            f.write(f"- 信息密度提升显著，每个文档平均包含 {stats['average_fragments_per_document']:.1f} 个高价值片段\n")

def main():
    """主执行函数"""
    print("[INFO] 启动智能内容提取和整合工具...")

    # 检测项目根目录
    current_path = Path.cwd()
    if (current_path / 'docs').exists() and (current_path / '.spec-workflow').exists():
        project_root = current_path
    else:
        print(f"[ERROR] 无法找到项目根目录: {current_path}")
        return

    extractor = ContentExtractor(str(project_root))
    extractor.extract_and_consolidate()

    print("[SUCCESS] 内容提取和整合完成！")
    print("[OUTPUT] 精要文档保存在: docs/consolidated/")
    print("[OUTPUT] 详细报告: docs/reports/content-extraction-summary.md")
    print("[OUTPUT] 完整数据: docs/reports/content-extraction-results.json")

if __name__ == "__main__":
    main()
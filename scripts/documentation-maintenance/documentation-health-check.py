#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
文档健康监控和检查系统

定期检查文档链接有效性、内容过时检测、重复文档识别
确保文档导航门户的长期健康和准确性
"""

import os
import re
import json
import time
from pathlib import Path
from typing import Dict, List, Tuple, Set, Optional
from dataclasses import dataclass, asdict
from collections import defaultdict, Counter
from datetime import datetime, timedelta
import requests
from urllib.parse import urljoin, urlparse

@dataclass
class HealthIssue:
    """健康问题记录"""
    issue_type: str  # 'broken_link', 'stale_content', 'duplicate', 'orphaned'
    severity: str  # 'critical', 'warning', 'info'
    file_path: str
    description: str
    suggestion: str
    detected_at: str

@dataclass
class LinkCheckResult:
    """链接检查结果"""
    url: str
    status_code: Optional[int]
    accessible: bool
    error_message: Optional[str]
    redirect_url: Optional[str]

@dataclass
class DocumentMetrics:
    """文档指标"""
    file_path: str
    file_size: int
    last_modified: datetime
    word_count: int
    link_count: int
    section_count: int
    has_table_of_contents: bool
    estimated_read_time: int  # 分钟

class DocumentationHealthChecker:
    """文档健康检查器"""

    def __init__(self, project_root: str):
        self.project_root = Path(project_root)
        self.docs_dir = self.project_root / "docs"
        self.spec_dir = self.project_root / ".spec-workflow"

        # 健康检查配置
        self.config = {
            "max_link_redirects": 3,
            "request_timeout": 10,
            "stale_content_days": 90,
            "duplicate_similarity_threshold": 0.8,
            "orphaned_days": 180,
            "min_word_count": 100
        }

        self.issues: List[HealthIssue] = []
        self.metrics: Dict[str, DocumentMetrics] = {}

        # 过期关键词模式
        self.stale_patterns = [
            r'\b\d{4}-\d{2}-\d{2}\b',  # 日期
            r'\b版本\s*[\d.]+\b',     # 版本号
            r'\bv\d+\.\d+\b',          # 版本号v开头
            r'\bTODO\b',              # TODO标记
            r'\bFIXME\b',             # FIXME标记
            r'\b待完成\b',            # 中文待完成
            r'\b临时\b',              # 中文临时
        ]

    def run_full_health_check(self) -> Dict:
        """运行完整的健康检查"""
        print("[INFO] 开始文档健康检查...")
        start_time = time.time()

        # 1. 扫描文档并计算指标
        self._scan_documents()

        # 2. 检查链接有效性
        self._check_links()

        # 3. 检测内容过时
        self._detect_stale_content()

        # 4. 识别重复文档
        self._detect_duplicates()

        # 5. 检查孤立文档
        self._detect_orphaned_documents()

        # 6. 生成健康报告
        end_time = time.time()
        report = self._generate_health_report(end_time - start_time)

        print(f"[SUCCESS] 健康检查完成，耗时 {end_time - start_time:.2f} 秒")
        return report

    def _scan_documents(self) -> None:
        """扫描所有文档并计算指标"""
        print("[INFO] 扫描文档并计算指标...")

        # 扫描docs/目录
        self._scan_directory(self.docs_dir)

        # 扫描spec-workflow/目录
        if self.spec_dir.exists():
            self._scan_directory(self.spec_dir / "specs")
            self._scan_directory(self.spec_dir / "archive")
            self._scan_directory(self.spec_dir / "steering")

        print(f"[INFO] 共扫描 {len(self.metrics)} 个文档")

    def _scan_directory(self, directory: Path) -> None:
        """扫描指定目录下的文档"""
        if not directory.exists():
            return

        for file_path in directory.rglob("*.md"):
            if file_path.is_file():
                try:
                    metrics = self._calculate_metrics(file_path)
                    relative_path = str(file_path.relative_to(self.project_root))
                    self.metrics[relative_path] = metrics
                except Exception as e:
                    print(f"[WARNING] 计算文档指标失败 {file_path}: {e}")

    def _calculate_metrics(self, file_path: Path) -> DocumentMetrics:
        """计算文档指标"""
        stat = file_path.stat()

        try:
            with open(file_path, 'r', encoding='utf-8') as f:
                content = f.read()
        except Exception:
            return None

        # 基础指标
        file_size = stat.st_size
        last_modified = datetime.fromtimestamp(stat.st_mtime)
        word_count = len(re.findall(r'\b\w+\b', content))

        # 链接数量
        link_count = len(re.findall(r'\[([^\]]+)\]\(([^)]+)\)', content))

        # 章节数量
        section_count = len(re.findall(r'^#+\s+', content, re.MULTILINE))

        # 是否有目录
        has_table_of_contents = bool(re.search(r'^#+\s+(目录|TOC|Table of Contents)', content, re.MULTILINE | re.IGNORECASE))

        # 估算阅读时间（假设每分钟阅读200词）
        estimated_read_time = max(1, word_count // 200)

        return DocumentMetrics(
            file_path=str(file_path.relative_to(self.project_root)),
            file_size=file_size,
            last_modified=last_modified,
            word_count=word_count,
            link_count=link_count,
            section_count=section_count,
            has_table_of_contents=has_table_of_contents,
            estimated_read_time=estimated_read_time
        )

    def _check_links(self) -> None:
        """检查链接有效性"""
        print("[INFO] 检查链接有效性...")

        # 收集所有内部链接
        internal_links = set()
        external_links = set()

        for doc_path, metrics in self.metrics.items():
            try:
                full_path = self.project_root / doc_path
                with open(full_path, 'r', encoding='utf-8') as f:
                    content = f.read()

                # 查找所有链接
                matches = re.finditer(r'\[([^\]]+)\]\(([^)]+)\)', content)

                for match in matches:
                    link_url = match.group(2)

                    if link_url.startswith(('http://', 'https://')):
                        external_links.add(link_url)
                    elif not link_url.startswith(('mailto:', '#', 'javascript:')):
                        # 处理相对路径
                        resolved_link = self._resolve_relative_path(doc_path, link_url)
                        if resolved_link:
                            internal_links.add(resolved_link)

            except Exception as e:
                print(f"[WARNING] 检查链接失败 {doc_path}: {e}")

        # 检查内部链接是否存在
        self._check_internal_links(internal_links)

        # 检查外部链接（限制数量以避免过多请求）
        self._check_external_links(list(external_links)[:50])  # 最多检查50个外部链接

    def _resolve_relative_path(self, source_path: str, relative_path: str) -> str:
        """解析相对路径"""
        try:
            source_dir = Path(source_path).parent
            target_path = (source_dir / relative_path).resolve()
            relative = target_path.relative_to(self.project_root)
            return str(relative)
        except (ValueError, FileNotFoundError):
            return None

    def _check_internal_links(self, internal_links: Set[str]) -> None:
        """检查内部链接"""
        print(f"[INFO] 检查 {len(internal_links)} 个内部链接...")

        existing_files = set(self.metrics.keys())

        for link in internal_links:
            # 规范化路径
            normalized_link = link.replace('\\', '/')

            # 检查不同变体
            link_variants = [
                normalized_link,
                normalized_link + '.md',
                normalized_link.rstrip('.md'),
                normalized_link.replace('.md', ''),
            ]

            if not any(variant in existing_files for variant in link_variants):
                self.issues.append(HealthIssue(
                    issue_type="broken_link",
                    severity="warning",
                    file_path="internal_links",
                    description=f"内部链接目标不存在: {link}",
                    suggestion=f"检查链接目标是否存在，或更新链接路径",
                    detected_at=datetime.now().isoformat()
                ))

    def _check_external_links(self, external_links: List[str]) -> None:
        """检查外部链接"""
        print(f"[INFO] 检查 {len(external_links)} 个外部链接...")

        for url in external_links:
            try:
                response = requests.head(
                    url,
                    timeout=self.config["request_timeout"],
                    allow_redirects=True,
                    headers={'User-Agent': 'Documentation-Health-Checker/1.0'}
                )

                if response.status_code >= 400:
                    severity = "critical" if response.status_code >= 500 else "warning"
                    self.issues.append(HealthIssue(
                        issue_type="broken_link",
                        severity=severity,
                        file_path="external_links",
                        description=f"外部链接不可访问: {url} (HTTP {response.status_code})",
                        suggestion=f"检查URL是否正确，或替换为可用链接",
                        detected_at=datetime.now().isoformat()
                    ))

            except requests.RequestException as e:
                self.issues.append(HealthIssue(
                    issue_type="broken_link",
                    severity="warning",
                    file_path="external_links",
                    description=f"外部链接访问失败: {url} - {str(e)}",
                    suggestion=f"检查网络连接或URL有效性",
                    detected_at=datetime.now().isoformat()
                ))

    def _detect_stale_content(self) -> None:
        """检测过时内容"""
        print("[INFO] 检测过时内容...")

        now = datetime.now()
        stale_threshold = timedelta(days=self.config["stale_content_days"])

        for doc_path, metrics in self.metrics.items():
            # 检查文件修改时间
            if now - metrics.last_modified > stale_threshold:
                self.issues.append(HealthIssue(
                    issue_type="stale_content",
                    severity="info",
                    file_path=doc_path,
                    description=f"文档可能过时，最后修改于 {metrics.last_modified.strftime('%Y-%m-%d')}",
                    suggestion=f"检查文档内容是否需要更新，或添加最后更新时间说明",
                    detected_at=datetime.now().isoformat()
                ))

            # 检查文档内容中的过期标记
            try:
                full_path = self.project_root / doc_path
                with open(full_path, 'r', encoding='utf-8') as f:
                    content = f.read()

                for pattern in self.stale_patterns:
                    matches = re.findall(pattern, content, re.IGNORECASE)
                    if matches:
                        severity = "warning" if any(keyword in content.lower() for keyword in ['todo', 'fixme', '待完成']) else "info"
                        self.issues.append(HealthIssue(
                            issue_type="stale_content",
                            severity=severity,
                            file_path=doc_path,
                            description=f"发现可能过时的内容标记: {', '.join(matches[:3])}",
                            suggestion=f"更新相关内容或移除过时标记",
                            detected_at=datetime.now().isoformat()
                        ))
                        break

            except Exception as e:
                print(f"[WARNING] 检查过期内容失败 {doc_path}: {e}")

    def _detect_duplicates(self) -> None:
        """识别重复文档"""
        print("[INFO] 检测重复文档...")

        # 按文档大小分组，相似大小的文档更可能重复
        size_groups = defaultdict(list)
        for doc_path, metrics in self.metrics.items():
            size_key = metrics.file_size // 1000  # 按1KB分组
            size_groups[size_key].append(doc_path)

        for size_key, docs in size_groups.items():
            if len(docs) < 2:
                continue

            # 比较同组文档的内容相似度
            for i in range(len(docs)):
                for j in range(i + 1, len(docs)):
                    similarity = self._calculate_document_similarity(docs[i], docs[j])
                    if similarity > self.config["duplicate_similarity_threshold"]:
                        self.issues.append(HealthIssue(
                            issue_type="duplicate",
                            severity="warning",
                            file_path=docs[i],
                            description=f"与文档 {docs[j]} 高度相似 (相似度: {similarity:.2f})",
                            suggestion=f"考虑合并文档或明确区分它们的用途",
                            detected_at=datetime.now().isoformat()
                        ))

    def _calculate_document_similarity(self, doc1: str, doc2: str) -> float:
        """计算文档相似度"""
        try:
            # 读取文档内容
            content1 = (self.project_root / doc1).read_text(encoding='utf-8')
            content2 = (self.project_root / doc2).read_text(encoding='utf-8')

            # 提取关键词
            words1 = set(re.findall(r'\b\w+\b', content1.lower()))
            words2 = set(re.findall(r'\b\w+\b', content2.lower()))

            # 计算Jaccard相似度
            intersection = words1 & words2
            union = words1 | words2

            if not union:
                return 0.0

            return len(intersection) / len(union)

        except Exception:
            return 0.0

    def _detect_orphaned_documents(self) -> None:
        """检查孤立文档"""
        print("[INFO] 检测孤立文档...")

        # 收集所有被引用的文档
        referenced_docs = set()

        for doc_path, metrics in self.metrics.items():
            try:
                full_path = self.project_root / doc_path
                with open(full_path, 'r', encoding='utf-8') as f:
                    content = f.read()

                # 查找所有内部链接
                matches = re.finditer(r'\[([^\]]+)\]\(([^)]+)\)', content)

                for match in matches:
                    link_url = match.group(2)
                    if not link_url.startswith(('http://', 'https://', 'mailto:', '#', 'javascript:')):
                        resolved_link = self._resolve_relative_path(doc_path, link_url)
                        if resolved_link:
                            referenced_docs.add(resolved_link)

            except Exception as e:
                print(f"[WARNING] 检查引用失败 {doc_path}: {e}")

        # 查找未被引用的文档（排除一些特殊文档）
        excluded_patterns = ['README', 'index', 'CLAUDE']

        for doc_path in self.metrics.keys():
            if doc_path not in referenced_docs:
                # 检查是否为排除的特殊文档
                if not any(pattern in doc_path for pattern in excluded_patterns):
                    metrics = self.metrics[doc_path]

                    # 检查是否为很旧的文档
                    if datetime.now() - metrics.last_modified > timedelta(days=self.config["orphaned_days"]):
                        self.issues.append(HealthIssue(
                            issue_type="orphaned",
                            severity="info",
                            file_path=doc_path,
                            description=f"文档未被其他文档引用，且最后修改于 {metrics.last_modified.strftime('%Y-%m-%d')}",
                            suggestion="检查文档是否仍需要，或添加到相关文档的引用中",
                            detected_at=datetime.now().isoformat()
                        ))

    def _generate_health_report(self, execution_time: float) -> Dict:
        """生成健康检查报告"""
        print("[INFO] 生成健康检查报告...")

        # 按严重程度分组问题
        issues_by_severity = defaultdict(list)
        issues_by_type = defaultdict(list)

        for issue in self.issues:
            issues_by_severity[issue.severity].append(asdict(issue))
            issues_by_type[issue.issue_type].append(asdict(issue))

        # 计算统计信息
        total_issues = len(self.issues)
        critical_issues = len(issues_by_severity['critical'])
        warning_issues = len(issues_by_severity['warning'])
        info_issues = len(issues_by_severity['info'])

        # 生成健康评分
        health_score = max(0, 100 - (critical_issues * 10 + warning_issues * 3 + info_issues))

        return {
            "summary": {
                "health_score": health_score,
                "total_documents": len(self.metrics),
                "total_issues": total_issues,
                "critical_issues": critical_issues,
                "warning_issues": warning_issues,
                "info_issues": info_issues,
                "execution_time": execution_time,
                "checked_at": datetime.now().isoformat()
            },
            "issues": [asdict(issue) for issue in self.issues],
            "issues_by_severity": dict(issues_by_severity),
            "issues_by_type": dict(issues_by_type),
            "metrics": {path: asdict(metrics) for path, metrics in self.metrics.items()},
            "recommendations": self._generate_recommendations(issues_by_severity)
        }

    def _generate_recommendations(self, issues_by_severity: Dict) -> List[Dict]:
        """生成改进建议"""
        recommendations = []

        if issues_by_severity['critical']:
            recommendations.append({
                "priority": "urgent",
                "category": "critical_issues",
                "title": "立即修复关键问题",
                "description": f"发现 {len(issues_by_severity['critical'])} 个关键问题，需要立即处理",
                "actions": [
                    "修复所有断开的链接",
                    "更新无法访问的外部资源",
                    "处理重复或过时的内容"
                ]
            })

        if issues_by_severity['warning']:
            recommendations.append({
                "priority": "high",
                "category": "warning_issues",
                "title": "处理警告级别问题",
                "description": f"发现 {len(issues_by_severity['warning'])} 个警告问题，建议尽快处理",
                "actions": [
                    "检查并更新过时内容",
                    "解决重复文档问题",
                    "清理临时或待完成标记"
                ]
            })

        if issues_by_severity['info']:
            recommendations.append({
                "priority": "medium",
                "category": "info_issues",
                "title": "优化信息级别问题",
                "description": f"发现 {len(issues_by_severity['info'])} 个信息问题，可以逐步优化",
                "actions": [
                    "检查孤立文档的用途",
                    "为长期未更新的文档添加说明",
                    "改进文档的可发现性"
                ]
            })

        return recommendations

    def save_report(self, report: Dict, output_path: str) -> None:
        """保存健康检查报告"""
        output_file = self.project_root / output_path

        # 保存JSON格式
        with open(output_file, 'w', encoding='utf-8') as f:
            json.dump(report, f, ensure_ascii=False, indent=2, default=str)

        # 生成人类可读的报告
        self._generate_human_readable_report(report, output_file.with_suffix('.md'))

        print(f"[SUCCESS] 健康检查报告已保存到: {output_file}")

    def _generate_human_readable_report(self, data: Dict, output_path: Path) -> None:
        """生成人类可读的健康检查报告"""
        summary = data["summary"]

        report = f"""# 文档健康检查报告

## 概览

- **健康评分**: {summary['health_score']}/100
- **检查文档数**: {summary['total_documents']}
- **发现问题数**: {summary['total_issues']}
- **执行时间**: {summary['execution_time']:.2f} 秒
- **检查时间**: {summary['checked_at']}

## 问题统计

| 严重程度 | 数量 | 说明 |
|----------|------|------|
| 🔴 关键 | {summary['critical_issues']} | 需要立即处理的问题 |
| 🟡 警告 | {summary['warning_issues']} | 建议尽快处理的问题 |
| 🔵 信息 | {summary['info_issues']} | 可以逐步优化的问题 |

## 改进建议

"""

        for rec in data["recommendations"]:
            priority_emoji = {"urgent": "🔴", "high": "🟡", "medium": "🔵"}.get(rec["priority"], "⚪")
            report += f"### {priority_emoji} {rec['title']}\n\n"
            report += f"{rec['description']}\n\n"
            report += "**建议操作**:\n"
            for action in rec["actions"]:
                report += f"- {action}\n"
            report += "\n"

        report += "## 详细问题列表\n\n"

        # 按类型分组显示问题
        for issue_type, issues in data["issues_by_type"].items():
            type_names = {
                "broken_link": "🔗 链接问题",
                "stale_content": "📅 内容过时",
                "duplicate": "📋 重复文档",
                "orphaned": "🏝️ 孤立文档"
            }

            type_name = type_names.get(issue_type, issue_type)
            report += f"### {type_name} ({len(issues)}个)\n\n"

            for issue in issues[:10]:  # 每个类型最多显示10个
                severity_emoji = {"critical": "🔴", "warning": "🟡", "info": "🔵"}.get(issue["severity"], "⚪")
                report += f"{severity_emoji} **{issue['file_path']}**\n"
                report += f"- 问题: {issue['description']}\n"
                report += f"- 建议: {issue['suggestion']}\n\n"

            if len(issues) > 10:
                report += f"*... 还有 {len(issues) - 10} 个类似问题，详见完整报告 *\n\n"

        with open(output_path, 'w', encoding='utf-8') as f:
            f.write(report)

        print(f"[SUCCESS] 人类可读报告已保存到: {output_path}")

def main():
    """主函数"""
    project_root = Path(__file__).parent.parent.parent

    print("[INFO] 开始文档健康检查...")

    checker = DocumentationHealthChecker(str(project_root))

    # 运行健康检查
    report = checker.run_full_health_check()

    # 保存报告
    checker.save_report(report, "scripts/documentation-maintenance/documentation-health-report.json")

    print("[SUCCESS] 文档健康检查完成!")

if __name__ == "__main__":
    main()
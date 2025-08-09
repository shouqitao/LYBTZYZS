#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
UltraThink 代码质量检查工具
用于检查LYBTZYZS项目的代码质量，确保遵循项目规范和SOLID原则

功能：
1. 检查文件大小是否超过500行限制
2. 分析代码结构和质量指标
3. 检查编译状态
4. 生成详细质量报告
"""

import os
import subprocess
import sys
import json
import datetime
from pathlib import Path
from typing import Dict, List, Tuple, Any
from dataclasses import dataclass, asdict
import re

@dataclass
class FileAnalysis:
    """文件分析结果"""
    path: str
    line_count: int
    size_kb: float
    exceeds_limit: bool
    file_type: str
    complexity_score: int = 0
    
@dataclass  
class QualityReport:
    """质量报告"""
    timestamp: str
    total_files: int
    oversized_files: int
    total_lines: int
    build_status: str
    warnings_count: int
    errors_count: int
    file_analyses: List[FileAnalysis]
    summary: Dict[str, Any]

class UltraThinkQualityChecker:
    """UltraThink代码质量检查器"""
    
    def __init__(self, root_path: str = "."):
        self.root_path = Path(root_path).resolve()
        self.line_limit = 500  # CLAUDE.md规定的文件行数限制
        self.source_extensions = {'.cs', '.xaml', '.py', '.js', '.ts', '.sql'}
        self.ignore_paths = {
            'bin', 'obj', 'node_modules', '.git', 
            'packages', '.vs', 'TestResults', 'coverage'
        }
        
    def should_analyze_file(self, file_path: Path) -> bool:
        """判断是否应该分析该文件"""
        # 检查文件扩展名
        if file_path.suffix.lower() not in self.source_extensions:
            return False
            
        # 检查是否在忽略路径中
        for ignore in self.ignore_paths:
            if ignore in file_path.parts:
                return False
                
        return True
        
    def count_lines(self, file_path: Path) -> int:
        """计算文件行数"""
        try:
            with open(file_path, 'r', encoding='utf-8', errors='ignore') as f:
                return sum(1 for line in f)
        except Exception as e:
            print(f"警告: 无法读取文件 {file_path}: {e}")
            return 0
            
    def calculate_complexity_score(self, file_path: Path) -> int:
        """计算代码复杂度评分（简化版）"""
        try:
            with open(file_path, 'r', encoding='utf-8', errors='ignore') as f:
                content = f.read()
                
            complexity = 0
            
            # C# 复杂度指标
            if file_path.suffix == '.cs':
                # 循环结构
                complexity += len(re.findall(r'\b(for|foreach|while|do)\b', content))
                # 条件结构
                complexity += len(re.findall(r'\b(if|else|switch|case)\b', content))
                # 异常处理
                complexity += len(re.findall(r'\b(try|catch|finally)\b', content))
                # 方法数量
                complexity += len(re.findall(r'(public|private|protected|internal)\s+\w+\s+\w+\s*\(', content))
                
            # XAML 复杂度指标  
            elif file_path.suffix == '.xaml':
                # 控件数量
                complexity += len(re.findall(r'<\w+[^/>]*>', content))
                # 绑定数量
                complexity += len(re.findall(r'Binding\s*=', content))
                
            return min(complexity, 100)  # 限制在0-100范围内
            
        except Exception:
            return 0
            
    def analyze_file(self, file_path: Path) -> FileAnalysis:
        """分析单个文件"""
        line_count = self.count_lines(file_path)
        size_kb = file_path.stat().st_size / 1024
        exceeds_limit = line_count > self.line_limit
        complexity = self.calculate_complexity_score(file_path)
        
        return FileAnalysis(
            path=str(file_path.relative_to(self.root_path)),
            line_count=line_count,
            size_kb=round(size_kb, 2),
            exceeds_limit=exceeds_limit,
            file_type=file_path.suffix,
            complexity_score=complexity
        )
        
    def scan_project_files(self) -> List[FileAnalysis]:
        """扫描项目文件"""
        analyses = []
        
        print("扫描项目文件...")
        for file_path in self.root_path.rglob('*'):
            if file_path.is_file() and self.should_analyze_file(file_path):
                analysis = self.analyze_file(file_path)
                analyses.append(analysis)
                
                if analysis.exceeds_limit:
                    print(f"警告: {analysis.path} 超过{self.line_limit}行限制: {analysis.line_count}行")
                    
        return analyses
        
    def check_build_status(self) -> Tuple[str, int, int]:
        """检查编译状态"""
        print("检查编译状态...")
        
        # 检查后端编译
        backend_result = self.run_build_command(['dotnet', 'build', 'LYBT.Backend.sln', '--verbosity', 'quiet'])
        
        # 检查前端编译  
        frontend_result = self.run_build_command(['dotnet', 'build', 'LYBT.Desktop.sln', '--verbosity', 'quiet'])
        
        # 分析编译结果
        total_warnings = 0
        total_errors = 0
        build_status = "成功"
        
        if backend_result:
            backend_warnings, backend_errors = self.parse_build_output(backend_result)
            total_warnings += backend_warnings
            total_errors += backend_errors
            
        if frontend_result:
            frontend_warnings, frontend_errors = self.parse_build_output(frontend_result)
            total_warnings += frontend_warnings  
            total_errors += frontend_errors
            
        if total_errors > 0:
            build_status = "失败"
        elif total_warnings > 0:
            build_status = "成功但有警告"
            
        return build_status, total_warnings, total_errors
        
    def run_build_command(self, cmd: List[str]) -> str:
        """执行编译命令"""
        try:
            result = subprocess.run(
                cmd, 
                cwd=self.root_path, 
                capture_output=True, 
                text=True,
                encoding='utf-8',
                timeout=300  # 5分钟超时
            )
            return result.stdout + result.stderr
        except subprocess.TimeoutExpired:
            return "编译超时"
        except Exception as e:
            return f"编译命令执行失败: {e}"
            
    def parse_build_output(self, output: str) -> Tuple[int, int]:
        """解析编译输出，提取警告和错误数量"""
        warnings = len(re.findall(r'warning \w+:', output))
        errors = len(re.findall(r'error \w+:', output))
        
        # 从摘要中提取数字
        warning_match = re.search(r'(\d+)\s*个警告', output)
        error_match = re.search(r'(\d+)\s*个错误', output)
        
        if warning_match:
            warnings = max(warnings, int(warning_match.group(1)))
        if error_match:
            errors = max(errors, int(error_match.group(1)))
            
        return warnings, errors
        
    def generate_summary(self, analyses: List[FileAnalysis], build_status: str, 
                        warnings: int, errors: int) -> Dict[str, Any]:
        """生成摘要信息"""
        oversized_files = [a for a in analyses if a.exceeds_limit]
        total_lines = sum(a.line_count for a in analyses)
        
        # 文件类型统计
        file_type_stats = {}
        for analysis in analyses:
            ftype = analysis.file_type
            if ftype not in file_type_stats:
                file_type_stats[ftype] = {'count': 0, 'lines': 0, 'oversized': 0}
            file_type_stats[ftype]['count'] += 1
            file_type_stats[ftype]['lines'] += analysis.line_count
            if analysis.exceeds_limit:
                file_type_stats[ftype]['oversized'] += 1
                
        # 复杂度统计
        complexity_stats = {
            'average': round(sum(a.complexity_score for a in analyses) / len(analyses), 2) if analyses else 0,
            'high_complexity': len([a for a in analyses if a.complexity_score > 50])
        }
        
        return {
            'file_type_statistics': file_type_stats,
            'complexity_statistics': complexity_stats,
            'oversized_files_detail': [
                {'path': a.path, 'lines': a.line_count, 'excess': a.line_count - self.line_limit}
                for a in oversized_files
            ],
            'quality_score': self.calculate_quality_score(analyses, warnings, errors),
            'ultrathink_compliance': self.check_ultrathink_compliance(analyses)
        }
        
    def calculate_quality_score(self, analyses: List[FileAnalysis], warnings: int, errors: int) -> float:
        """计算整体质量分数 (0-100)"""
        if not analyses:
            return 0
            
        base_score = 100
        
        # 扣分项
        oversized_penalty = len([a for a in analyses if a.exceeds_limit]) * 5  # 每个超大文件扣5分
        warning_penalty = min(warnings * 0.5, 20)  # 警告扣分，最多扣20分
        error_penalty = errors * 10  # 每个错误扣10分
        
        high_complexity_penalty = len([a for a in analyses if a.complexity_score > 70]) * 3  # 高复杂度扣分
        
        final_score = base_score - oversized_penalty - warning_penalty - error_penalty - high_complexity_penalty
        
        return max(0, round(final_score, 2))
        
    def check_ultrathink_compliance(self, analyses: List[FileAnalysis]) -> Dict[str, Any]:
        """检查UltraThink方法论合规性"""
        total_files = len(analyses)
        compliant_files = len([a for a in analyses if not a.exceeds_limit])
        
        compliance_rate = (compliant_files / total_files * 100) if total_files > 0 else 100
        
        return {
            'compliance_rate': round(compliance_rate, 2),
            'compliant_files': compliant_files,
            'non_compliant_files': total_files - compliant_files,
            'status': '优秀' if compliance_rate >= 95 else '良好' if compliance_rate >= 85 else '需改进'
        }
        
    def generate_report(self) -> QualityReport:
        """生成完整质量报告"""
        print("启动 UltraThink 代码质量检查...")
        
        # 扫描文件
        analyses = self.scan_project_files()
        
        # 检查编译状态
        build_status, warnings, errors = self.check_build_status()
        
        # 生成摘要
        summary = self.generate_summary(analyses, build_status, warnings, errors)
        
        return QualityReport(
            timestamp=datetime.datetime.now().isoformat(),
            total_files=len(analyses),
            oversized_files=len([a for a in analyses if a.exceeds_limit]),
            total_lines=sum(a.line_count for a in analyses),
            build_status=build_status,
            warnings_count=warnings,
            errors_count=errors,
            file_analyses=analyses,
            summary=summary
        )
        
    def print_report(self, report: QualityReport):
        """打印报告到控制台"""
        print("\n" + "="*60)
        print("UltraThink 代码质量检查报告")
        print("="*60)
        print(f"检查时间: {report.timestamp}")
        print(f"总文件数: {report.total_files}")
        print(f"总代码行数: {report.total_lines:,}")
        print(f"编译状态: {report.build_status}")
        print(f"编译警告: {report.warnings_count}")
        print(f"编译错误: {report.errors_count}")
        
        print(f"\n文件大小合规性:")
        print(f"符合500行限制的文件: {report.total_files - report.oversized_files}")
        print(f"超过500行限制的文件: {report.oversized_files}")
        
        if report.oversized_files > 0:
            print(f"\n超大文件详情:")
            for detail in report.summary['oversized_files_detail']:
                print(f"   {detail['path']}: {detail['lines']}行 (超出{detail['excess']}行)")
                
        print(f"\n质量评分: {report.summary['quality_score']}/100")
        
        compliance = report.summary['ultrathink_compliance']
        print(f"\nUltraThink合规性:")
        print(f"   合规率: {compliance['compliance_rate']}%")
        print(f"   评级: {compliance['status']}")
        
        print(f"\n文件类型统计:")
        for ftype, stats in report.summary['file_type_statistics'].items():
            print(f"   {ftype}: {stats['count']}个文件, {stats['lines']:,}行代码")
            if stats['oversized'] > 0:
                print(f"        (其中{stats['oversized']}个文件超大)")
                
        print("\n" + "="*60)
        
    def save_report(self, report: QualityReport, output_path: str = None):
        """保存报告到JSON文件"""
        if not output_path:
            timestamp = datetime.datetime.now().strftime("%Y%m%d_%H%M%S")
            output_path = f"quality_report_{timestamp}.json"
            
        with open(output_path, 'w', encoding='utf-8') as f:
            json.dump(asdict(report), f, ensure_ascii=False, indent=2)
            
        print(f"详细报告已保存到: {output_path}")

def main():
    """主函数"""
    try:
        # 创建检查器实例
        checker = UltraThinkQualityChecker()
        
        # 生成报告
        report = checker.generate_report()
        
        # 打印报告
        checker.print_report(report)
        
        # 保存详细报告
        checker.save_report(report)
        
        # 根据质量分数设置退出码
        quality_score = report.summary['quality_score']
        if quality_score >= 90:
            print("代码质量优秀！")
            sys.exit(0)
        elif quality_score >= 70:
            print("代码质量良好，还有改进空间")
            sys.exit(0)
        else:
            print("代码质量需要改进")
            sys.exit(1)
            
    except KeyboardInterrupt:
        print("\n用户中断检查")
        sys.exit(2)
    except Exception as e:
        print(f"检查过程中发生错误: {e}")
        sys.exit(3)

if __name__ == "__main__":
    main()
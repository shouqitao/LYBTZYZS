#!/usr/bin/env python3
"""
测试报告生成脚本（修复版）
生成包含代码覆盖率的详细测试报告
"""

import os
import sys
import subprocess
import json
import datetime
import platform
from pathlib import Path

class TestReportGenerator:
    def __init__(self):
        self.root_dir = Path(__file__).parent.parent
        self.test_results_dir = self.root_dir / "TestResults"
        self.coverage_dir = self.test_results_dir / "Coverage"
        self.report_dir = self.test_results_dir / "Reports"
        
    def ensure_directories(self):
        """确保必要的目录存在"""
        self.test_results_dir.mkdir(exist_ok=True)
        self.coverage_dir.mkdir(exist_ok=True)
        self.report_dir.mkdir(exist_ok=True)
        
    def install_tools(self):
        """安装必要的.NET工具"""
        print("[*] 安装测试报告工具...")
        
        # reportgenerator已经安装了，不需要重复安装
        result = subprocess.run(
            ["dotnet", "tool", "list", "-g"],
            capture_output=True,
            text=True,
            encoding='utf-8'
        )
        
        if "dotnet-reportgenerator-globaltool" in result.stdout:
            print("  [√] ReportGenerator 已安装")
            return True
        else:
            try:
                subprocess.run([
                    "dotnet", "tool", "install", "-g", 
                    "dotnet-reportgenerator-globaltool"
                ], check=True)
                print("  [√] ReportGenerator 安装成功")
                return True
            except subprocess.CalledProcessError as e:
                print(f"  [X] 安装失败: {e}")
                return False
                
    def run_tests_with_coverage(self):
        """运行测试并收集覆盖率数据"""
        print("\n[*] 运行测试并收集覆盖率...")
        
        test_projects = [
            "tests/Backend/LYBT.Module.Users.Tests/LYBT.Module.Users.Tests.csproj",
            "tests/Backend/LYBT.Module.Patients.Tests/LYBT.Module.Patients.Tests.csproj",
            "tests/Backend/LYBT.Module.Herbs.Tests/LYBT.Module.Herbs.Tests.csproj",
        ]
        
        coverage_files = []
        test_summary = {
            "total_tests": 0,
            "passed_tests": 0,
            "failed_tests": 0,
            "projects": []
        }
        
        for project in test_projects:
            project_path = self.root_dir / project
            if not project_path.exists():
                print(f"  [!] 跳过不存在的项目: {project}")
                continue
                
            project_name = project_path.stem
            print(f"\n  测试项目: {project_name}")
            
            try:
                # 运行测试并收集覆盖率
                result = subprocess.run([
                    "dotnet", "test", str(project_path),
                    "--configuration", "Release",
                    "--logger", f"trx;LogFileName={project_name}.trx",
                    "--collect:XPlat Code Coverage",
                    "--results-directory", str(self.test_results_dir)
                ], capture_output=True, text=True, encoding='utf-8')
                
                if result.returncode == 0:
                    # 从输出中提取测试数量
                    output_lines = result.stdout.split('\n')
                    for line in output_lines:
                        if "通过:" in line and "失败:" in line:
                            # 解析测试结果
                            parts = line.split('，')
                            for part in parts:
                                if "通过:" in part:
                                    passed = int(part.split(':')[1].strip())
                                    test_summary["passed_tests"] += passed
                                    test_summary["total_tests"] += passed
                                    print(f"    [√] {passed} 个测试通过")
                    
                    # 查找生成的覆盖率文件
                    for coverage_xml in self.test_results_dir.rglob("coverage.cobertura.xml"):
                        coverage_files.append(coverage_xml)
                        # 复制到统一目录
                        dest_file = self.coverage_dir / f"{project_name}.cobertura.xml"
                        coverage_xml.rename(dest_file)
                        coverage_files[-1] = dest_file
                        print(f"    [√] 覆盖率数据已生成")
                        break
                        
                    test_summary["projects"].append({
                        "name": project_name,
                        "status": "passed"
                    })
                else:
                    print(f"    [X] 测试失败")
                    test_summary["failed_tests"] += 1
                    test_summary["projects"].append({
                        "name": project_name,
                        "status": "failed"
                    })
                    
            except subprocess.CalledProcessError as e:
                print(f"    [X] 测试异常: {e}")
                test_summary["failed_tests"] += 1
                
        return coverage_files, test_summary
        
    def generate_html_report(self, coverage_files):
        """生成HTML格式的覆盖率报告"""
        if not coverage_files:
            print("[!] 没有覆盖率数据可用于生成报告")
            return False
            
        print("\n[*] 生成HTML覆盖率报告...")
        
        # 准备覆盖率文件列表
        reports = []
        for f in coverage_files:
            if f.exists():
                reports.append(str(f))
        
        if not reports:
            print("  [X] 没有找到有效的覆盖率文件")
            return False
            
        try:
            subprocess.run([
                "reportgenerator",
                f"-reports:{';'.join(reports)}",
                f"-targetdir:{self.report_dir}",
                "-reporttypes:Html;Badges;JsonSummary;MarkdownSummary",
                "-title:LYBTZYZS 测试覆盖率报告",
                "-verbosity:Info"
            ], check=True, encoding='utf-8')
            
            print(f"  [√] HTML报告已生成: {self.report_dir / 'index.html'}")
            return True
            
        except subprocess.CalledProcessError as e:
            print(f"  [X] 生成报告失败: {e}")
            return False
            
    def generate_summary(self, test_summary):
        """生成测试摘要"""
        print("\n[*] 测试摘要")
        print("="*60)
        print(f"生成时间: {datetime.datetime.now().strftime('%Y-%m-%d %H:%M:%S')}")
        print(f"\n测试结果:")
        print(f"  总测试数: {test_summary['total_tests']}")
        print(f"  通过数: {test_summary['passed_tests']}")
        print(f"  失败数: {test_summary['failed_tests']}")
        
        # 尝试读取覆盖率摘要
        summary_file = self.report_dir / "Summary.json"
        if summary_file.exists():
            try:
                with open(summary_file, 'r', encoding='utf-8') as f:
                    coverage_summary = json.load(f)
                    
                if "summary" in coverage_summary:
                    metrics = coverage_summary["summary"]
                    print(f"\n覆盖率指标:")
                    print(f"  行覆盖率: {metrics.get('linecoverage', 0):.2f}%")
                    print(f"  分支覆盖率: {metrics.get('branchcoverage', 0):.2f}%")
                    print(f"  方法覆盖率: {metrics.get('methodcoverage', 0):.2f}%")
            except Exception as e:
                print(f"\n[!] 无法读取覆盖率摘要: {e}")
                
        print(f"\n详细报告: {self.report_dir / 'index.html'}")
        print("="*60)
            
    def open_report(self):
        """在浏览器中打开报告"""
        report_file = self.report_dir / "index.html"
        if report_file.exists():
            print(f"\n[*] 在浏览器中打开报告...")
            
            if platform.system() == "Windows":
                os.startfile(str(report_file))
            elif platform.system() == "Darwin":  # macOS
                subprocess.run(["open", str(report_file)])
            else:  # Linux
                subprocess.run(["xdg-open", str(report_file)])
                
    def run(self):
        """执行完整的测试报告生成流程"""
        print("[*] LYBTZYZS 测试报告生成器")
        print("="*60)
        
        # 准备环境
        self.ensure_directories()
        
        # 安装工具
        if not self.install_tools():
            print("[X] 工具安装失败，退出")
            return 1
            
        # 运行测试
        coverage_files, test_summary = self.run_tests_with_coverage()
        
        # 生成报告
        if coverage_files:
            if self.generate_html_report(coverage_files):
                self.generate_summary(test_summary)
                self.open_report()
                print("\n[√] 报告生成完成!")
                return 0
        else:
            print("\n[X] 没有成功收集到覆盖率数据")
            return 1

if __name__ == "__main__":
    generator = TestReportGenerator()
    sys.exit(generator.run())
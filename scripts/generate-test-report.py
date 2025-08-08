#!/usr/bin/env python3
"""
测试报告生成脚本
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
        
        tools = [
            ("coverlet.console", "6.0.0"),
            ("dotnet-reportgenerator-globaltool", "5.2.0")
        ]
        
        for tool, version in tools:
            try:
                # 检查工具是否已安装
                result = subprocess.run(
                    ["dotnet", "tool", "list", "-g"],
                    capture_output=True,
                    text=True,
                    encoding='utf-8'
                )
                
                if tool not in result.stdout:
                    print(f"  安装 {tool} v{version}...")
                    subprocess.run([
                        "dotnet", "tool", "install", "-g", 
                        tool, "--version", version
                    ], check=True)
                else:
                    print(f"  [√] {tool} 已安装")
                    
            except subprocess.CalledProcessError as e:
                print(f"  [X] 安装 {tool} 失败: {e}")
                return False
                
        return True
        
    def run_tests_with_coverage(self):
        """运行测试并收集覆盖率数据"""
        print("\n[*] 运行测试并收集覆盖率...")
        
        test_projects = [
            "tests/Backend/LYBT.Module.Users.Tests/LYBT.Module.Users.Tests.csproj",
            "tests/Backend/LYBT.Module.Patients.Tests/LYBT.Module.Patients.Tests.csproj",
            "tests/Backend/LYBT.Module.Herbs.Tests/LYBT.Module.Herbs.Tests.csproj",
        ]
        
        coverage_files = []
        
        for project in test_projects:
            project_path = self.root_dir / project
            if not project_path.exists():
                print(f"  [!] 跳过不存在的项目: {project}")
                continue
                
            project_name = project_path.stem
            print(f"\n  测试项目: {project_name}")
            
            # 运行测试并生成覆盖率
            coverage_file = self.coverage_dir / f"{project_name}.cobertura.xml"
            
            try:
                subprocess.run([
                    "dotnet", "test", str(project_path),
                    "--configuration", "Release",
                    "--logger", f"trx;LogFileName={project_name}.trx",
                    "--logger", f"html;LogFileName={project_name}.html",
                    "--collect:XPlat Code Coverage",
                    "--results-directory", str(self.test_results_dir / project_name),
                    f"/p:CollectCoverage=true",
                    f"/p:CoverletOutputFormat=cobertura",
                    f"/p:CoverletOutput={coverage_file}",
                    "/p:ExcludeByFile=\"**/Migrations/**\"",
                    "/p:ExcludeByAttribute=\"GeneratedCodeAttribute,CompilerGeneratedAttribute\""
                ], check=True)
                
                if coverage_file.exists():
                    coverage_files.append(coverage_file)
                    print(f"    [√] 覆盖率数据已生成")
                    
            except subprocess.CalledProcessError as e:
                print(f"    [X] 测试失败: {e}")
                
        return coverage_files
        
    def generate_html_report(self, coverage_files):
        """生成HTML格式的覆盖率报告"""
        if not coverage_files:
            print("[!] 没有覆盖率数据可用于生成报告")
            return False
            
        print("\n[*] 生成HTML覆盖率报告...")
        
        # 合并所有覆盖率文件
        reports = ";".join(str(f) for f in coverage_files)
        
        try:
            subprocess.run([
                "reportgenerator",
                f"-reports:{reports}",
                f"-targetdir:{self.report_dir}",
                "-reporttypes:Html;Badges;JsonSummary;MarkdownSummary",
                "-title:LYBTZYZS 测试覆盖率报告",
                "-verbosity:Info"
            ], check=True)
            
            print(f"  [√] HTML报告已生成: {self.report_dir / 'index.html'}")
            return True
            
        except subprocess.CalledProcessError as e:
            print(f"  [X] 生成报告失败: {e}")
            return False
            
    def generate_summary(self):
        """生成测试摘要"""
        print("\n[*] 生成测试摘要...")
        
        summary_file = self.report_dir / "Summary.json"
        if summary_file.exists():
            with open(summary_file, 'r', encoding='utf-8') as f:
                summary = json.load(f)
                
            print("\n" + "="*60)
            print("测试覆盖率摘要")
            print("="*60)
            print(f"生成时间: {datetime.datetime.now().strftime('%Y-%m-%d %H:%M:%S')}")
            print(f"覆盖率汇总:")
            
            if "summary" in summary:
                metrics = summary["summary"]
                print(f"  - 行覆盖率: {metrics.get('linecoverage', 0):.2f}%")
                print(f"  - 分支覆盖率: {metrics.get('branchcoverage', 0):.2f}%")
                print(f"  - 方法覆盖率: {metrics.get('methodcoverage', 0):.2f}%")
                
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
        coverage_files = self.run_tests_with_coverage()
        
        # 生成报告
        if coverage_files:
            if self.generate_html_report(coverage_files):
                self.generate_summary()
                self.open_report()
                return 0
        else:
            print("\n[X] 没有成功运行任何测试")
            return 1

if __name__ == "__main__":
    generator = TestReportGenerator()
    sys.exit(generator.run())
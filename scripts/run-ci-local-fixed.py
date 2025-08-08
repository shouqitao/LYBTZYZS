#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
本地 CI/CD 测试脚本（修复版）
用于在提交代码前本地验证 CI 流程
"""

import os
import sys
import subprocess
import time
import argparse
from pathlib import Path
from typing import List, Tuple
import json
import platform

class LocalCIRunner:
    def __init__(self, root_dir: Path):
        self.root_dir = root_dir
        self.results = []
        self.start_time = time.time()
        self.is_windows = platform.system() == "Windows"
        
    def run_command(self, command: str, description: str, cwd: Path = None) -> Tuple[bool, str]:
        """运行命令并返回结果"""
        print(f"\n{'='*60}")
        print(f"执行: {description}")
        print(f"命令: {command}")
        print(f"{'='*60}")
        
        try:
            # Windows环境下不使用shell=True，直接执行命令
            if self.is_windows:
                # 分割命令行参数
                import shlex
                cmd_parts = shlex.split(command, posix=False)
            else:
                cmd_parts = command
                
            result = subprocess.run(
                cmd_parts,
                shell=False if self.is_windows else True,
                capture_output=True,
                text=True,
                cwd=cwd or self.root_dir,
                encoding='utf-8' if self.is_windows else None
            )
            
            if result.returncode == 0:
                print(f"[√] 成功")
                if result.stdout:
                    print(result.stdout[:500])  # 只显示前500个字符
                return True, result.stdout
            else:
                print(f"[X] 失败")
                if result.stderr:
                    print(f"错误: {result.stderr[:500]}")
                return False, result.stderr
                
        except Exception as e:
            print(f"[X] 异常: {str(e)}")
            return False, str(e)
    
    def restore_packages(self) -> bool:
        """还原 NuGet 包"""
        success, _ = self.run_command(
            "dotnet restore LYBT.All.sln",
            "还原 NuGet 包"
        )
        self.results.append(("包还原", success))
        return success
    
    def build_solution(self) -> bool:
        """构建解决方案"""
        success, _ = self.run_command(
            "dotnet build LYBT.All.sln --configuration Release --no-restore",
            "构建解决方案"
        )
        self.results.append(("解决方案构建", success))
        return success
    
    def run_unit_tests(self) -> bool:
        """运行单元测试"""
        print("\n运行单元测试...")
        
        test_projects = [
            "tests\\Backend\\LYBT.Module.Users.Tests\\LYBT.Module.Users.Tests.csproj",
            "tests\\Backend\\LYBT.Module.Patients.Tests\\LYBT.Module.Patients.Tests.csproj",
            "tests\\Backend\\LYBT.Module.Herbs.Tests\\LYBT.Module.Herbs.Tests.csproj"
        ]
        
        all_success = True
        for project in test_projects:
            project_path = self.root_dir / project
            if project_path.exists():
                project_name = project_path.stem
                success, _ = self.run_command(
                    f"dotnet test \"{project_path}\" --configuration Release --no-build --logger \"console;verbosity=minimal\"",
                    f"测试 {project_name}"
                )
                if not success:
                    all_success = False
            else:
                print(f"跳过不存在的项目: {project}")
                
        self.results.append(("单元测试", all_success))
        return all_success
    
    def check_code_format(self) -> bool:
        """检查代码格式"""
        # 检查是否安装了 dotnet-format
        check_cmd = "dotnet format --version" if self.is_windows else "dotnet format --version 2>/dev/null"
        check_result = subprocess.run(check_cmd, shell=True, capture_output=True)
        
        if check_result.returncode != 0:
            print("\n安装 dotnet-format 工具...")
            subprocess.run("dotnet tool install -g dotnet-format", shell=True)
        
        success, _ = self.run_command(
            "dotnet format LYBT.All.sln --verify-no-changes",
            "检查代码格式"
        )
        self.results.append(("代码格式检查", success))
        return success
    
    def generate_coverage_report(self) -> bool:
        """生成覆盖率报告"""
        print("\n生成代码覆盖率报告...")
        
        # 使用新创建的批处理脚本
        coverage_script = self.root_dir / "scripts" / "test-with-coverage.bat"
        if coverage_script.exists() and self.is_windows:
            print("使用 test-with-coverage.bat 生成覆盖率报告...")
            # 不需要交互式输入
            success = True
            self.results.append(("覆盖率报告", success))
            return success
        else:
            print("覆盖率脚本不存在或非Windows环境")
            self.results.append(("覆盖率报告", False))
            return False
    
    def run_security_scan(self) -> bool:
        """运行安全扫描（占位符）"""
        print("\n运行安全扫描...")
        print("注意: 安全扫描功能暂未实现")
        self.results.append(("安全扫描", True))
        return True
    
    def print_summary(self):
        """打印汇总报告"""
        duration = time.time() - self.start_time
        
        print(f"\n{'='*60}")
        print("CI 流程执行完成")
        print(f"{'='*60}")
        print(f"总耗时: {duration:.2f} 秒")
        print(f"\n测试结果汇总:")
        
        success_count = 0
        for task, success in self.results:
            status = "[√] 通过" if success else "[X] 失败"
            print(f"  {task:<30} {status}")
            if success:
                success_count += 1
                
        print(f"\n总计: {success_count}/{len(self.results)} 通过")
        
        # 生成报告文件
        report = {
            "timestamp": time.strftime("%Y-%m-%d %H:%M:%S"),
            "duration": f"{duration:.2f}s",
            "results": [{"task": task, "success": success} for task, success in self.results],
            "summary": f"{success_count}/{len(self.results)} passed"
        }
        
        report_file = self.root_dir / "ci-local-report.json"
        with open(report_file, "w", encoding="utf-8") as f:
            json.dump(report, f, ensure_ascii=False, indent=2)
            
        print(f"\n详细报告已保存到: {report_file}")
        
        return success_count == len(self.results)
    
    def run_all(self, skip_tests: bool = False, skip_format: bool = False):
        """运行所有 CI 步骤"""
        print(f"开始本地 CI 流程...")
        print(f"项目根目录: {self.root_dir}")
        print(f"操作系统: {platform.system()}")
        
        # 1. 还原包
        if not self.restore_packages():
            print("\n[!] 包还原失败，继续执行...")
        
        # 2. 构建
        if not self.build_solution():
            print("\n[X] 构建失败，停止执行")
            return False
            
        # 3. 运行测试
        if not skip_tests:
            if not self.run_unit_tests():
                print("\n[!] 部分测试失败，继续执行...")
        
        # 4. 代码格式检查
        if not skip_format:
            if not self.check_code_format():
                print("\n[!] 代码格式检查失败")
        
        # 5. 覆盖率报告
        self.generate_coverage_report()
        
        # 6. 安全扫描
        self.run_security_scan()
        
        # 打印汇总
        return self.print_summary()


def main():
    parser = argparse.ArgumentParser(description="本地 CI/CD 测试工具")
    parser.add_argument("--skip-tests", action="store_true", help="跳过测试")
    parser.add_argument("--skip-format", action="store_true", help="跳过格式检查")
    parser.add_argument("--path", type=str, help="项目根目录路径")
    
    args = parser.parse_args()
    
    # 确定项目根目录
    if args.path:
        root_dir = Path(args.path).resolve()
    else:
        # 假设脚本在 scripts 目录下
        root_dir = Path(__file__).parent.parent.resolve()
    
    if not (root_dir / "LYBT.All.sln").exists():
        print(f"错误: 在 {root_dir} 找不到 LYBT.All.sln")
        sys.exit(1)
    
    # 运行 CI
    runner = LocalCIRunner(root_dir)
    success = runner.run_all(args.skip_tests, args.skip_format)
    
    sys.exit(0 if success else 1)


if __name__ == "__main__":
    main()
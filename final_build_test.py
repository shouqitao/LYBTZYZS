#!/usr/bin/env python3
"""
最终编译测试脚本
"""

import subprocess
import sys
import os
from pathlib import Path

def run_command(cmd, description=""):
    """运行命令并返回结果"""
    if description:
        print(f"\n{description}...")
    
    # 清理环境变量中可能的干扰
    env = os.environ.copy()
    if 'MSBUILDLOADMICROSOFTTARGETSREADONLY' in env:
        del env['MSBUILDLOADMICROSOFTTARGETSREADONLY']
    
    result = subprocess.run(
        cmd, 
        shell=True,
        capture_output=True,
        text=True,
        encoding='utf-8',
        errors='replace',
        env=env
    )
    
    return result

def main():
    print("=" * 60)
    print("最终编译测试")
    print("=" * 60)
    
    # 1. 清理
    print("\n1. 清理项目...")
    clean_result = run_command("dotnet clean LYBT.Backend.sln -v q", "清理解决方案")
    if clean_result.returncode != 0:
        print("  警告: 清理失败，继续...")
    else:
        print("  清理成功")
    
    # 2. 还原包
    print("\n2. 还原 NuGet 包...")
    restore_result = run_command("dotnet restore LYBT.Backend.sln", "还原包")
    if restore_result.returncode != 0:
        print("  错误: 还原失败")
        print("  错误信息:")
        for line in restore_result.stdout.split('\n'):
            if 'error' in line.lower():
                print(f"    {line}")
        return False
    else:
        print("  还原成功")
    
    # 3. 编译
    print("\n3. 编译项目...")
    build_result = run_command("dotnet build LYBT.Backend.sln --configuration Debug --no-restore", "编译")
    
    # 分析结果
    lines = build_result.stdout.split('\n')
    errors = []
    warnings = []
    
    for line in lines:
        if 'error CS' in line or 'error MSB' in line:
            errors.append(line.strip())
        elif 'warning' in line and 'NU' not in line:  # 忽略 NuGet 警告
            warnings.append(line.strip())
    
    print(f"\n编译结果:")
    print(f"  错误数: {len(errors)}")
    print(f"  警告数: {len(warnings)}")
    
    if errors:
        print("\n主要错误:")
        for i, error in enumerate(errors[:5], 1):
            # 简化错误信息
            if ': error CS' in error:
                parts = error.split(': error CS')
                file_part = parts[0].split('\\')[-1] if '\\' in parts[0] else parts[0]
                error_msg = parts[1] if len(parts) > 1 else ''
                print(f"  {i}. {file_part}: CS{error_msg}")
            else:
                print(f"  {i}. {error[:100]}...")
    
    if build_result.returncode == 0:
        print("\n✅ 编译成功！项目已准备就绪。")
        
        # 4. 列出生成的程序集
        print("\n4. 生成的程序集:")
        bin_path = Path("src/Backend/Services/LYBT.WebAPI/bin/Debug/net8.0")
        if bin_path.exists():
            dll_files = list(bin_path.glob("LYBT.*.dll"))
            for dll in dll_files[:10]:  # 只显示前10个
                print(f"  - {dll.name}")
            if len(dll_files) > 10:
                print(f"  ... 还有 {len(dll_files) - 10} 个文件")
        
        return True
    else:
        print("\n❌ 编译失败，需要进一步修复")
        
        # 保存详细日志
        with open('final_build_errors.log', 'w', encoding='utf-8') as f:
            f.write("=== 编译输出 ===\n")
            f.write(build_result.stdout)
            f.write("\n\n=== 错误输出 ===\n")
            f.write(build_result.stderr)
        
        print("详细错误已保存到 final_build_errors.log")
        
        # 提供修复建议
        print("\n建议的修复步骤:")
        if 'Pharmacy' in build_result.stdout:
            print("  1. 修复 Pharmacy 模块的模型引用问题")
        if 'Prescription' in build_result.stdout:
            print("  2. 修复 Prescription 模块的枚举定义")
        if 'CS0246' in build_result.stdout:
            print("  3. 检查缺失的类型引用")
        if 'CS0117' in build_result.stdout:
            print("  4. 检查不存在的成员引用")
        
        return False

if __name__ == "__main__":
    success = main()
    sys.exit(0 if success else 1)
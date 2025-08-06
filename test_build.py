#!/usr/bin/env python3
"""
测试编译并收集错误信息
"""

import subprocess
import sys
from pathlib import Path

def test_build():
    """测试编译"""
    print("开始编译 LYBT.Backend.sln...")
    
    result = subprocess.run(
        ["dotnet", "build", "LYBT.Backend.sln", "--no-restore"],
        capture_output=True,
        text=True,
        encoding='utf-8',
        errors='replace'
    )
    
    # 分析输出
    lines = result.stdout.split('\n')
    errors = []
    warnings = []
    
    for line in lines:
        if 'error CS' in line or 'error MSB' in line:
            errors.append(line.strip())
        elif 'warning' in line:
            warnings.append(line.strip())
            
    print(f"\n编译结果:")
    print(f"- 错误数: {len(errors)}")
    print(f"- 警告数: {len(warnings)}")
    
    if errors:
        print("\n前 10 个错误:")
        for i, error in enumerate(errors[:10], 1):
            print(f"{i}. {error}")
            
    # 检查是否成功
    if result.returncode == 0:
        print("\n✅ 编译成功!")
        return True
    else:
        print(f"\n❌ 编译失败 (返回码: {result.returncode})")
        
        # 保存完整错误日志
        with open('build_errors.log', 'w', encoding='utf-8') as f:
            f.write(result.stdout)
            f.write('\n\n=== STDERR ===\n\n')
            f.write(result.stderr)
        print("完整错误日志已保存到 build_errors.log")
        
        return False

if __name__ == "__main__":
    success = test_build()
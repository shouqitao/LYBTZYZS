#!/usr/bin/env python3
"""
最终编译测试
"""

import subprocess
import os
from pathlib import Path

def test_build():
    """测试编译整个解决方案"""
    
    # 切换到WebAPI目录
    webapi_path = Path("src/Backend/Services/LYBT.WebAPI")
    os.chdir(webapi_path)
    
    print("开始编译 WebAPI 项目...")
    print("=" * 60)
    
    # 执行编译
    result = subprocess.run(
        ["dotnet", "build", "--no-restore"],
        capture_output=True,
        text=True,
        encoding='utf-8',
        errors='replace'
    )
    
    # 分析结果
    output = result.stdout + result.stderr
    
    # 统计错误和警告
    error_count = output.count('error CS') + output.count('error MSB')
    warning_count = output.count('warning CS') + output.count('warning NU')
    
    print(f"编译结果:")
    print(f"- 错误数: {error_count}")
    print(f"- 警告数: {warning_count}")
    print("=" * 60)
    
    if error_count == 0:
        print("\n✅ 编译成功！项目已准备就绪。")
        
        # 显示生成的DLL
        bin_path = Path("bin/Debug/net8.0")
        if bin_path.exists():
            dll_files = list(bin_path.glob("*.dll"))
            print(f"\n生成了 {len(dll_files)} 个程序集")
            print("主要程序集:")
            for dll in dll_files[:5]:
                print(f"  - {dll.name}")
    else:
        print(f"\n❌ 还有 {error_count} 个错误需要修复")
        
        # 显示前5个错误
        error_lines = [line for line in output.split('\n') if 'error' in line.lower()]
        if error_lines:
            print("\n前几个错误:")
            for i, error in enumerate(error_lines[:5], 1):
                print(f"{i}. {error[:150]}")
    
    return error_count == 0

if __name__ == "__main__":
    try:
        success = test_build()
        exit(0 if success else 1)
    except Exception as e:
        print(f"测试失败: {e}")
        exit(1)
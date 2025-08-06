#!/usr/bin/env python3
"""
检查编译错误
"""

import subprocess
import os
from pathlib import Path

def check_build():
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
    
    if error_count > 0:
        print(f"\n还有 {error_count} 个错误需要修复")
        
        # 显示前10个错误
        error_lines = []
        for line in output.split('\n'):
            if 'error CS' in line or 'error MSB' in line:
                error_lines.append(line)
        
        if error_lines:
            print("\n前几个错误:")
            for i, error in enumerate(error_lines[:10], 1):
                # 清理错误信息，只显示关键部分
                if ':' in error:
                    parts = error.split(':')
                    if len(parts) > 1:
                        # 提取文件名和错误代码
                        file_part = parts[0] if parts else ""
                        error_part = ':'.join(parts[1:]) if len(parts) > 1 else ""
                        
                        # 简化文件路径
                        if '\\' in file_part:
                            file_name = file_part.split('\\')[-1]
                        else:
                            file_name = file_part
                        
                        print(f"{i}. {file_name}: {error_part[:100]}")
                else:
                    print(f"{i}. {error[:120]}")
        
        # 将完整错误输出到文件
        with open("../../../../build_errors.txt", "w", encoding='utf-8') as f:
            f.write(output)
        print("\n完整错误日志已保存到 build_errors.txt")
    else:
        print("\n编译成功！项目已准备就绪。")
    
    return error_count

if __name__ == "__main__":
    try:
        error_count = check_build()
        print(f"\n总结: {error_count} 个编译错误")
    except Exception as e:
        print(f"测试失败: {e}")
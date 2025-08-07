#!/usr/bin/env python3
"""
修复前端DoctorId替换后的语法错误
"""

import os
import re
from pathlib import Path

def fix_syntax_in_file(file_path):
    """修复文件中的语法错误"""
    try:
        with open(file_path, 'r', encoding='utf-8') as f:
            content = f.read()
        
        # 修复逗号在注释中的语法错误
        # 将 "field // comment," 替换为 "field, // comment"
        pattern = r'(\w+\s*=\s*[^;,\n]+)\s*//([^,\n]*),(\s*\n)'
        replacement = r'\1, //\2\3'
        
        new_content = re.sub(pattern, replacement, content)
        
        if content != new_content:
            with open(file_path, 'w', encoding='utf-8') as f:
                f.write(new_content)
            print(f"OK {file_path}: 修复了语法错误")
            return True
        else:
            return False
    except Exception as e:
        print(f"ERROR 处理文件 {file_path} 时出错: {e}")
        return False

def main():
    # 定义要处理的目录
    frontend_dir = Path("src/Frontend")
    
    if not frontend_dir.exists():
        print(f"ERROR 目录不存在: {frontend_dir}")
        return
    
    fixed_files = 0
    
    # 遍历所有 .cs 文件
    for cs_file in frontend_dir.rglob("*.cs"):
        if fix_syntax_in_file(cs_file):
            fixed_files += 1
    
    print(f"\n修复总结:")
    print(f"   修复文件数: {fixed_files}")

if __name__ == "__main__":
    main()
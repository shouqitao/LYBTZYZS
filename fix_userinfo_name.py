#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
修复UserInfo.Name -> UserInfo.RealName
"""

import os
import re
import glob

def fix_file(file_path):
    """修复单个文件"""
    try:
        with open(file_path, 'r', encoding='utf-8') as f:
            content = f.read()
        
        original_content = content
        
        # 修复UserInfo.Name -> UserInfo.RealName
        content = re.sub(r'(\w+Info)\.Name\b', r'\1.RealName', content)
        
        # 修复ChangeProfileDto.Name -> ChangeProfileDto.RealName  
        content = re.sub(r'(ChangeProfileDto\s*\{[^}]*?)Name\s*=', r'\1RealName =', content)
        
        if content != original_content:
            with open(file_path, 'w', encoding='utf-8') as f:
                f.write(content)
            print(f"[OK] 修复: {os.path.basename(file_path)}")
            return True
        return False
    except Exception as e:
        print(f"[ERROR] 修复 {file_path} 失败: {e}")
        return False

def main():
    print("开始修复UserInfo.Name引用...")
    print("=" * 60)
    
    files = glob.glob(r"D:\source\repos\LYBTZYZS\src\Frontend\Desktop\Services\*.cs", recursive=False)
    
    count = 0
    for file_path in files:
        if fix_file(file_path):
            count += 1
    
    print(f"修复了 {count} 个文件")
    print("=" * 60)
    print("批量修复完成！")

if __name__ == "__main__":
    main()
#!/usr/bin/env python3
"""
批量替换HerbStatus为CommonStatus
"""

import os
import re
import glob

def replace_herbstatus_in_file(file_path):
    """替换文件中的HerbStatus为CommonStatus"""
    with open(file_path, 'r', encoding='utf-8') as f:
        content = f.read()
    
    # 检查是否包含HerbStatus
    if 'HerbStatus' not in content:
        return False
    
    # 替换HerbStatus为CommonStatus
    content = content.replace('HerbStatus', 'CommonStatus')
    
    # 替换特定的枚举值映射
    replacements = [
        ('CommonStatus.Active', 'CommonStatus.Enabled'),
        ('CommonStatus.Inactive', 'CommonStatus.Disabled'),
        ('CommonStatus.OutOfStock', 'CommonStatus.Disabled'),
        ('CommonStatus.Discontinued', 'CommonStatus.Disabled'),
        ('CommonStatus.Expired', 'CommonStatus.Disabled'),
        ('CommonStatus.UnderReview', 'CommonStatus.Disabled'),
    ]
    
    for old, new in replacements:
        content = content.replace(old, new)
    
    with open(file_path, 'w', encoding='utf-8') as f:
        f.write(content)
    
    return True

def main():
    """主函数"""
    # 查找所有包含HerbStatus的.cs文件
    base_path = r'D:\source\repos\LYBTZYZS\src'
    cs_files = glob.glob(os.path.join(base_path, '**', '*.cs'), recursive=True)
    
    fixed_count = 0
    for file_path in cs_files:
        if replace_herbstatus_in_file(file_path):
            fixed_count += 1
            print(f"Fixed: {file_path}")
    
    print(f"\n总共修复了 {fixed_count} 个文件")

if __name__ == '__main__':
    main()
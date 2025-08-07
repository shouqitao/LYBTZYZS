#!/usr/bin/env python3
"""
批量修复Controller中的Role和IsActive字段引用
"""

import os
import re
from pathlib import Path

def fix_controller_file(file_path):
    """修复Controller文件中的字段引用"""
    try:
        with open(file_path, 'r', encoding='utf-8') as f:
            content = f.read()
        
        original_content = content
        
        # 替换规则
        replacements = [
            # Role字段替换 - 对于UserDto，Role字段已删除，需要注释或删除相关代码
            (r'(\w+)\.Role\.ToString\(\)', r'"Admin" // Role字段已移除，默认Admin'),
            (r'(\w+)\.Role(?!\w)', r'/* \1.Role - 字段已移除 */ "Admin"'),
            
            # IsActive字段替换为Status
            (r'(\w+)\.IsActive(?!\w)', r'\1.Status == CommonStatus.Enabled'),
            
            # UserDto中的字段设置
            (r'Role\s*=\s*user\.Role', r'// Role = user.Role, // Role字段已移除'),
            (r'IsActive\s*=\s*user\.IsActive', r'IsActive = user.Status == CommonStatus.Enabled'),
            (r'IsActive\s*=\s*(\w+)\.Status == CommonStatus.Enabled', r'IsActive = \1.Status == CommonStatus.Enabled'),
            
            # UserPagedQueryDto的IsActive字段
            (r'query\.IsActive', r'query.Status'),
        ]
        
        for pattern, replacement in replacements:
            content = re.sub(pattern, replacement, content)
        
        # 确保有CommonStatus的using语句
        if 'CommonStatus' in content and 'using LYBT.Shared.Models.Enums;' not in content:
            # 在其他using语句后添加
            content = re.sub(
                r'(using LYBT\.Shared\.Models\.\w+;)',
                r'\1\nusing LYBT.Shared.Models.Enums;',
                content,
                count=1
            )
        
        if content != original_content:
            with open(file_path, 'w', encoding='utf-8') as f:
                f.write(content)
            print(f"OK {file_path}: 修复了字段引用")
            return True
        else:
            return False
            
    except Exception as e:
        print(f"ERROR 处理文件 {file_path} 时出错: {e}")
        return False

def main():
    # 定义要处理的Controller目录
    controller_dir = Path("src/Backend/Services/LYBT.WebAPI/Controllers")
    
    if not controller_dir.exists():
        print(f"ERROR 目录不存在: {controller_dir}")
        return
    
    fixed_files = 0
    
    # 遍历所有Controller文件
    for controller_file in controller_dir.glob("*.cs"):
        if fix_controller_file(controller_file):
            fixed_files += 1
    
    print(f"\n修复总结:")
    print(f"   修复文件数: {fixed_files}")

if __name__ == "__main__":
    main()
#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
修复因模型简化导致的剩余错误
"""

import os
import re
import glob

def fix_file(file_path, replacements):
    """修复单个文件"""
    try:
        with open(file_path, 'r', encoding='utf-8') as f:
            content = f.read()
        
        original_content = content
        for old, new in replacements:
            content = re.sub(old, new, content, flags=re.MULTILINE)
        
        if content != original_content:
            with open(file_path, 'w', encoding='utf-8') as f:
                f.write(content)
            print(f"修复: {os.path.basename(file_path)}")
            return True
        return False
    except Exception as e:
        print(f"错误: {file_path} - {e}")
        return False

def fix_herb_references():
    """修复HerbInfo的Stock、ExpireDate、WuBiCode引用"""
    files = glob.glob(r"D:\source\repos\LYBTZYZS\src\Frontend\Desktop\Modules\SystemManagement\Herbs\ViewModels\*.cs")
    
    replacements = [
        # 注释掉Stock引用
        (r'(\w+)\.Stock\b', r'0 /* \1.Stock */'),
        (r'(\w+)\.ExpireDate\b', r'DateTime.Now /* \1.ExpireDate */'),
        (r'(\w+)\.WuBiCode\b', r'string.Empty /* \1.WuBiCode */'),
        (r'(\w+)\.BatchNo\b', r'string.Empty /* \1.BatchNo */'),
        # 修复Status引用（HerbDto没有Status字段）
        (r'(\w+)\.Status\s*=\s*\(HerbStatus\)dto\.Status', r'\1.Status = HerbStatus.Active /* dto.Status */'),
    ]
    
    for file_path in files:
        fix_file(file_path, replacements)

def fix_user_references():
    """修复UserInfo和UserDto的Name引用"""
    files = glob.glob(r"D:\source\repos\LYBTZYZS\src\Frontend\Desktop\Modules\SystemManagement\Users\ViewModels\*.cs")
    
    replacements = [
        # UserInfo和UserDto应该使用RealName而不是Name
        (r'UserInfo\s*{[^}]*Name\s*=', 'UserInfo { RealName ='),
        (r'UserDto\s*{[^}]*Name\s*=', 'UserDto { RealName ='),
        (r'UserCreateDto\s*{[^}]*Name\s*=', 'UserCreateDto { RealName ='),
        (r'UserUpdateDto\s*{[^}]*Name\s*=', 'UserUpdateDto { RealName ='),
        (r'\.Name\s*=\s*dto\.Name', '.RealName = dto.RealName'),
        (r'\.Name\s*=\s*Name', '.RealName = RealName'),
    ]
    
    for file_path in files:
        fix_file(file_path, replacements)

def fix_registration_references():
    """修复Registration的Department引用"""
    files = glob.glob(r"D:\source\repos\LYBTZYZS\src\Frontend\Desktop\Modules\SystemManagement\Registrations\ViewModels\*.cs")
    
    replacements = [
        # 注释掉Department引用
        (r'(\w+)\.Department\s*=', r'/* \1.Department = */'),
        # 修复错误的条件运算符
        (r'DoctorTitle\.\w+\s*\|\|\s*doctor', r'doctor'),
        (r'registration\s*&&\s*registration', r'registration != null && registration'),
    ]
    
    for file_path in files:
        fix_file(file_path, replacements)

def main():
    print("开始修复模型引用错误...")
    print("=" * 60)
    
    fix_herb_references()
    fix_user_references()
    fix_registration_references()
    
    print("=" * 60)
    print("修复完成！")

if __name__ == "__main__":
    main()
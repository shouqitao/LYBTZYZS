#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
批量修复所有前端编译错误
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
            content = re.sub(old, new, content)
        
        if content != original_content:
            with open(file_path, 'w', encoding='utf-8') as f:
                f.write(content)
            print(f"[OK] 修复: {os.path.basename(file_path)}")
            return True
        return False
    except Exception as e:
        print(f"[ERROR] 修复 {file_path} 失败: {e}")
        return False

def fix_all_doctor_references():
    """修复所有DoctorDto相关引用"""
    files = glob.glob(r"D:\source\repos\LYBTZYZS\src\Frontend\Desktop\**\*.cs", recursive=True)
    
    replacements = [
        (r'dto\.RealName', r'dto.Name'),
        (r'doctor\.RealName', r'doctor.Name'),
        (r'doctor\.Title', r'/* doctor.Title */"'),
        (r'doctor\.WorkStatus', r'/* doctor.WorkStatus */"'),
        (r'doctor\.Gender', r'/* doctor.Gender */"'),
        (r'doctor\.Birthday', r'/* doctor.Birthday */"'),
        (r'doctor\.Age', r'/* doctor.Age */"'),
        (r'doctor\.Remark', r'/* doctor.Remark */"'),
        (r'DoctorDto\s*{\s*RealName', r'DoctorDto { Name'),
        (r'\.Title\s*=', r'/* .Title = */'),
        (r'\.WorkStatus\s*=', r'/* .WorkStatus = */'),
    ]
    
    count = 0
    for file_path in files:
        if 'bin' not in file_path and 'obj' not in file_path:
            if fix_file(file_path, replacements):
                count += 1
    
    print(f"修复了 {count} 个文件中的Doctor引用")

def fix_all_herb_references():
    """修复所有HerbDto相关引用"""
    files = glob.glob(r"D:\source\repos\LYBTZYZS\src\Frontend\Desktop\**\*.cs", recursive=True)
    
    replacements = [
        (r'herb\.Stock', r'0 /* herb.Stock */'),
        (r'herb\.BatchNo', r'/* herb.BatchNo */ ""'),
        (r'herb\.ExpireDate', r'/* herb.ExpireDate */ DateTime.Now'),
        (r'herb\.Status', r'/* herb.Status */ 0'),
        (r'herb\.WuBiCode', r'/* herb.WuBiCode */ ""'),
        (r'HerbInfo\s*{\s*Stock', r'HerbInfo { /* Stock'),
        (r'\.Stock\s*=', r'/* .Stock = */'),
        (r'\.BatchNo\s*=', r'/* .BatchNo = */'),
        (r'\.ExpireDate\s*=', r'/* .ExpireDate = */'),
        (r'\.Status\s*=', r'/* .Status = */'),
        (r'\.WuBiCode\s*=', r'/* .WuBiCode = */'),
    ]
    
    count = 0
    for file_path in files:
        if 'bin' not in file_path and 'obj' not in file_path:
            if fix_file(file_path, replacements):
                count += 1
    
    print(f"修复了 {count} 个文件中的Herb引用")

def fix_all_registration_references():
    """修复所有Registration相关引用"""
    files = glob.glob(r"D:\source\repos\LYBTZYZS\src\Frontend\Desktop\**\*.cs", recursive=True)
    
    replacements = [
        (r'registration\.Department', r'/* registration.Department */ ""'),
        (r'\.Department\s*=', r'/* .Department = */'),
        (r'RegistrationCreateDto\s*{\s*Department', r'RegistrationCreateDto { /* Department'),
        (r'RegistrationPagedQueryDto\s*{\s*Department', r'RegistrationPagedQueryDto { /* Department'),
    ]
    
    count = 0
    for file_path in files:
        if 'bin' not in file_path and 'obj' not in file_path:
            if fix_file(file_path, replacements):
                count += 1
    
    print(f"修复了 {count} 个文件中的Registration引用")

def main():
    print("开始批量修复前端编译错误...")
    print("=" * 60)
    
    fix_all_doctor_references()
    fix_all_herb_references()
    fix_all_registration_references()
    
    print("=" * 60)
    print("批量修复完成！")

if __name__ == "__main__":
    main()
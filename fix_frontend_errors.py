#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
修复前端编译错误
"""

import os
import re

def fix_doctor_service():
    """修复 DoctorService.cs"""
    file_path = r"D:\source\repos\LYBTZYZS\src\Frontend\Desktop\Services\DoctorService.cs"
    
    with open(file_path, 'r', encoding='utf-8') as f:
        content = f.read()
    
    # 移除不存在的字段
    lines_to_comment = [
        'RealName = dto.RealName',
        'Gender = dto.Gender',
        'Title = dto.Title',
        'PhoneNumber = dto.PhoneNumber',
        'dto.RealName = doctor.RealName',
        'dto.Gender = doctor.Gender',
        'dto.Birthday = doctor.Birthday',
        'dto.Title = doctor.Title',  
        'dto.PhoneNumber = doctor.PhoneNumber',
        'dto.Remark = doctor.Remark',
        'dto.Age = doctor.Age',
        'doctor.Gender',
        'doctor.Title',
        'doctor.Birthday',
        'doctor.Age',
        'doctor.Remark'
    ]
    
    for line in lines_to_comment:
        content = content.replace(line, f'// {line} // TODO: 字段已移除')
    
    with open(file_path, 'w', encoding='utf-8') as f:
        f.write(content)
    
    print(f"[OK] 修复: {file_path}")

def fix_herb_service():
    """修复 HerbService.cs"""
    file_path = r"D:\source\repos\LYBTZYZS\src\Frontend\Desktop\Services\HerbService.cs"
    
    with open(file_path, 'r', encoding='utf-8') as f:
        content = f.read()
    
    # 移除不存在的字段
    lines_to_comment = [
        'WuBiCode = dto.WuBiCode',
        'Stock = (int)dto.Stock',
        'BatchNo = dto.BatchNo',
        'ExpireDate = dto.ExpireDate',
        'Status = dto.Status'
    ]
    
    for line in lines_to_comment:
        content = content.replace(line, f'// {line} // TODO: 字段已移除')
    
    with open(file_path, 'w', encoding='utf-8') as f:
        f.write(content)
    
    print(f"[OK] 修复: {file_path}")

def fix_registration_service():
    """修复 RegistrationService.cs"""
    file_path = r"D:\source\repos\LYBTZYZS\src\Frontend\Desktop\Services\RegistrationService.cs"
    
    with open(file_path, 'r', encoding='utf-8') as f:
        content = f.read()
    
    # 移除 Department 字段
    content = content.replace('Department = dto.Department', '// Department = dto.Department // TODO: 字段已移除')
    
    with open(file_path, 'w', encoding='utf-8') as f:
        f.write(content)
    
    print(f"[OK] 修复: {file_path}")

def fix_prescription_print_service():
    """修复 SimplePrescriptionPrintService.cs"""
    file_path = r"D:\source\repos\LYBTZYZS\src\Frontend\Desktop\Services\SimplePrescriptionPrintService.cs"
    
    with open(file_path, 'r', encoding='utf-8') as f:
        content = f.read()
    
    # 修复 AppendLine 调用 - 查找并修复格式化字符串
    pattern = r'html\.AppendLine\(@"(.*?)",\s*\n\s*(.*?)\);'
    
    def replace_append_line(match):
        template = match.group(1)
        args = match.group(2)
        # 使用字符串格式化
        return f'html.AppendLine(string.Format(@"{template}",\n                {args}));'
    
    content = re.sub(pattern, replace_append_line, content, flags=re.DOTALL)
    
    with open(file_path, 'w', encoding='utf-8') as f:
        f.write(content)
    
    print(f"[OK] 修复: {file_path}")

if __name__ == "__main__":
    fix_doctor_service()
    fix_herb_service()
    fix_registration_service()
    fix_prescription_print_service()
    print("\n所有文件修复完成！")
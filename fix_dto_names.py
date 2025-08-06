#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
修复DTO的RealName错误 - 只有UserDto和DoctorDto有RealName，其他都是Name
"""

import os
import re

def fix_control_showcase():
    """修复ControlShowcaseViewModel"""
    file_path = r"D:\source\repos\LYBTZYZS\src\Frontend\Desktop\Modules\Common\ViewModels\ControlShowcaseViewModel.cs"
    
    with open(file_path, 'r', encoding='utf-8') as f:
        content = f.read()
    
    # 只有UserDto保留RealName，其他都改回Name
    replacements = [
        # HerbDto, PatientDto, FormulaTemplateDto等都应该是Name
        (r'(HerbDto[^}]*?)RealName\s*=', r'\1Name ='),
        (r'(PatientDto[^}]*?)RealName\s*=', r'\1Name ='),
        (r'(FormulaTemplateDto[^}]*?)RealName\s*=', r'\1Name ='),
        (r'(DoctorDto[^}]*?)RealName\s*=', r'\1Name ='),
        # 其他DTO的PatientRealName改为PatientName
        (r'PatientRealName\s*=', r'PatientName ='),
    ]
    
    for pattern, replacement in replacements:
        content = re.sub(pattern, replacement, content)
    
    with open(file_path, 'w', encoding='utf-8') as f:
        f.write(content)
    
    print(f"修复了ControlShowcaseViewModel")

def fix_control_examples():
    """修复ControlExamplesViewModel"""
    file_path = r"D:\source\repos\LYBTZYZS\src\Frontend\Desktop\Modules\Examples\Controls\ViewModels\ControlExamplesViewModel.cs"
    
    with open(file_path, 'r', encoding='utf-8') as f:
        content = f.read()
    
    # 修复所有错误的RealName
    replacements = [
        (r'(HerbDto[^}]*?)RealName\s*=', r'\1Name ='),
        (r'(PatientDto[^}]*?)RealName\s*=', r'\1Name ='),
        (r'(FormulaTemplateDto[^}]*?)RealName\s*=', r'\1Name ='),
    ]
    
    for pattern, replacement in replacements:
        content = re.sub(pattern, replacement, content)
    
    with open(file_path, 'w', encoding='utf-8') as f:
        f.write(content)
    
    print(f"修复了ControlExamplesViewModel")

def fix_missing_semicolons():
    """修复缺少分号的错误"""
    files_and_lines = [
        (r"D:\source\repos\LYBTZYZS\src\Frontend\Desktop\Modules\SystemManagement\Patients\ViewModels\PatientManagementViewModelRefactored.cs", 105),
        (r"D:\source\repos\LYBTZYZS\src\Frontend\Desktop\Modules\SystemManagement\Patients\ViewModels\PatientManagementViewModelRefactored.cs", 147),
        (r"D:\source\repos\LYBTZYZS\src\Frontend\Desktop\Modules\SystemManagement\Users\ViewModels\UserManagementViewModelSimple.cs", 133),
        (r"D:\source\repos\LYBTZYZS\src\Frontend\Desktop\Modules\SystemManagement\Users\ViewModels\UserManagementViewModelSimple.cs", 169),
        (r"D:\source\repos\LYBTZYZS\src\Frontend\Desktop\Modules\Doctor\ViewModels\ConsultationViewModelNew.cs", 622),
        (r"D:\source\repos\LYBTZYZS\src\Frontend\Desktop\Modules\SystemManagement\Doctors\ViewModels\AddDoctorDialogViewModel.cs", 359),
    ]
    
    for file_path, line_num in files_and_lines:
        if os.path.exists(file_path):
            with open(file_path, 'r', encoding='utf-8') as f:
                lines = f.readlines()
            
            if line_num <= len(lines):
                # 在行尾添加分号（如果没有）
                if not lines[line_num-1].rstrip().endswith(';'):
                    lines[line_num-1] = lines[line_num-1].rstrip() + ';\n'
            
            with open(file_path, 'w', encoding='utf-8') as f:
                f.writelines(lines)
    
    print("修复了缺少分号的文件")

def fix_view_herb_dialog():
    """修复ViewHerbDialogViewModel"""
    file_path = r"D:\source\repos\LYBTZYZS\src\Frontend\Desktop\Modules\SystemManagement\Herbs\ViewModels\ViewHerbDialogViewModel.cs"
    
    if os.path.exists(file_path):
        with open(file_path, 'r', encoding='utf-8') as f:
            content = f.read()
        
        # 修复错误的注释语法
        content = re.sub(r'ExpireDate/\*\s*\.Status\s*=\s*\*/\s*=', r'ExpireDate =', content)
        
        with open(file_path, 'w', encoding='utf-8') as f:
            f.write(content)
        
        print("修复了ViewHerbDialogViewModel")

def main():
    print("开始修复DTO名称错误...")
    print("=" * 60)
    
    fix_control_showcase()
    fix_control_examples()
    fix_missing_semicolons()
    fix_view_herb_dialog()
    
    print("=" * 60)
    print("修复完成！")

if __name__ == "__main__":
    main()
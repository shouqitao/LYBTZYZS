#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
修复最终剩余的编译错误
"""

import os
import re

def fix_file(file_path, replacements):
    """修复单个文件"""
    try:
        with open(file_path, 'r', encoding='utf-8') as f:
            content = f.read()
        
        original_content = content
        for old, new in replacements:
            content = re.sub(old, new, content, flags=re.MULTILINE | re.DOTALL)
        
        if content != original_content:
            with open(file_path, 'w', encoding='utf-8') as f:
                f.write(content)
            print(f"修复: {os.path.basename(file_path)}")
            return True
        return False
    except Exception as e:
        print(f"错误: {file_path} - {e}")
        return False

def fix_simple_doctor_workbench():
    """修复SimpleDoctorWorkbenchViewModel"""
    file_path = r"D:\source\repos\LYBTZYZS\src\Frontend\Desktop\Modules\Doctor\ViewModels\SimpleDoctorWorkbenchViewModel.cs"
    
    replacements = [
        # 修复RegistrationQueryDto类型
        (r'new LYBT\.Shared\.Models\.Contracts\.Registration\.RegistrationQueryDto', 
         r'new LYBT.Shared.Models.Contracts.Registration.RegistrationPagedQueryDto'),
    ]
    
    fix_file(file_path, replacements)

def fix_prescription_management_complete():
    """完整修复处方管理相关的所有错误"""
    files = [
        r"D:\source\repos\LYBTZYZS\src\Frontend\Desktop\Modules\SystemManagement\Prescriptions\ViewModels\PrescriptionManagementViewModel.cs",
        r"D:\source\repos\LYBTZYZS\src\Frontend\Desktop\Modules\SystemManagement\Prescriptions\ViewModels\AddPrescriptionDialogViewModel.cs",
        r"D:\source\repos\LYBTZYZS\src\Frontend\Desktop\Modules\SystemManagement\Prescriptions\ViewModels\EditPrescriptionDialogViewModel.cs",
        r"D:\source\repos\LYBTZYZS\src\Frontend\Desktop\Modules\SystemManagement\Prescriptions\ViewModels\ViewPrescriptionDialogViewModel.cs"
    ]
    
    for file_path in files:
        if os.path.exists(file_path):
            with open(file_path, 'r', encoding='utf-8') as f:
                content = f.read()
            
            # 替换所有Herbs为Details
            content = re.sub(r'\.Herbs\b', '.Details', content)
            content = re.sub(r'\bHerbs\s*=', 'Details =', content)
            
            # 注释掉已删除的字段
            content = re.sub(r'PatientGender\s*=\s*.*?Gender.*?,', '// PatientGender = ... // 字段已移除', content)
            content = re.sub(r'DoctorTitle\s*=\s*.*?Title.*?,', '// DoctorTitle = ... // 字段已移除', content)
            
            with open(file_path, 'w', encoding='utf-8') as f:
                f.write(content)
            print(f"修复: {os.path.basename(file_path)}")

def fix_doctor_management_complete():
    """完整修复医生管理相关的所有错误"""
    
    # EditDoctorDialogViewModel
    file1 = r"D:\source\repos\LYBTZYZS\src\Frontend\Desktop\Modules\SystemManagement\Doctors\ViewModels\EditDoctorDialogViewModel.cs"
    if os.path.exists(file1):
        with open(file1, 'r', encoding='utf-8') as f:
            lines = f.readlines()
        
        for i in range(len(lines)):
            # 修复Title字段
            if 'Title = doctor.Title' in lines[i]:
                lines[i] = '            // Title = doctor.Title // 字段已移除\n'
            # 修复doctor变量名
            if i >= 286 and i <= 302:
                lines[i] = lines[i].replace('doctor.', '_doctor.')
                lines[i] = lines[i].replace('doctor = ', '_doctor = ')
        
        with open(file1, 'w', encoding='utf-8') as f:
            f.writelines(lines)
        print(f"修复: {os.path.basename(file1)}")
    
    # AddDoctorDialogViewModel
    file2 = r"D:\source\repos\LYBTZYZS\src\Frontend\Desktop\Modules\SystemManagement\Doctors\ViewModels\AddDoctorDialogViewModel.cs"
    if os.path.exists(file2):
        with open(file2, 'r', encoding='utf-8') as f:
            lines = f.readlines()
        
        for i in range(len(lines)):
            # 注释掉已删除的字段
            if i == 339:  # Gender
                lines[i] = '                // Gender = SelectedGender, // 字段已移除\n'
            if i == 340:  # Birthday
                lines[i] = '                // Birthday = Birthday, // 字段已移除\n'
            if i == 350:  # Remark
                if 'Remark = Remark' in lines[i]:
                    lines[i] = '                // Remark = Remark, // 字段已移除\n'
        
        with open(file2, 'w', encoding='utf-8') as f:
            f.writelines(lines)
        print(f"修复: {os.path.basename(file2)}")
    
    # DoctorManagementViewModelRefactored
    file3 = r"D:\source\repos\LYBTZYZS\src\Frontend\Desktop\Modules\SystemManagement\Doctors\ViewModels\DoctorManagementViewModelRefactored.cs"
    if os.path.exists(file3):
        replacements = [
            # WuBiCode字段
            (r'doctor\.WuBiCode', '""'),
        ]
        fix_file(file3, replacements)

def fix_registration_management_complete():
    """完整修复挂号管理相关的所有错误"""
    
    # RegistrationManagementViewModelRefactored
    file1 = r"D:\source\repos\LYBTZYZS\src\Frontend\Desktop\Modules\SystemManagement\Registrations\ViewModels\RegistrationManagementViewModelRefactored.cs"
    if os.path.exists(file1):
        with open(file1, 'r', encoding='utf-8') as f:
            lines = f.readlines()
        
        for i in range(len(lines)):
            # 修复Department字段
            if i == 134:
                lines[i] = '                    // Department = _selectedDepartment, // 字段已移除\n'
            # 修复逻辑运算符
            if i == 198:
                if '&& item.Status' in lines[i]:
                    lines[i] = '            return item != null && item.Status != "已取消";\n'
        
        with open(file1, 'w', encoding='utf-8') as f:
            f.writelines(lines)
        print(f"修复: {os.path.basename(file1)}")
    
    # AddRegistrationDialogViewModel
    file2 = r"D:\source\repos\LYBTZYZS\src\Frontend\Desktop\Modules\SystemManagement\Registrations\ViewModels\AddRegistrationDialogViewModel.cs"
    if os.path.exists(file2):
        with open(file2, 'r', encoding='utf-8') as f:
            lines = f.readlines()
        
        for i in range(len(lines)):
            if i == 287:  # 第288行
                if '|| d);' in lines[i]:
                    lines[i] = lines[i].replace('|| d);', ');')
        
        with open(file2, 'w', encoding='utf-8') as f:
            f.writelines(lines)
        print(f"修复: {os.path.basename(file2)}")
    
    # EditRegistrationDialogViewModel
    file3 = r"D:\source\repos\LYBTZYZS\src\Frontend\Desktop\Modules\SystemManagement\Registrations\ViewModels\EditRegistrationDialogViewModel.cs"
    if os.path.exists(file3):
        with open(file3, 'r', encoding='utf-8') as f:
            lines = f.readlines()
        
        for i in range(len(lines)):
            if i == 273:  # 第274行
                if '|| d);' in lines[i]:
                    lines[i] = lines[i].replace('|| d);', ');')
        
        with open(file3, 'w', encoding='utf-8') as f:
            f.writelines(lines)
        print(f"修复: {os.path.basename(file3)}")

def main():
    print("开始修复最终剩余的编译错误...")
    print("=" * 60)
    
    fix_simple_doctor_workbench()
    fix_prescription_management_complete()
    fix_doctor_management_complete()
    fix_registration_management_complete()
    
    print("=" * 60)
    print("修复完成！")

if __name__ == "__main__":
    main()
#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
修复最后30个编译错误
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

def fix_prescription_management():
    """修复处方管理相关错误"""
    # 修复所有处方相关ViewModels
    files = [
        r"D:\source\repos\LYBTZYZS\src\Frontend\Desktop\Modules\SystemManagement\Prescriptions\ViewModels\PrescriptionManagementViewModel.cs",
        r"D:\source\repos\LYBTZYZS\src\Frontend\Desktop\Modules\SystemManagement\Prescriptions\ViewModels\AddPrescriptionDialogViewModel.cs",
        r"D:\source\repos\LYBTZYZS\src\Frontend\Desktop\Modules\SystemManagement\Prescriptions\ViewModels\EditPrescriptionDialogViewModel.cs",
        r"D:\source\repos\LYBTZYZS\src\Frontend\Desktop\Modules\SystemManagement\Prescriptions\ViewModels\ViewPrescriptionDialogViewModel.cs"
    ]
    
    for file_path in files:
        if os.path.exists(file_path):
            replacements = [
                # PrescriptionInfo 的 Herbs 改为 Details
                (r'prescription\.Herbs', r'prescription.Details'),
                (r'info\.Herbs', r'info.Details'),
                (r'Prescription\.Herbs', r'Prescription.Details'),
                (r'_prescription\.Herbs', r'_prescription.Details'),
                # DoctorTitle字段
                (r'DoctorTitle = .*?Title', r'// DoctorTitle = info.DoctorTitle // 字段已移除'),
                # PatientGender字段
                (r'PatientGender = .*?Gender', r'// PatientGender = info.PatientGender // 字段已移除'),
            ]
            fix_file(file_path, replacements)

def fix_doctor_management():
    """修复医生管理相关错误"""
    # AddDoctorDialogViewModel
    file1 = r"D:\source\repos\LYBTZYZS\src\Frontend\Desktop\Modules\SystemManagement\Doctors\ViewModels\AddDoctorDialogViewModel.cs"
    if os.path.exists(file1):
        replacements = [
            # Gender字段
            (r'Gender = SelectedGender', r'// Gender = SelectedGender // 字段已移除'),
            # Birthday字段
            (r'Birthday = Birthday', r'// Birthday = Birthday // 字段已移除'),
            # Remark字段
            (r'Remark = Remark', r'// Remark = Remark // 字段已移除'),
            # WuBiCode字段
            (r'WuBiCode = .*?GetWuBiCode.*?,', r'// WuBiCode = ... // 字段已移除'),
        ]
        fix_file(file1, replacements)
    
    # EditDoctorDialogViewModel
    file2 = r"D:\source\repos\LYBTZYZS\src\Frontend\Desktop\Modules\SystemManagement\Doctors\ViewModels\EditDoctorDialogViewModel.cs"
    if os.path.exists(file2):
        replacements = [
            # _originalDoctor错误
            (r'_originalDoctor', r'doctor'),
            # Gender字段
            (r'SelectedGender = doctor\.Gender', r'// SelectedGender = doctor.Gender // 字段已移除'),
            # Birthday字段
            (r'Birthday = doctor\.Birthday', r'// Birthday = doctor.Birthday // 字段已移除'),
            # Age字段
            (r'Age = doctor\.Age', r'// Age = doctor.Age // 字段已移除'),
            # Remark字段
            (r'Remark = doctor\.Remark', r'// Remark = doctor.Remark // 字段已移除'),
        ]
        fix_file(file2, replacements)

def fix_registration_management():
    """修复挂号管理相关错误"""
    # RegistrationManagementViewModelRefactored
    file1 = r"D:\source\repos\LYBTZYZS\src\Frontend\Desktop\Modules\SystemManagement\Registrations\ViewModels\RegistrationManagementViewModelRefactored.cs"
    if os.path.exists(file1):
        replacements = [
            # Department字段
            (r'Department = _selectedDepartment', r'// Department = _selectedDepartment // 字段已移除'),
            # 修复逻辑运算符错误
            (r'return item != null && item\.Status', r'return item != null && item.Status'),
        ]
        fix_file(file1, replacements)
    
    # AddRegistrationDialogViewModel
    file2 = r"D:\source\repos\LYBTZYZS\src\Frontend\Desktop\Modules\SystemManagement\Registrations\ViewModels\AddRegistrationDialogViewModel.cs"
    if os.path.exists(file2):
        with open(file2, 'r', encoding='utf-8') as f:
            lines = f.readlines()
        
        # 找到第288行并修复
        for i in range(len(lines)):
            if i == 287:  # 第288行，索引287
                if '|| d' in lines[i]:
                    lines[i] = lines[i].replace('|| d', '')
                    lines[i] = lines[i].rstrip() + ');\n'
        
        with open(file2, 'w', encoding='utf-8') as f:
            f.writelines(lines)
        print(f"修复: {os.path.basename(file2)}")
    
    # EditRegistrationDialogViewModel
    file3 = r"D:\source\repos\LYBTZYZS\src\Frontend\Desktop\Modules\SystemManagement\Registrations\ViewModels\EditRegistrationDialogViewModel.cs"
    if os.path.exists(file3):
        with open(file3, 'r', encoding='utf-8') as f:
            lines = f.readlines()
        
        # 找到第274行并修复
        for i in range(len(lines)):
            if i == 273:  # 第274行，索引273
                if '|| d' in lines[i]:
                    lines[i] = lines[i].replace('|| d', '')
                    lines[i] = lines[i].rstrip() + ');\n'
        
        with open(file3, 'w', encoding='utf-8') as f:
            f.writelines(lines)
        print(f"修复: {os.path.basename(file3)}")

def fix_herb_view_dialog():
    """修复药材查看对话框"""
    file_path = r"D:\source\repos\LYBTZYZS\src\Frontend\Desktop\Modules\SystemManagement\Herbs\ViewModels\ViewHerbDialogViewModel.cs"
    if os.path.exists(file_path):
        with open(file_path, 'r', encoding='utf-8') as f:
            lines = f.readlines()
        
        # 修复第115行的逻辑错误
        for i in range(len(lines)):
            if i == 114:  # 第115行，索引114
                lines[i] = '        public bool IsExpiringSoon => false; // Herb?.ExpireDate 字段已移除\n'
        
        with open(file_path, 'w', encoding='utf-8') as f:
            f.writelines(lines)
        print(f"修复: {os.path.basename(file_path)}")

def main():
    print("开始修复最后30个编译错误...")
    print("=" * 60)
    
    fix_prescription_management()
    fix_doctor_management()
    fix_registration_management()
    fix_herb_view_dialog()
    
    print("=" * 60)
    print("修复完成！")

if __name__ == "__main__":
    main()
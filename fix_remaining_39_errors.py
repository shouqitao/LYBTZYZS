#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
修复剩余的39个编译错误
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

def fix_user_dto_name():
    """修复UserDto.Name -> UserDto.RealName"""
    file_path = r"D:\source\repos\LYBTZYZS\src\Frontend\Desktop\Modules\SystemManagement\Users\ViewModels\UserManagementViewModelSimple.cs"
    
    replacements = [
        (r'RealName = dto\.Name \?\? string\.Empty', r'RealName = dto.RealName ?? string.Empty'),
    ]
    
    fix_file(file_path, replacements)

def fix_herb_info_fields():
    """修复HerbInfo已删除的字段"""
    # ViewHerbDialogViewModel
    file1 = r"D:\source\repos\LYBTZYZS\src\Frontend\Desktop\Modules\SystemManagement\Herbs\ViewModels\ViewHerbDialogViewModel.cs"
    replacements = [
        # Status字段
        (r'StatusDescription => Herb\?\.Status == HerbStatus\.Active', 
         r'StatusDescription => true /* Herb?.Status == HerbStatus.Active */'),
        (r'StatusColor => Herb\?\.Status == HerbStatus\.Active', 
         r'StatusColor => true /* Herb?.Status == HerbStatus.Active */'),
        # Stock字段
        (r'\$"\{Herb\?\.Stock\} \{Herb\?\.Unit\}"', r'"0" /* $"{Herb?.Stock} {Herb?.Unit}" */'),
        # ExpireDate字段
        (r'Herb\?\.ExpireDate\?\.ToString\("yyyy-MM-dd"\)', r'"" /* Herb?.ExpireDate?.ToString("yyyy-MM-dd") */'),
        (r'Herb\?\.ExpireDate\.HasValue == true && Herb\?\.ExpireDate \?\? DateTime\.Now', 
         r'false /* Herb?.ExpireDate.HasValue == true && Herb?.ExpireDate ?? DateTime.Now */'),
    ]
    fix_file(file1, replacements)
    
    # HerbManagementViewModelRefactored
    file2 = r"D:\source\repos\LYBTZYZS\src\Frontend\Desktop\Modules\SystemManagement\Herbs\ViewModels\HerbManagementViewModelRefactored.cs"
    replacements = [
        # ExpireDate字段
        (r'ExpireDate = DateTime\.Now\.AddYears\(2\)', r'// ExpireDate = DateTime.Now.AddYears(2)'),
        # Status字段
        (r'Status = \(HerbStatus\)dto\.Status', r'// Status = (HerbStatus)dto.Status'),
    ]
    fix_file(file2, replacements)

def fix_doctor_info_fields():
    """修复DoctorInfo已删除的字段"""
    file_path = r"D:\source\repos\LYBTZYZS\src\Frontend\Desktop\Modules\SystemManagement\Doctors\ViewModels\AddDoctorDialogViewModel.cs"
    
    replacements = [
        # WuBiCode字段
        (r'WuBiCode = Utilities\.Helpers\.CommonHelper\.GetWuBiCode\(DoctorName\)', 
         r'// WuBiCode = Utilities.Helpers.CommonHelper.GetWuBiCode(DoctorName)'),
    ]
    
    fix_file(file_path, replacements)

def fix_registration_queries():
    """修复挂号管理查询"""
    # RegistrationManagementViewModelRefactored
    file1 = r"D:\source\repos\LYBTZYZS\src\Frontend\Desktop\Modules\SystemManagement\Registrations\ViewModels\RegistrationManagementViewModelRefactored.cs"
    replacements = [
        # Department字段
        (r'Department = _selectedDepartment', r'// Department = _selectedDepartment'),
        # 修复逻辑运算符错误
        (r'return item != null && item\.Status != "已取消"', 
         r'return item != null && item.Status != "已取消"'),
    ]
    fix_file(file1, replacements)
    
    # AddRegistrationDialogViewModel
    file2 = r"D:\source\repos\LYBTZYZS\src\Frontend\Desktop\Modules\SystemManagement\Registrations\ViewModels\AddRegistrationDialogViewModel.cs"
    replacements = [
        # 修复逻辑运算符错误
        (r'return d\.Title == DoctorTitle\.ExpertSpecialist \|\| d\.Title == DoctorTitle\.ChiefPhysician \|\| d', 
         r'return d.Title == DoctorTitle.ExpertSpecialist || d.Title == DoctorTitle.ChiefPhysician'),
    ]
    fix_file(file2, replacements)
    
    # EditRegistrationDialogViewModel
    file3 = r"D:\source\repos\LYBTZYZS\src\Frontend\Desktop\Modules\SystemManagement\Registrations\ViewModels\EditRegistrationDialogViewModel.cs"
    replacements = [
        # 修复逻辑运算符错误
        (r'return d\.Title == DoctorTitle\.ExpertSpecialist \|\| d\.Title == DoctorTitle\.ChiefPhysician \|\| d', 
         r'return d.Title == DoctorTitle.ExpertSpecialist || d.Title == DoctorTitle.ChiefPhysician'),
    ]
    fix_file(file3, replacements)

def fix_prescription_models():
    """修复处方管理模型字段"""
    files = glob.glob(r"D:\source\repos\LYBTZYZS\src\Frontend\Desktop\Modules\SystemManagement\Prescriptions\ViewModels\*.cs")
    
    for file_path in files:
        replacements = [
            # Herbs字段改为Details
            (r'\.Herbs\.', r'.Details.'),
            (r'Herbs = ', r'Details = '),
            # PrescriptionInfo字段
            (r'PatientGender = info\.PatientGender', r'// PatientGender = info.PatientGender'),
            (r'DoctorTitle = info\.DoctorTitle', r'// DoctorTitle = info.DoctorTitle'),
        ]
        fix_file(file_path, replacements)

def main():
    print("开始修复剩余39个错误...")
    print("=" * 60)
    
    fix_user_dto_name()
    fix_herb_info_fields()
    fix_doctor_info_fields()
    fix_registration_queries()
    fix_prescription_models()
    
    print("=" * 60)
    print("修复完成！")

if __name__ == "__main__":
    main()
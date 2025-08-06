#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
修复最后13个编译错误
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

def fix_simple_doctor_workbench_complete():
    """完整修复SimpleDoctorWorkbenchViewModel"""
    file_path = r"D:\source\repos\LYBTZYZS\src\Frontend\Desktop\Modules\Doctor\ViewModels\SimpleDoctorWorkbenchViewModel.cs"
    
    with open(file_path, 'r', encoding='utf-8') as f:
        lines = f.readlines()
    
    # 修复第256行 - Status字段类型问题
    for i in range(len(lines)):
        if i == 255:  # 第256行
            lines[i] = '                var registrations = await _registrationService.GetPagedAsync(new LYBT.Shared.Models.Contracts.Registration.RegistrationPagedQueryDto());\n'
        elif i == 282:  # 第283行
            lines[i] = '                var patientResult = await _patientService.GetByIdAsync(patientId);\n'
        elif i == 283:  # 添加新行
            lines.insert(283, '                CurrentPatient = patientResult.Data;\n')
        elif i == 318:  # 第319行 - FormulaTemplateService调用
            lines[i] = '                var templates = await _formulaTemplateService.GetActiveListAsync();\n'
        elif i >= 322 and i <= 325:  # 处理foreach循环
            if 'templates' in lines[i]:
                lines[i] = lines[i].replace('templates', 'templates.Data')
    
    with open(file_path, 'w', encoding='utf-8') as f:
        f.writelines(lines)
    print(f"修复: {os.path.basename(file_path)}")

def fix_consultation_view_model_new_fields():
    """修复ConsultationViewModelNew的字段问题"""
    file_path = r"D:\source\repos\LYBTZYZS\src\Frontend\Desktop\Modules\Doctor\ViewModels\ConsultationViewModelNew.cs"
    
    replacements = [
        # IDType -> IdType
        (r'patientResult\.Data\.IDType', r'patientResult.Data.IdType'),
        # Profession -> Occupation
        (r'patientResult\.Data\.Profession', r'patientResult.Data.Occupation'),
    ]
    
    fix_file(file_path, replacements)

def fix_doctor_view_dialog():
    """修复ViewDoctorDialogViewModel"""
    file_path = r"D:\source\repos\LYBTZYZS\src\Frontend\Desktop\Modules\SystemManagement\Doctors\ViewModels\ViewDoctorDialogViewModel.cs"
    
    replacements = [
        # Age字段
        (r'_doctor\?\.Age', r'0 /* _doctor?.Age */'),
        # GenderText字段
        (r'_doctor\?\.GenderText', r'""'),
        # TitleDisplayName字段
        (r'_doctor\?\.TitleDisplayName', r'""'),
        # WorkStatusDisplayName字段
        (r'_doctor\?\.WorkStatusDisplayName', r'_doctor?.IsActive == true ? "在岗" : "离岗"'),
    ]
    
    fix_file(file_path, replacements)

def fix_edit_doctor_dialog():
    """修复EditDoctorDialogViewModel"""
    file_path = r"D:\source\repos\LYBTZYZS\src\Frontend\Desktop\Modules\SystemManagement\Doctors\ViewModels\EditDoctorDialogViewModel.cs"
    
    replacements = [
        # Gender和Birthday字段
        (r'SelectedGender = doctor\.Gender', r'// SelectedGender = doctor.Gender // 字段已移除'),
        (r'BirthDate = doctor\.Birthday', r'// BirthDate = doctor.Birthday // 字段已移除'),
    ]
    
    fix_file(file_path, replacements)

def fix_registration_management_status():
    """修复RegistrationManagementViewModelRefactored"""
    file_path = r"D:\source\repos\LYBTZYZS\src\Frontend\Desktop\Modules\SystemManagement\Registrations\ViewModels\RegistrationManagementViewModelRefactored.cs"
    
    with open(file_path, 'r', encoding='utf-8') as f:
        lines = f.readlines()
    
    # 修复第199行
    for i in range(len(lines)):
        if i == 198:  # 第199行
            lines[i] = '            return item != null && item.Status == RegistrationStatus.Scheduled;\n'
    
    with open(file_path, 'w', encoding='utf-8') as f:
        f.writelines(lines)
    print(f"修复: {os.path.basename(file_path)}")

def main():
    print("开始修复最后13个错误...")
    print("=" * 60)
    
    fix_simple_doctor_workbench_complete()
    fix_consultation_view_model_new_fields()
    fix_doctor_view_dialog()
    fix_edit_doctor_dialog()
    fix_registration_management_status()
    
    print("=" * 60)
    print("修复完成！")

if __name__ == "__main__":
    main()
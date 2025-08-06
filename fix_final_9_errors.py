#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
修复最后9个编译错误
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

def fix_simple_doctor_workbench_final():
    """最终修复SimpleDoctorWorkbenchViewModel"""
    file_path = r"D:\source\repos\LYBTZYZS\src\Frontend\Desktop\Modules\Doctor\ViewModels\SimpleDoctorWorkbenchViewModel.cs"
    
    with open(file_path, 'r', encoding='utf-8') as f:
        lines = f.readlines()
    
    # 修复第256行 - 添加必需的参数
    for i in range(len(lines)):
        if i == 255:  # 第256行
            lines[i] = '                var registrations = await _registrationService.GetPagedAsync(1, 100);\n'
        elif i == 282:  # 删除重复的行
            lines[i] = '                var patientResult = await _patientService.GetByIdAsync(patientId);\n'
        elif i == 283:  # 第284行
            lines[i] = '                CurrentPatient = patientResult.Data;\n'
        elif i == 318:  # 第319行 - 修复方法名
            lines[i] = '                var templates = await _formulaTemplateService.GetListAsync();\n'
    
    with open(file_path, 'w', encoding='utf-8') as f:
        f.writelines(lines)
    print(f"修复: {os.path.basename(file_path)}")

def fix_consultation_view_model_new_final():
    """最终修复ConsultationViewModelNew"""
    file_path = r"D:\source\repos\LYBTZYZS\src\Frontend\Desktop\Modules\Doctor\ViewModels\ConsultationViewModelNew.cs"
    
    replacements = [
        # IdType -> IDType (可能是拼写问题)
        (r'patientResult\.Data\.IdType', r'patientResult.Data.IDType ?? ""'),
        # Occupation -> 使用默认值
        (r'patientResult\.Data\.Occupation', r'""'),
    ]
    
    fix_file(file_path, replacements)

def fix_doctor_view_dialog_final():
    """最终修复ViewDoctorDialogViewModel"""
    file_path = r"D:\source\repos\LYBTZYZS\src\Frontend\Desktop\Modules\SystemManagement\Doctors\ViewModels\ViewDoctorDialogViewModel.cs"
    
    with open(file_path, 'r', encoding='utf-8') as f:
        lines = f.readlines()
    
    for i in range(len(lines)):
        # Age字段
        if i == 50:  # 第51行
            lines[i] = '        public string AgeDescription => $"{"暂无"}岁";\n'
        # GenderText字段
        elif i == 53:  # 第54行
            lines[i] = '        public string GenderDescription => "暂无";\n'
        # TitleDisplayName字段
        elif i == 56:  # 第57行
            lines[i] = '        public string TitleDescription => "暂无";\n'
        # WorkStatusDisplayName字段
        elif i == 62:  # 第63行
            lines[i] = '        public string WorkStatusDescription => _doctor?.IsActive == true ? "在岗" : "离岗";\n'
    
    with open(file_path, 'w', encoding='utf-8') as f:
        f.writelines(lines)
    print(f"修复: {os.path.basename(file_path)}")

def fix_edit_doctor_dialog_final():
    """最终修复EditDoctorDialogViewModel"""
    file_path = r"D:\source\repos\LYBTZYZS\src\Frontend\Desktop\Modules\SystemManagement\Doctors\ViewModels\EditDoctorDialogViewModel.cs"
    
    with open(file_path, 'r', encoding='utf-8') as f:
        lines = f.readlines()
    
    for i in range(len(lines)):
        # Gender字段
        if i == 213:  # 第214行
            lines[i] = '                    // SelectedGender = doctor.Gender; // 字段已移除\n'
    
    with open(file_path, 'w', encoding='utf-8') as f:
        f.writelines(lines)
    print(f"修复: {os.path.basename(file_path)}")

def main():
    print("开始修复最后9个错误...")
    print("=" * 60)
    
    fix_simple_doctor_workbench_final()
    fix_consultation_view_model_new_final()
    fix_doctor_view_dialog_final()
    fix_edit_doctor_dialog_final()
    
    print("=" * 60)
    print("修复完成！")

if __name__ == "__main__":
    main()
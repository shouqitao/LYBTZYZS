#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
最终修复所有剩余的编译错误
"""

import os
import re

def fix_file_at_line(file_path, line_num, fix_func):
    """修复文件特定行"""
    if not os.path.exists(file_path):
        return False
    
    with open(file_path, 'r', encoding='utf-8') as f:
        lines = f.readlines()
    
    if line_num <= len(lines):
        lines[line_num-1] = fix_func(lines[line_num-1])
        
        with open(file_path, 'w', encoding='utf-8') as f:
            f.writelines(lines)
        return True
    return False

def add_semicolon(line):
    """在行尾添加分号"""
    if not line.rstrip().endswith(';'):
        return line.rstrip() + ';\n'
    return line

def fix_consultation_viewmodel():
    """修复ConsultationViewModelNew第622行"""
    file_path = r"D:\source\repos\LYBTZYZS\src\Frontend\Desktop\Modules\Doctor\ViewModels\ConsultationViewModelNew.cs"
    
    with open(file_path, 'r', encoding='utf-8') as f:
        lines = f.readlines()
    
    # 修复第622行的string.Format问题
    if len(lines) > 621:
        lines[621] = re.sub(r'string\.Format\("处方打印失败：{ex\.Message\)"\)', 
                            r'string.Format("处方打印失败：{0}", ex.Message)', 
                            lines[621])
    
    with open(file_path, 'w', encoding='utf-8') as f:
        f.writelines(lines)
    
    print("修复了ConsultationViewModelNew")

def fix_add_doctor_dialog():
    """修复AddDoctorDialogViewModel第359行"""
    file_path = r"D:\source\repos\LYBTZYZS\src\Frontend\Desktop\Modules\SystemManagement\Doctors\ViewModels\AddDoctorDialogViewModel.cs"
    
    with open(file_path, 'r', encoding='utf-8') as f:
        lines = f.readlines()
    
    # 修复第359行的字符串常量问题
    if len(lines) > 358:
        lines[358] = re.sub(r'"医生信息保存成功！\)', r'"医生信息保存成功！"', lines[358])
        if not lines[358].rstrip().endswith(';'):
            lines[358] = lines[358].rstrip() + ';\n'
    
    with open(file_path, 'w', encoding='utf-8') as f:
        f.writelines(lines)
    
    print("修复了AddDoctorDialogViewModel")

def fix_patient_management():
    """修复PatientManagementViewModelRefactored"""
    file_path = r"D:\source\repos\LYBTZYZS\src\Frontend\Desktop\Modules\SystemManagement\Patients\ViewModels\PatientManagementViewModelRefactored.cs"
    
    # 修复第105和147行
    fix_file_at_line(file_path, 105, add_semicolon)
    fix_file_at_line(file_path, 147, add_semicolon)
    
    print("修复了PatientManagementViewModelRefactored")

def fix_user_management():
    """修复UserManagementViewModelSimple"""
    file_path = r"D:\source\repos\LYBTZYZS\src\Frontend\Desktop\Modules\SystemManagement\Users\ViewModels\UserManagementViewModelSimple.cs"
    
    # 修复第133和169行
    fix_file_at_line(file_path, 133, add_semicolon)
    fix_file_at_line(file_path, 169, add_semicolon)
    
    print("修复了UserManagementViewModelSimple")

def fix_prescription_management():
    """修复PrescriptionManagementViewModel"""
    file_path = r"D:\source\repos\LYBTZYZS\src\Frontend\Desktop\Modules\SystemManagement\Prescriptions\ViewModels\PrescriptionManagementViewModel.cs"
    
    with open(file_path, 'r', encoding='utf-8') as f:
        lines = f.readlines()
    
    # 修复第280、282、350、385行
    if len(lines) > 279:
        lines[279] = add_semicolon(lines[279])
    
    if len(lines) > 281:
        # 修复第282行的注释语法错误
        lines[281] = re.sub(r'/\*([^*]+)\*/', r'// \1', lines[281])
    
    if len(lines) > 349:
        lines[349] = add_semicolon(lines[349])
    
    if len(lines) > 384:
        lines[384] = add_semicolon(lines[384])
    
    with open(file_path, 'w', encoding='utf-8') as f:
        f.writelines(lines)
    
    print("修复了PrescriptionManagementViewModel")

def fix_view_prescription_dialog():
    """修复ViewPrescriptionDialogViewModel"""
    file_path = r"D:\source\repos\LYBTZYZS\src\Frontend\Desktop\Modules\SystemManagement\Prescriptions\ViewModels\ViewPrescriptionDialogViewModel.cs"
    
    with open(file_path, 'r', encoding='utf-8') as f:
        content = f.read()
    
    # 修复Status赋值语法错误
    content = re.sub(r'Status/\*\s*\.Status\s*=\s*\*/\s*=', r'Status =', content)
    
    with open(file_path, 'w', encoding='utf-8') as f:
        f.write(content)
    
    print("修复了ViewPrescriptionDialogViewModel")

def main():
    print("开始最终修复...")
    print("=" * 60)
    
    fix_consultation_viewmodel()
    fix_add_doctor_dialog()
    fix_patient_management()
    fix_user_management()
    fix_prescription_management()
    fix_view_prescription_dialog()
    
    print("=" * 60)
    print("最终修复完成！")

if __name__ == "__main__":
    main()
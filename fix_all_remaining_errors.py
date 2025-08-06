#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
修复所有剩余的编译错误
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
        for pattern, replacement in replacements:
            content = re.sub(pattern, replacement, content, flags=re.MULTILINE | re.DOTALL)
        
        if content != original_content:
            with open(file_path, 'w', encoding='utf-8') as f:
                f.write(content)
            print(f"[OK] 修复: {os.path.basename(file_path)}")
            return True
        return False
    except Exception as e:
        print(f"[ERROR] 修复 {file_path} 失败: {e}")
        return False

def fix_userDto_name():
    """修复UserDto.Name -> UserDto.RealName"""
    files = [
        r"D:\source\repos\LYBTZYZS\src\Frontend\Desktop\Modules\Common\ViewModels\ControlShowcaseViewModel.cs",
        r"D:\source\repos\LYBTZYZS\src\Frontend\Desktop\Modules\Examples\Controls\ViewModels\ControlExamplesViewModel.cs"
    ]
    
    replacements = [
        (r'(\w+Dto\s*\{[^}]*?)Name\s*=\s*"([^"]+)"', r'\1RealName = "\2"'),
        (r'\.Name\s*=\s*"([^"]+)"', r'.RealName = "\1"'),
    ]
    
    count = 0
    for file_path in files:
        if os.path.exists(file_path) and fix_file(file_path, replacements):
            count += 1
    
    print(f"修复了 {count} 个UserDto.Name引用")
    return count

def fix_backup_viewmodel():
    """修复BackupViewModel中的语法错误"""
    file_path = r"D:\source\repos\LYBTZYZS\src\Frontend\Desktop\Modules\SystemManagement\Backup\ViewModels\BackupViewModel.cs"
    
    if not os.path.exists(file_path):
        return 0
    
    replacements = [
        # 修复错误的Status比较语法
        (r'b/\*\s*\.Status\s*=\s*\*/\s*=\s*value', r'b.Status == value'),
        (r'b/\*\s*\.Status\s*=\s*\*/=\s*value', r'b.Status == value'),
    ]
    
    if fix_file(file_path, replacements):
        print("修复了BackupViewModel语法错误")
        return 1
    return 0

def fix_herb_management():
    """修复HerbManagementViewModelRefactored语法错误"""
    file_path = r"D:\source\repos\LYBTZYZS\src\Frontend\Desktop\Modules\SystemManagement\Herbs\ViewModels\HerbManagementViewModelRefactored.cs"
    
    if not os.path.exists(file_path):
        return 0
    
    # 读取文件查看具体错误
    with open(file_path, 'r', encoding='utf-8') as f:
        lines = f.readlines()
    
    # 检查330-340行附近的内容
    if len(lines) > 330:
        # 修复可能的语法错误
        for i in range(min(330, len(lines)), min(340, len(lines))):
            # 修复可能的注释错误
            lines[i] = re.sub(r'/\*\s*\.Stock\s*=\s*\*/', '/* .Stock = */', lines[i])
            lines[i] = re.sub(r'/\*\s*\.BatchNo\s*=\s*\*/', '/* .BatchNo = */', lines[i])
            
    # 写回文件
    with open(file_path, 'w', encoding='utf-8') as f:
        f.writelines(lines)
    
    print("尝试修复HerbManagementViewModelRefactored")
    return 1

def fix_consultation_viewmodel():
    """修复ConsultationViewModelNew语法错误"""
    file_path = r"D:\source\repos\LYBTZYZS\src\Frontend\Desktop\Modules\Doctor\ViewModels\ConsultationViewModelNew.cs"
    
    if not os.path.exists(file_path):
        return 0
    
    replacements = [
        # 修复第622行的string.Format错误
        (r'string\.Format\("处方打印失败：{ex\.Message\)"\)', r'string.Format("处方打印失败：{0}", ex.Message)'),
    ]
    
    if fix_file(file_path, replacements):
        print("修复了ConsultationViewModelNew语法错误")
        return 1
    return 0

def fix_view_dialogs():
    """修复所有ViewDialog中的Status赋值错误"""
    files = glob.glob(r"D:\source\repos\LYBTZYZS\src\Frontend\Desktop\Modules\SystemManagement\**\*ViewModels\*.cs", recursive=True)
    
    replacements = [
        # 修复错误的Status赋值语法
        (r'ExpireDate/\*\s*\.Status\s*=\s*\*/\s*=\s*DateTime\.Now', r'ExpireDate = DateTime.Now /* .Status = DateTime.Now */'),
        (r'Status/\*\s*\.Status\s*=\s*\*/\s*=\s*"([^"]+)"', r'Status = "\1" /* .Status = "\1" */'),
        (r'(\w+)/\*\s*\.Status\s*=\s*\*/\s*=\s*([^,;]+)', r'\1 = \2 /* .Status = \2 */'),
    ]
    
    count = 0
    for file_path in files:
        if 'bin' not in file_path and 'obj' not in file_path:
            if fix_file(file_path, replacements):
                count += 1
    
    print(f"修复了 {count} 个ViewDialog文件")
    return count

def fix_missing_semicolons():
    """修复缺少分号的错误"""
    files = [
        r"D:\source\repos\LYBTZYZS\src\Frontend\Desktop\Modules\SystemManagement\Patients\ViewModels\PatientManagementViewModelRefactored.cs",
        r"D:\source\repos\LYBTZYZS\src\Frontend\Desktop\Modules\SystemManagement\Prescriptions\ViewModels\PrescriptionManagementViewModel.cs",
        r"D:\source\repos\LYBTZYZS\src\Frontend\Desktop\Modules\SystemManagement\Users\ViewModels\UserManagementViewModelSimple.cs",
    ]
    
    replacements = [
        (r'await _dialogService\.ShowInformationAsync\("操作成功！", "提示"\)(?!;)', 
         r'await _dialogService.ShowInformationAsync("操作成功！", "提示");'),
    ]
    
    count = 0
    for file_path in files:
        if os.path.exists(file_path) and fix_file(file_path, replacements):
            count += 1
    
    print(f"修复了 {count} 个缺少分号的文件")
    return count

def fix_addDoctor_dialog():
    """修复AddDoctorDialogViewModel字符串常量错误"""
    file_path = r"D:\source\repos\LYBTZYZS\src\Frontend\Desktop\Modules\SystemManagement\Doctors\ViewModels\AddDoctorDialogViewModel.cs"
    
    if not os.path.exists(file_path):
        return 0
    
    replacements = [
        (r'"医生信息保存成功！\)', r'"医生信息保存成功！"'),
    ]
    
    if fix_file(file_path, replacements):
        print("修复了AddDoctorDialogViewModel字符串常量")
        return 1
    return 0

def main():
    print("开始修复所有剩余的编译错误...")
    print("=" * 60)
    
    total_fixed = 0
    
    # 执行所有修复
    total_fixed += fix_userDto_name()
    total_fixed += fix_backup_viewmodel()
    total_fixed += fix_consultation_viewmodel()
    total_fixed += fix_view_dialogs()
    total_fixed += fix_missing_semicolons()
    total_fixed += fix_addDoctor_dialog()
    total_fixed += fix_herb_management()
    
    print("=" * 60)
    print(f"总共修复了 {total_fixed} 个文件")
    print("批量修复完成！")

if __name__ == "__main__":
    main()
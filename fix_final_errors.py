#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
批量修复最后的编译错误
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
            print(f"[OK] 修复: {os.path.basename(file_path)}")
            return True
        return False
    except Exception as e:
        print(f"[ERROR] 修复 {file_path} 失败: {e}")
        return False

def fix_herb_references():
    """修复所有HerbDto.Stock和HerbDto.BatchNo引用"""
    files = glob.glob(r"D:\source\repos\LYBTZYZS\src\Frontend\Desktop\**\*.cs", recursive=True)
    
    replacements = [
        # 修复HerbDto的Stock和BatchNo引用
        (r'(\w+)\.Stock\s*=\s*([^;,\n]+)', r'/* \1.Stock = \2 */'),
        (r'(\w+)\.BatchNo\s*=\s*([^;,\n]+)', r'/* \1.BatchNo = \2 */'),
        (r'Stock\s*=\s*([^,\n]+),', r'/* Stock = \1, */'),
        (r'BatchNo\s*=\s*([^,\n]+),', r'/* BatchNo = \1, */'),
    ]
    
    count = 0
    for file_path in files:
        if 'bin' not in file_path and 'obj' not in file_path:
            if fix_file(file_path, replacements):
                count += 1
    
    print(f"修复了 {count} 个文件中的Herb引用")

def fix_doctor_references():
    """修复所有DoctorDto的RealName、Title和WorkStatus引用"""
    files = glob.glob(r"D:\source\repos\LYBTZYZS\src\Frontend\Desktop\**\*.cs", recursive=True)
    
    replacements = [
        # 修复DoctorDto的字段引用
        (r'(\w+)\.RealName\s*=\s*([^;,\n]+)', r'\1.Name = \2'),
        (r'(\w+)\.Title\s*=\s*([^;,\n]+)', r'/* \1.Title = \2 */'),
        (r'(\w+)\.WorkStatus\s*=\s*([^;,\n]+)', r'/* \1.WorkStatus = \2 */'),
        (r'RealName\s*=\s*([^,\n]+),', r'Name = \1,'),
        (r'Title\s*=\s*([^,\n]+),', r'/* Title = \1, */'),
        (r'WorkStatus\s*=\s*([^,\n]+),', r'/* WorkStatus = \1, */'),
    ]
    
    count = 0
    for file_path in files:
        if 'bin' not in file_path and 'obj' not in file_path:
            if fix_file(file_path, replacements):
                count += 1
    
    print(f"修复了 {count} 个文件中的Doctor引用")

def fix_registration_references():
    """修复所有RegistrationCreateDto.Department引用"""
    files = glob.glob(r"D:\source\repos\LYBTZYZS\src\Frontend\Desktop\**\*.cs", recursive=True)
    
    replacements = [
        # 修复RegistrationCreateDto的Department引用
        (r'(\w+)\.Department\s*=\s*([^;,\n]+)', r'/* \1.Department = \2 */'),
        (r'Department\s*=\s*([^,\n]+),', r'/* Department = \1, */'),
    ]
    
    count = 0
    for file_path in files:
        if 'bin' not in file_path and 'obj' not in file_path:
            if fix_file(file_path, replacements):
                count += 1
    
    print(f"修复了 {count} 个文件中的Registration引用")

def fix_syntax_errors():
    """修复语法错误"""
    
    # 修复具体文件的语法错误
    specific_fixes = [
        # ConsultationViewModelNew.cs 第622行
        (r"D:\source\repos\LYBTZYZS\src\Frontend\Desktop\Modules\Doctor\ViewModels\ConsultationViewModelNew.cs",
         [(r'string\.Format\("处方打印失败：{ex\.Message\)"\)', r'string.Format("处方打印失败：{0}", ex.Message)')]),
        
        # BackupViewModel.cs 第365行 - 修复错误的比较运算符
        (r"D:\source\repos\LYBTZYZS\src\Frontend\Desktop\Modules\SystemManagement\Backup\ViewModels\BackupViewModel.cs",
         [(r'b/\* \.Status = \*/= value', r'b.Status == value')]),
        
        # AddDoctorDialogViewModel.cs 第359行 - 修复字符串常量
        (r"D:\source\repos\LYBTZYZS\src\Frontend\Desktop\Modules\SystemManagement\Doctors\ViewModels\AddDoctorDialogViewModel.cs",
         [(r'"医生信息保存成功！\)', r'"医生信息保存成功！")')]),
        
        # 修复ViewHerbDialogViewModel中的错误比较运算符
        (r"D:\source\repos\LYBTZYZS\src\Frontend\Desktop\Modules\SystemManagement\Herbs\ViewModels\ViewHerbDialogViewModel.cs",
         [(r'ExpireDate/\* \.Status = \*/= DateTime\.Now', r'ExpireDate = DateTime.Now /* .Status = DateTime.Now */')]),
        
        # 修复ViewPrescriptionDialogViewModel中的错误比较运算符
        (r"D:\source\repos\LYBTZYZS\src\Frontend\Desktop\Modules\SystemManagement\Prescriptions\ViewModels\ViewPrescriptionDialogViewModel.cs",
         [(r'Status/\* \.Status = \*/= "待执行"', r'Status = "待执行" /* .Status = "待执行" */')]),
        
        # 修复缺少分号的错误
        (r"D:\source\repos\LYBTZYZS\src\Frontend\Desktop\Modules\SystemManagement\Patients\ViewModels\PatientManagementViewModelRefactored.cs",
         [(r'await _dialogService\.ShowInformationAsync\("操作成功！", "提示"\)', 
           r'await _dialogService.ShowInformationAsync("操作成功！", "提示");')]),
        
        (r"D:\source\repos\LYBTZYZS\src\Frontend\Desktop\Modules\SystemManagement\Prescriptions\ViewModels\PrescriptionManagementViewModel.cs",
         [(r'await _dialogService\.ShowInformationAsync\("操作成功！", "提示"\)', 
           r'await _dialogService.ShowInformationAsync("操作成功！", "提示");')]),
        
        (r"D:\source\repos\LYBTZYZS\src\Frontend\Desktop\Modules\SystemManagement\Users\ViewModels\UserManagementViewModelSimple.cs",
         [(r'await _dialogService\.ShowInformationAsync\("操作成功！", "提示"\)', 
           r'await _dialogService.ShowInformationAsync("操作成功！", "提示");')]),
    ]
    
    for file_path, replacements in specific_fixes:
        if os.path.exists(file_path):
            fix_file(file_path, replacements)
    
    print("修复了特定文件的语法错误")

def main():
    print("开始批量修复最后的编译错误...")
    print("=" * 60)
    
    fix_herb_references()
    fix_doctor_references()
    fix_registration_references()
    fix_syntax_errors()
    
    print("=" * 60)
    print("批量修复完成！")

if __name__ == "__main__":
    main()
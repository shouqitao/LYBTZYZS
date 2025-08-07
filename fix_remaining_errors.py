#!/usr/bin/env python3
"""
修复剩余的编译错误
"""

import os

def fix_files():
    """修复剩余的编译错误"""
    
    fixes = [
        # 修复ConsultationManagementViewModel.cs
        {
            'file': r"D:\source\repos\LYBTZYZS\src\Frontend\Desktop\Modules\SystemManagement\Consultations\ViewModels\ConsultationManagementViewModel.cs",
            'old': 'ConsultationDate =',
            'new': 'ConsultationTime ='
        },
        # 修复UserManagementViewModelSimple.cs - Email
        {
            'file': r"D:\source\repos\LYBTZYZS\src\Frontend\Desktop\Modules\SystemManagement\Users\ViewModels\UserManagementViewModelSimple.cs",
            'old': 'Email = dto.Email',
            'new': '// Email字段已按优化标准移除'
        },
        # 修复UserManagementViewModelSimple.cs - IsActive
        {
            'file': r"D:\source\repos\LYBTZYZS\src\Frontend\Desktop\Modules\SystemManagement\Users\ViewModels\UserManagementViewModelSimple.cs", 
            'old': 'IsActive = dto.IsActive',
            'new': 'Status = dto.Status == CommonStatus.Enabled ? "启用" : "禁用"'
        },
        # 修复AddPatientDialogViewModel.cs
        {
            'file': r"D:\source\repos\LYBTZYZS\src\Frontend\Desktop\Modules\SystemManagement\Patients\ViewModels\AddPatientDialogViewModel.cs",
            'old': 'IsActive = true',
            'new': 'Status = CommonStatus.Enabled'
        },
        # 修复EditFormulaDialogViewModel.cs
        {
            'file': r"D:\source\repos\LYBTZYZS\src\Frontend\Desktop\Modules\SystemManagement\Formulas\ViewModels\EditFormulaDialogViewModel.cs",
            'old': 'TemplateHerbs[i].SortOrder = i;',
            'new': '// SortOrder字段已按优化标准移除'
        }
    ]
    
    fixed_count = 0
    
    for fix in fixes:
        file_path = fix['file']
        if not os.path.exists(file_path):
            print(f"File not found: {file_path}")
            continue
            
        with open(file_path, 'r', encoding='utf-8') as f:
            content = f.read()
        
        original_content = content
        
        # 执行替换
        if fix['old'] in content:
            content = content.replace(fix['old'], fix['new'])
            
            with open(file_path, 'w', encoding='utf-8') as f:
                f.write(content)
            print(f"Fixed: {file_path}")
            fixed_count += 1
        else:
            print(f"Pattern not found in: {file_path}")
            print(f"  Looking for: {fix['old']}")
    
    print(f"\nTotal files fixed: {fixed_count}")

if __name__ == "__main__":
    fix_files()
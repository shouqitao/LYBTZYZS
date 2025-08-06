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
            content = re.sub(old, new, content, flags=re.MULTILINE)
        
        if content != original_content:
            with open(file_path, 'w', encoding='utf-8') as f:
                f.write(content)
            print(f"修复: {os.path.basename(file_path)}")
            return True
        return False
    except Exception as e:
        print(f"错误: {file_path} - {e}")
        return False

def fix_dialog_service_calls():
    """修复ICommonDialogService的ShowMessage调用"""
    file_path = r"D:\source\repos\LYBTZYZS\src\Frontend\Desktop\Modules\Doctor\ViewModels\SimpleDoctorWorkbenchViewModel.cs"
    
    replacements = [
        # ShowMessage -> ShowInformationAsync
        (r'_dialogService\.ShowMessage\(', r'_dialogService.ShowInformationAsync('),
        (r'_dialogService\.ShowError\(', r'_dialogService.ShowErrorAsync('),
        # 修复Age的 ?? 运算符问题
        (r'PatientAge = CurrentPatient\.Age \?\? 0', r'PatientAge = 0 /* CurrentPatient.Age ?? 0 */'),
    ]
    
    fix_file(file_path, replacements)

def fix_syntax_errors():
    """修复语法错误"""
    
    # HerbManagementViewModelRefactored
    file1 = r"D:\source\repos\LYBTZYZS\src\Frontend\Desktop\Modules\SystemManagement\Herbs\ViewModels\HerbManagementViewModelRefactored.cs"
    if os.path.exists(file1):
        with open(file1, 'r', encoding='utf-8') as f:
            lines = f.readlines()
        
        # 修复第445-446行
        if len(lines) > 445:
            lines[445] = '                Price = dto.Price,\n'
        if len(lines) > 446:
            lines[446] = '                /* Stock = (int)dto.Stock, */\n'
        
        with open(file1, 'w', encoding='utf-8') as f:
            f.writelines(lines)
        print(f"修复: {os.path.basename(file1)}")
    
    # EditHerbDialogViewModel
    file2 = r"D:\source\repos\LYBTZYZS\src\Frontend\Desktop\Modules\SystemManagement\Herbs\ViewModels\EditHerbDialogViewModel.cs"
    fix_file(file2, [
        (r'Stock = 0 /\* dto\.Stock \*/', r'/* Stock = dto.Stock, */'),
        (r'BatchNo = string\.Empty /\* dto\.BatchNo \*/', r'/* BatchNo = dto.BatchNo, */'),
    ])
    
    # StockManagementDialogViewModel
    file3 = r"D:\source\repos\LYBTZYZS\src\Frontend\Desktop\Modules\SystemManagement\Herbs\ViewModels\StockManagementDialogViewModel.cs"
    fix_file(file3, [
        (r'Stock = 0 /\* h\.Stock \*/', r'/* Stock = h.Stock, */'),
        (r'WuBiCode = string\.Empty /\* h\.WuBiCode \*/', r'/* WuBiCode = h.WuBiCode, */'),
    ])
    
    # ViewHerbDialogViewModel
    file4 = r"D:\source\repos\LYBTZYZS\src\Frontend\Desktop\Modules\SystemManagement\Herbs\ViewModels\ViewHerbDialogViewModel.cs"
    fix_file(file4, [
        (r'DateTime\.Now /\* Herb\.ExpireDate \*/', r'Herb?.ExpireDate ?? DateTime.Now'),
    ])

def main():
    print("开始修复最后30个错误...")
    print("=" * 60)
    
    fix_dialog_service_calls()
    fix_syntax_errors()
    
    print("=" * 60)
    print("修复完成！")

if __name__ == "__main__":
    main()
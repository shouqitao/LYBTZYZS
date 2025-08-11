#!/usr/bin/env python3
"""
修复特定有编码问题的文件
"""

import os
from pathlib import Path

def fix_file_with_multiple_encodings(file_path, replacements):
    """尝试多种编码读取并修复文件"""
    encodings = ['utf-8-sig', 'utf-8', 'gbk', 'gb2312', 'cp936']
    
    content = None
    used_encoding = None
    
    # 尝试读取文件
    for encoding in encodings:
        try:
            with open(file_path, 'r', encoding=encoding) as f:
                content = f.read()
                used_encoding = encoding
                break
        except UnicodeDecodeError:
            continue
    
    if content is None:
        print(f"  ! 无法读取文件: {file_path}")
        return False
    
    # 应用替换
    original_content = content
    for old_text, new_text in replacements:
        content = content.replace(old_text, new_text)
    
    if content != original_content:
        # 写回文件（使用UTF-8编码）
        try:
            with open(file_path, 'w', encoding='utf-8') as f:
                f.write(content)
            print(f"  > 修复: {file_path} (从 {used_encoding} 转换为 utf-8)")
            return True
        except Exception as e:
            print(f"  ! 写入失败: {file_path} - {e}")
            return False
    
    return False

def main():
    base_path = r"D:\source\repos\LYBTZYZS"
    
    # 需要修复的文件和替换内容
    files_to_fix = [
        (
            r"src\Frontend\Desktop\BusinessModules\Users\Base\BaseServiceManagementViewModel.cs",
            [
                ("using LYBT.WPF.Client.Core.ViewModels;", "using LYBT.Desktop.Core.ViewModels;"),
                ("using LYBT.WPF.Client.Core.Models;", "using LYBT.Desktop.Core.Models;"),
                ("using LYBT.WPF.Client.Core.Models.Common;", "using LYBT.Desktop.Core.Models.Common;"),
            ]
        ),
        (
            r"src\Frontend\Desktop\BusinessModules\Patients\Base\BaseServiceManagementViewModel.cs",
            [
                ("using LYBT.WPF.Client.Core.ViewModels;", "using LYBT.Desktop.Core.ViewModels;"),
                ("using LYBT.WPF.Client.Core.ViewModels.Base;", "using LYBT.Desktop.Core.ViewModels.Base;"),
                ("using LYBT.WPF.Client.Core.Models;", "using LYBT.Desktop.Core.Models;"),
                ("using LYBT.WPF.Client.Core.Models.Common;", "using LYBT.Desktop.Core.Models.Common;"),
            ]
        ),
        (
            r"src\Frontend\Desktop\BusinessModules\Patients\Views\PatientAddEditDialog.xaml.cs",
            [
                ("LYBT.WPF.Client.", "LYBT.Desktop."),
            ]
        ),
        (
            r"src\Frontend\Desktop\BusinessModules\Patients\Views\PatientManagementView.xaml.cs",
            [
                ("LYBT.WPF.Client.", "LYBT.Desktop."),
            ]
        ),
    ]
    
    print("[FIXING] 修复特定文件的命名空间...")
    fixed_count = 0
    
    for relative_path, replacements in files_to_fix:
        file_path = os.path.join(base_path, relative_path)
        if fix_file_with_multiple_encodings(file_path, replacements):
            fixed_count += 1
    
    # 修复其他文件中的LYBT.WPF.Client引用
    desktop_path = Path(base_path) / "src" / "Frontend" / "Desktop"
    
    other_files = [
        "Modules/Authentication/ViewModels/LoginViewModel.cs",
        "Services/CredentialService.cs",
        "Services/ApiTestService.cs",
        "Services/ApiService.cs",
        "Core/Controls/Auth/LoginStatusControl.xaml.cs",
        "Core/Controls/Users/UserListItemControl.xaml.cs",
        "Core/Controls/Users/UserDisplayControl.xaml.cs",
        "Core/Controls/FormulaTemplates/FormulaTemplateListItemControl.xaml.cs",
        "Core/Controls/Authentication/LoginControl.xaml.cs",
        "Core/Controls/Prescriptions/PrescriptionListItemControl.xaml.cs",
    ]
    
    replacements = [
        ("LYBT.WPF.Client.Core", "LYBT.Desktop.Core"),
        ("LYBT.WPF.Client.Services", "LYBT.Desktop.Services"),
        ("LYBT.WPF.Client.Infrastructure", "LYBT.Desktop.Infrastructure"),
        ("LYBT.WPF.Client.Modules", "LYBT.Desktop"),
        ("LYBT.WPF.Client.BusinessModules", "LYBT.Desktop"),
        ("LYBT.WPF.Client.Workbenches", "LYBT.Desktop.Workbench"),
    ]
    
    for file_rel_path in other_files:
        file_path = desktop_path / file_rel_path
        if file_path.exists():
            if fix_file_with_multiple_encodings(str(file_path), replacements):
                fixed_count += 1
    
    print(f"\n[COMPLETE] 修复了 {fixed_count} 个文件")

if __name__ == "__main__":
    main()
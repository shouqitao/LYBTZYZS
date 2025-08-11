#!/usr/bin/env python3
"""
修复XAML文件中的assembly引用
"""

import os
import re
from pathlib import Path

# assembly重命名映射
ASSEMBLY_RENAME_MAP = {
    "LYBT.WPF.Client.Core": "LYBT.Desktop.Core",
    "LYBT.WPF.Client.Shell": "LYBT.Desktop.Shell",
    "LYBT.WPF.Client.Infrastructure": "LYBT.Desktop.Infrastructure",
    "LYBT.WPF.Client.Services": "LYBT.Desktop.Services",
    "LYBT.WPF.Client.Modules.Authentication": "LYBT.Desktop.Auth",
    "LYBT.WPF.Client.Modules.Users": "LYBT.Desktop.Users",
    "LYBT.WPF.Client.Modules.Patients": "LYBT.Desktop.Patients",
    "LYBT.WPF.Client.Modules.Consultation": "LYBT.Desktop.Consultation",
    "LYBT.WPF.Client.Modules.Prescriptions": "LYBT.Desktop.Prescriptions",
    "LYBT.WPF.Client.Modules.Herbs": "LYBT.Desktop.Herbs",
    "LYBT.WPF.Client.Modules.Formula": "LYBT.Desktop.Formula",
    "LYBT.WPF.Client.Modules.MedicalCase": "LYBT.Desktop.MedicalCase",
    "LYBT.WPF.Client.Modules.SystemManagement": "LYBT.Desktop.Admin",
    "LYBT.WPF.Client.BusinessModules.Shared": "LYBT.Desktop.Shared",
    "LYBT.WPF.Client.BusinessModules.Users": "LYBT.Desktop.Users.Shared",
    "LYBT.WPF.Client.BusinessModules.Patients": "LYBT.Desktop.Patients.Shared",
    "LYBT.WPF.Client.BusinessModules.Consultations": "LYBT.Desktop.Consultation.Shared",
    "LYBT.WPF.Client.BusinessModules.Prescriptions": "LYBT.Desktop.Prescriptions.Shared",
    "LYBT.WPF.Client.BusinessModules.Herbs": "LYBT.Desktop.Herbs.Shared",
    "LYBT.WPF.Client.BusinessModules.Formula": "LYBT.Desktop.Formula.Shared",
    "LYBT.WPF.Client.BusinessModules.MedicalCase": "LYBT.Desktop.MedicalCase.Shared",
    "LYBT.WPF.Client.Workbenches.Core": "LYBT.Desktop.Workbench.Core",
    "LYBT.WPF.Client.Workbenches.SystemWorkbench": "LYBT.Desktop.Workbench.Admin",
    "LYBT.WPF.Client.Workbenches.ConsultationWorkbench": "LYBT.Desktop.Workbench.Consultation",
}

def fix_xaml_assemblies(base_path):
    """修复所有XAML文件中的assembly引用"""
    desktop_path = Path(base_path) / "src" / "Frontend" / "Desktop"
    fixed_count = 0
    
    print("[FIXING XAML] 修复XAML文件中的assembly引用...")
    
    for xaml_file in desktop_path.rglob('*.xaml'):
        if 'bin' in xaml_file.parts or 'obj' in xaml_file.parts:
            continue
        
        try:
            content = xaml_file.read_text(encoding='utf-8-sig')
        except UnicodeDecodeError:
            try:
                content = xaml_file.read_text(encoding='gbk')
            except:
                print(f"  ! 跳过编码错误的文件: {xaml_file.name}")
                continue
        
        original_content = content
        
        # 修复assembly引用
        for old_name, new_name in ASSEMBLY_RENAME_MAP.items():
            # 修复 assembly= 引用
            content = re.sub(
                f'assembly={re.escape(old_name)}',
                f'assembly={new_name}',
                content
            )
        
        if content != original_content:
            xaml_file.write_text(content, encoding='utf-8')
            print(f"  > 修复: {xaml_file.relative_to(base_path)}")
            fixed_count += 1
    
    return fixed_count

def main():
    base_path = r"D:\source\repos\LYBTZYZS"
    
    print("WARNING: 将修复所有XAML文件中的assembly引用")
    print(f"   目标路径: {base_path}")
    response = input("\n是否继续？(y/n): ")
    
    if response.lower() != 'y':
        print("操作已取消")
        return
    
    fixed = fix_xaml_assemblies(base_path)
    
    print(f"\n[COMPLETE] 修复了 {fixed} 个XAML文件")
    
    if fixed > 0:
        print("\n[NEXT] 请重新编译项目")

if __name__ == "__main__":
    main()
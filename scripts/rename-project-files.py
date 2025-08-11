#!/usr/bin/env python3
"""
重命名实际的.csproj文件名
"""

import os
import shutil
from pathlib import Path

# 项目文件重命名映射
FILE_RENAME_MAP = {
    # 核心项目
    "LYBT.WPF.Client.Core.csproj": "LYBT.Desktop.Core.csproj",
    "LYBT.WPF.Client.Shell.csproj": "LYBT.Desktop.Shell.csproj",
    "LYBT.WPF.Client.Infrastructure.csproj": "LYBT.Desktop.Infrastructure.csproj",
    "LYBT.WPF.Client.Services.csproj": "LYBT.Desktop.Services.csproj",
    
    # 功能模块 (Modules)
    "LYBT.WPF.Client.Modules.Authentication.csproj": "LYBT.Desktop.Auth.csproj",
    "LYBT.WPF.Client.Modules.Users.csproj": "LYBT.Desktop.Users.csproj",
    "LYBT.WPF.Client.Modules.Patients.csproj": "LYBT.Desktop.Patients.csproj",
    "LYBT.WPF.Client.Modules.Consultation.csproj": "LYBT.Desktop.Consultation.csproj",
    "LYBT.WPF.Client.Modules.Prescriptions.csproj": "LYBT.Desktop.Prescriptions.csproj",
    "LYBT.WPF.Client.Modules.Herbs.csproj": "LYBT.Desktop.Herbs.csproj",
    "LYBT.WPF.Client.Modules.Formula.csproj": "LYBT.Desktop.Formula.csproj",
    "LYBT.WPF.Client.Modules.MedicalCase.csproj": "LYBT.Desktop.MedicalCase.csproj",
    "LYBT.WPF.Client.Modules.SystemManagement.csproj": "LYBT.Desktop.Admin.csproj",
    
    # 共享业务模块 (BusinessModules)
    "LYBT.WPF.Client.BusinessModules.Shared.csproj": "LYBT.Desktop.Shared.csproj",
    "LYBT.WPF.Client.BusinessModules.Users.csproj": "LYBT.Desktop.Users.Shared.csproj",
    "LYBT.WPF.Client.BusinessModules.Patients.csproj": "LYBT.Desktop.Patients.Shared.csproj",
    "LYBT.WPF.Client.BusinessModules.Consultations.csproj": "LYBT.Desktop.Consultation.Shared.csproj",
    "LYBT.WPF.Client.BusinessModules.Prescriptions.csproj": "LYBT.Desktop.Prescriptions.Shared.csproj",
    "LYBT.WPF.Client.BusinessModules.Herbs.csproj": "LYBT.Desktop.Herbs.Shared.csproj",
    "LYBT.WPF.Client.BusinessModules.Formula.csproj": "LYBT.Desktop.Formula.Shared.csproj",
    "LYBT.WPF.Client.BusinessModules.MedicalCase.csproj": "LYBT.Desktop.MedicalCase.Shared.csproj",
    
    # 工作台 (Workbenches)
    "LYBT.WPF.Client.Workbenches.Core.csproj": "LYBT.Desktop.Workbench.Core.csproj",
    "LYBT.WPF.Client.Workbenches.SystemWorkbench.csproj": "LYBT.Desktop.Workbench.Admin.csproj",
    "LYBT.WPF.Client.Workbenches.ConsultationWorkbench.csproj": "LYBT.Desktop.Workbench.Consultation.csproj",
}

def rename_project_files(base_path):
    """重命名所有项目文件"""
    desktop_path = Path(base_path) / "src" / "Frontend" / "Desktop"
    renamed_count = 0
    
    print("[FILE RENAME] 开始重命名项目文件...")
    
    for old_name, new_name in FILE_RENAME_MAP.items():
        # 查找旧文件
        for proj_file in desktop_path.rglob(old_name):
            new_file = proj_file.parent / new_name
            
            if proj_file.exists():
                # 重命名文件
                proj_file.rename(new_file)
                print(f"  > {old_name} -> {new_name}")
                renamed_count += 1
    
    print(f"\n[COMPLETE] 重命名了 {renamed_count} 个项目文件")
    return renamed_count

def main():
    base_path = r"D:\source\repos\LYBTZYZS"
    
    print("WARNING: 将重命名所有.csproj文件")
    print(f"   目标路径: {base_path}")
    response = input("\n是否继续？(y/n): ")
    
    if response.lower() != 'y':
        print("操作已取消")
        return
    
    renamed = rename_project_files(base_path)
    
    if renamed > 0:
        print("\n[NEXT] 请在Visual Studio中重新加载解决方案")

if __name__ == "__main__":
    main()
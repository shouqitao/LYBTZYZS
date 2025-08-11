#!/usr/bin/env python3
"""
WPF项目重命名脚本
将冗长的项目命名简化为更简洁的格式
"""

import os
import re
import xml.etree.ElementTree as ET
from pathlib import Path
import shutil
from typing import Dict, List, Tuple

# 项目重命名映射表
PROJECT_RENAME_MAP = {
    # 核心项目
    "LYBT.WPF.Client.Core": "LYBT.Desktop.Core",
    "LYBT.WPF.Client.Shell": "LYBT.Desktop.Shell",
    "LYBT.WPF.Client.Infrastructure": "LYBT.Desktop.Infrastructure",
    "LYBT.WPF.Client.Services": "LYBT.Desktop.Services",
    
    # 功能模块 (Modules)
    "LYBT.WPF.Client.Modules.Authentication": "LYBT.Desktop.Auth",
    "LYBT.WPF.Client.Modules.Users": "LYBT.Desktop.Users",
    "LYBT.WPF.Client.Modules.Patients": "LYBT.Desktop.Patients",
    "LYBT.WPF.Client.Modules.Consultation": "LYBT.Desktop.Consultation",
    "LYBT.WPF.Client.Modules.Prescriptions": "LYBT.Desktop.Prescriptions",
    "LYBT.WPF.Client.Modules.Herbs": "LYBT.Desktop.Herbs",
    "LYBT.WPF.Client.Modules.Formula": "LYBT.Desktop.Formula",
    "LYBT.WPF.Client.Modules.MedicalCase": "LYBT.Desktop.MedicalCase",
    "LYBT.WPF.Client.Modules.SystemManagement": "LYBT.Desktop.Admin",
    
    # 共享业务模块 (BusinessModules)
    "LYBT.WPF.Client.BusinessModules.Shared": "LYBT.Desktop.Shared",
    "LYBT.WPF.Client.BusinessModules.Users": "LYBT.Desktop.Users.Shared",
    "LYBT.WPF.Client.BusinessModules.Patients": "LYBT.Desktop.Patients.Shared",
    "LYBT.WPF.Client.BusinessModules.Consultations": "LYBT.Desktop.Consultation.Shared",
    "LYBT.WPF.Client.BusinessModules.Prescriptions": "LYBT.Desktop.Prescriptions.Shared",
    "LYBT.WPF.Client.BusinessModules.Herbs": "LYBT.Desktop.Herbs.Shared",
    "LYBT.WPF.Client.BusinessModules.Formula": "LYBT.Desktop.Formula.Shared",
    "LYBT.WPF.Client.BusinessModules.MedicalCase": "LYBT.Desktop.MedicalCase.Shared",
    
    # 工作台 (Workbenches)
    "LYBT.WPF.Client.Workbenches.Core": "LYBT.Desktop.Workbench.Core",
    "LYBT.WPF.Client.Workbenches.SystemWorkbench": "LYBT.Desktop.Workbench.Admin",
    "LYBT.WPF.Client.Workbenches.ConsultationWorkbench": "LYBT.Desktop.Workbench.Consultation",
}

class ProjectRenamer:
    def __init__(self, base_path: str):
        self.base_path = Path(base_path)
        self.desktop_path = self.base_path / "src" / "Frontend" / "Desktop"
        self.backup_path = self.base_path / "backup_before_rename"
        self.changes_log = []
        
    def backup_files(self):
        """备份所有将要修改的文件"""
        print("[BACKUP] 创建备份...")
        if self.backup_path.exists():
            shutil.rmtree(self.backup_path)
        
        # 备份所有项目文件
        for ext in ['.csproj', '.cs', '.xaml', '.xaml.cs']:
            for file in self.desktop_path.rglob(f'*{ext}'):
                if 'bin' not in file.parts and 'obj' not in file.parts:
                    rel_path = file.relative_to(self.base_path)
                    backup_file = self.backup_path / rel_path
                    backup_file.parent.mkdir(parents=True, exist_ok=True)
                    shutil.copy2(file, backup_file)
        
        # 备份解决方案文件
        for sln_file in self.base_path.glob('*.sln'):
            shutil.copy2(sln_file, self.backup_path / sln_file.name)
        
        print(f"[OK] 备份已创建在: {self.backup_path}")
    
    def update_csproj_files(self):
        """更新所有.csproj文件的AssemblyName和RootNamespace"""
        print("\n[UPDATE] 更新项目文件...")
        
        for csproj_file in self.desktop_path.rglob('*.csproj'):
            if 'bin' in csproj_file.parts or 'obj' in csproj_file.parts:
                continue
                
            content = csproj_file.read_text(encoding='utf-8-sig')
            original_content = content
            
            # 查找当前的AssemblyName和RootNamespace
            for old_name, new_name in PROJECT_RENAME_MAP.items():
                # 更新AssemblyName
                content = re.sub(
                    f'<AssemblyName>{re.escape(old_name)}</AssemblyName>',
                    f'<AssemblyName>{new_name}</AssemblyName>',
                    content
                )
                # 更新RootNamespace
                content = re.sub(
                    f'<RootNamespace>{re.escape(old_name)}</RootNamespace>',
                    f'<RootNamespace>{new_name}</RootNamespace>',
                    content
                )
                
            if content != original_content:
                csproj_file.write_text(content, encoding='utf-8')
                self.changes_log.append(f"Updated: {csproj_file.relative_to(self.base_path)}")
                print(f"  > {csproj_file.name}")
    
    def update_project_references(self):
        """更新所有项目引用"""
        print("\n[LINK] 更新项目引用...")
        
        for csproj_file in self.desktop_path.rglob('*.csproj'):
            if 'bin' in csproj_file.parts or 'obj' in csproj_file.parts:
                continue
                
            content = csproj_file.read_text(encoding='utf-8-sig')
            original_content = content
            
            # 更新ProjectReference中的项目名称
            for old_name, new_name in PROJECT_RENAME_MAP.items():
                old_pattern = f'{old_name}.csproj'
                new_pattern = f'{new_name}.csproj'
                content = content.replace(old_pattern, new_pattern)
            
            if content != original_content:
                csproj_file.write_text(content, encoding='utf-8')
                print(f"  > {csproj_file.name}")
    
    def update_cs_files(self):
        """更新所有.cs文件中的命名空间"""
        print("\n[NAMESPACE] 更新C#文件命名空间...")
        
        for cs_file in self.desktop_path.rglob('*.cs'):
            if 'bin' in cs_file.parts or 'obj' in cs_file.parts:
                continue
            
            # 尝试不同的编码
            try:
                content = cs_file.read_text(encoding='utf-8-sig')
            except UnicodeDecodeError:
                try:
                    content = cs_file.read_text(encoding='gbk')
                except UnicodeDecodeError:
                    print(f"  ! 跳过编码错误的文件: {cs_file.name}")
                    continue
            original_content = content
            
            # 更新namespace声明和using语句
            for old_name, new_name in PROJECT_RENAME_MAP.items():
                # 更新namespace
                content = re.sub(
                    f'namespace {re.escape(old_name)}',
                    f'namespace {new_name}',
                    content
                )
                # 更新using语句
                content = re.sub(
                    f'using {re.escape(old_name)}',
                    f'using {new_name}',
                    content
                )
                # 更新完全限定名
                content = content.replace(f'{old_name}.', f'{new_name}.')
                content = content.replace(f'{old_name};', f'{new_name};')
                
            if content != original_content:
                cs_file.write_text(content, encoding='utf-8')
                self.changes_log.append(f"Updated namespace: {cs_file.relative_to(self.base_path)}")
    
    def update_xaml_files(self):
        """更新所有.xaml文件中的命名空间引用"""
        print("\n[XAML] 更新XAML文件...")
        
        for xaml_file in self.desktop_path.rglob('*.xaml'):
            if 'bin' in xaml_file.parts or 'obj' in xaml_file.parts:
                continue
            
            # 尝试不同的编码
            try:
                content = xaml_file.read_text(encoding='utf-8-sig')
            except UnicodeDecodeError:
                try:
                    content = xaml_file.read_text(encoding='gbk')
                except UnicodeDecodeError:
                    print(f"  ! 跳过编码错误的文件: {xaml_file.name}")
                    continue
            original_content = content
            
            # 更新xmlns命名空间声明
            for old_name, new_name in PROJECT_RENAME_MAP.items():
                # 更新clr-namespace
                content = re.sub(
                    f'clr-namespace:{re.escape(old_name)}',
                    f'clr-namespace:{new_name}',
                    content
                )
                # 更新x:Class属性
                content = re.sub(
                    f'x:Class="{re.escape(old_name)}',
                    f'x:Class="{new_name}',
                    content
                )
                
            if content != original_content:
                xaml_file.write_text(content, encoding='utf-8')
                self.changes_log.append(f"Updated XAML: {xaml_file.relative_to(self.base_path)}")
    
    def update_solution_files(self):
        """更新解决方案文件"""
        print("\n[SOLUTION] 更新解决方案文件...")
        
        for sln_file in self.base_path.glob('*.sln'):
            content = sln_file.read_text(encoding='utf-8-sig')
            original_content = content
            
            # 更新项目名称引用
            for old_name, new_name in PROJECT_RENAME_MAP.items():
                content = content.replace(f'"{old_name}"', f'"{new_name}"')
                content = content.replace(f'{old_name}.csproj', f'{new_name}.csproj')
            
            if content != original_content:
                sln_file.write_text(content, encoding='utf-8')
                print(f"  > {sln_file.name}")
    
    def clean_build_folders(self):
        """清理bin和obj文件夹"""
        print("\n[CLEAN] 清理编译文件夹...")
        
        for folder in ['bin', 'obj']:
            for dir_path in self.desktop_path.rglob(folder):
                if dir_path.is_dir():
                    try:
                        shutil.rmtree(dir_path)
                        print(f"  > 删除 {dir_path.relative_to(self.base_path)}")
                    except Exception as e:
                        print(f"  ! 无法删除 {dir_path}: {e}")
    
    def save_changes_log(self):
        """保存更改日志"""
        log_file = self.base_path / "rename_changes.log"
        with open(log_file, 'w', encoding='utf-8') as f:
            f.write("WPF项目重命名更改日志\n")
            f.write("=" * 50 + "\n\n")
            for change in self.changes_log:
                f.write(f"{change}\n")
        print(f"\n[LOG] 更改日志已保存到: {log_file}")
    
    def run(self):
        """执行重命名过程"""
        print("[START] 开始WPF项目重命名过程...")
        print(f"   工作目录: {self.desktop_path}")
        
        # 1. 备份文件
        self.backup_files()
        
        # 2. 更新.csproj文件
        self.update_csproj_files()
        
        # 3. 更新项目引用
        self.update_project_references()
        
        # 4. 更新C#文件
        self.update_cs_files()
        
        # 5. 更新XAML文件
        self.update_xaml_files()
        
        # 6. 更新解决方案文件
        self.update_solution_files()
        
        # 7. 清理编译文件夹
        self.clean_build_folders()
        
        # 8. 保存更改日志
        self.save_changes_log()
        
        print("\n[COMPLETE] WPF项目重命名完成！")
        print(f"   共更改 {len(self.changes_log)} 个文件")
        print("\n[NEXT] 下一步:")
        print("   1. 在Visual Studio中重新加载解决方案")
        print("   2. 执行清理和重建")
        print("   3. 运行测试验证功能")
        print("\n[INFO] 如需恢复，请使用备份文件夹:", self.backup_path)

def main():
    """主函数"""
    base_path = r"D:\source\repos\LYBTZYZS"
    
    # 确认执行
    print("WARNING: 此操作将重命名所有WPF项目")
    print(f"   目标路径: {base_path}")
    response = input("\n是否继续？(y/n): ")
    
    if response.lower() != 'y':
        print("操作已取消")
        return
    
    # 执行重命名
    renamer = ProjectRenamer(base_path)
    renamer.run()

if __name__ == "__main__":
    main()
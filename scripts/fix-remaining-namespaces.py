#!/usr/bin/env python3
"""
修复剩余的命名空间引用问题
"""

import os
import re
from pathlib import Path

def fix_remaining_namespaces(base_path):
    """修复所有.cs文件中的剩余命名空间引用"""
    desktop_path = Path(base_path) / "src" / "Frontend" / "Desktop"
    fixed_count = 0
    
    print("[FIXING] 修复剩余的命名空间引用...")
    
    # 需要修复的映射
    namespace_fixes = [
        # 修复对旧命名空间的引用
        ("using LYBT.WPF.Client.Core", "using LYBT.Desktop.Core"),
        ("using LYBT.WPF.Client.Services", "using LYBT.Desktop.Services"),
        ("using LYBT.WPF.Client.Infrastructure", "using LYBT.Desktop.Infrastructure"),
        ("using LYBT.WPF.Client.Modules", "using LYBT.Desktop"),
        ("using LYBT.WPF.Client.BusinessModules", "using LYBT.Desktop"),
        ("using LYBT.WPF.Client.Workbenches", "using LYBT.Desktop.Workbench"),
        
        # 修复完全限定名
        ("LYBT.WPF.Client.Core.", "LYBT.Desktop.Core."),
        ("LYBT.WPF.Client.Services.", "LYBT.Desktop.Services."),
        ("LYBT.WPF.Client.Infrastructure.", "LYBT.Desktop.Infrastructure."),
        ("LYBT.WPF.Client.Modules.", "LYBT.Desktop."),
        ("LYBT.WPF.Client.BusinessModules.", "LYBT.Desktop."),
        ("LYBT.WPF.Client.Workbenches.", "LYBT.Desktop.Workbench."),
        
        # 修复子命名空间问题
        ("LYBT.Desktop.Users.Shared.Base", "LYBT.Desktop.Users.Shared.Base"),
        ("LYBT.Desktop.Patients.Shared.Base", "LYBT.Desktop.Patients.Shared.Base"),
        
        # 修复Shared.Models问题
        ("LYBT.Desktop.Shared.Models", "LYBT.Shared.Models"),
    ]
    
    for cs_file in desktop_path.rglob('*.cs'):
        if 'bin' in cs_file.parts or 'obj' in cs_file.parts:
            continue
        
        try:
            content = cs_file.read_text(encoding='utf-8-sig')
        except UnicodeDecodeError:
            try:
                content = cs_file.read_text(encoding='gbk')
            except:
                print(f"  ! 跳过编码错误的文件: {cs_file.name}")
                continue
        
        original_content = content
        
        # 应用所有修复
        for old_ns, new_ns in namespace_fixes:
            content = content.replace(old_ns, new_ns)
        
        if content != original_content:
            cs_file.write_text(content, encoding='utf-8')
            print(f"  > 修复: {cs_file.relative_to(base_path)}")
            fixed_count += 1
    
    return fixed_count

def fix_base_namespace_declarations(base_path):
    """修复Base文件夹中的命名空间声明"""
    desktop_path = Path(base_path) / "src" / "Frontend" / "Desktop"
    fixed_count = 0
    
    print("\n[FIXING BASE] 修复Base文件夹的命名空间声明...")
    
    # 查找所有Base文件夹中的文件
    for base_file in desktop_path.rglob('**/Base/*.cs'):
        if 'bin' in base_file.parts or 'obj' in base_file.parts:
            continue
        
        try:
            content = base_file.read_text(encoding='utf-8-sig')
        except UnicodeDecodeError:
            try:
                content = base_file.read_text(encoding='gbk')
            except:
                print(f"  ! 跳过编码错误的文件: {base_file.name}")
                continue
        
        original_content = content
        
        # 获取父文件夹名称来确定正确的命名空间
        parent_folder = base_file.parent.parent.name
        
        # 确定新的命名空间
        if parent_folder == "Users":
            if "BusinessModules" in str(base_file):
                new_namespace = "LYBT.Desktop.Users.Shared.Base"
            else:
                new_namespace = "LYBT.Desktop.Users.Base"
        elif parent_folder == "Patients":
            if "BusinessModules" in str(base_file):
                new_namespace = "LYBT.Desktop.Patients.Shared.Base"
            else:
                new_namespace = "LYBT.Desktop.Patients.Base"
        else:
            continue
        
        # 替换namespace声明
        content = re.sub(
            r'namespace\s+[\w.]+\.Base',
            f'namespace {new_namespace}',
            content
        )
        
        if content != original_content:
            base_file.write_text(content, encoding='utf-8')
            print(f"  > 修复Base命名空间: {base_file.name} -> {new_namespace}")
            fixed_count += 1
    
    return fixed_count

def main():
    base_path = r"D:\source\repos\LYBTZYZS"
    
    print("WARNING: 将修复剩余的命名空间引用问题")
    print(f"   目标路径: {base_path}")
    response = input("\n是否继续？(y/n): ")
    
    if response.lower() != 'y':
        print("操作已取消")
        return
    
    fixed1 = fix_remaining_namespaces(base_path)
    fixed2 = fix_base_namespace_declarations(base_path)
    
    total_fixed = fixed1 + fixed2
    print(f"\n[COMPLETE] 共修复了 {total_fixed} 个文件")
    
    if total_fixed > 0:
        print("\n[NEXT] 请重新编译项目")

if __name__ == "__main__":
    main()
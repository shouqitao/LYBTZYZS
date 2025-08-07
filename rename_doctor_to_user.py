#!/usr/bin/env python3
"""
将前端的Doctor模块重命名为User模块
"""

import os
import re
import shutil
from pathlib import Path

def rename_doctor_models():
    """重命名Doctor模型为User"""
    # 1. 重命名目录
    old_dir = Path("src/Frontend/Desktop/Core/Models/Doctors")
    new_dir = Path("src/Frontend/Desktop/Core/Models/Users")
    
    if old_dir.exists():
        # 如果新目录已存在，先删除
        if new_dir.exists():
            shutil.rmtree(new_dir)
        
        # 重命名目录
        old_dir.rename(new_dir)
        print(f"Renamed directory: {old_dir} -> {new_dir}")
        
        # 2. 重命名文件
        old_file = new_dir / "DoctorInfo.cs"
        new_file = new_dir / "UserInfo.cs"
        
        if old_file.exists():
            # 读取文件内容
            with open(old_file, 'r', encoding='utf-8') as f:
                content = f.read()
            
            # 替换类名和命名空间
            content = content.replace("namespace LYBT.WPF.Client.Core.Models.Doctors", 
                                    "namespace LYBT.WPF.Client.Core.Models.Users")
            content = content.replace("public class DoctorInfo", "public class UserInfo")
            content = content.replace("/// 医生信息", "/// 用户信息")
            content = content.replace("医生编码", "用户编码")
            
            # 写入新文件
            with open(new_file, 'w', encoding='utf-8') as f:
                f.write(content)
            
            # 删除旧文件
            old_file.unlink()
            print(f"Renamed file: DoctorInfo.cs -> UserInfo.cs")
            
            return True
    
    return False

def rename_doctor_interfaces():
    """重命名Doctor接口为User"""
    interfaces_dir = Path("src/Frontend/Desktop/Core/Interfaces/Services")
    
    # 查找并重命名IDoctorService
    old_interface = interfaces_dir / "IDoctorService.cs"
    
    if old_interface.exists():
        # 读取内容
        with open(old_interface, 'r', encoding='utf-8') as f:
            content = f.read()
        
        # 由于User服务已存在，删除Doctor服务
        old_interface.unlink()
        print(f"Removed IDoctorService.cs (using IUserService instead)")
        return True
    
    return False

def update_references():
    """更新所有对Doctor的引用为User"""
    
    # 定义需要搜索的目录
    search_dirs = [
        "src/Frontend/Desktop/Core",
        "src/Frontend/Desktop/Modules",
        "src/Frontend/Desktop/Shell"
    ]
    
    replacements = [
        (r'using LYBT\.WPF\.Client\.Core\.Models\.Doctors', 'using LYBT.WPF.Client.Core.Models.Users'),
        (r'DoctorInfo', 'UserInfo'),
        (r'IDoctorService', 'IUserService'),
        (r'DoctorService', 'UserService'),
        (r'doctorService', 'userService'),
        (r'_doctorService', '_userService'),
        (r'DoctorListItemControl', 'UserListItemControl'),
        (r'DoctorDisplayControl', 'UserDisplayControl'),
    ]
    
    fixed_count = 0
    
    for search_dir in search_dirs:
        dir_path = Path(search_dir)
        if not dir_path.exists():
            continue
            
        # 搜索所有.cs和.xaml文件
        for ext in ['*.cs', '*.xaml']:
            for file_path in dir_path.rglob(ext):
                try:
                    with open(file_path, 'r', encoding='utf-8') as f:
                        content = f.read()
                    
                    original_content = content
                    
                    # 应用所有替换规则
                    for pattern, replacement in replacements:
                        content = re.sub(pattern, replacement, content)
                    
                    # 如果内容有变化，写回文件
                    if content != original_content:
                        with open(file_path, 'w', encoding='utf-8') as f:
                            f.write(content)
                        print(f"Updated references in: {file_path}")
                        fixed_count += 1
                        
                except Exception as e:
                    print(f"Error processing {file_path}: {e}")
    
    return fixed_count

def rename_doctor_views():
    """重命名Doctor视图文件"""
    modules_dir = Path("src/Frontend/Desktop/Modules")
    
    # 查找Doctors模块
    doctors_module = modules_dir / "Doctors"
    users_module = modules_dir / "Users"
    
    if doctors_module.exists():
        # 如果Users模块已存在，删除Doctors模块
        if users_module.exists():
            shutil.rmtree(doctors_module)
            print(f"Removed Doctors module (Users module already exists)")
        else:
            # 重命名模块目录
            doctors_module.rename(users_module)
            print(f"Renamed module: Doctors -> Users")
            
            # 更新模块内的文件名和内容
            for file_path in users_module.rglob("*Doctor*"):
                new_name = str(file_path).replace("Doctor", "User")
                new_path = Path(new_name)
                file_path.rename(new_path)
                print(f"Renamed: {file_path.name} -> {new_path.name}")
        
        return True
    
    return False

def main():
    print("Starting Doctor to User renaming...")
    
    # 1. 重命名模型
    model_renamed = rename_doctor_models()
    print(f"\nModel rename status: {'Success' if model_renamed else 'Not found'}")
    
    # 2. 重命名接口
    interface_renamed = rename_doctor_interfaces()
    print(f"Interface rename status: {'Success' if interface_renamed else 'Not found'}")
    
    # 3. 重命名视图模块
    view_renamed = rename_doctor_views()
    print(f"View module rename status: {'Success' if view_renamed else 'Not found'}")
    
    # 4. 更新所有引用
    updated_refs = update_references()
    print(f"\nUpdated {updated_refs} file references")
    
    print("\n=== Summary ===")
    print(f"Model renamed: {model_renamed}")
    print(f"Interface handled: {interface_renamed}")
    print(f"View module renamed: {view_renamed}")
    print(f"References updated: {updated_refs}")

if __name__ == "__main__":
    main()
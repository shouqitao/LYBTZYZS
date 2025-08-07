#!/usr/bin/env python3
"""
删除UserInfo，统一使用BaseUserModel
"""

import os
import re
from pathlib import Path

def delete_userinfo_file():
    """删除UserInfo.cs文件"""
    file_path = Path("src/Frontend/Desktop/Core/Models/Users/UserInfo.cs")
    if file_path.exists():
        file_path.unlink()
        print(f"Deleted: {file_path}")
        return 1
    return 0

def replace_userinfo_references():
    """替换所有UserInfo引用为BaseUserModel"""
    patterns = [
        "src/Frontend/Desktop/**/*.cs",
        "src/Frontend/Desktop/**/*.xaml",
        "src/Frontend/Desktop/**/*.xaml.cs"
    ]
    
    replacements = [
        (r'\bUserInfo\b', 'BaseUserModel'),
        (r'using LYBT\.WPF\.Client\.Core\.Models\.Users;', 'using LYBT.Shared.Models.Core;'),
        (r'Models\.Users\.UserInfo', 'Shared.Models.Core.BaseUserModel'),
    ]
    
    modified_files = 0
    for pattern in patterns:
        for file_path in Path(".").glob(pattern):
            if not file_path.exists():
                continue
                
            try:
                content = file_path.read_text(encoding='utf-8')
                original_content = content
                
                for old, new in replacements:
                    content = re.sub(old, new, content)
                
                if content != original_content:
                    file_path.write_text(content, encoding='utf-8')
                    print(f"Modified: {file_path}")
                    modified_files += 1
            except Exception as e:
                print(f"Error processing {file_path}: {e}")
    
    return modified_files

def add_missing_fields_to_baseusermodel():
    """向BaseUserModel添加缺失的字段"""
    file_path = Path("src/Shared/LYBT.Shared.Models/Core/BaseUserModel.cs")
    if not file_path.exists():
        return 0
    
    content = file_path.read_text(encoding='utf-8')
    
    # 检查是否已有Role字段
    if 'public string Role' not in content:
        # 在Status字段后添加Role字段
        status_pattern = r'(public CommonStatus Status[^}]+})'
        replacement = r'''\1

        /// <summary>用户角色</summary>
        [DisplayName("角色")]
        public string Role { get; set; } = "User";

        /// <summary>是否为管理员</summary>
        [NotMapped]
        public bool IsAdmin => Role == "Admin";

        /// <summary>是否为超级管理员</summary>
        [NotMapped]
        public bool IsSuperAdmin => Username == "sysadmin";'''
        
        content = re.sub(status_pattern, replacement, content)
        
        # 确保添加了NotMapped的using
        if 'using System.ComponentModel.DataAnnotations.Schema;' not in content:
            content = 'using System.ComponentModel.DataAnnotations.Schema;\n' + content
        
        file_path.write_text(content, encoding='utf-8')
        print(f"Updated BaseUserModel with Role fields")
        return 1
    
    return 0

def fix_service_errors():
    """修复服务层中的特定错误"""
    fixes = [
        # AuthenticationService - 修复dto.Email -> dto.Username
        {
            'file': 'src/Frontend/Desktop/Services/AuthenticationService.cs',
            'replacements': [
                (r'dto\.Email', 'dto.Username'),
                (r'string\.Admin', '"Admin"'),
                (r'string\.Doctor', '"Doctor"'),
                (r'string\.FrontDesk', '"FrontDesk"'),
                (r'string\.Cashier', '"Cashier"'),
                (r'string\.Pharmacist', '"Pharmacist"'),
            ]
        },
        # PermissionService - 修复string.XXX -> "XXX"
        {
            'file': 'src/Frontend/Desktop/Services/PermissionService.cs',
            'replacements': [
                (r'string\.Admin', '"Admin"'),
                (r'string\.DiagnosingDoctor', '"Doctor"'),
                (r'string\.RegistrationStaff', '"FrontDesk"'),
                (r'string\.CashierStaff', '"Cashier"'),
                (r'string\.PharmacyStaff', '"Pharmacist"'),
                (r'string\.PhysiotherapyStaff', '"Therapist"'),
            ]
        },
        # PatientService - 修复IsActive -> Status
        {
            'file': 'src/Frontend/Desktop/Services/PatientService.cs',
            'replacements': [
                (r'dto\.IsActive \? PatientStatus\.Active : PatientStatus\.Inactive',
                 'dto.Status == CommonStatus.Enabled ? PatientStatus.Active : PatientStatus.Inactive'),
            ]
        },
        # UserService - 修复Email -> Username
        {
            'file': 'src/Frontend/Desktop/Services/UserService.cs',
            'replacements': [
                (r'dto\.Email', 'dto.Username'),
            ]
        }
    ]
    
    fixed = 0
    for fix_info in fixes:
        file_path = Path(fix_info['file'])
        if not file_path.exists():
            continue
            
        try:
            content = file_path.read_text(encoding='utf-8')
            original_content = content
            
            for old, new in fix_info['replacements']:
                content = re.sub(old, new, content)
            
            if content != original_content:
                file_path.write_text(content, encoding='utf-8')
                print(f"Fixed: {file_path}")
                fixed += 1
        except Exception as e:
            print(f"Error fixing {file_path}: {e}")
    
    return fixed

def add_commonStatus_using():
    """确保需要的文件都有CommonStatus的using"""
    files_need_commonStatus = [
        "src/Frontend/Desktop/Services/PatientService.cs",
        "src/Frontend/Desktop/Services/AuthenticationService.cs",
        "src/Frontend/Desktop/Services/UserService.cs",
    ]
    
    fixed = 0
    for file_path_str in files_need_commonStatus:
        file_path = Path(file_path_str)
        if not file_path.exists():
            continue
            
        content = file_path.read_text(encoding='utf-8')
        
        if 'CommonStatus' in content and 'using LYBT.Shared.Models.Enums;' not in content:
            # 在其他using后添加
            content = re.sub(
                r'(using System[^;]*;\n)',
                r'\1using LYBT.Shared.Models.Enums;\n',
                content,
                count=1
            )
            file_path.write_text(content, encoding='utf-8')
            print(f"Added CommonStatus using to: {file_path}")
            fixed += 1
    
    return fixed

def clean_obj_bin():
    """清理obj和bin目录"""
    import shutil
    dirs_to_clean = [
        "src/Frontend/Desktop/Core/obj",
        "src/Frontend/Desktop/Core/bin",
        "src/Frontend/Desktop/Services/obj",
        "src/Frontend/Desktop/Services/bin",
        "src/Frontend/Desktop/Infrastructure/obj",
        "src/Frontend/Desktop/Infrastructure/bin",
    ]
    
    cleaned = 0
    for dir_path_str in dirs_to_clean:
        dir_path = Path(dir_path_str)
        if dir_path.exists():
            shutil.rmtree(dir_path)
            print(f"Cleaned: {dir_path}")
            cleaned += 1
    
    return cleaned

def main():
    print("Removing UserInfo and using BaseUserModel instead...")
    
    # 1. 先更新BaseUserModel
    updated = add_missing_fields_to_baseusermodel()
    print(f"Updated BaseUserModel: {updated}")
    
    # 2. 替换所有UserInfo引用
    modified = replace_userinfo_references()
    print(f"Modified {modified} files with UserInfo references")
    
    # 3. 删除UserInfo文件
    deleted = delete_userinfo_file()
    print(f"Deleted {deleted} UserInfo file")
    
    # 4. 修复服务层错误
    fixed = fix_service_errors()
    print(f"Fixed {fixed} service files")
    
    # 5. 添加必要的using语句
    added = add_commonStatus_using()
    print(f"Added {added} using statements")
    
    # 6. 清理obj/bin
    cleaned = clean_obj_bin()
    print(f"Cleaned {cleaned} directories")
    
    print("\n=== Summary ===")
    print(f"BaseUserModel updated: {updated}")
    print(f"Files modified: {modified}")
    print(f"UserInfo deleted: {deleted}")
    print(f"Services fixed: {fixed}")
    print(f"Using statements added: {added}")
    print(f"Directories cleaned: {cleaned}")

if __name__ == "__main__":
    main()
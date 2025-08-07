#!/usr/bin/env python3
"""
修复前端服务层的编译错误
"""

import os
import re
from pathlib import Path

def fix_authentication_service():
    """修复AuthenticationService中的错误"""
    file_path = Path("src/Frontend/Desktop/Services/AuthenticationService.cs")
    if not file_path.exists():
        return 0
    
    content = file_path.read_text(encoding='utf-8')
    
    # 1. 修复using声明
    content = content.replace('using BaseUserModel = LYBT.WPF.Client.Core.Models.Users.BaseUserModel;',
                            'using LYBT.Shared.Models.Core;')
    
    # 2. 修复Role字符串引用
    content = re.sub(r'string\.RegistrationStaff', '"FrontDesk"', content)
    content = re.sub(r'string\.DiagnosingDoctor', '"Doctor"', content)
    content = re.sub(r'"Cashier"Staff', '"Cashier"', content)  # 修复之前的错误
    content = re.sub(r'string\.PharmacyStaff', '"Pharmacist"', content)
    content = re.sub(r'string\.PhysiotherapyStaff', '"Therapist"', content)
    
    # 3. 修复IsActive -> Status
    content = content.replace('IsActive = baseUser.IsActive,', 
                            'Status = baseUser.Status,')
    content = content.replace('IsActive = baseUser.Status == CommonStatus.Enabled,', 
                            'Status = baseUser.Status,')
    
    # 4. 修复Email字段（BaseUserModel中没有Email）
    content = re.sub(r'Email = [^,\n]+,', '', content)
    
    file_path.write_text(content, encoding='utf-8')
    print(f"Fixed: {file_path}")
    return 1

def fix_user_service():
    """修复UserService中的错误"""
    file_path = Path("src/Frontend/Desktop/Services/UserService.cs")
    if not file_path.exists():
        return 0
    
    content = file_path.read_text(encoding='utf-8')
    
    # 1. 修复IsActive -> Status
    content = content.replace('IsActive = dto.Status == CommonStatus.Enabled,',
                            'Status = dto.Status,')
    
    # 2. 删除Email和IsSuperAdmin的赋值（BaseUserModel中IsSuperAdmin是计算属性）
    content = re.sub(r'Email = dto\.Username,\s*\n', '', content)
    content = re.sub(r'IsSuperAdmin = dto\.Username\?\.Equals\("sysadmin", StringComparison\.OrdinalIgnoreCase\) == true\s*\n', '', content)
    
    # 3. 修复ConvertToUserInfo方法的返回类型问题
    # 删除或注释掉有问题的字段
    pattern = r'Email = dto\.Username,?\s*\n'
    content = re.sub(pattern, '', content)
    
    file_path.write_text(content, encoding='utf-8')
    print(f"Fixed: {file_path}")
    return 1

def fix_patient_service():
    """修复PatientService中的DTO字段引用"""
    file_path = Path("src/Frontend/Desktop/Services/PatientService.cs")
    if not file_path.exists():
        return 0
    
    content = file_path.read_text(encoding='utf-8')
    
    # 添加CommonStatus的using（如果没有）
    if 'using LYBT.Shared.Models.Enums;' not in content:
        content = 'using LYBT.Shared.Models.Enums;\n' + content
    
    file_path.write_text(content, encoding='utf-8')
    print(f"Fixed: {file_path}")
    return 1

def fix_userinfo_references_in_interfaces():
    """修复接口中的UserInfo引用"""
    interface_files = [
        "src/Frontend/Desktop/Core/Interfaces/Services/IAuthenticationService.cs",
        "src/Frontend/Desktop/Core/Interfaces/Services/IUserService.cs",
        "src/Frontend/Desktop/Core/Interfaces/Services/IUserSessionManager.cs",
        "src/Frontend/Desktop/Core/Interfaces/Services/IPermissionService.cs"
    ]
    
    fixed = 0
    for file_path_str in interface_files:
        file_path = Path(file_path_str)
        if not file_path.exists():
            continue
            
        content = file_path.read_text(encoding='utf-8')
        original = content
        
        # 确保有正确的using
        if 'BaseUserModel' in content and 'using LYBT.Shared.Models.Core;' not in content:
            # 在namespace之前添加using
            content = re.sub(r'(namespace\s+)', r'using LYBT.Shared.Models.Core;\n\n\1', content)
        
        if content != original:
            file_path.write_text(content, encoding='utf-8')
            print(f"Fixed interface: {file_path}")
            fixed += 1
    
    return fixed

def fix_dto_references():
    """修复UserDto的字段引用"""
    files_to_check = [
        "src/Frontend/Desktop/Services/UserService.cs",
        "src/Frontend/Desktop/Services/AuthenticationService.cs"
    ]
    
    fixed = 0
    for file_path_str in files_to_check:
        file_path = Path(file_path_str)
        if not file_path.exists():
            continue
            
        content = file_path.read_text(encoding='utf-8')
        original = content
        
        # UserDto中的Status字段需要正确处理
        # 确保有CommonStatus的using
        if 'CommonStatus' in content and 'using LYBT.Shared.Models.Enums;' not in content:
            content = 'using LYBT.Shared.Models.Enums;\n' + content
        
        if content != original:
            file_path.write_text(content, encoding='utf-8')
            print(f"Added using to: {file_path}")
            fixed += 1
    
    return fixed

def clean_obj_bin():
    """清理obj和bin目录"""
    import shutil
    dirs_to_clean = [
        "src/Frontend/Desktop/Services/obj",
        "src/Frontend/Desktop/Services/bin",
        "src/Frontend/Desktop/Core/obj",
        "src/Frontend/Desktop/Core/bin",
        "src/Frontend/Desktop/Infrastructure/obj",
        "src/Frontend/Desktop/Infrastructure/bin"
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
    print("Fixing frontend service compilation errors...")
    
    fixed = 0
    
    # 1. 修复各个服务文件
    fixed += fix_authentication_service()
    fixed += fix_user_service()
    fixed += fix_patient_service()
    
    # 2. 修复接口文件
    fixed += fix_userinfo_references_in_interfaces()
    
    # 3. 修复DTO引用
    fixed += fix_dto_references()
    
    # 4. 清理obj/bin
    cleaned = clean_obj_bin()
    
    print(f"\n=== Summary ===")
    print(f"Total files fixed: {fixed}")
    print(f"Directories cleaned: {cleaned}")

if __name__ == "__main__":
    main()
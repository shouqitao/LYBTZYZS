#!/usr/bin/env python3
"""
修复前端中BaseUserModel引用，统一使用UserInfo
"""

import os
import re
from pathlib import Path

def fix_service_layer():
    """修复服务层中的BaseUserModel引用"""
    service_files = [
        "src/Frontend/Desktop/Services/UserService.cs",
        "src/Frontend/Desktop/Services/UserSessionManager.cs",
        "src/Frontend/Desktop/Services/PermissionService.cs"
    ]
    
    fixed = 0
    for file_path_str in service_files:
        file_path = Path(file_path_str)
        if not file_path.exists():
            continue
        
        content = file_path.read_text(encoding='utf-8')
        original = content
        
        # UserService特殊处理
        if "UserService.cs" in file_path_str:
            # ConvertToUserInfo方法的返回类型
            content = re.sub(
                r'private BaseUserModel ConvertToUserInfo',
                'private UserInfo ConvertToUserInfo',
                content
            )
            content = re.sub(
                r'return new BaseUserModel\b',
                'return new UserInfo',
                content
            )
            # 方法返回类型
            content = re.sub(
                r'Task<ServiceResult<BaseUserModel>>',
                'Task<ServiceResult<UserInfo>>',
                content
            )
            content = re.sub(
                r'Task<ServiceResult<List<BaseUserModel>>>',
                'Task<ServiceResult<List<UserInfo>>>',
                content
            )
            content = re.sub(
                r'Task<List<BaseUserModel>>',
                'Task<List<UserInfo>>',
                content
            )
            content = re.sub(
                r'PagedResult<BaseUserModel>',
                'PagedResult<UserInfo>',
                content
            )
            content = re.sub(
                r'ServiceResult<BaseUserModel>',
                'ServiceResult<UserInfo>',
                content
            )
            content = re.sub(
                r'List<BaseUserModel>',
                'List<UserInfo>',
                content
            )
        
        # UserSessionManager特殊处理
        if "UserSessionManager.cs" in file_path_str:
            content = re.sub(
                r'private BaseUserModel\? _currentUser;',
                'private UserInfo? _currentUser;',
                content
            )
            content = re.sub(
                r'public BaseUserModel\? CurrentUser',
                'public UserInfo? CurrentUser',
                content
            )
            content = re.sub(
                r'public void SetUserSession\(BaseUserModel user',
                'public void SetUserSession(UserInfo user',
                content
            )
            content = re.sub(
                r'public void RefreshUserInfo\(BaseUserModel user',
                'public void RefreshUserInfo(UserInfo user',
                content
            )
        
        # PermissionService特殊处理
        if "PermissionService.cs" in file_path_str:
            content = re.sub(
                r'public bool HasPermission\(BaseUserModel user',
                'public bool HasPermission(UserInfo user',
                content
            )
            content = re.sub(
                r'public bool HasAdminPermission\(BaseUserModel user',
                'public bool HasAdminPermission(UserInfo user',
                content
            )
            content = re.sub(
                r'public bool HasSuperAdminPermission\(BaseUserModel user',
                'public bool HasSuperAdminPermission(UserInfo user',
                content
            )
            content = re.sub(
                r'public List<string> GetAccessibleModules\(BaseUserModel user',
                'public List<string> GetAccessibleModules(UserInfo user',
                content
            )
        
        # 添加UserInfo的using（如果需要）
        if 'UserInfo' in content and 'using LYBT.WPF.Client.Core.Models.Users;' not in content:
            # 在namespace之前添加
            content = re.sub(
                r'(namespace\s+)',
                r'using LYBT.WPF.Client.Core.Models.Users;\n\n\1',
                content
            )
        
        if content != original:
            file_path.write_text(content, encoding='utf-8')
            print(f"Fixed service: {file_path}")
            fixed += 1
    
    return fixed

def fix_interfaces():
    """修复接口中的BaseUserModel引用"""
    interface_files = [
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
        
        # 替换BaseUserModel为UserInfo
        content = re.sub(r'\bBaseUserModel\b', 'UserInfo', content)
        
        # 确保有正确的using
        if 'UserInfo' in content and 'using LYBT.WPF.Client.Core.Models.Users;' not in content:
            content = re.sub(
                r'(using System[^;]*;\n)',
                r'\1using LYBT.WPF.Client.Core.Models.Users;\n',
                content,
                count=1
            )
        
        if content != original:
            file_path.write_text(content, encoding='utf-8')
            print(f"Fixed interface: {file_path}")
            fixed += 1
    
    return fixed

def fix_viewmodels():
    """修复ViewModel中的BaseUserModel引用"""
    viewmodel_dirs = [
        "src/Frontend/Desktop/Modules",
        "src/Frontend/Desktop/Shell"
    ]
    
    fixed = 0
    for dir_path in viewmodel_dirs:
        for file_path in Path(dir_path).rglob("*ViewModel*.cs"):
            if not file_path.exists():
                continue
            
            try:
                content = file_path.read_text(encoding='utf-8')
                original = content
                
                # 替换BaseUserModel为UserInfo
                content = re.sub(r'\bBaseUserModel\b', 'UserInfo', content)
                
                # 添加UserInfo的using（如果需要）
                if 'UserInfo' in content and 'using LYBT.WPF.Client.Core.Models.Users;' not in content:
                    # 在namespace之前添加
                    content = re.sub(
                        r'(namespace\s+)',
                        r'using LYBT.WPF.Client.Core.Models.Users;\n\n\1',
                        content
                    )
                
                if content != original:
                    file_path.write_text(content, encoding='utf-8')
                    print(f"Fixed ViewModel: {file_path}")
                    fixed += 1
            except Exception as e:
                print(f"Error processing {file_path}: {e}")
    
    return fixed

def main():
    print("Fixing BaseUserModel references to UserInfo...")
    
    fixed = 0
    
    # 1. 修复服务层
    fixed += fix_service_layer()
    
    # 2. 修复接口
    fixed += fix_interfaces()
    
    # 3. 修复ViewModels
    fixed += fix_viewmodels()
    
    print(f"\n=== Summary ===")
    print(f"Total files fixed: {fixed}")

if __name__ == "__main__":
    main()
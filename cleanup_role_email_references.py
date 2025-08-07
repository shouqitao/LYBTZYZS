#!/usr/bin/env python3
"""
清理前端所有Role、Email、IsAdmin、IsSuperAdmin引用
"""

import os
import re
from pathlib import Path

def cleanup_authentication_service():
    """清理AuthenticationService中的Role和Email引用"""
    file_path = Path("src/Frontend/Desktop/Services/AuthenticationService.cs")
    if not file_path.exists():
        return 0
    
    content = file_path.read_text(encoding='utf-8')
    original = content
    
    # 删除Role相关的行
    content = re.sub(r'.*Role = .*\n', '', content)
    
    # 删除Email相关的行
    content = re.sub(r'.*Email = .*\n', '', content)
    
    # 删除IsAdmin相关的行
    content = re.sub(r'.*IsAdmin = .*\n', '', content)
    
    # 删除IsSuperAdmin赋值（但保留方法判断）
    content = re.sub(r'.*IsSuperAdmin = .*\n', '', content)
    
    # 修复ParseUserRole方法，返回空字符串
    content = re.sub(
        r'private string ParseUserRole\(string\? roleString\)[\s\S]*?return result;[\s\S]*?\}',
        'private string ParseUserRole(string? roleString) {\n            return "";\n        }',
        content
    )
    
    if content != original:
        file_path.write_text(content, encoding='utf-8')
        print(f"Cleaned: {file_path}")
        return 1
    return 0

def cleanup_user_service():
    """清理UserService中的Role和Email引用"""
    file_path = Path("src/Frontend/Desktop/Services/UserService.cs")
    if not file_path.exists():
        return 0
    
    content = file_path.read_text(encoding='utf-8')
    original = content
    
    # 删除Role相关的行
    content = re.sub(r'.*// Role = .*\n', '', content)
    content = re.sub(r'.*Role = .*\n', '', content)
    
    # 删除Email相关的行
    content = re.sub(r'.*Email = .*\n', '', content)
    
    # 删除IsSuperAdmin赋值
    content = re.sub(r'.*IsSuperAdmin = .*\n', '', content)
    
    # 删除GetRolesAsync方法
    content = re.sub(
        r'public async Task<List<string>> GetRolesAsync\(\)[\s\S]*?\}[\s\S]*?\}',
        'public async Task<List<string>> GetRolesAsync() {\n            return await Task.FromResult(new List<string>());\n        }',
        content
    )
    
    if content != original:
        file_path.write_text(content, encoding='utf-8')
        print(f"Cleaned: {file_path}")
        return 1
    return 0

def cleanup_permission_service():
    """清理PermissionService中的Role引用"""
    file_path = Path("src/Frontend/Desktop/Services/PermissionService.cs")
    if not file_path.exists():
        return 0
    
    content = file_path.read_text(encoding='utf-8')
    original = content
    
    # 简化HasPermission方法
    content = re.sub(
        r'public bool HasPermission\(BaseUserModel user, string permission\)[\s\S]*?return false;[\s\S]*?\}',
        '''public bool HasPermission(BaseUserModel user, string permission) {
            if (user == null) return false;
            // 只有sysadmin有所有权限
            return user.Username == "sysadmin";
        }''',
        content
    )
    
    # 简化HasAdminPermission
    content = re.sub(
        r'public bool HasAdminPermission\(BaseUserModel user\)[\s\S]*?\}',
        '''public bool HasAdminPermission(BaseUserModel user) {
            return user?.Username == "sysadmin";
        }''',
        content
    )
    
    # 简化HasSuperAdminPermission
    content = re.sub(
        r'public bool HasSuperAdminPermission\(BaseUserModel user\)[\s\S]*?\}',
        '''public bool HasSuperAdminPermission(BaseUserModel user) {
            return user?.Username == "sysadmin";
        }''',
        content
    )
    
    # 简化GetAccessibleModules - sysadmin有所有模块，其他用户有基础模块
    content = re.sub(
        r'public List<string> GetAccessibleModules\(BaseUserModel user\)[\s\S]*?return user\.Role switch[\s\S]*?\};[\s\S]*?\}',
        '''public List<string> GetAccessibleModules(BaseUserModel user) {
            if (user == null) return new List<string>();
            
            if (user.Username == "sysadmin") {
                // 管理员有所有模块
                return new List<string> { 
                    "患者管理", "药材管理", "处方管理", "看诊管理", 
                    "系统设置", "用户管理", "日志管理" 
                };
            }
            
            // 普通用户的基础模块
            return new List<string> { 
                "患者管理", "药材管理", "处方管理", "看诊管理" 
            };
        }''',
        content
    )
    
    # 删除GetRoleDisplayName中的Role判断
    content = re.sub(
        r'public string GetRoleDisplayName\(string role\)[\s\S]*?\}',
        '''public string GetRoleDisplayName(string role) {
            return "用户";
        }''',
        content
    )
    
    if content != original:
        file_path.write_text(content, encoding='utf-8')
        print(f"Cleaned: {file_path}")
        return 1
    return 0

def cleanup_user_session_manager():
    """清理UserSessionManager中的Role引用"""
    file_path = Path("src/Frontend/Desktop/Services/UserSessionManager.cs")
    if not file_path.exists():
        return 0
    
    content = file_path.read_text(encoding='utf-8')
    original = content
    
    # 修改HasRole方法
    content = re.sub(
        r'public bool HasRole\(string role\)[\s\S]*?\}',
        '''public bool HasRole(string role) {
            // 不再有角色概念
            return false;
        }''',
        content
    )
    
    # 简化GetRoleDisplayName
    content = re.sub(
        r'return RoleNavigationConfig\.GetRoleDisplayName\(_currentUser\.Role\);',
        'return _currentUser?.Username == "sysadmin" ? "管理员" : "用户";',
        content
    )
    
    if content != original:
        file_path.write_text(content, encoding='utf-8')
        print(f"Cleaned: {file_path}")
        return 1
    return 0

def create_userinfo_model():
    """创建UserInfo模型"""
    file_path = Path("src/Frontend/Desktop/Core/Models/Users/UserInfo.cs")
    
    # 确保目录存在
    file_path.parent.mkdir(parents=True, exist_ok=True)
    
    content = """using System;
using LYBT.Shared.Models.Core;
using LYBT.Shared.Models.Enums;
using LYBT.Shared.Models.Extensions;

namespace LYBT.WPF.Client.Core.Models.Users
{
    /// <summary>
    /// 用户信息模型 - 前端专用，继承共享基础模型
    /// </summary>
    public class UserInfo : BaseUserModel
    {
        /// <summary>是否选中（用于批量操作）</summary>
        public bool IsSelected { get; set; }
        
        /// <summary>显示名称</summary>
        public string DisplayName => string.IsNullOrEmpty(RealName) ? Username : RealName;
        
        /// <summary>状态文本</summary>
        public string StatusText => Status.GetDescription();
        
        /// <summary>是否为系统管理员（基于用户名判断）</summary>
        public bool IsSysAdmin => Username == "sysadmin";
    }
}"""
    
    file_path.write_text(content, encoding='utf-8')
    print(f"Created: {file_path}")
    return 1

def cleanup_interfaces():
    """清理接口中的Role引用"""
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
        
        # 确保使用正确的命名空间
        if 'BaseUserModel' in content and 'using LYBT.Shared.Models.Core;' not in content:
            content = re.sub(
                r'(using System[^;]*;\n)',
                r'\1using LYBT.Shared.Models.Core;\n',
                content,
                count=1
            )
        
        # 确保使用UserInfo而不是BaseUserModel作为返回类型（前端模型）
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

def cleanup_viewmodels():
    """清理ViewModel中的Role引用"""
    viewmodel_patterns = [
        "src/Frontend/Desktop/**/*ViewModel*.cs"
    ]
    
    fixed = 0
    for pattern in viewmodel_patterns:
        for file_path in Path(".").glob(pattern):
            if not file_path.exists():
                continue
            
            try:
                content = file_path.read_text(encoding='utf-8')
                original = content
                
                # 替换Role引用
                content = re.sub(r'\.Role\b', '.Username == "sysadmin" ? "管理员" : "用户"', content)
                
                # 替换IsAdmin引用
                content = re.sub(r'\.IsAdmin\b', '.Username == "sysadmin"', content)
                
                # 替换IsSuperAdmin引用
                content = re.sub(r'\.IsSuperAdmin\b', '.Username == "sysadmin"', content)
                
                if content != original:
                    file_path.write_text(content, encoding='utf-8')
                    print(f"Fixed ViewModel: {file_path}")
                    fixed += 1
            except Exception as e:
                print(f"Error processing {file_path}: {e}")
    
    return fixed

def main():
    print("Cleaning up Role, Email, IsAdmin, IsSuperAdmin references...")
    
    fixed = 0
    
    # 1. 创建UserInfo模型
    fixed += create_userinfo_model()
    
    # 2. 清理服务层
    fixed += cleanup_authentication_service()
    fixed += cleanup_user_service()
    fixed += cleanup_permission_service()
    fixed += cleanup_user_session_manager()
    
    # 3. 清理接口
    fixed += cleanup_interfaces()
    
    # 4. 清理ViewModels
    fixed += cleanup_viewmodels()
    
    print(f"\n=== Summary ===")
    print(f"Total files fixed: {fixed}")
    print("\nNext steps:")
    print("1. Clean and rebuild the solution")
    print("2. Fix any remaining compilation errors")
    print("3. Test sysadmin login")
    print("4. Test normal user login")

if __name__ == "__main__":
    main()
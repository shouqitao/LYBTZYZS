#!/usr/bin/env python3
"""
修复前端最终的编译错误
"""

import os
import re
from pathlib import Path

def fix_patients_api_service():
    """修复IPatientsApiService中的Records引用"""
    file_path = Path("src/Frontend/Desktop/Services/Interfaces/IPatientsApiService.cs")
    if not file_path.exists():
        return 0
    
    content = file_path.read_text(encoding='utf-8')
    
    # 删除Records引用
    content = content.replace('using LYBT.Shared.Models.Contracts.Records;\n', '')
    
    # 将RecordDto改为object
    content = content.replace('Task<Refit.ApiResponse<List<RecordDto>>> GetHistoryAsync', 
                            'Task<Refit.ApiResponse<List<object>>> GetHistoryAsync')
    
    file_path.write_text(content, encoding='utf-8')
    print(f"Fixed: {file_path}")
    return 1

def fix_authentication_service():
    """修复AuthenticationService中的UserRole引用"""
    file_path = Path("src/Frontend/Desktop/Services/AuthenticationService.cs")
    if not file_path.exists():
        return 0
    
    content = file_path.read_text(encoding='utf-8')
    
    # 将UserRole改为string
    content = re.sub(r'\bUserRole\b(?!\w)', 'string', content)
    
    file_path.write_text(content, encoding='utf-8')
    print(f"Fixed: {file_path}")
    return 1

def fix_permission_service():
    """修复PermissionService中的UserRole引用和缺失方法"""
    file_path = Path("src/Frontend/Desktop/Services/PermissionService.cs")
    if not file_path.exists():
        return 0
    
    content = file_path.read_text(encoding='utf-8')
    
    # 将UserRole改为string
    content = re.sub(r'\bUserRole\b(?!\w)', 'string', content)
    
    # 在类的末尾添加GetRoleDisplayName方法(在最后一个}之前)
    if 'GetRoleDisplayName' not in content:
        # 找到类的最后一个方法，在其后添加
        last_brace = content.rfind('}')
        if last_brace > 0:
            # 找到倒数第二个}，即类的结束括号前
            second_last_brace = content.rfind('}', 0, last_brace)
            if second_last_brace > 0:
                insert_pos = second_last_brace
                new_method = """

        /// <summary>
        /// 获取角色显示名称
        /// </summary>
        public string GetRoleDisplayName(string role)
        {
            return role switch
            {
                "Admin" => "管理员",
                "Doctor" => "医生",
                "FrontDesk" => "前台",
                "Cashier" => "收费员",
                "Pharmacist" => "药剂师",
                _ => role
            };
        }"""
                content = content[:insert_pos] + new_method + "\n    " + content[insert_pos:]
    
    file_path.write_text(content, encoding='utf-8')
    print(f"Fixed: {file_path}")
    return 1

def fix_user_service():
    """修复UserService中的UserRole引用"""
    file_path = Path("src/Frontend/Desktop/Services/UserService.cs")
    if not file_path.exists():
        return 0
    
    content = file_path.read_text(encoding='utf-8')
    
    # 将UserRole改为string
    content = re.sub(r'\bUserRole\b(?!\w)', 'string', content)
    
    # 修复GetRolesAsync方法返回类型
    content = re.sub(r'public async Task<List<UserRole>> GetRolesAsync\(\)',
                    'public async Task<List<string>> GetRolesAsync()',
                    content)
    
    # 修复返回值
    content = re.sub(r'return Enum\.GetValues<UserRole>\(\)\.ToList\(\);',
                    'return new List<string> { "Admin", "Doctor", "FrontDesk", "Cashier", "Pharmacist" };',
                    content)
    
    file_path.write_text(content, encoding='utf-8')
    print(f"Fixed: {file_path}")
    return 1

def fix_user_session_manager():
    """修复UserSessionManager中的UserRole引用和缺失方法"""
    file_path = Path("src/Frontend/Desktop/Services/UserSessionManager.cs")
    if not file_path.exists():
        return 0
    
    content = file_path.read_text(encoding='utf-8')
    
    # 将UserRole改为string
    content = re.sub(r'\bUserRole\b(?!\w)', 'string', content)
    
    # 在类的末尾添加HasRole方法(在最后一个}之前)
    if 'HasRole' not in content:
        # 找到类的最后一个方法
        last_brace = content.rfind('}')
        if last_brace > 0:
            second_last_brace = content.rfind('}', 0, last_brace)
            if second_last_brace > 0:
                insert_pos = second_last_brace
                new_method = """

        /// <summary>
        /// 检查当前用户是否具有指定角色
        /// </summary>
        public bool HasRole(string role)
        {
            return CurrentUser?.Role == role;
        }"""
                content = content[:insert_pos] + new_method + "\n    " + content[insert_pos:]
    
    file_path.write_text(content, encoding='utf-8')
    print(f"Fixed: {file_path}")
    return 1

def fix_refit_configuration():
    """修复RefitConfiguration中的JsonConverters引用"""
    file_path = Path("src/Frontend/Desktop/Infrastructure/RefitConfiguration.cs")
    if not file_path.exists():
        return 0
    
    content = file_path.read_text(encoding='utf-8')
    
    # 删除JsonConverters引用
    content = content.replace('using LYBT.WPF.Client.Infrastructure.JsonConverters;\n', '')
    
    # 如果有使用UserRoleJsonConverter的地方，注释掉
    content = re.sub(r'.*UserRoleJsonConverter.*\n', '// UserRoleJsonConverter removed\n', content)
    
    file_path.write_text(content, encoding='utf-8')
    print(f"Fixed: {file_path}")
    return 1

def clean_generated_files():
    """清理生成的文件"""
    patterns = [
        "src/Frontend/Desktop/Services/obj",
        "src/Frontend/Desktop/Infrastructure/obj",
        "src/Frontend/Desktop/Core/obj"
    ]
    
    import shutil
    cleaned = 0
    for pattern in patterns:
        path = Path(pattern)
        if path.exists():
            shutil.rmtree(path)
            print(f"Cleaned: {path}")
            cleaned += 1
    return cleaned

def main():
    print("Fixing final frontend compilation errors...")
    
    # 1. 修复各个文件
    fixed = 0
    fixed += fix_patients_api_service()
    fixed += fix_authentication_service()
    fixed += fix_permission_service()
    fixed += fix_user_service()
    fixed += fix_user_session_manager()
    fixed += fix_refit_configuration()
    
    print(f"\nFixed {fixed} files")
    
    # 2. 清理生成的文件
    cleaned = clean_generated_files()
    print(f"Cleaned {cleaned} directories")
    
    print("\n=== Summary ===")
    print(f"Total files fixed: {fixed}")
    print(f"Total directories cleaned: {cleaned}")
    print("\nNext step: Rebuild the frontend solution")

if __name__ == "__main__":
    main()
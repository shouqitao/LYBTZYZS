#!/usr/bin/env python3
"""
前端最终修复脚本
"""

import os
import re
from pathlib import Path

def fix_enum_converters():
    """修复EnumConverters.cs"""
    file_path = Path("src/Frontend/Desktop/Core/Converters/EnumConverters.cs")
    
    if file_path.exists():
        try:
            with open(file_path, 'r', encoding='utf-8') as f:
                content = f.read()
            
            # 删除或注释掉UserRole、BillingStatus、PharmacyStatus相关的代码
            content = re.sub(r'.*UserRole.*\n', '', content)
            content = re.sub(r'.*BillingStatus.*\n', '', content)
            content = re.sub(r'.*PharmacyStatus.*\n', '', content)
            
            with open(file_path, 'w', encoding='utf-8') as f:
                f.write(content)
            print(f"Fixed EnumConverters.cs")
            return True
        except Exception as e:
            print(f"Error fixing {file_path}: {e}")
    
    return False

def fix_user_info():
    """修复UserInfo.cs"""
    file_path = Path("src/Frontend/Desktop/Core/Models/Users/UserInfo.cs")
    
    if file_path.exists():
        try:
            with open(file_path, 'r', encoding='utf-8') as f:
                content = f.read()
            
            # 替换ContactNumber为PhoneNumber（BaseUserModel中应该是PhoneNumber）
            content = content.replace('ContactNumber', 'PhoneNumber')
            
            with open(file_path, 'w', encoding='utf-8') as f:
                f.write(content)
            print(f"Fixed UserInfo.cs")
            return True
        except Exception as e:
            print(f"Error fixing {file_path}: {e}")
    
    return False

def fix_log_info():
    """修复LogInfo.cs"""
    file_path = Path("src/Frontend/Desktop/Core/Models/Logs/LogInfo.cs")
    
    if file_path.exists():
        try:
            with open(file_path, 'r', encoding='utf-8') as f:
                content = f.read()
            
            # 修复LogLevel的switch语句
            content = re.sub(r'string\.(\w+)', r'"\1"', content)
            
            with open(file_path, 'w', encoding='utf-8') as f:
                f.write(content)
            print(f"Fixed LogInfo.cs")
            return True
        except Exception as e:
            print(f"Error fixing {file_path}: {e}")
    
    return False

def fix_user_action_log():
    """修复UserActionLogInfo.cs"""
    file_path = Path("src/Frontend/Desktop/Core/Models/Logs/UserActionLogInfo.cs")
    
    if file_path.exists():
        try:
            with open(file_path, 'r', encoding='utf-8') as f:
                content = f.read()
            
            # 修复LogActionType的switch语句
            content = re.sub(r'string\.(\w+)', r'"\1"', content)
            
            with open(file_path, 'w', encoding='utf-8') as f:
                f.write(content)
            print(f"Fixed UserActionLogInfo.cs")
            return True
        except Exception as e:
            print(f"Error fixing {file_path}: {e}")
    
    return False

def fix_prescription_info():
    """修复PrescriptionInfo.cs"""
    file_path = Path("src/Frontend/Desktop/Core/Models/Prescriptions/PrescriptionInfo.cs")
    
    if file_path.exists():
        try:
            with open(file_path, 'r', encoding='utf-8') as f:
                content = f.read()
            
            # 简化PrescriptionStatus只有Draft和Completed
            # 将所有其他状态映射到这两个状态
            replacements = [
                ('PrescriptionStatus.Issued', 'PrescriptionStatus.Completed'),
                ('PrescriptionStatus.Confirmed', 'PrescriptionStatus.Completed'),
                ('PrescriptionStatus.Dispensed', 'PrescriptionStatus.Completed'),
                ('PrescriptionStatus.Cancelled', 'PrescriptionStatus.Draft'),
                ('PrescriptionStatus.Voided', 'PrescriptionStatus.Draft'),
            ]
            
            for old, new in replacements:
                content = content.replace(old, new)
            
            with open(file_path, 'w', encoding='utf-8') as f:
                f.write(content)
            print(f"Fixed PrescriptionInfo.cs")
            return True
        except Exception as e:
            print(f"Error fixing {file_path}: {e}")
    
    return False

def main():
    print("Running final frontend fixes...")
    
    # 1. 修复枚举转换器
    fixed_converters = fix_enum_converters()
    print(f"Fixed EnumConverters: {fixed_converters}")
    
    # 2. 修复UserInfo
    fixed_user = fix_user_info()
    print(f"Fixed UserInfo: {fixed_user}")
    
    # 3. 修复LogInfo
    fixed_log = fix_log_info()
    print(f"Fixed LogInfo: {fixed_log}")
    
    # 4. 修复UserActionLogInfo
    fixed_action_log = fix_user_action_log()
    print(f"Fixed UserActionLogInfo: {fixed_action_log}")
    
    # 5. 修复PrescriptionInfo
    fixed_prescription = fix_prescription_info()
    print(f"Fixed PrescriptionInfo: {fixed_prescription}")
    
    print("\n=== Summary ===")
    total_fixed = sum([fixed_converters, fixed_user, fixed_log, 
                      fixed_action_log, fixed_prescription])
    print(f"Total files fixed: {total_fixed}")

if __name__ == "__main__":
    main()
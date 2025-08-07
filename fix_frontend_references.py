#!/usr/bin/env python3
"""
修复前端中已删除模块的引用
"""

import os
import re
from pathlib import Path

def remove_unused_controls():
    """删除未使用模块的控件文件"""
    controls_to_remove = [
        "src/Frontend/Desktop/Core/Controls/Billing",
        "src/Frontend/Desktop/Core/Controls/DiagnosisTreatment", 
        "src/Frontend/Desktop/Core/Controls/Doctors",
        "src/Frontend/Desktop/Core/Controls/Pharmacy",
        "src/Frontend/Desktop/Core/Controls/Queueing",
        "src/Frontend/Desktop/Core/Controls/Records",
        "src/Frontend/Desktop/Core/Controls/Registration",
        "src/Frontend/Desktop/Core/Controls/Sync",
        "src/Frontend/Desktop/Core/Controls/TreatmentRoom"
    ]
    
    removed_count = 0
    for control_dir in controls_to_remove:
        control_path = Path(control_dir)
        if control_path.exists():
            # 删除目录及其内容
            import shutil
            shutil.rmtree(control_path)
            print(f"Removed: {control_dir}")
            removed_count += 1
    
    return removed_count

def fix_model_references():
    """修复模型引用"""
    models_dir = Path("src/Frontend/Desktop/Core/Models")
    
    # 删除未使用的模型目录
    models_to_remove = [
        "Billing",
        "DiagnosisTreatment",
        "Pharmacy",
        "Queueing",
        "Records",
        "Registration",
        "Sync",
        "TreatmentRoom"
    ]
    
    removed_count = 0
    for model_dir in models_to_remove:
        model_path = models_dir / model_dir
        if model_path.exists():
            import shutil
            shutil.rmtree(model_path)
            print(f"Removed model directory: {model_path}")
            removed_count += 1
    
    return removed_count

def fix_interface_references():
    """修复接口引用"""
    interfaces_dir = Path("src/Frontend/Desktop/Core/Interfaces/Services")
    
    # 删除未使用的接口文件
    interfaces_to_remove = [
        "IBillingService.cs",
        "IRecordService.cs",
        "IRegistrationService.cs"
    ]
    
    removed_count = 0
    for interface_file in interfaces_to_remove:
        interface_path = interfaces_dir / interface_file
        if interface_path.exists():
            interface_path.unlink()
            print(f"Removed interface: {interface_path}")
            removed_count += 1
    
    return removed_count

def fix_user_role_references():
    """修复UserRole引用"""
    # UserRole已经不存在，需要修改相关文件
    files_to_fix = [
        "src/Frontend/Desktop/Core/Interfaces/Services/IPermissionService.cs",
        "src/Frontend/Desktop/Core/Interfaces/Services/IUserService.cs",
        "src/Frontend/Desktop/Core/Interfaces/Services/IUserSessionManager.cs",
        "src/Frontend/Desktop/Core/Models/Roles/RolePermissionInfo.cs"
    ]
    
    fixed_count = 0
    for file_path in files_to_fix:
        path = Path(file_path)
        if path.exists():
            try:
                with open(path, 'r', encoding='utf-8') as f:
                    content = f.read()
                
                original_content = content
                
                # 替换UserRole为string
                content = re.sub(r'\bUserRole\b', 'string', content)
                
                if content != original_content:
                    with open(path, 'w', encoding='utf-8') as f:
                        f.write(content)
                    print(f"Fixed UserRole references in: {path}")
                    fixed_count += 1
            except Exception as e:
                print(f"Error fixing {path}: {e}")
    
    return fixed_count

def fix_patient_service():
    """修复PatientService中的Records引用"""
    patient_service_path = Path("src/Frontend/Desktop/Core/Interfaces/Services/IPatientService.cs")
    
    if patient_service_path.exists():
        try:
            with open(patient_service_path, 'r', encoding='utf-8') as f:
                content = f.read()
            
            # 删除Records相关的using和方法
            content = re.sub(r'using LYBT\.Shared\.Models\.Contracts\.Records;.*\n', '', content)
            content = re.sub(r'.*Task<List<RecordDto>>.*\n', '', content)
            
            with open(patient_service_path, 'w', encoding='utf-8') as f:
                f.write(content)
            print(f"Fixed IPatientService")
            return 1
        except Exception as e:
            print(f"Error fixing IPatientService: {e}")
    
    return 0

def fix_log_models():
    """修复日志模型引用"""
    log_files = [
        "src/Frontend/Desktop/Core/Models/Logs/LogInfo.cs",
        "src/Frontend/Desktop/Core/Models/Logs/SystemLogQueryInfo.cs",
        "src/Frontend/Desktop/Core/Models/Logs/UserActionLogInfo.cs"
    ]
    
    fixed_count = 0
    for file_path in log_files:
        path = Path(file_path)
        if path.exists():
            try:
                with open(path, 'r', encoding='utf-8') as f:
                    content = f.read()
                
                original_content = content
                
                # 删除Infrastructure.Logging.Enums引用
                content = re.sub(r'using LYBT\.Infrastructure\.Logging\.Enums;.*\n', '', content)
                
                # 替换LogLevel和LogActionType为string或int
                content = re.sub(r'\bLogLevel\b', 'string', content)
                content = re.sub(r'\bLogActionType\b', 'string', content)
                
                if content != original_content:
                    with open(path, 'w', encoding='utf-8') as f:
                        f.write(content)
                    print(f"Fixed log model: {path}")
                    fixed_count += 1
            except Exception as e:
                print(f"Error fixing {path}: {e}")
    
    return fixed_count

def main():
    print("Starting frontend reference fixes...")
    
    # 1. 删除未使用的控件
    removed_controls = remove_unused_controls()
    print(f"\nRemoved {removed_controls} control directories")
    
    # 2. 删除未使用的模型
    removed_models = fix_model_references()
    print(f"\nRemoved {removed_models} model directories")
    
    # 3. 删除未使用的接口
    removed_interfaces = fix_interface_references()
    print(f"\nRemoved {removed_interfaces} interface files")
    
    # 4. 修复UserRole引用
    fixed_user_roles = fix_user_role_references()
    print(f"\nFixed {fixed_user_roles} UserRole references")
    
    # 5. 修复PatientService
    fixed_patient_service = fix_patient_service()
    print(f"\nFixed {fixed_patient_service} patient service file")
    
    # 6. 修复日志模型
    fixed_log_models = fix_log_models()
    print(f"\nFixed {fixed_log_models} log model files")
    
    print("\n=== Summary ===")
    print(f"Total removed: {removed_controls + removed_models + removed_interfaces}")
    print(f"Total fixed: {fixed_user_roles + fixed_patient_service + fixed_log_models}")

if __name__ == "__main__":
    main()
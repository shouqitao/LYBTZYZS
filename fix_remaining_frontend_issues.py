#!/usr/bin/env python3
"""
修复前端剩余的编译问题
"""

import os
import re
from pathlib import Path

def fix_pharmacy_service():
    """删除IPharmacyService接口"""
    file_path = Path("src/Frontend/Desktop/Core/Interfaces/Services/IPharmacyService.cs")
    if file_path.exists():
        file_path.unlink()
        print(f"Removed: {file_path}")
        return True
    return False

def fix_physiotherapy_service():
    """删除IPhysiotherapyService接口"""
    file_path = Path("src/Frontend/Desktop/Core/Interfaces/Services/IPhysiotherapyService.cs")
    if file_path.exists():
        file_path.unlink()
        print(f"Removed: {file_path}")
        return True
    return False

def fix_role_navigation_config():
    """修复RoleNavigationConfig中的UserRole引用"""
    file_path = Path("src/Frontend/Desktop/Core/Configuration/RoleNavigationConfig.cs")
    
    if file_path.exists():
        try:
            with open(file_path, 'r', encoding='utf-8') as f:
                content = f.read()
            
            # 替换UserRole为string
            content = re.sub(r'\bUserRole\b', 'string', content)
            
            # 替换枚举值为字符串
            content = content.replace('string.Admin', '"Admin"')
            content = content.replace('string.Doctor', '"Doctor"')
            content = content.replace('string.Nurse', '"Nurse"')
            content = content.replace('string.Pharmacist', '"Pharmacist"')
            content = content.replace('string.FrontDesk', '"FrontDesk"')
            content = content.replace('string.Finance', '"Finance"')
            content = content.replace('string.Cashier', '"Cashier"')
            
            with open(file_path, 'w', encoding='utf-8') as f:
                f.write(content)
            print(f"Fixed: {file_path}")
            return True
        except Exception as e:
            print(f"Error fixing {file_path}: {e}")
    
    return False

def fix_patient_service_records():
    """修复IPatientService中的Records引用"""
    file_path = Path("src/Frontend/Desktop/Core/Interfaces/Services/IPatientService.cs")
    
    if file_path.exists():
        try:
            with open(file_path, 'r', encoding='utf-8') as f:
                lines = f.readlines()
            
            # 过滤掉包含RecordDto的行
            filtered_lines = [line for line in lines if 'RecordDto' not in line]
            
            with open(file_path, 'w', encoding='utf-8') as f:
                f.writelines(filtered_lines)
            print(f"Fixed IPatientService (removed RecordDto references)")
            return True
        except Exception as e:
            print(f"Error fixing {file_path}: {e}")
    
    return False

def fix_formula_template_model():
    """修复FormulaTemplateInfo模型"""
    file_path = Path("src/Frontend/Desktop/Core/Models/FormulaTemplates/FormulaTemplateInfo.cs")
    
    if file_path.exists():
        try:
            with open(file_path, 'r', encoding='utf-8') as f:
                content = f.read()
            
            # 替换BaseFormulaTemplateModel为BaseFormulaModel
            content = content.replace('BaseFormulaTemplateModel', 'BaseFormulaModel')
            
            # 确保有正确的using语句
            if 'using LYBT.Shared.Models.Core;' not in content:
                # 在namespace之前添加using
                content = re.sub(
                    r'(using System.*?\n)',
                    r'\1using LYBT.Shared.Models.Core;\n',
                    content,
                    count=1
                )
            
            with open(file_path, 'w', encoding='utf-8') as f:
                f.write(content)
            print(f"Fixed FormulaTemplateInfo model")
            return True
        except Exception as e:
            print(f"Error fixing {file_path}: {e}")
    
    return False

def remove_sync_models():
    """删除Sync模型目录"""
    sync_dir = Path("src/Frontend/Desktop/Core/Models/Sync")
    if sync_dir.exists():
        import shutil
        shutil.rmtree(sync_dir)
        print(f"Removed: {sync_dir}")
        return True
    return False

def cleanup_obj_folders():
    """清理obj文件夹"""
    base_dirs = [
        "src/Frontend/Desktop/Core",
        "src/Frontend/Desktop/Modules",
        "src/Frontend/Desktop/Shell"
    ]
    
    cleaned = 0
    for base_dir in base_dirs:
        obj_dir = Path(base_dir) / "obj"
        if obj_dir.exists():
            import shutil
            shutil.rmtree(obj_dir)
            print(f"Cleaned: {obj_dir}")
            cleaned += 1
    
    return cleaned

def main():
    print("Fixing remaining frontend issues...")
    
    # 1. 删除未使用的服务接口
    removed_pharmacy = fix_pharmacy_service()
    removed_physiotherapy = fix_physiotherapy_service()
    print(f"Removed services: Pharmacy={removed_pharmacy}, Physiotherapy={removed_physiotherapy}")
    
    # 2. 修复角色导航配置
    fixed_nav = fix_role_navigation_config()
    print(f"Fixed RoleNavigationConfig: {fixed_nav}")
    
    # 3. 修复患者服务
    fixed_patient = fix_patient_service_records()
    print(f"Fixed PatientService: {fixed_patient}")
    
    # 4. 修复验方模板模型
    fixed_formula = fix_formula_template_model()
    print(f"Fixed FormulaTemplate: {fixed_formula}")
    
    # 5. 删除Sync模型
    removed_sync = remove_sync_models()
    print(f"Removed Sync models: {removed_sync}")
    
    # 6. 清理obj文件夹
    cleaned = cleanup_obj_folders()
    print(f"Cleaned {cleaned} obj folders")
    
    print("\n=== Summary ===")
    total_fixed = sum([removed_pharmacy, removed_physiotherapy, fixed_nav, 
                      fixed_patient, fixed_formula, removed_sync])
    print(f"Total fixes: {total_fixed}")
    print(f"Obj folders cleaned: {cleaned}")

if __name__ == "__main__":
    main()
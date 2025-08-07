#!/usr/bin/env python3
"""
批量修复LYBTZYZS项目的编译错误
"""

import os
import re
import glob

def fix_gender_conversion(file_path):
    """修复Gender类型转换问题"""
    with open(file_path, 'r', encoding='utf-8') as f:
        content = f.read()
    
    # 修复Gender = 1 或 Gender = 0 的问题
    content = re.sub(r'Gender\s*=\s*1([,\s\)])', r'Gender = (Gender)1\1', content)
    content = re.sub(r'Gender\s*=\s*0([,\s\)])', r'Gender = (Gender)0\1', content)
    
    # 修复Gender == 1的比较
    content = re.sub(r'\.Gender\s*==\s*1', r'.Gender == Gender.Male', content)
    content = re.sub(r'\.Gender\s*==\s*0', r'.Gender == Gender.Female', content)
    
    with open(file_path, 'w', encoding='utf-8') as f:
        f.write(content)
    
    return True

def fix_formula_template_references(file_path):
    """修复FormulaTemplate引用为Formula"""
    with open(file_path, 'r', encoding='utf-8') as f:
        content = f.read()
    
    # 批量替换FormulaTemplate相关引用
    replacements = [
        ('FormulaTemplateInfo', 'FormulaInfo'),
        ('FormulaTemplateDto', 'FormulaDto'),
        ('FormulaTemplateCreateDto', 'FormulaCreateDto'),
        ('FormulaTemplateUpdateDto', 'FormulaUpdateDto'),
        ('FormulaTemplateHerbItem', 'FormulaHerbItem'),
        ('FormulaTemplateManagementViewModel', 'FormulaManagementViewModel'),
        ('FormulaTemplateManagementView', 'FormulaManagementView'),
        ('IFormulaTemplateService', 'IFormulaService'),
        ('FormulaTemplateService', 'FormulaService'),
    ]
    
    modified = False
    for old, new in replacements:
        if old in content:
            content = content.replace(old, new)
            modified = True
    
    if modified:
        with open(file_path, 'w', encoding='utf-8') as f:
            f.write(content)
    
    return modified

def add_missing_prescription_status_values():
    """添加缺失的PrescriptionStatus枚举值"""
    enum_file = r'D:\source\repos\LYBTZYZS\src\Shared\LYBT.Shared.Models\Enums\PrescriptionStatus.cs'
    
    if os.path.exists(enum_file):
        with open(enum_file, 'r', encoding='utf-8') as f:
            content = f.read()
        
        # 检查是否缺少某些枚举值
        missing_values = []
        if 'Issued' not in content:
            missing_values.append('        Issued = 1,')
        if 'Confirmed' not in content:
            missing_values.append('        Confirmed = 2,')
        if 'Dispensed' not in content:
            missing_values.append('        Dispensed = 3,')
        if 'Cancelled' not in content:
            missing_values.append('        Cancelled = 10,')
        if 'Voided' not in content:
            missing_values.append('        Voided = 11,')
        
        if missing_values:
            # 在枚举定义中添加缺失的值
            lines = content.split('\n')
            for i, line in enumerate(lines):
                if 'enum PrescriptionStatus' in line:
                    # 找到枚举开始的位置
                    for j in range(i+1, len(lines)):
                        if '{' in lines[j]:
                            # 在大括号后插入缺失的值
                            lines[j] = lines[j] + '\n' + '\n'.join(missing_values)
                            break
                    break
            
            content = '\n'.join(lines)
            with open(enum_file, 'w', encoding='utf-8') as f:
                f.write(content)
            
            print(f"已添加缺失的PrescriptionStatus枚举值: {', '.join([v.split('=')[0].strip() for v in missing_values])}")
            return True
    
    return False

def fix_consultation_view_model():
    """修复ConsultationMainViewModel中的问题"""
    file_path = r'D:\source\repos\LYBTZYZS\src\Frontend\Desktop\Modules\Consultation\ViewModels\ConsultationMainViewModel.cs'
    
    if os.path.exists(file_path):
        with open(file_path, 'r', encoding='utf-8') as f:
            content = f.read()
        
        # 修复Gender类型转换
        content = re.sub(r'Gender = 1([,\s\)])', r'Gender = (Gender)1\1', content)
        content = re.sub(r'Gender = 0([,\s\)])', r'Gender = (Gender)0\1', content)
        content = re.sub(r'Gender == 1', r'Gender == Gender.Male', content)
        
        # 添加缺失的using语句
        if 'using LYBT.Shared.Models.Enums;' not in content:
            content = 'using LYBT.Shared.Models.Enums;\n' + content
        
        with open(file_path, 'w', encoding='utf-8') as f:
            f.write(content)
        
        print(f"已修复ConsultationMainViewModel中的Gender类型问题")
        return True
    
    return False

def main():
    """主函数"""
    print("开始批量修复编译错误...")
    
    # 1. 修复Gender类型转换问题
    print("\n1. 修复Gender类型转换问题...")
    fix_consultation_view_model()
    
    # 2. 修复FormulaTemplate引用
    print("\n2. 批量重命名FormulaTemplate为Formula...")
    cs_files = glob.glob(r'D:\source\repos\LYBTZYZS\src\Frontend\Desktop\**\*.cs', recursive=True)
    cs_files.extend(glob.glob(r'D:\source\repos\LYBTZYZS\src\Frontend\Desktop\**\*.xaml', recursive=True))
    
    fixed_count = 0
    for file_path in cs_files:
        if fix_formula_template_references(file_path):
            fixed_count += 1
    
    print(f"已修复 {fixed_count} 个文件中的FormulaTemplate引用")
    
    # 3. 添加缺失的枚举值
    print("\n3. 添加缺失的PrescriptionStatus枚举值...")
    add_missing_prescription_status_values()
    
    print("\n修复完成！请重新编译项目查看剩余错误。")
    print("\n注意：仍有一些错误需要手动修复：")
    print("- HerbInfo和UserInfo缺少的属性需要在模型定义中添加")
    print("- 一些服务接口需要定义或简化")
    print("- XAML绑定错误需要逐个检查")

if __name__ == '__main__':
    main()
#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
批量将FormulaTemplate重命名为Formula
"""

import os
import re

def replace_in_file(file_path):
    """替换文件中的FormulaTemplate为Formula"""
    try:
        with open(file_path, 'r', encoding='utf-8') as f:
            content = f.read()
    except:
        try:
            with open(file_path, 'r', encoding='gbk') as f:
                content = f.read()
        except:
            print(f"[ERROR] Cannot read file: {file_path}")
            return False
    
    original_content = content
    
    # 替换各种形式的FormulaTemplate
    replacements = [
        ('FormulaTemplateInfo', 'FormulaInfo'),
        ('FormulaTemplateService', 'FormulaService'),
        ('FormulaTemplateController', 'FormulaController'),
        ('FormulaTemplateDto', 'FormulaDto'),
        ('FormulaTemplateCreateDto', 'FormulaCreateDto'),
        ('FormulaTemplateUpdateDto', 'FormulaUpdateDto'),
        ('FormulaTemplateDetailDto', 'FormulaDetailDto'),
        ('IFormulaTemplateService', 'IFormulaService'),
        ('IFormulaTemplateRepository', 'IFormulaRepository'),
        ('formulaTemplate', 'formula'),
        ('FormulaTemplate', 'Formula'),  # 通用替换
    ]
    
    for old, new in replacements:
        content = content.replace(old, new)
    
    if content != original_content:
        try:
            with open(file_path, 'w', encoding='utf-8') as f:
                f.write(content)
            print(f"[OK] Updated: {file_path}")
            return True
        except Exception as e:
            print(f"[ERROR] Write failed {file_path}: {e}")
            return False
    return False

def find_files_with_formula_template():
    """查找包含FormulaTemplate的文件"""
    files_to_update = []
    
    # 搜索src目录
    src_dir = r"D:\source\repos\LYBTZYZS\src"
    
    for root, dirs, files in os.walk(src_dir):
        # 跳过bin、obj、BIN目录
        dirs[:] = [d for d in dirs if d not in ['bin', 'obj', 'BIN', '.git']]
        
        for file in files:
            # 只处理代码文件
            if file.endswith(('.cs', '.csproj', '.xaml', '.json')):
                file_path = os.path.join(root, file)
                
                # 检查文件是否包含FormulaTemplate
                try:
                    with open(file_path, 'r', encoding='utf-8') as f:
                        content = f.read()
                        if 'FormulaTemplate' in content:
                            files_to_update.append(file_path)
                except:
                    pass
    
    return files_to_update

def main():
    print("Starting FormulaTemplate -> Formula renaming...")
    print("="*60)
    
    # 查找需要更新的文件
    files_to_update = find_files_with_formula_template()
    
    print(f"Found {len(files_to_update)} files containing 'FormulaTemplate'")
    
    if len(files_to_update) > 0:
        print("\nFiles to update:")
        for f in files_to_update[:10]:  # 只显示前10个
            print(f"  - {f}")
        if len(files_to_update) > 10:
            print(f"  ... and {len(files_to_update) - 10} more files")
        
        print("\nProcessing files...")
        updated_count = 0
        for file_path in files_to_update:
            if replace_in_file(file_path):
                updated_count += 1
        
        print(f"\nCompleted! Updated {updated_count} files")
    else:
        print("\nNo files need updating")

if __name__ == "__main__":
    main()
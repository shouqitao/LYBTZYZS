#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
重命名FormulaTemplateInfo为FormulaInfo
"""

import os
import re

def replace_in_file(file_path):
    """替换文件中的FormulaTemplateInfo为FormulaInfo"""
    try:
        with open(file_path, 'r', encoding='utf-8') as f:
            content = f.read()
    except:
        try:
            with open(file_path, 'r', encoding='gbk') as f:
                content = f.read()
        except:
            print(f"无法读取文件: {file_path}")
            return False
    
    original_content = content
    
    # 替换FormulaTemplateInfo为FormulaInfo
    content = content.replace('FormulaTemplateInfo', 'FormulaInfo')
    
    if content != original_content:
        try:
            with open(file_path, 'w', encoding='utf-8') as f:
                f.write(content)
            print(f"[OK] 更新: {file_path}")
            return True
        except Exception as e:
            print(f"[ERROR] 写入失败 {file_path}: {e}")
            return False
    return False

def main():
    print("开始重命名FormulaTemplateInfo为FormulaInfo...")
    
    # 需要更新的文件列表
    files_to_update = [
        r"src\Frontend\Desktop\Services\FormulaTemplateService.cs",
        r"src\Frontend\Desktop\Modules\SystemManagement\FormulaTemplates\ViewModels\FormulaTemplateManagementViewModel.cs",
        r"src\Frontend\Desktop\Core\Interfaces\Services\IFormulaTemplateService.cs"
    ]
    
    updated_count = 0
    for file_path in files_to_update:
        full_path = os.path.join(r"D:\source\repos\LYBTZYZS", file_path)
        if os.path.exists(full_path):
            if replace_in_file(full_path):
                updated_count += 1
        else:
            print(f"[ERROR] 文件不存在: {full_path}")
    
    print(f"\n完成！更新了 {updated_count} 个文件")

if __name__ == "__main__":
    main()
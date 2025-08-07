#!/usr/bin/env python3
"""
修复FormulaHerbItem属性引用问题
移除对不存在的Remark和SortOrder属性的引用
"""

import os

def fix_formula_herb_item_references():
    """修复FormulaHerbItem的Remark和SortOrder属性引用"""
    
    files_to_fix = [
        r"D:\source\repos\LYBTZYZS\src\Frontend\Desktop\Modules\SystemManagement\Formulas\ViewModels\ViewFormulaDialogViewModel.cs",
        r"D:\source\repos\LYBTZYZS\src\Frontend\Desktop\Modules\SystemManagement\Formulas\ViewModels\EditFormulaDialogViewModel.cs",
        r"D:\source\repos\LYBTZYZS\src\Frontend\Desktop\Modules\SystemManagement\Formulas\ViewModels\AddFormulaDialogViewModel.cs"
    ]
    
    fixed_count = 0
    
    for file_path in files_to_fix:
        if not os.path.exists(file_path):
            print(f"文件不存在: {file_path}")
            continue
            
        with open(file_path, 'r', encoding='utf-8') as f:
            content = f.read()
        
        original_content = content
        
        # 移除Remark和SortOrder属性引用
        # 处理多行赋值语句
        lines = content.split('\n')
        new_lines = []
        skip_next = False
        
        for i, line in enumerate(lines):
            if skip_next:
                skip_next = False
                continue
                
            # 检查是否是Remark或SortOrder的赋值行
            if 'Remark = herb.Remark' in line or 'Remark = h.Remark' in line:
                # 检查是否有逗号结尾
                if line.strip().endswith(','):
                    continue  # 跳过这一行
                elif i + 1 < len(lines) and 'SortOrder = herb.SortOrder' in lines[i + 1]:
                    skip_next = True  # 也跳过下一行
                    continue
                else:
                    continue  # 跳过这一行
            elif 'SortOrder = herb.SortOrder' in line:
                continue  # 跳过这一行
            elif 'TemplateHerbs[i].SortOrder = i;' in line:
                # 移除对SortOrder的赋值
                continue
            else:
                new_lines.append(line)
        
        content = '\n'.join(new_lines)
        
        # 处理template.Remark的引用（这些是合法的，因为template有Remark属性）
        # 不需要修改
        
        if content != original_content:
            with open(file_path, 'w', encoding='utf-8') as f:
                f.write(content)
            print(f"Fixed: {file_path}")
            fixed_count += 1
        else:
            print(f"Skipped: {file_path} (no changes needed)")
    
    print(f"\n总共修复了 {fixed_count} 个文件")

if __name__ == "__main__":
    fix_formula_herb_item_references()
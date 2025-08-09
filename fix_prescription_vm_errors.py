import os
import re

# 修复PrescriptionViewModel中的语法错误
prescription_vm_file = r"D:\source\repos\LYBTZYZS\src\Frontend\Desktop\Modules\Consultation\ViewModels\PrescriptionViewModel.cs"

try:
    with open(prescription_vm_file, 'r', encoding='utf-8') as f:
        content = f.read()
    
    # 修复SaveAsync方法中的错误语法
    # 第349行的错误 - 移除多余的花括号和属性赋值
    content = re.sub(
        r'Items = PrescriptionItems\.Select\(item => item\.GetModel\(\)\)\.ToList\(\)\s*\{[^}]*\}[^,]*,',
        r'Items = PrescriptionItems.Select(item => item.GetModel()).ToList(),',
        content,
        flags=re.DOTALL
    )
    
    # 修复GetPrescriptionData方法中的相同错误
    content = re.sub(
        r'Items = PrescriptionItems\.Select\(item => item\.GetModel\(\)\)\.ToList\(\)\s*\{[^}]*\}[^,]*,',
        r'Items = PrescriptionItems.Select(item => item.GetModel()).ToList(),',
        content,
        flags=re.DOTALL
    )
    
    # 修复LoadExistingDataAsync中创建PrescriptionItemViewModel的代码
    pattern = r'PrescriptionItems\.Add\(new PrescriptionItem\s*\{([^}]*)\}\)'
    def replace_prescription_item(match):
        props = match.group(1)
        return f'''PrescriptionItems.Add(new PrescriptionItemViewModel(new PrescriptionItem
                        {{
{props}
                        }}))'''
    
    content = re.sub(pattern, replace_prescription_item, content, flags=re.DOTALL)
    
    # 修复AddHerbItems方法中添加PrescriptionItemViewModel的代码
    pattern2 = r'PrescriptionItems\.Add\(new PrescriptionItem\s*\{([^}]*)\}\)'
    content = re.sub(pattern2, replace_prescription_item, content, flags=re.DOTALL)
    
    # 修复ImportFormulaItems方法中添加PrescriptionItemViewModel的代码
    pattern3 = r'PrescriptionItems\.Add\(new PrescriptionItem\s*\{([^}]*)\}\)'
    content = re.sub(pattern3, replace_prescription_item, content, flags=re.DOTALL)
    
    # 移除重复的类定义 - 删除第817-824行的重复内容
    lines = content.split('\n')
    filtered_lines = []
    skip_start = False
    skip_count = 0
    
    for i, line in enumerate(lines):
        # 检测重复的HerbName定义开始
        if 'public string HerbName { get; set; } = "";' in line and i > 810:
            skip_start = True
            skip_count = 0
        
        if skip_start:
            skip_count += 1
            if skip_count > 8:  # 跳过大约8行重复内容
                skip_start = False
            continue
        
        filtered_lines.append(line)
    
    content = '\n'.join(filtered_lines)
    
    with open(prescription_vm_file, 'w', encoding='utf-8') as f:
        f.write(content)
    
    print(f"Fixed syntax errors in PrescriptionViewModel")
    
except Exception as e:
    print(f"Error fixing PrescriptionViewModel: {e}")
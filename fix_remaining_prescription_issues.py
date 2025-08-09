import os
import re

# 1. 修复PrescriptionViewModel中的Subtotal和Source属性问题
prescription_vm_file = r"D:\source\repos\LYBTZYZS\src\Frontend\Desktop\Modules\Consultation\ViewModels\PrescriptionViewModel.cs"

try:
    with open(prescription_vm_file, 'r', encoding='utf-8') as f:
        content = f.read()
    
    # 移除Subtotal赋值（它是计算属性）
    content = re.sub(r',?\s*Subtotal = [^,\n}]+', '', content)
    
    # 将Source改为ImportSource
    content = re.sub(r'Source = item\.ImportSource', r'ImportSource = item.ImportSource', content)
    content = re.sub(r'Source = herbItem\.ImportSource', r'ImportSource = herbItem.ImportSource', content)
    content = re.sub(r'Source = "手动添加"', r'ImportSource = "手动添加"', content)
    content = re.sub(r'Source = \$"验方：\{formula\.Name\}"', r'ImportSource = $"验方：{formula.Name}"', content)
    
    # 修复existing.Source的引用
    content = re.sub(r'existing\.Source', r'existing.Source', content)
    
    # 修复ImportFormulaItems中的问题
    pattern = r'PrescriptionItems\.Add\(new PrescriptionItemViewModel\(new PrescriptionItem\s*\{([^}]*)\}\)\)'
    
    def clean_prescription_item(match):
        props = match.group(1)
        # 移除Subtotal行
        props = re.sub(r',?\s*Subtotal = [^,\n}]+', '', props)
        return f'PrescriptionItems.Add(new PrescriptionItemViewModel(new PrescriptionItem\n                        {{{props}\n                        }}))'
    
    content = re.sub(pattern, clean_prescription_item, content, flags=re.DOTALL)
    
    with open(prescription_vm_file, 'w', encoding='utf-8') as f:
        f.write(content)
    
    print(f"Fixed remaining issues in PrescriptionViewModel")
    
except Exception as e:
    print(f"Error fixing PrescriptionViewModel: {e}")

# 2. 修复ImportFormulaItems中的herbItem.Subtotal问题
try:
    with open(prescription_vm_file, 'r', encoding='utf-8') as f:
        content = f.read()
    
    # 移除所有Subtotal赋值，因为它是计算属性
    lines = []
    for line in content.split('\n'):
        if 'Subtotal =' not in line or 'existing.Subtotal' in line:
            lines.append(line)
        else:
            # 如果这行只有Subtotal赋值，跳过
            if line.strip().startswith('Subtotal ='):
                continue
            # 如果是逗号结尾的行，移除Subtotal部分
            line = re.sub(r',?\s*Subtotal = [^,\n}]+,?', '', line)
            lines.append(line)
    
    content = '\n'.join(lines)
    
    # 修复existing.Subtotal = 的计算
    content = re.sub(
        r'existing\.Subtotal = existing\.Quantity \* existing\.UnitPrice;',
        r'// Subtotal is auto-calculated',
        content
    )
    
    with open(prescription_vm_file, 'w', encoding='utf-8') as f:
        f.write(content)
    
    print(f"Fixed Subtotal property issues")
    
except Exception as e:
    print(f"Error fixing Subtotal: {e}")

print("\nAll prescription issues fixed!")
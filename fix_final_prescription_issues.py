import os
import re

prescription_vm_file = r"D:\source\repos\LYBTZYZS\src\Frontend\Desktop\Modules\Consultation\ViewModels\PrescriptionViewModel.cs"

try:
    with open(prescription_vm_file, 'r', encoding='utf-8') as f:
        lines = f.readlines()
    
    # 修复有问题的行
    for i in range(len(lines)):
        # 删除孤立的 existing. 行
        if lines[i].strip() == 'existing.':
            lines[i] = ''
        
        # 修复第589行的问题 - 应该是 PrescriptionItemViewModel
        if 'PrescriptionItems.Add(new PrescriptionItem' in lines[i] and 'PrescriptionItemViewModel' not in lines[i]:
            lines[i] = lines[i].replace('new PrescriptionItem', 'new PrescriptionItemViewModel(new PrescriptionItem')
            # 找到对应的结束括号并添加额外的括号
            for j in range(i+1, min(i+20, len(lines))):
                if '});' in lines[j]:
                    lines[j] = lines[j].replace('});', '}));')
                    break
        
        # 修复空的花括号内容
        if i > 0 and lines[i-1].strip().endswith('{') and lines[i].strip() == '}':
            # 检查是否是第815-817行的情况
            if 'if (SetProperty(ref _quantity, value))' in lines[i-2]:
                lines[i-1] = '                    // Quantity changed, Subtotal will auto-update\n'
    
    # 修复PrescriptionItemViewModel类中的问题
    content = ''.join(lines)
    
    # 移除重复的_quantity定义
    content = re.sub(
        r'private decimal _quantity;\s*public decimal Quantity\s*\{\s*get => _quantity;[^}]*\}',
        '''public decimal Quantity
        {
            get => _item.Quantity;
            set
            {
                _item.Quantity = value;
                RaisePropertyChanged();
                RaisePropertyChanged(nameof(Subtotal));
            }
        }''',
        content,
        flags=re.DOTALL
    )
    
    # 移除多余的_subtotal定义
    content = re.sub(
        r'private decimal _subtotal;\s*public decimal Subtotal[^}]*\}',
        'public decimal Subtotal => _item.Subtotal;',
        content
    )
    
    # 确保PrescriptionItemViewModel的结束花括号正确
    # 找到类的开始和结束
    lines = content.split('\n')
    in_prescription_item_vm = False
    brace_count = 0
    fixed_lines = []
    
    for line in lines:
        if 'public class PrescriptionItemViewModel : BindableBase' in line:
            in_prescription_item_vm = True
            brace_count = 0
        
        if in_prescription_item_vm:
            if '{' in line:
                brace_count += line.count('{')
            if '}' in line:
                brace_count -= line.count('}')
            
            # 如果这是类的结束
            if brace_count == 0 and '}' in line:
                in_prescription_item_vm = False
                # 确保下一行不是错误的属性定义
                fixed_lines.append(line)
                continue
        
        # 跳过类外部的错误属性定义
        if not in_prescription_item_vm and 'public string HerbName { get; set; } = "";' in line:
            continue
        if not in_prescription_item_vm and 'public string Unit { get; set; } = "g";' in line:
            continue
        if not in_prescription_item_vm and 'public decimal UnitPrice { get; set; }' in line:
            continue
        
        fixed_lines.append(line)
    
    content = '\n'.join(fixed_lines)
    
    with open(prescription_vm_file, 'w', encoding='utf-8') as f:
        f.write(content)
    
    print(f"Fixed final issues in PrescriptionViewModel")
    
except Exception as e:
    print(f"Error: {e}")

print("\nAll prescription issues should be fixed now!")
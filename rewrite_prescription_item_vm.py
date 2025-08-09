import os
import re

prescription_vm_file = r"D:\source\repos\LYBTZYZS\src\Frontend\Desktop\Modules\Consultation\ViewModels\PrescriptionViewModel.cs"

try:
    with open(prescription_vm_file, 'r', encoding='utf-8') as f:
        content = f.read()
    
    # 找到PrescriptionItemViewModel类的开始位置
    start_marker = 'public class PrescriptionItemViewModel : BindableBase'
    end_marker = '#endregion'
    
    # 找到类的开始
    start_idx = content.find(start_marker)
    if start_idx == -1:
        print("Could not find PrescriptionItemViewModel class")
        exit(1)
    
    # 找到内部类型区域的结束
    end_idx = content.find(end_marker, start_idx)
    if end_idx == -1:
        print("Could not find #endregion marker")
        exit(1)
    
    # 提取类之前和之后的内容
    before_class = content[:start_idx]
    after_region = content[end_idx:]
    
    # 新的PrescriptionItemViewModel类定义
    new_class = '''public class PrescriptionItemViewModel : BindableBase
        {
            private readonly PrescriptionItem _item;
            
            public PrescriptionItemViewModel(PrescriptionItem item)
            {
                _item = item ?? new PrescriptionItem();
            }
            
            public PrescriptionItemViewModel() : this(new PrescriptionItem())
            {
            }
            
            public Guid HerbId 
            { 
                get => _item.HerbId; 
                set { _item.HerbId = value; RaisePropertyChanged(); }
            }
            
            public string HerbName 
            { 
                get => _item.HerbName; 
                set { _item.HerbName = value; RaisePropertyChanged(); }
            }
            
            public decimal Quantity
            {
                get => _item.Quantity;
                set
                {
                    _item.Quantity = value;
                    RaisePropertyChanged();
                    RaisePropertyChanged(nameof(Subtotal));
                    RaisePropertyChanged(nameof(DisplayText));
                    RaisePropertyChanged(nameof(PriceText));
                }
            }
            
            public string Unit 
            { 
                get => _item.Unit; 
                set { _item.Unit = value; RaisePropertyChanged(); }
            }
            
            public decimal UnitPrice 
            { 
                get => _item.UnitPrice; 
                set 
                { 
                    _item.UnitPrice = value; 
                    RaisePropertyChanged(); 
                    RaisePropertyChanged(nameof(Subtotal));
                    RaisePropertyChanged(nameof(PriceText));
                }
            }
            
            public decimal Subtotal => _item.Subtotal;
            
            public string? Source 
            { 
                get => _item.ImportSource; 
                set { _item.ImportSource = value; RaisePropertyChanged(); }
            }
            
            public string DisplayText => _item.DisplayText;
            public string PriceText => _item.PriceText;
            
            public PrescriptionItem GetModel() => _item;
        }

        '''
    
    # 组合新的文件内容
    new_content = before_class + new_class + after_region
    
    with open(prescription_vm_file, 'w', encoding='utf-8') as f:
        f.write(new_content)
    
    print("Rewrote PrescriptionItemViewModel class successfully")
    
except Exception as e:
    print(f"Error: {e}")
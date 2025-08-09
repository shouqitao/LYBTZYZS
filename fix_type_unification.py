import os
import re

# 1. 创建统一的PrescriptionData和PrescriptionItem在Core.Models
prescription_models_file = r"D:\source\repos\LYBTZYZS\src\Frontend\Desktop\Core\Models\Prescriptions\PrescriptionModels.cs"

prescription_models_content = """using System;
using System.Collections.Generic;

namespace LYBT.WPF.Client.Core.Models.Prescriptions
{
    /// <summary>
    /// 处方数据
    /// </summary>
    public class PrescriptionData
    {
        public List<PrescriptionItem> Items { get; set; } = new();
        public int Dosage { get; set; } = 7;  // 默认7剂
        public string Usage { get; set; } = "每日1剂，水煎服，分早晚两次温服";
        public decimal TotalPrice { get; set; }
        public decimal Discount { get; set; } = 1.0m;
    }

    /// <summary>
    /// 处方项
    /// </summary>
    public class PrescriptionItem
    {
        public Guid HerbId { get; set; }
        public string HerbName { get; set; } = "";
        public decimal Quantity { get; set; }
        public string Unit { get; set; } = "g";
        public decimal UnitPrice { get; set; }
        public string? ImportSource { get; set; }
        
        // 计算属性
        public decimal Subtotal => Quantity * UnitPrice;
        public string DisplayText => $"{HerbName} {Quantity}{Unit}";
        public string PriceText => $"￥{Subtotal:F2}";
    }
}
"""

os.makedirs(os.path.dirname(prescription_models_file), exist_ok=True)
with open(prescription_models_file, 'w', encoding='utf-8') as f:
    f.write(prescription_models_content)
print(f"Created unified PrescriptionModels")

# 2. 修改ConsultationWorkflowViewModel使用统一的类型
consultation_workflow_file = r"D:\source\repos\LYBTZYZS\src\Frontend\Desktop\Modules\Consultation\ViewModels\ConsultationWorkflowViewModel.cs"
try:
    with open(consultation_workflow_file, 'r', encoding='utf-8') as f:
        content = f.read()
    
    # 添加using语句
    if 'using LYBT.WPF.Client.Core.Models.Prescriptions;' not in content:
        # 在其他using后添加
        content = re.sub(
            r'(using LYBT\.WPF\.Client\.Core\.Models\.Consultation;)',
            r'\1\nusing LYBT.WPF.Client.Core.Models.Prescriptions;',
            content
        )
    
    # 修改ConsultationData类使用外部PrescriptionData
    content = re.sub(
        r'public PrescriptionData\? Prescription \{ get; set; \}',
        r'public Core.Models.Prescriptions.PrescriptionData? Prescription { get; set; }',
        content
    )
    
    # 删除内部的PrescriptionData和PrescriptionItem类定义
    # 保留ConsultationData和FourDiagnosisData等其他内部类
    pattern = r'/// <summary>\s*\n\s*/// 处方数据\s*\n\s*/// </summary>\s*\n\s*public class PrescriptionData[^}]*\}\s*\n\s*/// <summary>\s*\n\s*/// 处方项\s*\n\s*/// </summary>\s*\n\s*public class PrescriptionItem[^}]*\}'
    content = re.sub(pattern, '', content, flags=re.DOTALL)
    
    with open(consultation_workflow_file, 'w', encoding='utf-8') as f:
        f.write(content)
    print(f"Updated ConsultationWorkflowViewModel to use unified types")
except Exception as e:
    print(f"Error updating ConsultationWorkflowViewModel: {e}")

# 3. 修改PrescriptionViewModel使用统一的类型
prescription_vm_file = r"D:\source\repos\LYBTZYZS\src\Frontend\Desktop\Modules\Consultation\ViewModels\PrescriptionViewModel.cs"
try:
    with open(prescription_vm_file, 'r', encoding='utf-8') as f:
        content = f.read()
    
    # 添加using语句
    if 'using LYBT.WPF.Client.Core.Models.Prescriptions;' not in content:
        content = re.sub(
            r'(using LYBT\.WPF\.Client\.Core\.Models\.Consultation;)',
            r'\1\nusing LYBT.WPF.Client.Core.Models.Prescriptions;',
            content
        )
    
    # 删除本地PrescriptionItem类定义，使用BindableBase包装器
    # 创建一个PrescriptionItemViewModel来处理UI绑定
    content = re.sub(
        r'public class PrescriptionItem : BindableBase[^}]*\}',
        '''public class PrescriptionItemViewModel : BindableBase
    {
        private readonly PrescriptionItem _item;
        
        public PrescriptionItemViewModel(PrescriptionItem item)
        {
            _item = item ?? new PrescriptionItem();
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
        
        private decimal _quantity;
        public decimal Quantity
        {
            get => _item.Quantity;
            set
            {
                _item.Quantity = value;
                RaisePropertyChanged();
                RaisePropertyChanged(nameof(Subtotal));
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
            set { _item.UnitPrice = value; RaisePropertyChanged(); RaisePropertyChanged(nameof(Subtotal)); }
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
    }''',
        content,
        flags=re.DOTALL
    )
    
    # 修改PrescriptionItems类型
    content = re.sub(
        r'private ObservableCollection<PrescriptionItem> _prescriptionItems',
        r'private ObservableCollection<PrescriptionItemViewModel> _prescriptionItems',
        content
    )
    
    content = re.sub(
        r'public ObservableCollection<PrescriptionItem> PrescriptionItems',
        r'public ObservableCollection<PrescriptionItemViewModel> PrescriptionItems',
        content
    )
    
    # 修改RemoveHerbCommand的参数类型
    content = re.sub(
        r'RemoveHerbCommand = new DelegateCommand<PrescriptionItem>',
        r'RemoveHerbCommand = new DelegateCommand<PrescriptionItemViewModel>',
        content
    )
    
    content = re.sub(
        r'private void RemoveHerb\(PrescriptionItem\? item\)',
        r'private void RemoveHerb(PrescriptionItemViewModel? item)',
        content
    )
    
    # 修改SelectedItem类型
    content = re.sub(
        r'private PrescriptionItem\? _selectedItem;',
        r'private PrescriptionItemViewModel? _selectedItem;',
        content
    )
    
    content = re.sub(
        r'public PrescriptionItem\? SelectedItem',
        r'public PrescriptionItemViewModel? SelectedItem',
        content
    )
    
    # 修改SaveAsync中的Items映射
    content = re.sub(
        r'Items = PrescriptionItems\.Select\(item => new PrescriptionItem',
        r'Items = PrescriptionItems.Select(item => item.GetModel()).ToList()',
        content
    )
    
    # 简化GetPrescriptionData方法
    content = re.sub(
        r'Items = PrescriptionItems\.Select\(item => new PrescriptionItem\s*\{[^}]*\}\)\.ToList\(\)',
        r'Items = PrescriptionItems.Select(item => item.GetModel()).ToList()',
        content
    )
    
    with open(prescription_vm_file, 'w', encoding='utf-8') as f:
        f.write(content)
    print(f"Updated PrescriptionViewModel to use unified types")
except Exception as e:
    print(f"Error updating PrescriptionViewModel: {e}")

print("\nType unification complete!")
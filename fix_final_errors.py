#!/usr/bin/env python3
"""修复最后剩余的编译错误"""

import os
import re

def fix_navigation_parameters():
    """修复NavigationParameters引用问题"""
    file_path = r"D:\source\repos\LYBTZYZS\src\Frontend\Desktop\Modules\Consultation\ViewModels\ConsultationMainViewModel.cs"
    
    with open(file_path, 'r', encoding='utf-8') as f:
        content = f.read()
    
    # 添加正确的using
    if "using Prism.Navigation.Regions;" not in content:
        content = content.replace(
            "using Prism.Navigation.Regions;",
            "using Prism.Navigation.Regions;\nusing Prism.Navigation.Regions;",
            1
        )
    
    # 替换NavigationParameters为Prism.Navigation.Regions.NavigationParameters
    content = content.replace(
        "var parameters = new NavigationParameters",
        "var parameters = new Prism.Navigation.Regions.NavigationParameters"
    )
    
    with open(file_path, 'w', encoding='utf-8') as f:
        f.write(content)
    print(f"[OK] 修复了 ConsultationMainViewModel 中的 NavigationParameters")

def fix_medical_case_conversion():
    """修复MedicalCase类型转换问题"""
    file_path = r"D:\source\repos\LYBTZYZS\src\Frontend\Desktop\Modules\Consultation\ViewModels\ConsultationMainViewModel.cs"
    
    with open(file_path, 'r', encoding='utf-8') as f:
        content = f.read()
    
    # 修复类型转换
    content = content.replace(
        "CurrentMedicalCase = result.Data as MedicalCaseInfo;",
        """var dto = result.Data;
                    CurrentMedicalCase = new MedicalCaseInfo
                    {
                        Id = dto.Id,
                        PatientId = dto.PatientId,
                        PatientName = dto.PatientName,
                        UserId = dto.UserId,
                        Status = dto.Status,
                        CreateTime = dto.CreateTime
                    };"""
    )
    
    with open(file_path, 'w', encoding='utf-8') as f:
        f.write(content)
    print(f"[OK] 修复了 MedicalCase 类型转换")

def fix_consultation_workflow_issues():
    """修复ConsultationWorkflowViewModel中的问题"""
    file_path = r"D:\source\repos\LYBTZYZS\src\Frontend\Desktop\Modules\Consultation\ViewModels\ConsultationWorkflowViewModel.cs"
    
    with open(file_path, 'r', encoding='utf-8') as f:
        content = f.read()
    
    # 1. 修复ConsultationData类型问题
    content = content.replace(
        "CurrentConsultationData = consultData.FourDiagnosis;",
        "CurrentConsultationData.FourDiagnosis = consultData.FourDiagnosis;"
    )
    
    # 2. 修复PatientInfo.Phone属性问题 - 添加Phone属性到PatientInfo
    patient_info_path = r"D:\source\repos\LYBTZYZS\src\Frontend\Desktop\Core\Models\Patients\PatientInfo.cs"
    if os.path.exists(patient_info_path):
        with open(patient_info_path, 'r', encoding='utf-8') as f:
            patient_content = f.read()
        if "public string? Phone { get; set; }" not in patient_content:
            patient_content = patient_content.replace(
                "public int Age { get; set; }",
                "public int Age { get; set; }\n        public string? Phone { get; set; }"
            )
            with open(patient_info_path, 'w', encoding='utf-8') as f:
                f.write(patient_content)
            print(f"[OK] 添加了 Phone 属性到 PatientInfo")
    
    # 3. 修复类型转换
    content = content.replace(
        "if (result.Data is ConsultationData consultData) { CurrentConsultationData.FourDiagnosis = consultData.FourDiagnosis; }",
        """var consultInfo = result.Data;
                    if (consultInfo != null && consultInfo.FourDiagnosisData != null)
                    {
                        CurrentConsultationData.FourDiagnosis = new FourDiagnosisData
                        {
                            Inspection = consultInfo.FourDiagnosisData.Inspection,
                            Auscultation = consultInfo.FourDiagnosisData.Auscultation,
                            Inquiry = consultInfo.FourDiagnosisData.Inquiry,
                            Palpation = consultInfo.FourDiagnosisData.Palpation
                        };
                    }"""
    )
    
    # 4. 修复SaveAsync参数
    content = content.replace(
        "var result = await _consultationService.SaveAsync(ConsultationData);",
        "var result = await _consultationService.SaveAsync(CurrentConsultationData);"
    )
    
    # 5. 创建缺失的视图文件
    views = [
        ("PatientSelectionView", "患者选择视图"),
    ]
    
    for view_name, desc in views:
        view_path = rf"D:\source\repos\LYBTZYZS\src\Frontend\Desktop\Modules\Consultation\Views\{view_name}.xaml"
        if not os.path.exists(view_path):
            xaml_content = f"""<UserControl x:Class="LYBT.WPF.Client.Modules.Consultation.Views.{view_name}"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
    <Grid>
        <TextBlock Text="{desc}" HorizontalAlignment="Center" VerticalAlignment="Center"/>
    </Grid>
</UserControl>"""
            
            cs_content = f"""using System.Windows.Controls;

namespace LYBT.WPF.Client.Modules.Consultation.Views
{{
    public partial class {view_name} : UserControl
    {{
        public {view_name}()
        {{
            InitializeComponent();
        }}
    }}
}}"""
            
            with open(view_path, 'w', encoding='utf-8') as f:
                f.write(xaml_content)
            
            cs_path = view_path + ".cs"
            with open(cs_path, 'w', encoding='utf-8') as f:
                f.write(cs_content)
            
            print(f"[OK] 创建了 {view_name}")
    
    with open(file_path, 'w', encoding='utf-8') as f:
        f.write(content)
    print(f"[OK] 修复了 ConsultationWorkflowViewModel")

def fix_create_medical_case_dialog():
    """修复CreateMedicalCaseViewModel"""
    file_path = r"D:\source\repos\LYBTZYZS\src\Frontend\Desktop\Modules\MedicalCase\ViewModels\CreateMedicalCaseViewModel.cs"
    
    if os.path.exists(file_path):
        with open(file_path, 'r', encoding='utf-8') as f:
            content = f.read()
        
        # 检查是否已有RequestClose实现
        if "public event Action<IDialogResult> RequestClose" not in content:
            # 在类的末尾添加RequestClose实现
            content = content.replace(
                "    }\n}",
                """        #region IDialogAware Implementation
        
        private Action<IDialogResult>? _requestClose;
        public event Action<IDialogResult> RequestClose
        {
            add { _requestClose += value; }
            remove { _requestClose -= value; }
        }
        
        protected void CloseDialog(IDialogResult result)
        {
            _requestClose?.Invoke(result);
        }
        
        #endregion
    }
}"""
            )
        
        with open(file_path, 'w', encoding='utf-8') as f:
            f.write(content)
        print(f"[OK] 修复了 CreateMedicalCaseViewModel")

def fix_herb_dialog():
    """修复SelectHerbDialogViewModel"""
    file_path = r"D:\source\repos\LYBTZYZS\src\Frontend\Desktop\Modules\Consultation\ViewModels\SelectHerbDialogViewModel.cs"
    
    if os.path.exists(file_path):
        with open(file_path, 'r', encoding='utf-8') as f:
            content = f.read()
        
        # 修复类型转换
        content = content.replace(
            "Herbs = herbs;",
            """Herbs = herbs?.Select(h => new HerbInfo
                {
                    Id = h.Id,
                    Name = h.Name,
                    Category = h.Category,
                    Price = h.Price,
                    Unit = h.Unit,
                    Stock = h.Stock,
                    IsActive = h.IsActive
                }).ToList() ?? new List<HerbInfo>();"""
        )
        
        with open(file_path, 'w', encoding='utf-8') as f:
            f.write(content)
        print(f"[OK] 修复了 SelectHerbDialogViewModel")

def main():
    print("开始修复最后的编译错误...")
    
    fix_navigation_parameters()
    fix_medical_case_conversion()
    fix_consultation_workflow_issues()
    fix_create_medical_case_dialog()
    fix_herb_dialog()
    
    print("\n所有修复已完成！")

if __name__ == "__main__":
    main()
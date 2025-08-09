import os
import re

# 1. 修复PatientInfo缺少Phone属性
patient_info_file = r"D:\source\repos\LYBTZYZS\src\Frontend\Desktop\Core\Models\Patients\PatientInfo.cs"
try:
    with open(patient_info_file, 'r', encoding='utf-8') as f:
        content = f.read()
    
    # 检查是否已有Phone属性
    if 'public string? Phone { get; set; }' not in content and 'public string Phone { get; set; }' not in content:
        # 在Gender属性后添加Phone
        pattern = r'(public string\? Gender { get; set; })'
        replacement = r'\1\n        public string? Phone { get; set; }'
        content = re.sub(pattern, replacement, content)
        
        with open(patient_info_file, 'w', encoding='utf-8') as f:
            f.write(content)
        print(f"Added Phone property to PatientInfo")
except Exception as e:
    print(f"Error modifying PatientInfo: {e}")

# 2. 修复MedicalCaseInfo缺少的属性
medical_case_info_file = r"D:\source\repos\LYBTZYZS\src\Frontend\Desktop\Core\Models\MedicalCase\MedicalCaseInfo.cs"
try:
    with open(medical_case_info_file, 'r', encoding='utf-8') as f:
        content = f.read()
    
    # 添加缺失的属性
    if 'public string? Diagnosis { get; set; }' not in content:
        # 在类的末尾添加属性
        pattern = r'(\s+)(}\s*}\s*$)'
        replacement = r'\1    public string? Diagnosis { get; set; }\n\1    public string? ChiefComplaint { get; set; }\n\1\2'
        content = re.sub(pattern, replacement, content, flags=re.MULTILINE)
        
        with open(medical_case_info_file, 'w', encoding='utf-8') as f:
            f.write(content)
        print(f"Added Diagnosis and ChiefComplaint properties to MedicalCaseInfo")
except Exception as e:
    print(f"Error modifying MedicalCaseInfo: {e}")

# 3. 修复ConsultationWorkflowViewModel中的类型问题
consultation_workflow_file = r"D:\source\repos\LYBTZYZS\src\Frontend\Desktop\Modules\Consultation\ViewModels\ConsultationWorkflowViewModel.cs"
try:
    with open(consultation_workflow_file, 'r', encoding='utf-8') as f:
        content = f.read()
    
    # 修复ConsultationData赋值问题（第388行）
    content = re.sub(
        r'ConsultationData = consultationResult\.Data;',
        r'CurrentConsultationData = consultationResult.Data;',
        content
    )
    
    # 修复第793行的ConsultationData赋值
    content = re.sub(
        r'(\s+)ConsultationData = data;',
        r'\1CurrentConsultationData = data;',
        content
    )
    
    # 修复MedicalCaseInfo类型转换（第311行）
    content = re.sub(
        r'MedicalCase = result\.Data;',
        r'''var dto = result.Data;
                    MedicalCase = new MedicalCaseInfo 
                    {
                        Id = dto.Id,
                        PatientId = dto.PatientId,
                        DoctorId = dto.DoctorId,
                        CreateTime = dto.CreateTime,
                        Status = dto.Status,
                        Diagnosis = dto.Diagnosis,
                        ChiefComplaint = dto.ChiefComplaint
                    };''',
        content
    )
    
    # 修复NavigationParameters类型（第314行）
    content = content.replace(
        'Prism.Navigation.Regions.NavigationParameters',
        'DialogParameters'
    )
    
    # 修复ConsultationInfo缺少FourDiagnosis属性引用
    content = re.sub(
        r'CurrentConsultationData\.FourDiagnosis = result\.Data\.FourDiagnosis;',
        r'if (result.Data is ConsultationData consultData) { CurrentConsultationData.FourDiagnosis = consultData.FourDiagnosis; }',
        content
    )
    
    # 修复PrescriptionData类型转换
    content = re.sub(
        r'CurrentConsultationData\.Prescription = new PrescriptionData\(\)',
        r'CurrentConsultationData.Prescription = new Core.Models.Prescriptions.PrescriptionData()',
        content
    )
    
    with open(consultation_workflow_file, 'w', encoding='utf-8') as f:
        f.write(content)
    print(f"Fixed ConsultationWorkflowViewModel type issues")
except Exception as e:
    print(f"Error modifying ConsultationWorkflowViewModel: {e}")

# 4. 修复ConsultationMainViewModel
consultation_main_file = r"D:\source\repos\LYBTZYZS\src\Frontend\Desktop\Modules\Consultation\ViewModels\ConsultationMainViewModel.cs"
try:
    with open(consultation_main_file, 'r', encoding='utf-8') as f:
        content = f.read()
    
    # 修复NavigationParameters引用
    content = content.replace(
        'var parameters = new Prism.Navigation.Regions.NavigationParameters',
        'var parameters = new NavigationParameters'
    )
    
    # 修复类型转换
    content = re.sub(
        r'var medicalCase = result\.Data as MedicalCaseInfo;',
        r'''var dto = result.Data;
                var medicalCase = new MedicalCaseInfo 
                {
                    Id = dto.Id,
                    PatientId = dto.PatientId,
                    DoctorId = dto.DoctorId,
                    CreateTime = dto.CreateTime,
                    Status = dto.Status
                };''',
        content
    )
    
    with open(consultation_main_file, 'w', encoding='utf-8') as f:
        f.write(content)
    print(f"Fixed ConsultationMainViewModel")
except Exception as e:
    print(f"Error modifying ConsultationMainViewModel: {e}")

# 5. 修复WorkflowNavigatorViewModel
workflow_navigator_file = r"D:\source\repos\LYBTZYZS\src\Frontend\Desktop\Modules\Consultation\ViewModels\WorkflowNavigatorViewModel.cs"
try:
    with open(workflow_navigator_file, 'r', encoding='utf-8') as f:
        content = f.read()
    
    # 修复WorkflowCompletionData类型冲突
    content = re.sub(
        r'\.Publish\(new WorkflowCompletionData',
        r'.Publish(new Core.Events.WorkflowCompletionData',
        content
    )
    
    # 在构造函数中初始化_steps
    content = re.sub(
        r'(public WorkflowNavigatorViewModel\([^)]+\)\s*{)',
        r'\1\n            _steps = new ObservableCollection<WorkflowStepViewModel>();',
        content
    )
    
    with open(workflow_navigator_file, 'w', encoding='utf-8') as f:
        f.write(content)
    print(f"Fixed WorkflowNavigatorViewModel")
except Exception as e:
    print(f"Error modifying WorkflowNavigatorViewModel: {e}")

# 6. 修复PrescriptionViewModel中的ImportSource问题
prescription_vm_file = r"D:\source\repos\LYBTZYZS\src\Frontend\Desktop\Modules\Consultation\ViewModels\PrescriptionViewModel.cs"
try:
    with open(prescription_vm_file, 'r', encoding='utf-8') as f:
        content = f.read()
    
    # 将ImportSource改为Remark（PrescriptionItemDto中的字段）
    content = re.sub(
        r'ImportSource = item\.ImportSource',
        r'ImportSource = item.Remark',
        content
    )
    
    # 修复PrescriptionData事件发布
    content = re.sub(
        r'_eventAggregator\.GetEvent<PrescriptionSavedEvent>\(\)\.Publish\(prescriptionData\);',
        r'// 发布处方保存事件',
        content
    )
    
    with open(prescription_vm_file, 'w', encoding='utf-8') as f:
        f.write(content)
    print(f"Fixed PrescriptionViewModel ImportSource issues")
except Exception as e:
    print(f"Error modifying PrescriptionViewModel: {e}")

print("\nAll fixes applied!")
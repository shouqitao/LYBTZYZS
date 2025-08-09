import os
import re

# 修复ConsultationWorkflowViewModel中的类型冲突
file_path = r"D:\source\repos\LYBTZYZS\src\Frontend\Desktop\Modules\Consultation\ViewModels\ConsultationWorkflowViewModel.cs"

with open(file_path, 'r', encoding='utf-8') as f:
    content = f.read()

# 1. 添加using语句（如果没有）
if 'using LYBT.WPF.Client.Core.Models.Consultation;' not in content:
    # 在namespace前添加
    namespace_pattern = r'(using LYBT\.WPF\.Client\.Core\.Extensions;\s*\n)(namespace )'
    content = re.sub(namespace_pattern, r'\1using LYBT.WPF.Client.Core.Models.Consultation;\n\2', content)

# 2. 替换内部定义的类型为完全限定名或直接使用
replacements = [
    # 移除内部的WorkflowStep枚举定义
    (r'public enum WorkflowStep\s*\{[^}]+\}\s*', ''),
    
    # 替换WorkflowStep引用
    ('ConsultationWorkflowViewModel.WorkflowStep', 'WorkflowStep'),
    
    # 替换FourDiagnosisData引用
    ('ConsultationWorkflowViewModel.FourDiagnosisData', 'FourDiagnosisData'),
    
    # 替换WorkflowStepData引用
    ('ConsultationWorkflowViewModel.WorkflowStepData', 'WorkflowStepData'),
    
    # 替换PrescriptionData引用
    ('ConsultationWorkflowViewModel.PrescriptionData', 'PrescriptionData'),
]

for old, new in replacements:
    content = re.sub(old, new, content)

# 写回文件
with open(file_path, 'w', encoding='utf-8') as f:
    f.write(content)

print(f"Fixed type conflicts in ConsultationWorkflowViewModel")

# 修复其他文件中的类型引用
modules_dir = r"D:\source\repos\LYBTZYZS\src\Frontend\Desktop\Modules"

for root, dirs, files in os.walk(modules_dir):
    for file in files:
        if file.endswith('.cs') and file != 'ConsultationWorkflowViewModel.cs':
            file_path = os.path.join(root, file)
            
            try:
                with open(file_path, 'r', encoding='utf-8') as f:
                    content = f.read()
                
                modified = False
                
                # 替换ConsultationWorkflowViewModel.WorkflowStep为WorkflowStep
                if 'ConsultationWorkflowViewModel.WorkflowStep' in content:
                    # 先确保有using语句
                    if 'using LYBT.WPF.Client.Core.Models.Consultation;' not in content:
                        # 在最后一个using后添加
                        last_using = list(re.finditer(r'using [^;]+;\s*\n', content))
                        if last_using:
                            insert_pos = last_using[-1].end()
                            content = content[:insert_pos] + 'using LYBT.WPF.Client.Core.Models.Consultation;\n' + content[insert_pos:]
                    
                    content = content.replace('ConsultationWorkflowViewModel.WorkflowStep', 'WorkflowStep')
                    content = content.replace('ConsultationWorkflowViewModel.FourDiagnosisData', 'FourDiagnosisData')
                    content = content.replace('ConsultationWorkflowViewModel.WorkflowStepData', 'WorkflowStepData')
                    content = content.replace('ConsultationWorkflowViewModel.PrescriptionData', 'PrescriptionData')
                    modified = True
                
                if modified:
                    with open(file_path, 'w', encoding='utf-8') as f:
                        f.write(content)
                    print(f"Fixed type references in: {file_path}")
                    
            except Exception as e:
                print(f"Error processing {file_path}: {e}")

print("\nType conflict resolution completed!")
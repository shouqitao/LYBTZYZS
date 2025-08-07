#!/usr/bin/env python3
"""
修复ConsultationMainViewModel中的nullable警告
"""

import os

def fix_nullable_warnings():
    """修复nullable警告"""
    
    file_path = r"D:\source\repos\LYBTZYZS\src\Frontend\Desktop\Modules\Consultation\ViewModels\ConsultationMainViewModel.cs"
    
    if not os.path.exists(file_path):
        print(f"File not found: {file_path}")
        return
    
    with open(file_path, 'r', encoding='utf-8') as f:
        content = f.read()
    
    original_content = content
    
    # 修复私有字段的nullable警告
    replacements = [
        # 将未初始化的引用类型字段声明为可为null
        ('private ObservableCollection<PatientInfo> _patients;',
         'private ObservableCollection<PatientInfo> _patients = new();'),
        
        ('private PatientInfo _selectedPatient;',
         'private PatientInfo? _selectedPatient;'),
        
        ('private ConsultationInfo _currentConsultation;',
         'private ConsultationInfo? _currentConsultation;'),
        
        ('private ObservableCollection<PrescriptionItemInfo> _prescriptionItems;',
         'private ObservableCollection<PrescriptionItemInfo> _prescriptionItems = new();'),
        
        ('private ObservableCollection<HerbInfo> _availableHerbs;',
         'private ObservableCollection<HerbInfo> _availableHerbs = new();'),
        
        ('private string _searchKeyword;',
         'private string _searchKeyword = string.Empty;'),
        
        ('private string _herbName;',
         'private string _herbName = string.Empty;'),
        
        ('private string _unit;',
         'private string _unit = "g";'),  # 默认单位为克
         
        # 修复属性的返回类型
        ('public PatientInfo SelectedPatient',
         'public PatientInfo? SelectedPatient'),
        
        ('public ConsultationInfo CurrentConsultation',
         'public ConsultationInfo? CurrentConsultation'),
    ]
    
    for old, new in replacements:
        if old in content:
            content = content.replace(old, new)
            print(f"Fixed: {old[:50]}...")
    
    # 移除构造函数中的重复初始化（因为字段已经初始化了）
    lines = content.split('\n')
    new_lines = []
    skip_lines = False
    
    for i, line in enumerate(lines):
        # 跳过构造函数中的集合初始化（因为已经在字段声明时初始化）
        if 'Patients = new ObservableCollection<PatientInfo>();' in line:
            continue
        elif 'PrescriptionItems = new ObservableCollection<PrescriptionItemInfo>();' in line:
            continue
        elif 'AvailableHerbs = new ObservableCollection<HerbInfo>();' in line:
            continue
        else:
            new_lines.append(line)
    
    content = '\n'.join(new_lines)
    
    if content != original_content:
        with open(file_path, 'w', encoding='utf-8') as f:
            f.write(content)
        print(f"Fixed nullable warnings in: {file_path}")
        return True
    else:
        print(f"No changes needed in: {file_path}")
        return False

if __name__ == "__main__":
    fix_nullable_warnings()
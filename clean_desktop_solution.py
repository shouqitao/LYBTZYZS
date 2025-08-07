#!/usr/bin/env python3
"""
清理LYBT.Desktop.sln文件，移除已删除的项目并添加Consultation模块
"""

import re

def clean_solution_file(file_path):
    """清理解决方案文件"""
    
    # 要移除的项目
    projects_to_remove = [
        'FrontDesk',
        'Doctor', 
        'Cashier',
        'Pharmacist',
        'Common'
    ]
    
    # 读取解决方案文件
    with open(file_path, 'r', encoding='utf-8-sig') as f:
        content = f.read()
    
    lines = content.split('\n')
    new_lines = []
    skip_project = False
    skip_project_config = False
    skip_nested = False
    removed_guids = []
    
    # 第一遍：收集要删除的GUID
    for line in lines:
        for project in projects_to_remove:
            if f'LYBT.WPF.Client.Modules.{project}' in line and 'Project(' in line:
                # 提取GUID
                match = re.search(r'\{([A-F0-9\-]+)\}"\s*$', line)
                if match:
                    removed_guids.append(match.group(1))
    
    # 第二遍：移除项目定义和配置
    i = 0
    while i < len(lines):
        line = lines[i]
        
        # 检查是否是要删除的项目定义
        skip_this = False
        for project in projects_to_remove:
            if f'LYBT.WPF.Client.Modules.{project}' in line and 'Project(' in line:
                skip_this = True
                break
        
        if skip_this:
            # 跳过Project行和EndProject行
            i += 1
            while i < len(lines) and 'EndProject' not in lines[i]:
                i += 1
            i += 1  # 跳过EndProject行
            continue
        
        # 检查是否是要删除的项目配置
        skip_config = False
        for guid in removed_guids:
            if guid in line:
                skip_config = True
                break
        
        if not skip_config:
            new_lines.append(line)
        
        i += 1
    
    # 添加Consultation模块（如果不存在）
    content_str = '\n'.join(new_lines)
    if 'LYBT.WPF.Client.Modules.Consultation' not in content_str:
        # 找到最后一个模块项目的位置
        insert_index = -1
        for i, line in enumerate(new_lines):
            if 'LYBT.WPF.Client.Modules.SystemManagement' in line and 'EndProject' in new_lines[i+1]:
                insert_index = i + 2
                break
        
        if insert_index > 0:
            # 添加Consultation模块
            consultation_project = [
                'Project("{9A19103F-16F7-4668-BE54-9A1E7A4F7556}") = "LYBT.WPF.Client.Modules.Consultation", "src\\Frontend\\Desktop\\Modules\\Consultation\\LYBT.WPF.Client.Modules.Consultation.csproj", "{A1234567-1234-1234-1234-123456789018}"',
                'EndProject'
            ]
            
            for j, proj_line in enumerate(consultation_project):
                new_lines.insert(insert_index + j, proj_line)
            
            # 在项目配置部分添加配置
            config_insert_index = -1
            for i, line in enumerate(new_lines):
                if '{A1234567-1234-1234-1234-123456789012}.Release|Any CPU.Build.0' in line:
                    config_insert_index = i + 1
                    break
            
            if config_insert_index > 0:
                consultation_config = [
                    '\t\t{A1234567-1234-1234-1234-123456789018}.Debug|Any CPU.ActiveCfg = Debug|Any CPU',
                    '\t\t{A1234567-1234-1234-1234-123456789018}.Debug|Any CPU.Build.0 = Debug|Any CPU',
                    '\t\t{A1234567-1234-1234-1234-123456789018}.Release|Any CPU.ActiveCfg = Release|Any CPU',
                    '\t\t{A1234567-1234-1234-1234-123456789018}.Release|Any CPU.Build.0 = Release|Any CPU'
                ]
                
                for j, config_line in enumerate(consultation_config):
                    new_lines.insert(config_insert_index + j, config_line)
            
            # 在NestedProjects部分添加嵌套关系
            nested_insert_index = -1
            for i, line in enumerate(new_lines):
                if '{A1234567-1234-1234-1234-123456789012} = {A1234567-1234-1234-1234-123456789010}' in line:
                    nested_insert_index = i + 1
                    break
            
            if nested_insert_index > 0:
                new_lines.insert(nested_insert_index, '\t\t{A1234567-1234-1234-1234-123456789018} = {A1234567-1234-1234-1234-123456789010}')
    
    # 添加后端项目引用（Infrastructure和Models）
    content_str = '\n'.join(new_lines)
    if 'LYBT.Infrastructure' not in content_str:
        # 找到Shared节的位置
        insert_index = -1
        for i, line in enumerate(new_lines):
            if 'LYBT.Shared.Utilities' in line and 'EndProject' in new_lines[i+1]:
                insert_index = i + 2
                break
        
        if insert_index > 0:
            # 添加Infrastructure和Models项目
            backend_projects = [
                'Project("{9A19103F-16F7-4668-BE54-9A1E7A4F7556}") = "LYBT.Infrastructure", "src\\Backend\\Core\\LYBT.Infrastructure\\LYBT.Infrastructure.csproj", "{A1234567-1234-1234-1234-123456789030}"',
                'EndProject',
                'Project("{9A19103F-16F7-4668-BE54-9A1E7A4F7556}") = "LYBT.Models", "src\\Backend\\Core\\LYBT.Models\\LYBT.Models.csproj", "{A1234567-1234-1234-1234-123456789031}"',
                'EndProject'
            ]
            
            for j, proj_line in enumerate(backend_projects):
                new_lines.insert(insert_index + j, proj_line)
    
    # 写回文件
    with open(file_path, 'w', encoding='utf-8-sig') as f:
        f.write('\n'.join(new_lines))
    
    print(f"已清理解决方案文件: {file_path}")
    print(f"移除的项目: {', '.join(projects_to_remove)}")
    print("添加的项目: Consultation")

if __name__ == '__main__':
    clean_solution_file('LYBT.Desktop.sln')
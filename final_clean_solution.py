#!/usr/bin/env python3
"""
最终清理解决方案文件 - 完全重新生成构建配置
"""
import re

def final_clean_solution():
    solution_file = r"D:\source\repos\LYBTZYZS\LYBT.Backend.sln"
    
    # 读取解决方案文件
    with open(solution_file, 'r', encoding='utf-8-sig') as f:
        content = f.read()
    
    print("最终清理解决方案文件...")
    
    # 收集所有有效的项目GUID
    valid_projects = []
    lines = content.split('\n')
    
    for line in lines:
        project_match = re.search(r'Project\(".*?"\) = "(.*?)", ".*?", "(.*?)"', line)
        if project_match:
            project_name = project_match.group(1)
            project_guid = project_match.group(2)
            valid_projects.append({
                'name': project_name,
                'guid': project_guid
            })
    
    print(f"找到 {len(valid_projects)} 个有效项目")
    
    # 重新生成ProjectConfigurationPlatforms部分
    new_project_configs = []
    for project in valid_projects:
        guid = project['guid']
        new_project_configs.extend([
            f"\t\t{guid}.Debug|Any CPU.ActiveCfg = Debug|Any CPU",
            f"\t\t{guid}.Debug|Any CPU.Build.0 = Debug|Any CPU",
            f"\t\t{guid}.Release|Any CPU.ActiveCfg = Release|Any CPU",
            f"\t\t{guid}.Release|Any CPU.Build.0 = Release|Any CPU"
        ])
    
    # 重新生成NestedProjects部分
    new_nested_configs = []
    modules_guid = "{8BC9CEB8-8B4A-11D0-8D11-00A0C91BC943}"
    core_guid = "{8BC9CEB8-8B4A-11D0-8D11-00A0C91BC942}"
    services_guid = "{8BC9CEB8-8B4A-11D0-8D11-00A0C91BC944}"
    shared_guid = "{02EA681E-C7D8-13C7-8484-4AC65E1B71E8}"
    
    for project in valid_projects:
        name = project['name']
        guid = project['guid']
        
        if name in ['LYBT.Models', 'LYBT.Infrastructure']:
            new_nested_configs.append(f"\t\t{guid} = {core_guid}")
        elif name == 'LYBT.WebAPI':
            new_nested_configs.append(f"\t\t{guid} = {services_guid}")
        elif name == 'LYBT.Shared.Models':
            new_nested_configs.append(f"\t\t{guid} = {shared_guid}")
        elif name.startswith('LYBT.Module.'):
            new_nested_configs.append(f"\t\t{guid} = {modules_guid}")
    
    # 替换ProjectConfigurationPlatforms部分
    project_config_pattern = r'(GlobalSection\(ProjectConfigurationPlatforms\) = postSolution\n)(.*?)(EndGlobalSection)'
    
    def replace_project_config(match):
        start = match.group(1)
        end = '\t' + match.group(3)
        return start + '\n'.join(new_project_configs) + '\n\t' + end
    
    content = re.sub(project_config_pattern, replace_project_config, content, flags=re.DOTALL)
    
    # 替换NestedProjects部分
    nested_pattern = r'(GlobalSection\(NestedProjects\) = preSolution\n)(.*?)(EndGlobalSection)'
    
    def replace_nested_config(match):
        start = match.group(1)
        end = '\t' + match.group(3)
        return start + '\n'.join(new_nested_configs) + '\n\t' + end
    
    content = re.sub(nested_pattern, replace_nested_config, content, flags=re.DOTALL)
    
    # 保存清理后的文件
    with open(solution_file, 'w', encoding='utf-8-sig') as f:
        f.write(content)
    
    print("解决方案文件最终清理完成!")
    print(f"重新生成了 {len(new_project_configs)} 行构建配置")
    print(f"重新生成了 {len(new_nested_configs)} 行嵌套项目配置")

if __name__ == "__main__":
    final_clean_solution()
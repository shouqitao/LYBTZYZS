#!/usr/bin/env python3
"""
修复解决方案文件 - 移除已删除的项目引用，添加新增的项目
"""
import os
import re
import shutil

def fix_backend_solution():
    """修复后端解决方案文件"""
    solution_file = r"D:\source\repos\LYBTZYZS\LYBT.Backend.sln"
    
    # 备份原文件
    shutil.copy2(solution_file, solution_file + ".bak")
    
    # 需要移除的项目
    projects_to_remove = [
        "LYBT.Module.Billing",
        "LYBT.Module.DiagnosisTreatment", 
        "LYBT.Module.Diagnostics",
        "LYBT.Module.Records",
        "LYBT.Module.Sync"  # Sync模块没有实际的csproj文件
    ]
    
    # 需要添加的新项目
    new_projects = [
        {
            "name": "LYBT.Module.Cashier",
            "path": "src\\Backend\\Modules\\LYBT.Module.Cashier\\LYBT.Module.Cashier.csproj",
            "guid": "{FAE04EC0-301F-11D3-BF4B-00C04F79EFD0}"
        },
        {
            "name": "LYBT.Module.Formula", 
            "path": "src\\Backend\\Modules\\LYBT.Module.Formula\\LYBT.Module.Formula.csproj",
            "guid": "{FAE04EC0-301F-11D3-BF4B-00C04F79EFD1}"
        },
        {
            "name": "LYBT.Module.TreatmentPlan",
            "path": "src\\Backend\\Modules\\LYBT.Module.TreatmentPlan\\LYBT.Module.TreatmentPlan.csproj", 
            "guid": "{FAE04EC0-301F-11D3-BF4B-00C04F79EFD2}"
        },
        {
            "name": "LYBT.Module.MedicalCase",
            "path": "src\\Backend\\Modules\\LYBT.Module.MedicalCase\\LYBT.Module.MedicalCase.csproj",
            "guid": "{FAE04EC0-301F-11D3-BF4B-00C04F79EFD3}" 
        },
        {
            "name": "LYBT.Module.Consultation",
            "path": "src\\Backend\\Modules\\LYBT.Module.Consultation\\LYBT.Module.Consultation.csproj",
            "guid": "{FAE04EC0-301F-11D3-BF4B-00C04F79EFD4}"
        }
    ]
    
    with open(solution_file, 'r', encoding='utf-8-sig') as f:
        content = f.read()
    
    print("正在修复后端解决方案文件...")
    
    # 移除已删除的项目引用
    removed_guids = []
    for project_name in projects_to_remove:
        # 移除项目声明行并提取GUID
        pattern = rf'Project\(".*?"\) = "{project_name}", ".*?", "(\{{.*?\}})"\s*\nEndProject\s*\n'
        matches = re.findall(pattern, content, flags=re.MULTILINE)
        if matches:
            removed_guids.append(matches[0])
        
        content = re.sub(pattern, '', content, flags=re.MULTILINE)
        print(f"  - 移除项目: {project_name}")
    
    # 移除构建配置和嵌套项目配置中的引用
    for guid in removed_guids:
        guid_clean = guid.strip('{}')
        # 移除构建配置
        build_config_pattern = rf'^\s*{re.escape(guid)}\..*?=.*?\n'
        content = re.sub(build_config_pattern, '', content, flags=re.MULTILINE)
        
        # 移除嵌套项目配置
        nested_pattern = rf'^\s*{re.escape(guid)} = .*?\n'
        content = re.sub(nested_pattern, '', content, flags=re.MULTILINE)
    
    # 找到合适的位置插入新项目（在Users项目之后）
    users_project_pattern = r'(Project\("[^"]+"\) = "LYBT\.Module\.Users"[^\n]*\nEndProject\n)'
    
    # 构建新项目的声明
    new_project_declarations = ""
    for project in new_projects:
        new_project_declarations += f'Project("{{9A19103F-16F7-4668-BE54-9A1E7A4F7556}}") = "{project["name"]}", "{project["path"]}", "{project["guid"]}"\n'
        new_project_declarations += f'EndProject\n'
    
    # 插入新项目
    content = re.sub(users_project_pattern, lambda m: m.group(1) + new_project_declarations, content)
    
    # 添加构建配置（在GlobalSection(ProjectConfigurationPlatforms)中）
    global_section_pattern = r'(GlobalSection\(ProjectConfigurationPlatforms\) = postSolution\n)(.*?)(EndGlobalSection)'
    
    def add_build_configs(match):
        start = match.group(1)
        existing_configs = match.group(2)
        end = match.group(3)
        
        new_configs = ""
        for project in new_projects:
            guid = project["guid"]
            new_configs += f'\t\t{guid}.Debug|Any CPU.ActiveCfg = Debug|Any CPU\n'
            new_configs += f'\t\t{guid}.Debug|Any CPU.Build.0 = Debug|Any CPU\n'
            new_configs += f'\t\t{guid}.Release|Any CPU.ActiveCfg = Release|Any CPU\n'
            new_configs += f'\t\t{guid}.Release|Any CPU.Build.0 = Release|Any CPU\n'
        
        return start + existing_configs + new_configs + '\t' + end
    
    content = re.sub(global_section_pattern, add_build_configs, content, flags=re.DOTALL)
    
    # 添加嵌套项目配置（在GlobalSection(NestedProjects)中）
    nested_pattern = r'(GlobalSection\(NestedProjects\) = preSolution\n)(.*?)(EndGlobalSection)'
    
    def add_nested_configs(match):
        start = match.group(1)
        existing_nested = match.group(2) 
        end = match.group(3)
        
        # 查找Modules文件夹的GUID
        modules_guid = "{8BC9CEB8-8B4A-11D0-8D11-00A0C91BC943}"
        
        new_nested = ""
        for project in new_projects:
            guid = project["guid"]
            new_nested += f'\t\t{guid} = {modules_guid}\n'
        
        return start + existing_nested + new_nested + '\t' + end
    
    content = re.sub(nested_pattern, add_nested_configs, content, flags=re.DOTALL)
    
    # 保存修复后的文件
    with open(solution_file, 'w', encoding='utf-8-sig') as f:
        f.write(content)
    
    print("后端解决方案文件修复完成!")
    
    # 显示添加的新项目
    print("  新增项目:")
    for project in new_projects:
        print(f"    + {project['name']}")

def create_missing_project_files():
    """为缺少.csproj文件的模块创建项目文件"""
    
    missing_projects = [
        {
            "name": "LYBT.Module.Cashier",
            "path": r"D:\source\repos\LYBTZYZS\src\Backend\Modules\LYBT.Module.Cashier\LYBT.Module.Cashier.csproj"
        },
        {
            "name": "LYBT.Module.Formula", 
            "path": r"D:\source\repos\LYBTZYZS\src\Backend\Modules\LYBT.Module.Formula\LYBT.Module.Formula.csproj"
        },
        {
            "name": "LYBT.Module.TreatmentPlan",
            "path": r"D:\source\repos\LYBTZYZS\src\Backend\Modules\LYBT.Module.TreatmentPlan\LYBT.Module.TreatmentPlan.csproj"
        }
    ]
    
    # 标准模块项目文件模板
    project_template = """<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>

  <ItemGroup>
    <ProjectReference Include="..\\..\\Core\\LYBT.Infrastructure\\LYBT.Infrastructure.csproj" />
    <ProjectReference Include="..\\..\\Core\\LYBT.Models\\LYBT.Models.csproj" />
    <ProjectReference Include="..\\..\\..\\Shared\\LYBT.Shared.Models\\LYBT.Shared.Models.csproj" />
    <ProjectReference Include="..\\..\\..\\Shared\\LYBT.Shared.Utilities\\LYBT.Shared.Utilities.csproj" />
  </ItemGroup>

  <ItemGroup>
    <PackageReference Include="AutoMapper" Version="13.0.1" />
    <PackageReference Include="Microsoft.Extensions.Logging.Abstractions" Version="8.0.1" />
  </ItemGroup>

</Project>
"""
    
    print("正在创建缺少的项目文件...")
    
    for project in missing_projects:
        project_file = project["path"]
        
        if not os.path.exists(project_file):
            # 确保目录存在
            os.makedirs(os.path.dirname(project_file), exist_ok=True)
            
            # 创建项目文件
            with open(project_file, 'w', encoding='utf-8') as f:
                f.write(project_template)
            
            print(f"  + 创建项目文件: {project['name']}")
        else:
            print(f"  * 项目文件已存在: {project['name']}")

if __name__ == "__main__":
    print("开始修复解决方案文件和创建缺失的项目文件...")
    
    # 1. 创建缺失的项目文件
    create_missing_project_files()
    
    # 2. 修复解决方案文件
    fix_backend_solution()
    
    print("\n解决方案修复完成！现在可以尝试构建项目了。")
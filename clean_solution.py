#!/usr/bin/env python3
"""
清理解决方案文件中的无效GUID引用
"""
import re

def clean_solution_file():
    solution_file = r"D:\source\repos\LYBTZYZS\LYBT.Backend.sln"
    
    # 读取解决方案文件
    with open(solution_file, 'r', encoding='utf-8-sig') as f:
        content = f.read()
    
    # 已删除模块的GUID（需要从NestedProjects中移除）
    removed_guids = [
        "{FAE04EC0-301F-11D3-BF4B-00C04F79EFC1}",  # Billing
        "{FAE04EC0-301F-11D3-BF4B-00C04F79EFC2}",  # DiagnosisTreatment
        "{FAE04EC0-301F-11D3-BF4B-00C04F79EFC3}",  # Diagnostics
        "{FAE04EC0-301F-11D3-BF4B-00C04F79EFCB}",  # Records
        "{FAE04EC0-301F-11D3-BF4B-00C04F79EFCD}"   # Sync
    ]
    
    print("清理解决方案文件中的无效GUID引用...")
    
    # 移除NestedProjects中的无效GUID
    for guid in removed_guids:
        nested_pattern = rf'^\s*{re.escape(guid)} = .*?\n'
        old_content = content
        content = re.sub(nested_pattern, '', content, flags=re.MULTILINE)
        if content != old_content:
            print(f"  - 移除嵌套项目引用: {guid}")
    
    # 移除重复的项目声明
    project_declarations = {}
    lines = content.split('\n')
    new_lines = []
    i = 0
    
    while i < len(lines):
        line = lines[i]
        
        # 检查是否为项目声明行
        project_match = re.search(r'Project\(".*?"\) = "(.*?)", ".*?", "(.*?)"', line)
        if project_match:
            project_name = project_match.group(1)
            project_guid = project_match.group(2)
            
            # 检查是否重复
            if project_name in project_declarations:
                print(f"  - 移除重复项目声明: {project_name}")
                # 跳过到EndProject
                while i < len(lines) and not lines[i].strip().startswith('EndProject'):
                    i += 1
                i += 1  # 跳过EndProject行
                continue
            else:
                project_declarations[project_name] = project_guid
                new_lines.append(line)
                i += 1
        else:
            new_lines.append(line)
            i += 1
    
    content = '\n'.join(new_lines)
    
    # 移除重复的嵌套项目配置
    nested_section = []
    nested_start = False
    final_lines = []
    
    for line in content.split('\n'):
        if 'GlobalSection(NestedProjects)' in line:
            nested_start = True
            final_lines.append(line)
        elif nested_start and line.strip() == 'EndGlobalSection':
            nested_start = False
            # 去重嵌套项目
            seen_guids = set()
            for nested_line in nested_section:
                guid = nested_line.split('=')[0].strip()
                if guid not in seen_guids:
                    final_lines.append(nested_line)
                    seen_guids.add(guid)
                else:
                    print(f"  - 移除重复嵌套项目: {guid}")
            final_lines.append(line)
            nested_section = []
        elif nested_start:
            nested_section.append(line)
        else:
            final_lines.append(line)
    
    content = '\n'.join(final_lines)
    
    # 保存清理后的文件
    with open(solution_file, 'w', encoding='utf-8-sig') as f:
        f.write(content)
    
    print("解决方案文件清理完成!")

if __name__ == "__main__":
    clean_solution_file()
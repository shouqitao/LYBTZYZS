#!/usr/bin/env python3
"""
修复解决方案文件中的错误配置
主要问题：Solution Folders（虚拟文件夹）不应该有构建配置
"""

import re
import shutil
from pathlib import Path

def fix_solution_file(sln_path):
    """修复解决方案文件"""
    
    # 备份原文件
    backup_path = str(sln_path) + '.backup'
    shutil.copy2(sln_path, backup_path)
    print(f"备份创建：{backup_path}")
    
    with open(sln_path, 'r', encoding='utf-8-sig') as f:
        content = f.read()
    
    # Solution Folder GUIDs (2150E333-8FDC-42A3-9474-1A3956D46DE8)
    folder_guids = set()
    
    # 查找所有 Solution Folder 的 GUID
    folder_pattern = r'Project\("\{2150E333-8FDC-42A3-9474-1A3956D46DE8\}"\).*?\{([A-F0-9\-]+)\}'
    for match in re.finditer(folder_pattern, content):
        folder_guids.add(match.group(1))
    
    print(f"找到 {len(folder_guids)} 个 Solution Folders")
    
    # 在 GlobalSection(ProjectConfigurationPlatforms) 中移除 Solution Folder 的构建配置
    lines = content.split('\n')
    new_lines = []
    in_config_section = False
    removed_count = 0
    
    for line in lines:
        if 'GlobalSection(ProjectConfigurationPlatforms)' in line:
            in_config_section = True
            new_lines.append(line)
        elif 'EndGlobalSection' in line and in_config_section:
            in_config_section = False
            new_lines.append(line)
        elif in_config_section:
            # 检查这行是否是 Solution Folder 的配置
            is_folder_config = False
            for guid in folder_guids:
                if guid in line:
                    is_folder_config = True
                    removed_count += 1
                    print(f"移除 Solution Folder 配置: {line.strip()}")
                    break
            
            if not is_folder_config:
                new_lines.append(line)
        else:
            new_lines.append(line)
    
    # 写回文件
    with open(sln_path, 'w', encoding='utf-8-sig') as f:
        f.write('\n'.join(new_lines))
    
    print(f"\n修复完成！")
    print(f"- 移除了 {removed_count} 行 Solution Folder 构建配置")
    print(f"- 解决方案文件已更新：{sln_path}")
    
    return True

if __name__ == "__main__":
    sln_file = Path("LYBT.Backend.sln")
    
    if not sln_file.exists():
        print(f"错误：找不到解决方案文件 {sln_file}")
        exit(1)
    
    fix_solution_file(sln_file)
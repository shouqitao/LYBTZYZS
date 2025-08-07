#!/usr/bin/env python3
"""
批量更新DTO中的IsActive字段为Status字段
"""

import os
import re
from pathlib import Path

def update_dto_file(file_path):
    """更新DTO文件中的IsActive字段为Status字段"""
    try:
        with open(file_path, 'r', encoding='utf-8') as f:
            content = f.read()
        
        # 添加必要的using语句（如果没有的话）
        if 'using LYBT.Shared.Models.Enums;' not in content:
            content = re.sub(
                r'(using System\.ComponentModel;)',
                r'using LYBT.Shared.Models.Enums;\n\1',
                content
            )
        
        # 替换IsActive字段定义为Status字段
        patterns_replacements = [
            # 替换属性定义
            (r'/// <summary>是否启用</summary>\s*\[DisplayName\("是否启用"\)\]\s*public bool IsActive { get; set; }(.*?);',
             r'/// <summary>状态</summary>\n        [DisplayName("状态")]\n        public CommonStatus Status { get; set; } = CommonStatus.Enabled;'),
            
            # 替换查询DTO中的可空字段
            (r'/// <summary>是否启用</summary>\s*\[DisplayName\("是否启用"\)\]\s*public bool\? IsActive { get; set; }(.*?);',
             r'/// <summary>状态筛选</summary>\n        [DisplayName("状态")]\n        public CommonStatus? Status { get; set; };'),
        ]
        
        original_content = content
        for pattern, replacement in patterns_replacements:
            content = re.sub(pattern, replacement, content, flags=re.DOTALL)
        
        # 如果有更改，写回文件
        if content != original_content:
            with open(file_path, 'w', encoding='utf-8') as f:
                f.write(content)
            print(f"OK {file_path}: 更新了IsActive字段")
            return True
        else:
            return False
            
    except Exception as e:
        print(f"ERROR 处理文件 {file_path} 时出错: {e}")
        return False

def main():
    # 定义要处理的目录
    contracts_dir = Path("src/Shared/LYBT.Shared.Models/Contracts")
    
    if not contracts_dir.exists():
        print(f"ERROR 目录不存在: {contracts_dir}")
        return
    
    updated_files = 0
    
    # 遍历所有DTO文件
    for dto_file in contracts_dir.rglob("*Dto.cs"):
        if update_dto_file(dto_file):
            updated_files += 1
    
    print(f"\n更新总结:")
    print(f"   更新文件数: {updated_files}")

if __name__ == "__main__":
    main()
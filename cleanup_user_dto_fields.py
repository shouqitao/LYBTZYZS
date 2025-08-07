#!/usr/bin/env python3
# -*- coding: utf-8 -*-

import os
import re

def cleanup_user_dto_fields():
    """清理User DTO中的Email, Department, Position字段"""
    
    # 需要处理的文件
    files_to_update = [
        'src/Shared/LYBT.Shared.Models/Contracts/Users/UserUpdateDto.cs',
        'src/Shared/LYBT.Shared.Models/Contracts/Users/UserDetailDto.cs',
        'src/Shared/LYBT.Shared.Models/Contracts/Users/UserPagedQueryDto.cs',
        'src/Shared/LYBT.Shared.Models/Contracts/Users/ChangeProfileDto.cs'
    ]
    
    # 定义需要删除的字段模式
    patterns_to_remove = [
        # Email字段及其相关注解
        r'\s*/// <summary>邮箱</summary>\s*\n\s*\[EmailAddress[^\]]*\]\s*\n\s*\[StringLength[^\]]*\]\s*\n\s*\[DisplayName\("邮箱"\)\]\s*\n\s*public string\? Email \{ get; set; \}\s*\n',
        r'\s*/// <summary>邮箱</summary>\s*\n\s*\[DisplayName\("邮箱"\)\]\s*\n\s*public string\? Email \{ get; set; \}\s*\n',
        
        # Department字段及其相关注解
        r'\s*/// <summary>部门/科室</summary>\s*\n\s*\[StringLength[^\]]*\]\s*\n\s*\[DisplayName\("部门"\)\]\s*\n\s*public string\? Department \{ get; set; \}\s*\n',
        r'\s*/// <summary>部门/科室</summary>\s*\n\s*\[DisplayName\("部门"\)\]\s*\n\s*public string\? Department \{ get; set; \}\s*\n',
        
        # Position字段及其相关注解
        r'\s*/// <summary>职位</summary>\s*\n\s*\[StringLength[^\]]*\]\s*\n\s*\[DisplayName\("职位"\)\]\s*\n\s*public string\? Position \{ get; set; \}\s*\n',
        r'\s*/// <summary>职位</summary>\s*\n\s*\[DisplayName\("职位"\)\]\s*\n\s*public string\? Position \{ get; set; \}\s*\n',
        
        # 简化的匹配模式
        r'.*Email.*\n.*public string\? Email.*\n',
        r'.*Department.*\n.*public string\? Department.*\n', 
        r'.*Position.*\n.*public string\? Position.*\n'
    ]
    
    updated_files = []
    
    for file_path in files_to_update:
        if not os.path.exists(file_path):
            print(f"文件不存在: {file_path}")
            continue
            
        try:
            with open(file_path, 'r', encoding='utf-8') as f:
                content = f.read()
            
            original_content = content
            
            # 应用删除模式
            for pattern in patterns_to_remove:
                content = re.sub(pattern, '', content, flags=re.MULTILINE | re.DOTALL)
            
            # 如果内容有变化，写回文件
            if content != original_content:
                with open(file_path, 'w', encoding='utf-8') as f:
                    f.write(content)
                updated_files.append(file_path)
                print(f"已更新: {file_path}")
                
        except Exception as e:
            print(f"处理文件失败 {file_path}: {e}")
    
    print(f"\n清理完成! 共更新 {len(updated_files)} 个文件")
    return updated_files

if __name__ == '__main__':
    print("开始清理User DTO中的过载字段...")
    cleanup_user_dto_fields()
    print("清理完成!")
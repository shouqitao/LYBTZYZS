#!/usr/bin/env python3
"""
批量替换前端代码中的 DoctorId 字段为 UserId
"""

import os
import re
from pathlib import Path

def find_and_replace_in_file(file_path, pattern, replacement):
    """在文件中查找并替换文本"""
    try:
        with open(file_path, 'r', encoding='utf-8') as f:
            content = f.read()
        
        # 计算替换次数
        count = len(re.findall(pattern, content))
        if count > 0:
            # 执行替换
            new_content = re.sub(pattern, replacement, content)
            
            with open(file_path, 'w', encoding='utf-8') as f:
                f.write(new_content)
            
            print(f"OK {file_path}: 替换了 {count} 处")
            return count
        else:
            return 0
    except Exception as e:
        print(f"ERROR 处理文件 {file_path} 时出错: {e}")
        return 0

def main():
    # 定义要处理的目录
    frontend_dir = Path("src/Frontend")
    
    if not frontend_dir.exists():
        print(f"ERROR 目录不存在: {frontend_dir}")
        return
    
    # 定义替换规则
    replacements = [
        # 属性声明替换
        (r'\bDoctorId\s*=\s*dto\.DoctorId\b', 'UserId = dto.UserId // 医生ID已更改为UserId'),
        (r'\bDoctorId\s*=\s*([^;,\n]+)', r'UserId = \1'),
        # 属性定义替换 
        (r'public\s+Guid\s+DoctorId\s*{\s*get;\s*set;\s*}', 'public Guid UserId { get; set; }'),
        (r'/// <summary>医生ID</summary>', '/// <summary>用户ID（医生）</summary>'),
        # 变量声明替换
        (r'\bvar\s+(\w+)\s*=\s*([^;]+)\.DoctorId\b', r'var \1 = \2.UserId'),
        # 方法参数替换
        (r'\bDoctorId\s*:', 'UserId:'),
        # LINQ 查询替换
        (r'\.DoctorId\s*==', '.UserId =='),
        (r'\.DoctorId\s*!=', '.UserId !='),
    ]
    
    total_files = 0
    total_replacements = 0
    
    # 遍历所有 .cs 文件
    for cs_file in frontend_dir.rglob("*.cs"):
        file_replacements = 0
        
        for pattern, replacement in replacements:
            count = find_and_replace_in_file(cs_file, pattern, replacement)
            file_replacements += count
        
        if file_replacements > 0:
            total_files += 1
            total_replacements += file_replacements
    
    print(f"\n替换总结:")
    print(f"   处理文件数: {total_files}")
    print(f"   总替换次数: {total_replacements}")

if __name__ == "__main__":
    main()
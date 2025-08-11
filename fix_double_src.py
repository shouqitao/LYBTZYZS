import os
import re
from pathlib import Path

def fix_double_src_paths(base_dir):
    """修复所有项目文件中的双重src路径"""
    
    # 查找所有 .csproj 文件
    for root, dirs, files in os.walk(base_dir):
        for file in files:
            if file.endswith('.csproj'):
                file_path = os.path.join(root, file)
                
                # 读取文件内容
                with open(file_path, 'r', encoding='utf-8-sig') as f:
                    content = f.read()
                
                # 修复路径
                original_content = content
                
                # 修复双重 src\src 路径
                content = content.replace('\\src\\src\\Shared\\', '\\src\\Shared\\')
                content = content.replace('..\\..\\..\\..\\src\\src\\', '..\\..\\..\\..\\src\\')
                
                # 只有内容有变化时才写入文件
                if content != original_content:
                    with open(file_path, 'w', encoding='utf-8') as f:
                        f.write(content)
                    print(f"修复: {file_path}")

if __name__ == "__main__":
    # 修复 Frontend Desktop 项目
    fix_double_src_paths(r"D:\source\repos\LYBTZYZS\src\Frontend\Desktop")
    print("双重src路径修复完成")
import os
import re
from pathlib import Path

def fix_project_references(base_dir):
    """修复所有项目文件中的引用路径"""
    
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
                
                # 修复缺少 src 的 Shared 路径
                content = content.replace('\\Shared\\LYBT.Shared.Models', '\\src\\Shared\\LYBT.Shared.Models')
                content = content.replace('\\Shared\\LYBT.Shared.Utilities', '\\src\\Shared\\LYBT.Shared.Utilities')
                
                # 只有内容有变化时才写入文件
                if content != original_content:
                    with open(file_path, 'w', encoding='utf-8') as f:
                        f.write(content)
                    print(f"修复: {file_path}")

if __name__ == "__main__":
    # 修复 Frontend Desktop 项目
    fix_project_references(r"D:\source\repos\LYBTZYZS\src\Frontend\Desktop")
    print("路径修复完成")
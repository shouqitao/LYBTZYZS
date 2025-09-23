#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
更新MedicalWorkbench文件夹中的所有命名空间
从 LYBT.Desktop.Workbench.Consultation 改为 LYBT.Desktop.Workbench.Medical
"""

import os
from pathlib import Path

def update_file(file_path):
    """更新单个文件中的命名空间"""
    try:
        with open(file_path, 'r', encoding='utf-8') as f:
            content = f.read()

        original = content

        # 更新using语句
        content = content.replace(
            'using LYBT.Desktop.Workbench.Consultation',
            'using LYBT.Desktop.Workbench.Medical'
        )

        # 更新namespace
        content = content.replace(
            'namespace LYBT.Desktop.Workbench.Consultation',
            'namespace LYBT.Desktop.Workbench.Medical'
        )

        # 更新x:Class属性
        content = content.replace(
            'x:Class="LYBT.Desktop.Workbench.Consultation.',
            'x:Class="LYBT.Desktop.Workbench.Medical.'
        )

        if content != original:
            with open(file_path, 'w', encoding='utf-8') as f:
                f.write(content)
            print(f"Updated: {file_path}")
            return True
    except Exception as e:
        print(f"Error updating {file_path}: {e}")
    return False

def main():
    # MedicalWorkbench文件夹路径
    workbench_path = Path(r"D:\source\repos\LYBTZYZS\src\Client\Desktop\Workbenches\MedicalWorkbench")

    updated_count = 0

    # 更新所有cs和xaml文件
    for ext in ['*.cs', '*.xaml']:
        for file_path in workbench_path.glob(f'**/{ext}'):
            if update_file(file_path):
                updated_count += 1

    print(f"\nTotal files updated: {updated_count}")

    # 同时更新项目文件中的RootNamespace
    csproj_path = workbench_path / "LYBT.Desktop.Workbench.Medical.csproj"
    if csproj_path.exists():
        with open(csproj_path, 'r', encoding='utf-8') as f:
            content = f.read()

        content = content.replace(
            '<RootNamespace>LYBT.Desktop.Workbench.Consultation</RootNamespace>',
            '<RootNamespace>LYBT.Desktop.Workbench.Medical</RootNamespace>'
        )

        with open(csproj_path, 'w', encoding='utf-8') as f:
            f.write(content)
        print(f"Updated project file: {csproj_path}")

if __name__ == "__main__":
    main()
#!/usr/bin/env python3
"""
验证 Dosage 到 Quantity 的重命名是否完成
"""

import os
import re
from pathlib import Path

def find_dosage_references(root_dir):
    """查找所有仍然使用 Dosage 的文件"""
    
    # 需要检查的文件扩展名
    extensions = ['.cs', '.xaml', '.json', '.xml', '.csproj']
    
    # 排除的目录
    exclude_dirs = ['bin', 'obj', '.git', '.vs', 'packages', 'node_modules']
    
    # 查找结果
    results = []
    
    for root, dirs, files in os.walk(root_dir):
        # 排除不需要检查的目录
        dirs[:] = [d for d in dirs if d not in exclude_dirs]
        
        for file in files:
            # 只检查指定扩展名的文件
            if not any(file.endswith(ext) for ext in extensions):
                continue
                
            file_path = os.path.join(root, file)
            
            # 跳过迁移文件
            if 'Migrations' in file_path:
                continue
                
            try:
                with open(file_path, 'r', encoding='utf-8') as f:
                    content = f.read()
                    
                # 查找 Dosage 相关的引用（区分大小写）
                # 排除注释中的引用
                lines = content.splitlines()
                for line_num, line in enumerate(lines, 1):
                    # 跳过注释行
                    if line.strip().startswith('//') or line.strip().startswith('<!--'):
                        continue
                        
                    # 查找 Dosage（但不包括 DosageForm、DosageInstruction 等复合词）
                    if re.search(r'\bDosage\b(?!Form|Instruction)', line):
                        results.append({
                            'file': file_path,
                            'line': line_num,
                            'content': line.strip()
                        })
                        
            except Exception as e:
                print(f"无法读取文件 {file_path}: {e}")
    
    return results

def main():
    # 项目根目录
    root_dir = r'D:\source\repos\LYBTZYZS'
    
    print("正在扫描项目中的 Dosage 引用...")
    print("=" * 60)
    
    results = find_dosage_references(root_dir)
    
    if not results:
        print("[OK] 太好了！没有找到任何 Dosage 引用。")
        print("     字段重命名已经完成。")
    else:
        print(f"[WARNING] 发现 {len(results)} 处 Dosage 引用需要检查：")
        print()
        
        # 按文件分组显示
        current_file = None
        for result in results:
            if result['file'] != current_file:
                current_file = result['file']
                # 显示相对路径
                rel_path = os.path.relpath(current_file, root_dir)
                print(f"\n[FILE] {rel_path}")
                
            print(f"   第 {result['line']} 行: {result['content'][:80]}...")
    
    print()
    print("=" * 60)
    print("扫描完成！")
    
    # 额外检查：查找 quantity 的使用情况
    print("\n正在验证 Quantity 字段的使用...")
    quantity_count = 0
    
    for root, dirs, files in os.walk(os.path.join(root_dir, 'src')):
        dirs[:] = [d for d in dirs if d not in ['bin', 'obj']]
        for file in files:
            if file.endswith('.cs') or file.endswith('.xaml'):
                file_path = os.path.join(root, file)
                try:
                    with open(file_path, 'r', encoding='utf-8') as f:
                        content = f.read()
                        # 统计 Quantity 在处方和验方相关代码中的使用
                        if ('Formula' in file_path or 'Prescription' in file_path or 
                            'Herb' in file_path or 'Record' in file_path):
                            quantity_matches = re.findall(r'\bQuantity\b', content)
                            if quantity_matches:
                                quantity_count += len(quantity_matches)
                except:
                    pass
    
    print(f"[OK] 找到 {quantity_count} 处 Quantity 字段的使用。")
    print("     这表明新的字段名称已经在使用中。")

if __name__ == '__main__':
    main()
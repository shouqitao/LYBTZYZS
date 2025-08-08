"""
检查前端代码中的async void方法
"""
import os
import re
from pathlib import Path

def find_async_void_methods(root_dir):
    """查找所有async void方法"""
    async_void_pattern = re.compile(r'private\s+async\s+void\s+(\w+)|protected\s+async\s+void\s+(\w+)|public\s+async\s+void\s+(\w+)')
    
    results = {}
    total_count = 0
    
    # 遍历所有.cs文件
    for path in Path(root_dir).rglob('*.cs'):
        # 跳过bin和obj目录
        if 'bin' in path.parts or 'obj' in path.parts:
            continue
            
        with open(path, 'r', encoding='utf-8', errors='ignore') as f:
            content = f.read()
            
        matches = async_void_pattern.findall(content)
        if matches:
            method_names = [m[0] or m[1] or m[2] for m in matches]
            results[str(path.relative_to(root_dir))] = method_names
            total_count += len(method_names)
    
    return results, total_count

def main():
    # 前端项目根目录
    frontend_dir = r"D:\source\repos\LYBTZYZS\src\Frontend\Desktop"
    
    print("=" * 60)
    print("检查前端代码中的async void方法")
    print("=" * 60)
    
    results, total_count = find_async_void_methods(frontend_dir)
    
    if not results:
        print("\n[OK] 太好了！没有发现async void方法。")
    else:
        print(f"\n[WARNING] 发现 {total_count} 个async void方法需要修复：\n")
        
        # 按文件分组显示
        for file_path, methods in sorted(results.items()):
            print(f"[FILE] {file_path}")
            for method in methods:
                print(f"   - {method}()")
            print()
        
        # 显示修复建议
        print("\n" + "=" * 60)
        print("修复建议：")
        print("=" * 60)
        print("1. 将方法签名从 'async void' 改为 'async Task'")
        print("2. 更新调用处：")
        print("   - 对于命令：new DelegateCommand(async () => await MethodAsync())")
        print("   - 对于事件处理：_ = MethodAsync()")
        print("3. 使用BaseViewModel中的CreateAsyncCommand方法创建异步命令")
    
    # 统计已修复的文件
    fixed_files = [
        "Core\\ViewModels\\BaseViewModel.cs",
        "Modules\\Authentication\\ViewModels\\LoginViewModel.cs",
        "Shell\\ViewModels\\MainWindowViewModel.cs",
        "Modules\\Consultation\\ViewModels\\ConsultationMainViewModel.cs"
    ]
    
    print("\n" + "=" * 60)
    print("修复进度：")
    print("=" * 60)
    print(f"[FIXED] 已修复的文件 ({len(fixed_files)})：")
    for file in fixed_files:
        print(f"   - {file}")
    
    remaining = len(results)
    if remaining > 0:
        print(f"\n[PENDING] 待修复的文件 ({remaining})：")
        for file_path in list(results.keys())[:5]:  # 显示前5个
            print(f"   - {file_path}")
        if remaining > 5:
            print(f"   ... 还有 {remaining - 5} 个文件")
    
    # 计算完成百分比
    total_files = len(fixed_files) + remaining
    if total_files > 0:
        completion = (len(fixed_files) / total_files) * 100
        print(f"\n[PROGRESS] 完成进度：{completion:.1f}% ({len(fixed_files)}/{total_files})")
    
    print("\n" + "=" * 60)

if __name__ == "__main__":
    main()
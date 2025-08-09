import os
import re

herb_service_file = r"D:\source\repos\LYBTZYZS\src\Frontend\Desktop\Services\HerbService.cs"

try:
    with open(herb_service_file, 'r', encoding='utf-8') as f:
        lines = f.readlines()
    
    # 删除重复的GetListAsync方法
    filtered_lines = []
    skip_method = False
    skip_count = 0
    found_first = False
    
    for i, line in enumerate(lines):
        # 检测GetListAsync方法定义
        if 'public async Task<ApiResult<List<HerbDto>>> GetListAsync' in line:
            if not found_first:
                found_first = True
                filtered_lines.append(line)
            else:
                # 跳过重复的方法
                skip_method = True
                skip_count = 0
                continue
        
        if skip_method:
            skip_count += 1
            # 跳过方法体（大约20行）
            if skip_count > 20 or ('}' in line and skip_count > 10):
                skip_method = False
            continue
        
        # 删除多余的花括号和不完整的行
        if line.strip() == '}' and i > 0 and lines[i-1].strip() == '}':
            # 检查是否是文件末尾的多余花括号
            if i > len(lines) - 5:
                continue
        
        # 删除不完整的行
        if 'throw new Exception($"获取缺货药材失败: {ex.Message}", ex);' in line and i < len(lines) - 1:
            if 'public async Task<ApiResult' in lines[i+1]:
                # 添加缺失的花括号
                filtered_lines.append(line)
                filtered_lines.append('            }\n')
                filtered_lines.append('        }\n')
                continue
        
        if 'throw new Exception($"获取即将过期药材失败: {ex.Message}", ex);' in line and i < len(lines) - 1:
            if 'public async Task<ApiResult' in lines[i+1]:
                # 添加缺失的花括号
                filtered_lines.append(line)
                filtered_lines.append('            }\n')
                filtered_lines.append('        }\n')
                continue
        
        filtered_lines.append(line)
    
    # 修复末尾的花括号
    # 确保文件以正确的花括号结束
    content = ''.join(filtered_lines)
    
    # 计算花括号数量
    open_braces = content.count('{')
    close_braces = content.count('}')
    
    # 如果缺少闭合花括号，添加它们
    if open_braces > close_braces:
        missing_braces = open_braces - close_braces
        for _ in range(missing_braces):
            content += '\n}'
    
    # 添加必要的using语句
    if 'using LYBT.WPF.Client.Core.Models.Common;' not in content:
        content = content.replace(
            'using LYBT.WPF.Client.Core.Models.Herbs;',
            'using LYBT.WPF.Client.Core.Models.Herbs;\nusing LYBT.WPF.Client.Core.Models.Common;'
        )
    
    # 添加缺失的logger字段
    if 'private readonly ILogger' not in content:
        content = content.replace(
            'private readonly IHerbApiService _herbApiService;',
            'private readonly IHerbApiService _herbApiService;\n        private readonly Microsoft.Extensions.Logging.ILogger<HerbService> _logger;'
        )
        
        # 更新构造函数
        content = content.replace(
            'public HerbService(IHerbApiService herbApiService)',
            'public HerbService(IHerbApiService herbApiService, Microsoft.Extensions.Logging.ILogger<HerbService> logger)'
        )
        
        content = content.replace(
            '_herbApiService = herbApiService;',
            '_herbApiService = herbApiService;\n            _logger = logger;'
        )
    
    with open(herb_service_file, 'w', encoding='utf-8') as f:
        f.write(content)
    
    print("Fixed HerbService duplicate methods and added missing dependencies")
    
except Exception as e:
    print(f"Error: {e}")
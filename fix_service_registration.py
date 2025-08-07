#!/usr/bin/env python3
"""
修复ServiceCollectionExtensions.cs中的服务注册错误
"""

import os
import re

def fix_service_registration():
    """注释掉已删除的服务注册"""
    
    file_path = r"D:\source\repos\LYBTZYZS\src\Frontend\Desktop\Shell\Extensions\ServiceCollectionExtensions.cs"
    
    if not os.path.exists(file_path):
        print(f"File not found: {file_path}")
        return
    
    with open(file_path, 'r', encoding='utf-8') as f:
        lines = f.readlines()
    
    # 需要注释掉的服务
    services_to_comment = [
        'IRecordApiService', 
        'RecordService',
        'IRecordService',
        'IRegistrationApiService',
        'RegistrationService',
        'IRegistrationService',
        'IDoctorsApiService',
        'DoctorsService',
        'IDoctorsService',
        'ILogsApiService',
        'LogsService',
        'ILogsService',
        'IBillingService',
        'BillingService',
        'IPharmacyService',
        'PharmacyService',
        'IPhysiotherapyService',
        'PhysiotherapyService'
    ]
    
    new_lines = []
    in_block = False
    block_start = -1
    brace_count = 0
    
    for i, line in enumerate(lines):
        # 检查是否是需要注释的服务注册开始
        should_comment = False
        for service in services_to_comment:
            if re.search(rf'\b{service}\b', line) and ('containerRegistry.Register' in line or 'services.AddScoped' in line or 'services.AddSingleton' in line):
                should_comment = True
                break
        
        if should_comment and not in_block:
            in_block = True
            block_start = i
            brace_count = 0
            new_lines.append('// ' + line)
            if '{' in line:
                brace_count += line.count('{')
            if '}' in line:
                brace_count -= line.count('}')
            if brace_count == 0 and ');' in line:
                in_block = False
        elif in_block:
            new_lines.append('// ' + line)
            if '{' in line:
                brace_count += line.count('{')
            if '}' in line:
                brace_count -= line.count('}')
            if brace_count == 0 and ');' in line:
                in_block = False
        else:
            new_lines.append(line)
    
    with open(file_path, 'w', encoding='utf-8') as f:
        f.writelines(new_lines)
    
    print(f"Fixed: {file_path}")

if __name__ == "__main__":
    fix_service_registration()
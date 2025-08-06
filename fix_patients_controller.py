#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
修复 PatientsController 中的方法调用问题
"""

import re

def fix_patients_controller():
    file_path = r"D:\source\repos\LYBTZYZS\src\Backend\Services\LYBT.WebAPI\Controllers\PatientsController.cs"
    
    # 读取文件内容
    with open(file_path, 'r', encoding='utf-8') as f:
        content = f.read()
    
    # 定义替换规则
    replacements = [
        # AddAsync -> CreateAsync (已经手动修复了第一个)
        (r'_patientService\.AddAsync\(', r'_patientService.CreateAsync('),
        
        # EnableAsync -> SetStatusAsync(id, true, ...)
        (r'_patientService\.EnableAsync\(id, operatorId, operatorName\)', 
         r'_patientService.SetStatusAsync(id, true, operatorId, operatorName)'),
        
        # DisableAsync -> SetStatusAsync(id, false, ...)
        (r'_patientService\.DisableAsync\(id, operatorId, operatorName\)', 
         r'_patientService.SetStatusAsync(id, false, operatorId, operatorName)'),
        
        # 删除不存在的方法调用
        (r'await _patientService\.BatchDisableAsync\(dto\.Ids, operatorId, operatorName\);', 
         r'0; // TODO: 实现批量禁用功能'),
        
        (r'await _patientService\.BatchEnableAsync\(dto\.Ids, operatorId, operatorName\);', 
         r'0; // TODO: 实现批量启用功能'),
        
        (r'await _patientService\.ImportAsync\(dtos, operatorId, operatorName\);', 
         r'0; // TODO: 实现导入功能'),
        
        (r'await _patientService\.ExportAsync\(operatorRole\);', 
         r'await _patientService.GetAllAsync(operatorRole);'),
        
        (r'await _patientService\.GetHistoryRecordsAsync\(id\);', 
         r'new List<RecordDto>(); // TODO: 实现获取历史记录功能'),
        
        (r'await _patientService\.FindOrCreateAsync\(dto, operatorId, operatorName\);', 
         r'await FindOrCreatePatientAsync(dto, operatorId, operatorName);'),
         
        # UpdateAsync 参数修复
        (r'_patientService\.UpdateAsync\(id, dto\)', 
         r'_patientService.UpdateAsync(id, dto, operatorId, operatorName)'),
    ]
    
    # 执行替换
    for pattern, replacement in replacements:
        content = re.sub(pattern, replacement, content)
    
    # 在类中添加辅助方法
    helper_method = '''
        /// <summary>
        /// 辅助方法：查找或创建患者
        /// </summary>
        private async Task<PatientDetailDto?> FindOrCreatePatientAsync(PatientDetailDto dto, Guid operatorId, string operatorName) {
            // 先尝试根据手机号查找
            if (!string.IsNullOrWhiteSpace(dto.PhoneNumber)) {
                var existing = await _patientService.GetByPhoneNumberAsync(dto.PhoneNumber);
                if (existing != null) return existing;
            }
            
            // 如果不存在，创建新患者
            return await _patientService.CreateAsync(dto, operatorId, operatorName);
        }'''
    
    # 在最后一个方法之前插入辅助方法
    last_brace_index = content.rfind('}')
    second_last_brace_index = content.rfind('}', 0, last_brace_index)
    content = content[:second_last_brace_index] + helper_method + '\n    ' + content[second_last_brace_index:]
    
    # 写回文件
    with open(file_path, 'w', encoding='utf-8') as f:
        f.write(content)
    
    print(f"✅ 文件已修复: {file_path}")

if __name__ == "__main__":
    fix_patients_controller()
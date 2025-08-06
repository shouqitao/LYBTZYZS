#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
修复最后18个编译错误
"""

import os
import re

def fix_file(file_path, replacements):
    """修复单个文件"""
    try:
        with open(file_path, 'r', encoding='utf-8') as f:
            content = f.read()
        
        original_content = content
        for old, new in replacements:
            content = re.sub(old, new, content, flags=re.MULTILINE)
        
        if content != original_content:
            with open(file_path, 'w', encoding='utf-8') as f:
                f.write(content)
            print(f"修复: {os.path.basename(file_path)}")
            return True
        return False
    except Exception as e:
        print(f"错误: {file_path} - {e}")
        return False

def fix_consultation_view_model():
    """修复ConsultationViewModel的编译错误"""
    file_path = r"D:\source\repos\LYBTZYZS\src\Frontend\Desktop\Modules\Doctor\ViewModels\ConsultationViewModel.cs"
    
    replacements = [
        # 修复HerbInfo.Status问题
        (r'herb\.Status = dto\.Status;', r'// herb.Status = dto.Status; // TODO: Status字段已移除'),
    ]
    
    fix_file(file_path, replacements)

def fix_consultation_view_model_new():
    """修复ConsultationViewModelNew的MaritalStatus等字段错误"""
    file_path = r"D:\source\repos\LYBTZYZS\src\Frontend\Desktop\Modules\Doctor\ViewModels\ConsultationViewModelNew.cs"
    
    replacements = [
        # 注释掉已删除的字段
        (r'MaritalStatus = CurrentPatient\.MaritalStatus\s*\?\?\s*"未婚"', r'MaritalStatus = "未婚" /* CurrentPatient.MaritalStatus ?? "未婚" */'),
        (r'Ethnicity = CurrentPatient\.Ethnicity\s*\?\?\s*"汉族"', r'Ethnicity = "汉族" /* CurrentPatient.Ethnicity ?? "汉族" */'),
        (r'Education = CurrentPatient\.Education\s*\?\?\s*"不详"', r'Education = "不详" /* CurrentPatient.Education ?? "不详" */'),
    ]
    
    fix_file(file_path, replacements)

def fix_simple_doctor_workbench():
    """修复SimpleDoctorWorkbenchViewModel的服务方法调用"""
    file_path = r"D:\source\repos\LYBTZYZS\src\Frontend\Desktop\Modules\Doctor\ViewModels\SimpleDoctorWorkbenchViewModel.cs"
    
    replacements = [
        # 修复GetTodayRegistrationsAsync方法调用
        (r'await _registrationService\.GetTodayRegistrationsAsync\(\)', 
         r'await _registrationService.GetListAsync(new LYBT.Shared.Models.Contracts.Registration.RegistrationQueryDto { Status = "待看诊" })'),
        
        # 修复GetPatientByIdAsync方法调用
        (r'await _patientService\.GetPatientByIdAsync\(patientId\)', 
         r'await _patientService.GetByIdAsync(patientId)'),
        
        # 修复GetAllActiveFormulasAsync方法调用
        (r'await _formulaTemplateService\.GetAllActiveFormulasAsync\(\)', 
         r'await _formulaTemplateService.GetListAsync()'),
         
        # 修复AvailableHerbs.Add类型问题 - 需要更复杂的处理
    ]
    
    fix_file(file_path, replacements)
    
    # 单独处理HerbInfo转换问题
    with open(file_path, 'r', encoding='utf-8') as f:
        lines = f.readlines()
    
    for i in range(len(lines)):
        if 'AvailableHerbs.Add(herb);' in lines[i]:
            # 找到这一行，查看herb的类型并处理
            lines[i] = lines[i].replace(
                'AvailableHerbs.Add(herb);',
                'AvailableHerbs.Add(new HerbDto { Id = herb.Id, Name = herb.Name, Price = herb.Price, Unit = herb.Unit ?? "克" });'
            )
    
    with open(file_path, 'w', encoding='utf-8') as f:
        f.writelines(lines)

def fix_herb_dialogs():
    """修复药材管理对话框的语法错误"""
    
    # EditHerbDialogViewModel
    file1 = r"D:\source\repos\LYBTZYZS\src\Frontend\Desktop\Modules\SystemManagement\Herbs\ViewModels\EditHerbDialogViewModel.cs"
    with open(file1, 'r', encoding='utf-8') as f:
        lines = f.readlines()
    
    for i in range(len(lines)):
        # 修复第150行的注释语法错误
        if i == 149:  # 第150行，索引149
            lines[i] = '                /* Stock = dto.Stock, */\n'
        # 修复第155行的注释语法错误
        if i == 154:  # 第155行，索引154
            lines[i] = '                /* BatchNo = dto.BatchNo, */\n'
    
    with open(file1, 'w', encoding='utf-8') as f:
        f.writelines(lines)
    print(f"修复: {os.path.basename(file1)}")
    
    # StockManagementDialogViewModel
    file2 = r"D:\source\repos\LYBTZYZS\src\Frontend\Desktop\Modules\SystemManagement\Herbs\ViewModels\StockManagementDialogViewModel.cs"
    with open(file2, 'r', encoding='utf-8') as f:
        lines = f.readlines()
    
    for i in range(len(lines)):
        # 修复第125行的注释语法错误
        if i == 124:  # 第125行，索引124
            lines[i] = '                /* Stock = h.Stock, */\n'
        # 修复第156行的语法错误
        if i == 155:  # 第156行，索引155
            if '/* WuBiCode = h.WuBiCode */' in lines[i]:
                lines[i] = '                /* WuBiCode = h.WuBiCode, */\n'
        # 修复第158行的语法错误
        if i == 157:  # 第158行，索引157
            if 'ExpireDate = DateTime.Now.AddYears(2) /' in lines[i]:
                lines[i] = '                ExpireDate = DateTime.Now.AddYears(2), /* h.ExpireDate */\n'
    
    with open(file2, 'w', encoding='utf-8') as f:
        f.writelines(lines)
    print(f"修复: {os.path.basename(file2)}")

def main():
    print("开始修复最后18个错误...")
    print("=" * 60)
    
    fix_consultation_view_model()
    fix_consultation_view_model_new()
    fix_simple_doctor_workbench()
    fix_herb_dialogs()
    
    print("=" * 60)
    print("修复完成！")

if __name__ == "__main__":
    main()
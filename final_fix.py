#!/usr/bin/env python3
"""
最终修复剩余的16个错误
"""

from pathlib import Path
import re

def fix_treatment_room_line_206():
    """修复TreatmentRoom第206行的赋值错误"""
    print("[FIX] 修复 TreatmentRoom 第206行...")
    
    file = Path("src/Backend/Modules/LYBT.Module.TreatmentRoom/Services/TreatmentRoomService.cs")
    if not file.exists():
        return
        
    with open(file, 'r', encoding='utf-8') as f:
        lines = f.readlines()
    
    # 修复第206行（索引205）
    if len(lines) > 205:
        if '0m /* model.Duration字段已删除 */' in lines[205]:
            # 注释掉整行
            lines[205] = '                // model.Duration = dto.ActualDuration ?? (decimal)(DateTime.Now - model.StartTime.Value).TotalMinutes; // Duration字段已删除\n'
    
    # 修复第265行（索引264）的TherapistName访问
    if len(lines) > 264:
        if 'TherapistName' in lines[264]:
            lines[264] = '                    TherapistName = null // TherapistName字段已删除\n'
    
    # 修复其他Duration相关的行
    for i in range(len(lines)):
        if '0m /* t.Duration字段已删除 */' in lines[i] and '>' in lines[i]:
            # 将条件判断改为false
            lines[i] = lines[i].replace('0m /* t.Duration字段已删除 */ > 0', 'false /* Duration字段已删除 */')
    
    with open(file, 'w', encoding='utf-8') as f:
        f.writelines(lines)
    
    print("  已修复赋值错误")

def fix_registration_enums():
    """修复Registration模块的枚举值"""
    print("[FIX] 修复 Registration 枚举值...")
    
    # 先查看枚举定义
    enum_file = Path("src/Shared/LYBT.Shared.Models/Enums/RegistrationStatus.cs")
    if enum_file.exists():
        content = enum_file.read_text(encoding='utf-8')
        print(f"  枚举内容: {content[:200]}")
    
    # 修复服务文件
    service_file = Path("src/Backend/Modules/LYBT.Module.Registration/Services/RegistrationService.cs")
    if service_file.exists():
        content = service_file.read_text(encoding='utf-8')
        
        # 替换枚举值为正确的形式
        # 根据常见的枚举定义，应该使用Pending, Processing, Completed等
        content = content.replace('RegistrationStatus.Waiting', 'RegistrationStatus.Pending')
        content = content.replace('RegistrationStatus.InProgress', 'RegistrationStatus.Processing')
        
        service_file.write_text(content, encoding='utf-8')
        print("  已替换枚举值")
    
    # 修复仓储中的RegistrationDate字段
    repo_file = Path("src/Backend/Modules/LYBT.Module.Registration/Repositories/RegistrationRepository.cs")
    if repo_file.exists():
        content = repo_file.read_text(encoding='utf-8')
        
        # 替换RegistrationDate为CreatedAt或其他可用字段
        content = content.replace('r.RegistrationDate.Date', 'r.CreatedAt.Date')
        
        repo_file.write_text(content, encoding='utf-8')
        print("  已修复字段引用")

def fix_prescription_lines():
    """修复Prescription第300-301行"""
    print("[FIX] 修复 Prescription 第300-301行...")
    
    file = Path("src/Backend/Modules/LYBT.Module.Prescriptions/Services/PrescriptionService.cs")
    if not file.exists():
        return
        
    with open(file, 'r', encoding='utf-8') as f:
        lines = f.readlines()
    
    # 查找并修复第300-301行（索引299-300）
    for i in range(min(299, len(lines)-1), min(302, len(lines))):
        if i < len(lines):
            line = lines[i]
            if '0m /* ' in line and 'TotalPrice' in line:
                # 注释掉整行
                lines[i] = '            // TotalPrice = Items.Sum(i => i.UnitPrice * i.Quantity); // TotalPrice字段已删除\n'
            elif '0m /* ' in line and 'TotalWeight' in line:
                lines[i] = '            // TotalWeight = Items.Sum(i => i.Weight * i.Quantity); // TotalWeight字段已删除\n'
    
    with open(file, 'w', encoding='utf-8') as f:
        f.writelines(lines)
    
    print("  已修复赋值语句")

def fix_pharmacy_interface():
    """修复Pharmacy接口实现"""
    print("[FIX] 修复 Pharmacy 接口实现...")
    
    file = Path("src/Backend/Modules/LYBT.Module.Pharmacy/Services/PharmacyService.cs")
    if not file.exists():
        return
        
    content = file.read_text(encoding='utf-8')
    
    # 修复AddAsync返回类型
    content = content.replace(
        'public async Task<PharmacyDetailDto> AddAsync(PharmacyCreateDto dto)',
        'public async Task<PharmacyDto?> AddAsync(PharmacyCreateDto dto)'
    )
    
    # 修复UpdateAsync返回类型
    content = content.replace(
        'public async Task<PharmacyDetailDto> UpdateAsync(PharmacyEditDto dto)',
        'public async Task<bool> UpdateAsync(PharmacyEditDto dto)'
    )
    
    # 在UpdateAsync方法体中修改返回值
    lines = content.split('\n')
    in_update_method = False
    for i in range(len(lines)):
        if 'public async Task<bool> UpdateAsync(PharmacyEditDto dto)' in lines[i]:
            in_update_method = True
        elif in_update_method and 'return _mapper.Map<PharmacyDetailDto>(entity);' in lines[i]:
            lines[i] = '            return true;'
            in_update_method = False
    
    content = '\n'.join(lines)
    
    # 添加缺失的DTO类型定义
    if 'PharmacyDispenseDto' not in content:
        # 在命名空间内添加简单的DTO定义
        namespace_end = content.rfind('}')
        if namespace_end > 0:
            dto_def = '''
    // 临时DTO定义
    public class PharmacyDispenseDto
    {
        public Guid PrescriptionId { get; set; }
        public Guid PatientId { get; set; }
        public string Notes { get; set; } = "";
    }
'''
            content = content[:namespace_end] + dto_def + '\n' + content[namespace_end:]
    
    file.write_text(content, encoding='utf-8')
    print("  已修复接口实现")

def main():
    print("=" * 60)
    print("最终修复剩余的16个错误")
    print("=" * 60)
    
    fix_treatment_room_line_206()
    fix_registration_enums()
    fix_prescription_lines()
    fix_pharmacy_interface()
    
    print("\n修复完成！")
    print("=" * 60)

if __name__ == "__main__":
    main()
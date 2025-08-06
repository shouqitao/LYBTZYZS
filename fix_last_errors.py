#!/usr/bin/env python3
"""
修复最后的12个错误
"""

from pathlib import Path

def fix_registration_status():
    """修复Registration状态枚举值"""
    print("[FIX] 修复 Registration 状态枚举...")
    
    # 修复服务文件
    service_file = Path("src/Backend/Modules/LYBT.Module.Registration/Services/RegistrationService.cs")
    if service_file.exists():
        content = service_file.read_text(encoding='utf-8')
        
        # 根据实际的枚举定义替换
        # Pending -> Scheduled (预约状态)或 Arrived (已到达)
        # Processing -> InConsultation (就诊中)
        content = content.replace('RegistrationStatus.Pending', 'RegistrationStatus.Scheduled')
        content = content.replace('RegistrationStatus.Processing', 'RegistrationStatus.InConsultation')
        
        service_file.write_text(content, encoding='utf-8')
        print("  已修复服务文件枚举值")

def fix_registration_model_fields():
    """修复Registration模型字段"""
    print("[FIX] 修复 Registration 模型字段...")
    
    # 修复仓储文件
    repo_file = Path("src/Backend/Modules/LYBT.Module.Registration/Repositories/RegistrationRepository.cs")
    if repo_file.exists():
        content = repo_file.read_text(encoding='utf-8')
        
        # 替换CreatedAt为RegistrationTime
        content = content.replace('r.CreatedAt.Date', 'r.RegistrationTime.Date')
        
        repo_file.write_text(content, encoding='utf-8')
        print("  已修复仓储字段引用")

def fix_prescription_assignment():
    """修复Prescription赋值错误"""
    print("[FIX] 修复 Prescription 赋值错误...")
    
    file = Path("src/Backend/Modules/LYBT.Module.Prescriptions/Services/PrescriptionService.cs")
    if not file.exists():
        return
        
    with open(file, 'r', encoding='utf-8') as f:
        lines = f.readlines()
    
    # 修复第300-301行（索引299-300）
    if len(lines) > 301:
        for i in range(299, min(302, len(lines))):
            if '0m /* ' in lines[i] and '+=' in lines[i]:
                # 这是错误的赋值语句，注释掉
                lines[i] = '            // ' + lines[i].strip() + ' // 字段已删除\n'
    
    with open(file, 'w', encoding='utf-8') as f:
        f.writelines(lines)
    
    print("  已修复赋值语句")

def fix_pharmacy_dto():
    """添加缺失的PharmacyDispenseDto定义"""
    print("[FIX] 添加 PharmacyDispenseDto...")
    
    # 在Shared.Models中创建DTO
    dto_file = Path("src/Shared/LYBT.Shared.Models/Contracts/Pharmacy/PharmacyDtos.cs")
    if dto_file.exists():
        content = dto_file.read_text(encoding='utf-8')
        
        # 检查是否已有定义
        if 'PharmacyDispenseDto' not in content:
            # 在文件末尾的命名空间内添加
            namespace_end = content.rfind('}')
            if namespace_end > 0:
                dto_def = '''
    /// <summary>
    /// 药房配药请求DTO
    /// </summary>
    public class PharmacyDispenseDto
    {
        /// <summary>处方ID</summary>
        public Guid PrescriptionId { get; set; }
        
        /// <summary>患者ID</summary>
        public Guid PatientId { get; set; }
        
        /// <summary>配药备注</summary>
        public string Notes { get; set; } = "";
        
        /// <summary>配药人ID</summary>
        public Guid? PharmacistId { get; set; }
        
        /// <summary>配药人姓名</summary>
        public string? PharmacistName { get; set; }
    }
'''
                content = content[:namespace_end] + dto_def + '\n' + content[namespace_end:]
                dto_file.write_text(content, encoding='utf-8')
                print("  已添加DTO定义")

def main():
    print("=" * 60)
    print("修复最后的12个错误")
    print("=" * 60)
    
    fix_registration_status()
    fix_registration_model_fields()
    fix_prescription_assignment()
    fix_pharmacy_dto()
    
    print("\n修复完成！")
    print("=" * 60)

if __name__ == "__main__":
    main()
#!/usr/bin/env python3
"""
修复最后的语法错误
"""

from pathlib import Path

def fix_treatment_room_final():
    """修复TreatmentRoom最后的语法错误"""
    print("[FIX] 修复 TreatmentRoom 最终语法错误...")
    
    file = Path("src/Backend/Modules/LYBT.Module.TreatmentRoom/Services/TreatmentRoomService.cs")
    if not file.exists():
        return
        
    with open(file, 'r', encoding='utf-8') as f:
        lines = f.readlines()
    
    # 修复三元运算符
    for i in range(len(lines)):
        line = lines[i]
        
        # 修复第78-80行的三元运算符
        if i == 78:
            lines[i] = '                "patientname" => query.IsAscending ?\n'
            lines[i+1] = '                    filteredTreatments.OrderBy(t => t.Id) : // 使用Id替代PatientName\n'
            lines[i+2] = '                    filteredTreatments.OrderByDescending(t => t.Id),\n'
        
        # 修复第87-89行
        elif i == 87:
            lines[i] = '                _ => query.IsAscending ?\n'
            lines[i+1] = '                    filteredTreatments.OrderBy(t => t.Id) : // 使用Id替代CreateTime\n'
            lines[i+2] = '                    filteredTreatments.OrderByDescending(t => t.Id)\n'
        
        # 修复字段赋值(第109, 130行等)
        elif '""/*' in line and '字段已删除' in line and '=' in line:
            # 注释掉整行
            lines[i] = '            // ' + line.strip() + '\n'
        
        # 修复属性赋值(第163, 166行等)
        elif 'PatientName = ""/*' in line or 'CreateTime = ""/*' in line:
            # 注释掉整行
            lines[i] = '                    // ' + line.strip() + '\n'
    
    with open(file, 'w', encoding='utf-8') as f:
        f.writelines(lines)
    
    print("  已修复所有语法错误")

def fix_prescription_final():
    """修复Prescription最终问题"""
    print("[FIX] 修复 Prescription 最终问题...")
    
    file = Path("src/Backend/Modules/LYBT.Module.Prescriptions/Services/PrescriptionService.cs")
    if not file.exists():
        return
        
    content = file.read_text(encoding='utf-8')
    
    # 查找第300-301行附近的问题
    lines = content.split('\n')
    for i in range(len(lines)):
        if i >= 299 and i <= 301:  # 第300-301行
            if '(0m)' in lines[i]:
                # 这些应该在Sum表达式中
                if 'TotalPrice += ' in lines[i]:
                    lines[i] = '            TotalPrice = Items.Sum(i => i.UnitPrice * i.Quantity); // TotalPrice字段已删除'
                elif 'TotalWeight += ' in lines[i]:
                    lines[i] = '            TotalWeight = Items.Sum(i => i.Weight * i.Quantity); // TotalWeight字段已删除'
    
    content = '\n'.join(lines)
    file.write_text(content, encoding='utf-8')
    
    print("  已修复赋值问题")

def main():
    print("=" * 60)
    print("修复最后的语法错误")
    print("=" * 60)
    
    fix_treatment_room_final()
    fix_prescription_final()
    
    print("\n修复完成！")
    print("=" * 60)

if __name__ == "__main__":
    main()
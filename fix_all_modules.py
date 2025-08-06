#!/usr/bin/env python3
"""
综合修复所有模块的编译错误
"""

import os
import sys
import subprocess
from pathlib import Path
from typing import List, Dict, Tuple

class ModuleFixer:
    def __init__(self, root_path: Path):
        self.root = root_path
        self.fixes_applied = []
        
    def log(self, message: str):
        """输出日志信息"""
        print(f"[修复] {message}")
        
    def fix_treatment_room_duplicate(self):
        """修复 TreatmentRoom 模块的重复定义"""
        self.log("修复 TreatmentRoom 模块重复定义...")
        
        # 删除重复的 TreatmentService.cs
        duplicate_file = self.root / "src/Backend/Modules/LYBT.Module.TreatmentRoom/Services/TreatmentService.cs"
        if duplicate_file.exists():
            duplicate_file.unlink()
            self.log(f"  删除重复文件: {duplicate_file.name}")
            self.fixes_applied.append("删除 TreatmentRoom/TreatmentService.cs")
            
    def fix_medical_case_automapper(self):
        """修复 MedicalCase 模块的 AutoMapper 依赖"""
        self.log("修复 MedicalCase 模块 AutoMapper 依赖...")
        
        csproj_path = self.root / "src/Backend/Modules/LYBT.Module.MedicalCase/LYBT.Module.MedicalCase.csproj"
        if csproj_path.exists():
            content = csproj_path.read_text(encoding='utf-8')
            if 'AutoMapper.Extensions.Microsoft.DependencyInjection' not in content:
                # 在 ItemGroup 中添加 AutoMapper 包引用
                new_package = '    <PackageReference Include="AutoMapper.Extensions.Microsoft.DependencyInjection" Version="12.0.1" />'
                content = content.replace('  </ItemGroup>', f'{new_package}\n  </ItemGroup>', 1)
                csproj_path.write_text(content, encoding='utf-8')
                self.log("  添加 AutoMapper 包引用")
                self.fixes_applied.append("MedicalCase 添加 AutoMapper 依赖")
                
    def fix_consultation_automapper(self):
        """修复 Consultation 模块的 AutoMapper 依赖"""
        self.log("修复 Consultation 模块 AutoMapper 依赖...")
        
        csproj_path = self.root / "src/Backend/Modules/LYBT.Module.Consultation/LYBT.Module.Consultation.csproj"
        if csproj_path.exists():
            content = csproj_path.read_text(encoding='utf-8')
            if 'AutoMapper.Extensions.Microsoft.DependencyInjection' not in content:
                new_package = '    <PackageReference Include="AutoMapper.Extensions.Microsoft.DependencyInjection" Version="12.0.1" />'
                content = content.replace('  </ItemGroup>', f'{new_package}\n  </ItemGroup>', 1)
                csproj_path.write_text(content, encoding='utf-8')
                self.log("  添加 AutoMapper 包引用")
                self.fixes_applied.append("Consultation 添加 AutoMapper 依赖")
                
    def fix_formula_module_dependencies(self):
        """修复 Formula 模块的依赖"""
        self.log("修复 Formula 模块依赖...")
        
        csproj_path = self.root / "src/Backend/Modules/LYBT.Module.Formula/LYBT.Module.Formula.csproj"
        if csproj_path.exists():
            content = csproj_path.read_text(encoding='utf-8')
            
            # 检查是否缺少必要的项目引用
            if 'LYBT.Infrastructure' not in content:
                # 添加 Infrastructure 项目引用
                project_refs = '''  <ItemGroup>
    <ProjectReference Include="..\\..\\Core\\LYBT.Infrastructure\\LYBT.Infrastructure.csproj" />
    <ProjectReference Include="..\\..\\Core\\LYBT.Models\\LYBT.Models.csproj" />
    <ProjectReference Include="..\\..\\..\\Shared\\LYBT.Shared.Models\\LYBT.Shared.Models.csproj" />
  </ItemGroup>'''
                
                # 在 </Project> 前插入
                content = content.replace('</Project>', f'{project_refs}\n</Project>')
                csproj_path.write_text(content, encoding='utf-8')
                self.log("  添加项目引用")
                self.fixes_applied.append("Formula 添加项目引用")
                
            # 添加必要的包引用
            if 'AutoMapper.Extensions.Microsoft.DependencyInjection' not in content:
                content = csproj_path.read_text(encoding='utf-8')
                new_packages = '''  <ItemGroup>
    <PackageReference Include="AutoMapper.Extensions.Microsoft.DependencyInjection" Version="12.0.1" />
    <PackageReference Include="Microsoft.Extensions.DependencyInjection.Abstractions" Version="8.0.0" />
    <PackageReference Include="Microsoft.Extensions.Logging.Abstractions" Version="8.0.0" />
  </ItemGroup>'''
                content = content.replace('</Project>', f'{new_packages}\n</Project>')
                csproj_path.write_text(content, encoding='utf-8')
                self.log("  添加包引用")
                self.fixes_applied.append("Formula 添加包依赖")
                
    def fix_cashier_module(self):
        """修复 Cashier 模块"""
        self.log("修复 Cashier 模块...")
        
        # 修复 ICashierRepository 中的引用
        repo_file = self.root / "src/Backend/Modules/LYBT.Module.Cashier/Interfaces/ICashierRepository.cs"
        if repo_file.exists():
            content = repo_file.read_text(encoding='utf-8')
            # 添加正确的 using
            if 'using LYBT.Models.Cashier;' not in content:
                content = 'using LYBT.Models.Cashier;\n' + content
                repo_file.write_text(content, encoding='utf-8')
                self.log("  修复 ICashierRepository 引用")
                self.fixes_applied.append("Cashier Repository 添加模型引用")
                
    def fix_pharmacy_model_references(self):
        """修复 Pharmacy 模块的模型引用"""
        self.log("修复 Pharmacy 模块模型引用...")
        
        # PharmacyModel 已被简化，需要更新 Service
        service_file = self.root / "src/Backend/Modules/LYBT.Module.Pharmacy/Services/PharmacyService.cs"
        if service_file.exists():
            self.log("  需要手动更新 PharmacyService 以适配新的模型结构")
            self.fixes_applied.append("Pharmacy 需要手动更新服务代码")
            
    def fix_prescription_enums(self):
        """修复 Prescription 模块的枚举引用"""
        self.log("修复 Prescription 模块枚举引用...")
        
        # PrescriptionStatus 枚举需要更新
        enum_file = self.root / "src/Shared/LYBT.Shared.Models/Enums/PrescriptionStatus.cs"
        if not enum_file.exists():
            # 创建枚举文件
            enum_file.parent.mkdir(parents=True, exist_ok=True)
            enum_content = '''namespace LYBT.Shared.Models.Enums
{
    /// <summary>
    /// 处方状态枚举
    /// </summary>
    public enum PrescriptionStatus
    {
        /// <summary>
        /// 待处理
        /// </summary>
        Pending = 0,
        
        /// <summary>
        /// 已审核
        /// </summary>
        Approved = 1,
        
        /// <summary>
        /// 配药中
        /// </summary>
        Dispensing = 2,
        
        /// <summary>
        /// 已完成
        /// </summary>
        Completed = 3,
        
        /// <summary>
        /// 已取消
        /// </summary>
        Cancelled = 4
    }
}'''
            enum_file.write_text(enum_content, encoding='utf-8')
            self.log("  创建 PrescriptionStatus 枚举")
            self.fixes_applied.append("创建 PrescriptionStatus 枚举")
            
    def remove_deleted_module_references(self):
        """移除对已删除模块的引用"""
        self.log("移除已删除模块的引用...")
        
        # 从 WebAPI 项目中移除已删除模块的引用
        webapi_csproj = self.root / "src/Backend/Services/LYBT.WebAPI/LYBT.WebAPI.csproj"
        if webapi_csproj.exists():
            content = webapi_csproj.read_text(encoding='utf-8')
            
            deleted_modules = [
                'LYBT.Module.Billing',
                'LYBT.Module.DiagnosisTreatment', 
                'LYBT.Module.Records',
                'LYBT.Module.Sync',
                'LYBT.Module.Diagnostics'
            ]
            
            modified = False
            for module in deleted_modules:
                if module in content:
                    # 移除整行引用
                    lines = content.split('\n')
                    new_lines = [line for line in lines if module not in line]
                    content = '\n'.join(new_lines)
                    modified = True
                    self.log(f"  移除 {module} 引用")
                    
            if modified:
                webapi_csproj.write_text(content, encoding='utf-8')
                self.fixes_applied.append("WebAPI 移除已删除模块引用")
                
    def run_all_fixes(self):
        """执行所有修复"""
        self.log("开始执行所有修复...")
        
        self.fix_treatment_room_duplicate()
        self.fix_medical_case_automapper()
        self.fix_consultation_automapper()
        self.fix_formula_module_dependencies()
        self.fix_cashier_module()
        self.fix_pharmacy_model_references()
        self.fix_prescription_enums()
        self.remove_deleted_module_references()
        
        self.log("\n修复完成！")
        self.log(f"共应用了 {len(self.fixes_applied)} 个修复:")
        for fix in self.fixes_applied:
            self.log(f"  - {fix}")
            
        return len(self.fixes_applied) > 0

def main():
    root_path = Path.cwd()
    
    fixer = ModuleFixer(root_path)
    success = fixer.run_all_fixes()
    
    if success:
        print("\n现在尝试重新编译项目...")
        # 可选：自动运行编译
        # subprocess.run(["dotnet", "build", "LYBT.Backend.sln"])
    else:
        print("\n没有需要修复的内容")

if __name__ == "__main__":
    main()
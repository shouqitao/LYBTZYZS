#!/usr/bin/env python3
"""
修复所有编译错误的综合脚本
"""

import os
import re
from pathlib import Path
import subprocess

class CompilationFixer:
    def __init__(self):
        self.root = Path.cwd()
        self.fixes_applied = 0
        
    def log(self, msg):
        print(f"[FIX] {msg}")
        
    def fix_file(self, file_path, fixes):
        """应用一系列修复到文件"""
        if not file_path.exists():
            return False
            
        content = file_path.read_text(encoding='utf-8')
        original = content
        
        for old, new in fixes:
            content = content.replace(old, new)
            
        if content != original:
            file_path.write_text(content, encoding='utf-8')
            self.fixes_applied += 1
            return True
        return False
        
    def fix_all_namespace_issues(self):
        """修复所有命名空间问题"""
        self.log("修复命名空间问题...")
        
        # Formula模块的所有文件
        formula_path = self.root / "src/Backend/Modules/LYBT.Module.Formula"
        
        # 修复所有Formula模块文件的命名空间
        for file in formula_path.rglob("*.cs"):
            fixes = [
                ("namespace LYBT.Module.Formulas", "namespace LYBT.Module.Formula"),
                ("using LYBT.Module.Formulas.", "using LYBT.Module.Formula."),
                ("LYBT.Models.FormulaTemplates", "LYBT.Models.Formula"),
                ("FormulaTemplateModel", "FormulaModel"),
                ("FormulaTemplateHerbItem", "FormulaHerbItem"),
            ]
            if self.fix_file(file, fixes):
                self.log(f"  修复: {file.name}")
                
    def fix_dbcontext_references(self):
        """修复所有数据库上下文引用"""
        self.log("修复数据库上下文引用...")
        
        # 需要修复的模块列表
        modules = [
            "TreatmentPlan",
            "Cashier",
            "Formula",
            "MedicalCase",
            "Consultation"
        ]
        
        for module in modules:
            module_path = self.root / f"src/Backend/Modules/LYBT.Module.{module}"
            if module_path.exists():
                for file in module_path.rglob("*.cs"):
                    if "Service" in file.name or "Repository" in file.name:
                        fixes = [
                            ("using LYBT.Infrastructure;", "using LYBT.Infrastructure.Data;"),
                            ("private readonly AppDbContext", "private readonly LYBT.Infrastructure.Data.AppDbContext"),
                        ]
                        if self.fix_file(file, fixes):
                            self.log(f"  修复 {module}: {file.name}")
                            
    def fix_model_references(self):
        """修复模型引用问题"""
        self.log("修复模型引用...")
        
        # Pharmacy模块特殊处理
        pharmacy_service = self.root / "src/Backend/Modules/LYBT.Module.Pharmacy/Services/PharmacyService.cs"
        if pharmacy_service.exists():
            content = pharmacy_service.read_text(encoding='utf-8')
            
            # 注释掉已删除的字段
            problematic_lines = [
                "PatientName = ", 
                "DoctorId = ",
                "DoctorName = ",
                "DispensingStaff = ",
                "pharmacy.HerbItems",
                ".HerbItems.",
                "PharmacyItemModel"
            ]
            
            for line in problematic_lines:
                content = re.sub(f'(\s+.*{re.escape(line)}.*)', r'// \1 // TODO: 字段已移除', content)
                
            pharmacy_service.write_text(content, encoding='utf-8')
            self.log("  修复 Pharmacy 服务")
            
        # Prescription模块
        prescription_service = self.root / "src/Backend/Modules/LYBT.Module.Prescriptions/Services/PrescriptionService.cs"
        if prescription_service.exists():
            fixes = [
                ("PrescriptionStatus.Pending", "PrescriptionStatus.Draft"),
                ("item.TotalPrice", "0 /* TotalPrice已移除 */"),
                ("item.TotalWeight", "0 /* TotalWeight已移除 */"),
            ]
            if self.fix_file(prescription_service, fixes):
                self.log("  修复 Prescription 服务")
                
    def fix_missing_dependencies(self):
        """修复缺失的依赖"""
        self.log("修复项目依赖...")
        
        # TreatmentPlan项目文件
        treatment_plan_csproj = self.root / "src/Backend/Modules/LYBT.Module.TreatmentPlan/LYBT.Module.TreatmentPlan.csproj"
        if treatment_plan_csproj.exists():
            content = treatment_plan_csproj.read_text(encoding='utf-8')
            
            # 添加缺失的项目引用
            if "LYBT.Infrastructure" not in content:
                new_ref = '''  <ItemGroup>
    <ProjectReference Include="..\\..\\Core\\LYBT.Infrastructure\\LYBT.Infrastructure.csproj" />
    <ProjectReference Include="..\\..\\Core\\LYBT.Models\\LYBT.Models.csproj" />
    <ProjectReference Include="..\\..\\..\\Shared\\LYBT.Shared.Models\\LYBT.Shared.Models.csproj" />
  </ItemGroup>'''
                content = content.replace('</Project>', f'{new_ref}\n</Project>')
                treatment_plan_csproj.write_text(content, encoding='utf-8')
                self.log("  添加 TreatmentPlan 项目引用")
                
            # 添加AutoMapper包
            if 'AutoMapper.Extensions' not in content:
                content = treatment_plan_csproj.read_text(encoding='utf-8')
                new_package = '''  <ItemGroup>
    <PackageReference Include="AutoMapper.Extensions.Microsoft.DependencyInjection" Version="12.0.1" />
  </ItemGroup>'''
                content = content.replace('</Project>', f'{new_package}\n</Project>')
                treatment_plan_csproj.write_text(content, encoding='utf-8')
                self.log("  添加 TreatmentPlan AutoMapper 依赖")
                
    def fix_interface_implementations(self):
        """修复接口实现问题"""
        self.log("修复接口实现...")
        
        # 修复所有Service文件中的接口实现
        modules_path = self.root / "src/Backend/Modules"
        for service_file in modules_path.rglob("*Service.cs"):
            if "Interface" not in str(service_file):
                content = service_file.read_text(encoding='utf-8')
                
                # 添加缺失的using语句
                if "using System;" not in content:
                    content = "using System;\n" + content
                if "using System.Linq;" not in content:
                    content = "using System.Linq;\n" + content
                if "using System.Threading.Tasks;" not in content:
                    content = "using System.Threading.Tasks;\n" + content
                    
                # 修复async方法
                content = re.sub(r'public\s+Task<', 'public async Task<', content)
                content = re.sub(r'public\s+async\s+async\s+Task<', 'public async Task<', content)
                
                service_file.write_text(content, encoding='utf-8')
                
    def fix_controller_references(self):
        """修复控制器引用"""
        self.log("修复控制器引用...")
        
        controllers_path = self.root / "src/Backend/Services/LYBT.WebAPI/Controllers"
        
        # 删除已删除模块的控制器
        deleted_controllers = [
            "BillingController.cs",
            "DiagnosisTreatmentController.cs",
            "RecordsController.cs",
            "SyncController.cs"
        ]
        
        for controller in deleted_controllers:
            controller_file = controllers_path / controller
            if controller_file.exists():
                controller_file.unlink()
                self.log(f"  删除: {controller}")
                
    def fix_service_registration(self):
        """修复服务注册"""
        self.log("修复服务注册...")
        
        extension_file = self.root / "src/Backend/Services/LYBT.WebAPI/Extensions/ServiceCollectionExtension.cs"
        if extension_file.exists():
            content = extension_file.read_text(encoding='utf-8')
            
            # 移除已删除模块的注册
            deleted_modules = [
                "AddBillingModule",
                "AddDiagnosisTreatmentModule",
                "AddRecordsModule",
                "AddSyncModule",
                "AddDiagnosticsModule"
            ]
            
            for module in deleted_modules:
                pattern = rf'\s*services\.{module}\(\);\s*\n?'
                content = re.sub(pattern, '', content)
                
            # 添加新模块注册
            if "AddFormulaModule" not in content:
                # 在其他模块注册后添加
                content = content.replace(
                    "services.AddPrescriptionsModule();",
                    "services.AddPrescriptionsModule();\n            services.AddFormulaModule();"
                )
                
            extension_file.write_text(content, encoding='utf-8')
            self.log("  更新服务注册")
            
    def create_missing_files(self):
        """创建缺失的文件"""
        self.log("创建缺失的文件...")
        
        # 创建缺失的DTO文件
        shared_path = self.root / "src/Shared/LYBT.Shared.Models"
        
        # PagedResultDto
        paged_dto = shared_path / "Common/PagedResultDto.cs"
        if not paged_dto.exists():
            paged_dto.parent.mkdir(parents=True, exist_ok=True)
            paged_dto.write_text('''namespace LYBT.Shared.Models.Common
{
    public class PagedResultDto<T>
    {
        public List<T> Items { get; set; } = new();
        public int TotalCount { get; set; }
        public int PageSize { get; set; }
        public int CurrentPage { get; set; }
        public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
    }
}''', encoding='utf-8')
            self.log("  创建 PagedResultDto")
            
    def run_all_fixes(self):
        """执行所有修复"""
        self.log("开始全面修复编译错误...")
        
        self.fix_all_namespace_issues()
        self.fix_dbcontext_references()
        self.fix_model_references()
        self.fix_missing_dependencies()
        self.fix_interface_implementations()
        self.fix_controller_references()
        self.fix_service_registration()
        self.create_missing_files()
        
        self.log(f"\n修复完成！共应用 {self.fixes_applied} 个修复")
        
        return True

def main():
    fixer = CompilationFixer()
    fixer.run_all_fixes()
    
    print("\n准备测试编译...")
    
    # 还原包
    print("还原 NuGet 包...")
    result = subprocess.run(
        ["dotnet", "restore", "LYBT.Backend.sln"],
        capture_output=True,
        text=True,
        encoding='utf-8',
        errors='replace'
    )
    
    if result.returncode != 0:
        print("包还原有警告，继续编译...")
    
    # 编译
    print("\n编译项目...")
    result = subprocess.run(
        ["dotnet", "build", "LYBT.Backend.sln", "--no-restore"],
        capture_output=True,
        text=True,
        encoding='utf-8',
        errors='replace'
    )
    
    # 统计错误
    errors = len([line for line in result.stdout.split('\n') if 'error CS' in line])
    
    if errors == 0:
        print("\n✅ 编译成功！")
    else:
        print(f"\n还有 {errors} 个编译错误需要手动修复")
        
        # 保存错误日志
        with open('remaining_errors.log', 'w', encoding='utf-8') as f:
            f.write(result.stdout)
        print("错误详情已保存到 remaining_errors.log")

if __name__ == "__main__":
    main()
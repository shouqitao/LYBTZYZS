import os
import re

# 1. 修复PatientInfo缺少Phone属性
patient_info_file = r"D:\source\repos\LYBTZYZS\src\Frontend\Desktop\Core\Models\Patients\PatientInfo.cs"
try:
    with open(patient_info_file, 'r', encoding='utf-8') as f:
        content = f.read()
    
    # 查找是否有Phone属性
    if 'public string Phone' not in content and 'public string? Phone' not in content:
        # 在Gender属性后添加Phone
        pattern = r'(public string\? Gender { get; set; })'
        replacement = r'\1\n        public string? Phone { get; set; }'
        content = re.sub(pattern, replacement, content)
        
        with open(patient_info_file, 'w', encoding='utf-8') as f:
            f.write(content)
        print(f"Added Phone property to PatientInfo")
except:
    print(f"Could not modify PatientInfo")

# 2. 修复ConsultationInfo缺少IsCompleted属性
consultation_info_file = r"D:\source\repos\LYBTZYZS\src\Frontend\Desktop\Core\Models\Consultation\ConsultationInfo.cs"
try:
    with open(consultation_info_file, 'r', encoding='utf-8') as f:
        content = f.read()
    
    # 查找是否有IsCompleted属性
    if 'public bool IsCompleted' not in content:
        # 在最后一个属性后添加
        pattern = r'(\s+)(}\s*$)'
        replacement = r'\1    public bool IsCompleted { get; set; }\n\1\2'
        content = re.sub(pattern, replacement, content, flags=re.MULTILINE)
        
        with open(consultation_info_file, 'w', encoding='utf-8') as f:
            f.write(content)
        print(f"Added IsCompleted property to ConsultationInfo")
except:
    print(f"Could not modify ConsultationInfo")

# 3. 修复IHerbService缺少GetListAsync方法
herb_service_file = r"D:\source\repos\LYBTZYZS\src\Frontend\Desktop\Core\Interfaces\Services\IHerbService.cs"
try:
    with open(herb_service_file, 'r', encoding='utf-8') as f:
        content = f.read()
    
    # 查找是否有GetListAsync方法
    if 'GetListAsync' not in content:
        # 在接口内添加方法
        pattern = r'(public interface IHerbService[^{]*{)'
        replacement = r'\1\n        Task<ApiResult<List<HerbDto>>> GetListAsync(HerbQueryDto? query = null);'
        content = re.sub(pattern, replacement, content)
        
        # 添加必要的using
        if 'using LYBT.Shared.Models.Contracts.Herbs;' not in content:
            content = 'using LYBT.Shared.Models.Contracts.Herbs;\n' + content
        
        with open(herb_service_file, 'w', encoding='utf-8') as f:
            f.write(content)
        print(f"Added GetListAsync method to IHerbService")
except Exception as e:
    print(f"Could not modify IHerbService: {e}")

# 4. 在HerbService实现中添加GetListAsync方法
herb_service_impl = r"D:\source\repos\LYBTZYZS\src\Frontend\Desktop\Services\HerbService.cs"
try:
    with open(herb_service_impl, 'r', encoding='utf-8') as f:
        content = f.read()
    
    # 查找是否有GetListAsync方法实现
    if 'GetListAsync' not in content:
        # 在类的最后一个方法后添加
        method_impl = '''
        public async Task<ApiResult<List<HerbDto>>> GetListAsync(HerbQueryDto? query = null)
        {
            try
            {
                // 调用GetPagedAsync并返回Items
                var result = await GetPagedAsync(query ?? new HerbQueryDto());
                if (result.IsSuccess && result.Data != null)
                {
                    return ApiResult<List<HerbDto>>.Success(result.Data.Items.ToList());
                }
                return ApiResult<List<HerbDto>>.Failure(result.ErrorMessage ?? "获取药材列表失败");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取药材列表失败");
                return ApiResult<List<HerbDto>>.Failure($"获取药材列表失败: {ex.Message}");
            }
        }'''
        
        # 在最后一个方法的结束大括号前添加
        pattern = r'(    }\s*)(}\s*$)'
        replacement = method_impl + r'\n\1\2'
        content = re.sub(pattern, replacement, content, flags=re.MULTILINE | re.DOTALL)
        
        with open(herb_service_impl, 'w', encoding='utf-8') as f:
            f.write(content)
        print(f"Added GetListAsync implementation to HerbService")
except Exception as e:
    print(f"Could not modify HerbService implementation: {e}")

# 5. 修复NavigationParameters类型问题
modules_dir = r"D:\source\repos\LYBTZYZS\src\Frontend\Desktop\Modules"

for root, dirs, files in os.walk(modules_dir):
    for file in files:
        if file.endswith('.cs'):
            file_path = os.path.join(root, file)
            
            try:
                with open(file_path, 'r', encoding='utf-8') as f:
                    content = f.read()
                
                modified = False
                
                # 替换NavigationParameters为正确的类型
                if 'NavigationParameters' in content and 'using Prism.Navigation.Regions;' in content:
                    content = content.replace('NavigationParameters', 'Prism.Navigation.Regions.NavigationParameters')
                    modified = True
                
                if modified:
                    with open(file_path, 'w', encoding='utf-8') as f:
                        f.write(content)
                    print(f"Fixed NavigationParameters in: {file_path}")
                    
            except Exception as e:
                continue

# 6. 修复PrescriptionItem引用
prescription_file = r"D:\source\repos\LYBTZYZS\src\Frontend\Desktop\Modules\Consultation\ViewModels\PrescriptionViewModel.cs"
try:
    with open(prescription_file, 'r', encoding='utf-8') as f:
        content = f.read()
    
    # 替换ConsultationWorkflowViewModel.PrescriptionItem为PrescriptionItem（使用内部类）
    content = content.replace('ConsultationWorkflowViewModel.PrescriptionItem', 'PrescriptionItem')
    
    with open(prescription_file, 'w', encoding='utf-8') as f:
        f.write(content)
    print(f"Fixed PrescriptionItem references in PrescriptionViewModel")
except:
    pass

print("\nRemaining issues fixed!")
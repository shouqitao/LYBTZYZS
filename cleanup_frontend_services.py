#!/usr/bin/env python3
"""
清理前端服务层中的无用文件
"""

import os
import shutil
from pathlib import Path

def cleanup_services():
    """删除无用的服务文件"""
    services_dir = Path("src/Frontend/Desktop/Services")
    
    # 要删除的服务文件
    files_to_delete = [
        "BillingService.cs",
        "DoctorService.cs",
        "RecordService.cs",
        "RegistrationService.cs",
        "TreatmentRoomService.cs",
        "Interfaces/IBillingApiService.cs",
        "Interfaces/IDiagnosisTreatmentApiService.cs",
        "Interfaces/IDoctorsApiService.cs",
        "Interfaces/ILogsApiService.cs",
        "Interfaces/IQueueingApiService.cs",
        "Interfaces/IRecordsApiService.cs",
        "Interfaces/IRegistrationApiService.cs",
        "Interfaces/ISyncApiService.cs",
        "Interfaces/ITreatmentRoomApiService.cs"
    ]
    
    deleted = 0
    for file_name in files_to_delete:
        file_path = services_dir / file_name
        if file_path.exists():
            file_path.unlink()
            print(f"Deleted: {file_path}")
            deleted += 1
    
    return deleted

def cleanup_infrastructure():
    """清理Infrastructure中的UserRole转换器"""
    file_path = Path("src/Frontend/Desktop/Infrastructure/JsonConverters/UserRoleJsonConverter.cs")
    if file_path.exists():
        file_path.unlink()
        print(f"Deleted: {file_path}")
        return 1
    return 0

def cleanup_obj_bin():
    """清理obj和bin目录"""
    dirs_to_clean = [
        "src/Frontend/Desktop/Services/obj",
        "src/Frontend/Desktop/Services/bin",
        "src/Frontend/Desktop/Infrastructure/obj",
        "src/Frontend/Desktop/Infrastructure/bin",
        "src/Frontend/Desktop/Modules/*/obj",
        "src/Frontend/Desktop/Modules/*/bin"
    ]
    
    cleaned = 0
    for pattern in dirs_to_clean:
        if "*" in pattern:
            # 处理通配符
            base_dir = Path(pattern.replace("/*", ""))
            if base_dir.exists():
                for sub_dir in base_dir.iterdir():
                    if sub_dir.is_dir():
                        for target in ["obj", "bin"]:
                            target_dir = sub_dir / target
                            if target_dir.exists():
                                shutil.rmtree(target_dir)
                                print(f"Cleaned: {target_dir}")
                                cleaned += 1
        else:
            dir_path = Path(pattern)
            if dir_path.exists():
                shutil.rmtree(dir_path)
                print(f"Cleaned: {dir_path}")
                cleaned += 1
    
    return cleaned

def main():
    print("Cleaning up frontend services...")
    
    # 1. 删除无用的服务文件
    deleted_services = cleanup_services()
    print(f"\nDeleted {deleted_services} service files")
    
    # 2. 删除UserRole转换器
    deleted_converter = cleanup_infrastructure()
    print(f"Deleted {deleted_converter} converter file")
    
    # 3. 清理obj和bin目录
    cleaned_dirs = cleanup_obj_bin()
    print(f"Cleaned {cleaned_dirs} obj/bin directories")
    
    print("\n=== Summary ===")
    print(f"Total files deleted: {deleted_services + deleted_converter}")
    print(f"Total directories cleaned: {cleaned_dirs}")

if __name__ == "__main__":
    main()
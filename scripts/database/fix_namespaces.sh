#!/bin/bash

# 批量修复LYBT.Models中的命名空间

# Auth模块
find "D:\source\repos\LYBTZYZS\LYBT.Models\Auth" -name "*.cs" -exec sed -i 's/namespace LYBT\.Module\.Auth\.Dtos/namespace LYBT.Models.Auth/g' {} \;

# Billing模块  
find "D:\source\repos\LYBTZYZS\LYBT.Models\Billing" -name "*.cs" -exec sed -i 's/namespace LYBT\.Module\.Billing\.Dtos/namespace LYBT.Models.Billing/g' {} \;

# DiagnosisTreatment模块
find "D:\source\repos\LYBTZYZS\LYBT.Models\DiagnosisTreatment" -name "*.cs" -exec sed -i 's/namespace LYBT\.Module\.DiagnosisTreatment\.Models\.Dtos/namespace LYBT.Models.DiagnosisTreatment/g' {} \;

# Doctors模块
find "D:\source\repos\LYBTZYZS\LYBT.Models\Doctors" -name "*.cs" -exec sed -i 's/namespace LYBT\.Module\.Doctors\.Dtos/namespace LYBT.Models.Doctors/g' {} \;

# FormulaTemplates模块
find "D:\source\repos\LYBTZYZS\LYBT.Models\FormulaTemplates" -name "*.cs" -exec sed -i 's/namespace LYBT\.Module\.FormulaTemplates\.Dtos/namespace LYBT.Models.FormulaTemplates/g' {} \;

# Herbs模块
find "D:\source\repos\LYBTZYZS\LYBT.Models\Herbs" -name "*.cs" -exec sed -i 's/namespace LYBT\.Module\.Herbs\.Dtos/namespace LYBT.Models.Herbs/g' {} \;

# Patients模块
find "D:\source\repos\LYBTZYZS\LYBT.Models\Patients" -name "*.cs" -exec sed -i 's/namespace LYBT\.Module\.Patients\.Dtos/namespace LYBT.Models.Patients/g' {} \;

# Pharmacy模块
find "D:\source\repos\LYBTZYZS\LYBT.Models\Pharmacy" -name "*.cs" -exec sed -i 's/namespace LYBT\.Module\.Pharmacy\.Dtos/namespace LYBT.Models.Pharmacy/g' {} \;

# Queueing模块
find "D:\source\repos\LYBTZYZS\LYBT.Models\Queueing" -name "*.cs" -exec sed -i 's/namespace LYBT\.Module\.Queueing\.Dtos/namespace LYBT.Models.Queueing/g' {} \;

# Records模块
find "D:\source\repos\LYBTZYZS\LYBT.Models\Records" -name "*.cs" -exec sed -i 's/namespace LYBT\.Module\.Records\.Dtos/namespace LYBT.Models.Records/g' {} \;

# Registration模块
find "D:\source\repos\LYBTZYZS\LYBT.Models\Registration" -name "*.cs" -exec sed -i 's/namespace LYBT\.Module\.Registration\.Dtos/namespace LYBT.Models.Registration/g' {} \;

# Sync模块
find "D:\source\repos\LYBTZYZS\LYBT.Models\Sync" -name "*.cs" -exec sed -i 's/namespace LYBT\.Module\.Sync\.Dtos/namespace LYBT.Models.Sync/g' {} \;

# TreatmentRoom模块
find "D:\source\repos\LYBTZYZS\LYBT.Models\TreatmentRoom" -name "*.cs" -exec sed -i 's/namespace LYBT\.Module\.TreatmentRoom\.Dtos/namespace LYBT.Models.TreatmentRoom/g' {} \;

# Users模块
find "D:\source\repos\LYBTZYZS\LYBT.Models\Users" -name "*.cs" -exec sed -i 's/namespace LYBT\.Module\.Users\.Dtos/namespace LYBT.Models.Users/g' {} \;
find "D:\source\repos\LYBTZYZS\LYBT.Models\Users" -name "*.cs" -exec sed -i 's/namespace LYBT\.Module\.Users\.Models/namespace LYBT.Models.Users/g' {} \;

echo "命名空间修复完成"
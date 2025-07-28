#!/bin/bash

echo "开始修复整个解决方案的Models引用..."

# 修复所有模块中的 using LYBT.Module.*.Models.Dtos; 引用
find "D:\source\repos\LYBTZYZS" -name "*.cs" -not -path "*/LYBT.Models/*" -exec sed -i 's/using LYBT\.Module\.Auth\.Models\.Dtos;/using LYBT.Models.Auth;/g' {} \;
find "D:\source\repos\LYBTZYZS" -name "*.cs" -not -path "*/LYBT.Models/*" -exec sed -i 's/using LYBT\.Module\.Billing\.Models\.Dtos;/using LYBT.Models.Billing;/g' {} \;
find "D:\source\repos\LYBTZYZS" -name "*.cs" -not -path "*/LYBT.Models/*" -exec sed -i 's/using LYBT\.Module\.DiagnosisTreatment\.Models\.Dtos;/using LYBT.Models.DiagnosisTreatment;/g' {} \;
find "D:\source\repos\LYBTZYZS" -name "*.cs" -not -path "*/LYBT.Models/*" -exec sed -i 's/using LYBT\.Module\.Doctors\.Models\.Dtos;/using LYBT.Models.Doctors;/g' {} \;
find "D:\source\repos\LYBTZYZS" -name "*.cs" -not -path "*/LYBT.Models/*" -exec sed -i 's/using LYBT\.Module\.FormulaTemplates\.Models\.Dtos;/using LYBT.Models.FormulaTemplates;/g' {} \;
find "D:\source\repos\LYBTZYZS" -name "*.cs" -not -path "*/LYBT.Models/*" -exec sed -i 's/using LYBT\.Module\.Herbs\.Models\.Dtos;/using LYBT.Models.Herbs;/g' {} \;
find "D:\source\repos\LYBTZYZS" -name "*.cs" -not -path "*/LYBT.Models/*" -exec sed -i 's/using LYBT\.Module\.Patients\.Models\.Dtos;/using LYBT.Models.Patients;/g' {} \;
find "D:\source\repos\LYBTZYZS" -name "*.cs" -not -path "*/LYBT.Models/*" -exec sed -i 's/using LYBT\.Module\.Pharmacy\.Models\.Dtos;/using LYBT.Models.Pharmacy;/g' {} \;
find "D:\source\repos\LYBTZYZS" -name "*.cs" -not -path "*/LYBT.Models/*" -exec sed -i 's/using LYBT\.Module\.Prescriptions\.Models\.Dtos;/using LYBT.Models.Prescriptions;/g' {} \;
find "D:\source\repos\LYBTZYZS" -name "*.cs" -not -path "*/LYBT.Models/*" -exec sed -i 's/using LYBT\.Module\.Queueing\.Models\.Dtos;/using LYBT.Models.Queueing;/g' {} \;
find "D:\source\repos\LYBTZYZS" -name "*.cs" -not -path "*/LYBT.Models/*" -exec sed -i 's/using LYBT\.Module\.Records\.Models\.Dtos;/using LYBT.Models.Records;/g' {} \;
find "D:\source\repos\LYBTZYZS" -name "*.cs" -not -path "*/LYBT.Models/*" -exec sed -i 's/using LYBT\.Module\.Registration\.Models\.Dtos;/using LYBT.Models.Registration;/g' {} \;
find "D:\source\repos\LYBTZYZS" -name "*.cs" -not -path "*/LYBT.Models/*" -exec sed -i 's/using LYBT\.Module\.Sync\.Models\.Dtos;/using LYBT.Models.Sync;/g' {} \;
find "D:\source\repos\LYBTZYZS" -name "*.cs" -not -path "*/LYBT.Models/*" -exec sed -i 's/using LYBT\.Module\.TreatmentRoom\.Models\.Dtos;/using LYBT.Models.TreatmentRoom;/g' {} \;
find "D:\source\repos\LYBTZYZS" -name "*.cs" -not -path "*/LYBT.Models/*" -exec sed -i 's/using LYBT\.Module\.Users\.Models\.Dtos;/using LYBT.Models.Users;/g' {} \;

# 修复所有模块中的 using LYBT.Module.*.Models; 引用
find "D:\source\repos\LYBTZYZS" -name "*.cs" -not -path "*/LYBT.Models/*" -exec sed -i 's/using LYBT\.Module\.Auth\.Models;/using LYBT.Models.Auth;/g' {} \;
find "D:\source\repos\LYBTZYZS" -name "*.cs" -not -path "*/LYBT.Models/*" -exec sed -i 's/using LYBT\.Module\.Billing\.Models;/using LYBT.Models.Billing;/g' {} \;
find "D:\source\repos\LYBTZYZS" -name "*.cs" -not -path "*/LYBT.Models/*" -exec sed -i 's/using LYBT\.Module\.DiagnosisTreatment\.Models;/using LYBT.Models.DiagnosisTreatment;/g' {} \;
find "D:\source\repos\LYBTZYZS" -name "*.cs" -not -path "*/LYBT.Models/*" -exec sed -i 's/using LYBT\.Module\.Doctors\.Models;/using LYBT.Models.Doctors;/g' {} \;
find "D:\source\repos\LYBTZYZS" -name "*.cs" -not -path "*/LYBT.Models/*" -exec sed -i 's/using LYBT\.Module\.FormulaTemplates\.Models;/using LYBT.Models.FormulaTemplates;/g' {} \;
find "D:\source\repos\LYBTZYZS" -name "*.cs" -not -path "*/LYBT.Models/*" -exec sed -i 's/using LYBT\.Module\.Herbs\.Models;/using LYBT.Models.Herbs;/g' {} \;
find "D:\source\repos\LYBTZYZS" -name "*.cs" -not -path "*/LYBT.Models/*" -exec sed -i 's/using LYBT\.Module\.Patients\.Models;/using LYBT.Models.Patients;/g' {} \;
find "D:\source\repos\LYBTZYZS" -name "*.cs" -not -path "*/LYBT.Models/*" -exec sed -i 's/using LYBT\.Module\.Pharmacy\.Models;/using LYBT.Models.Pharmacy;/g' {} \;
find "D:\source\repos\LYBTZYZS" -name "*.cs" -not -path "*/LYBT.Models/*" -exec sed -i 's/using LYBT\.Module\.Prescriptions\.Models;/using LYBT.Models.Prescriptions;/g' {} \;
find "D:\source\repos\LYBTZYZS" -name "*.cs" -not -path "*/LYBT.Models/*" -exec sed -i 's/using LYBT\.Module\.Queueing\.Models;/using LYBT.Models.Queueing;/g' {} \;
find "D:\source\repos\LYBTZYZS" -name "*.cs" -not -path "*/LYBT.Models/*" -exec sed -i 's/using LYBT\.Module\.Records\.Models;/using LYBT.Models.Records;/g' {} \;
find "D:\source\repos\LYBTZYZS" -name "*.cs" -not -path "*/LYBT.Models/*" -exec sed -i 's/using LYBT\.Module\.Registration\.Models;/using LYBT.Models.Registration;/g' {} \;
find "D:\source\repos\LYBTZYZS" -name "*.cs" -not -path "*/LYBT.Models/*" -exec sed -i 's/using LYBT\.Module\.Sync\.Models;/using LYBT.Models.Sync;/g' {} \;
find "D:\source\repos\LYBTZYZS" -name "*.cs" -not -path "*/LYBT.Models/*" -exec sed -i 's/using LYBT\.Module\.TreatmentRoom\.Models;/using LYBT.Models.TreatmentRoom;/g' {} \;
find "D:\source\repos\LYBTZYZS" -name "*.cs" -not -path "*/LYBT.Models/*" -exec sed -i 's/using LYBT\.Module\.Users\.Models;/using LYBT.Models.Users;/g' {} \;

echo "Models引用修复完成"
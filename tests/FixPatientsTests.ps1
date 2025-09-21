# 修复 Patients 测试模块的脚本

$testDir = "D:\source\repos\LYBTZYZS\tests\UnitTests\Modules\Patients.UnitTests"

# 修复 Repository 测试中的错误
$repoFile = "$testDir\Repositories\PatientRepositoryTests.cs"
if (Test-Path $repoFile) {
    (Get-Content $repoFile) -replace "Age = \d+,?", "// Age属性是只读计算属性" `
        -replace "IdCard =", "IdNumber =" `
        -replace "EmergencyContact =", "EmergencyContactName =" `
        -replace "EmergencyPhone =", "EmergencyContactPhone =" `
        -replace "Allergies =", "AllergyHistory =" `
        -replace "MedicalHistory = [^,]+,?", "// MedicalHistory不存在于实体中" `
        -replace "\.PageNumber", ".PageIndex" |
    Set-Content $repoFile
}

# 修复 Service 测试中的错误
$serviceFile = "$testDir\Services\PatientServiceTests.cs"
if (Test-Path $serviceFile) {
    (Get-Content $serviceFile) -replace "IdCard =", "IdNumber =" `
        -replace "Phone =", "PhoneNumber =" `
        -replace "\.IdCard", ".IdNumber" `
        -replace "\.Phone", ".PhoneNumber" `
        -replace "_mockBusinessService\.Setup\(x => x\.CreateAsync\(createDto\)\)", "_mockBusinessService.Setup(x => x.CreateAsync(createDto))" `
        -replace "_mockBusinessService\.Setup\(x => x\.UpdateAsync\(patientId, updateDto\)\)", "_mockBusinessService.Setup(x => x.UpdateAsync(patientId, updateDto))" `
        -replace "_mockBusinessService\.Setup\(x => x\.DeleteAsync\(patientId\)\)\.ReturnsAsync\(expectedResult\);",
            "_mockBusinessService.Setup(x => x.DeleteAsync(patientId)).ReturnsAsync(ServiceResult<bool>.Success(true));" |
    Set-Content $serviceFile
}

Write-Host "Patients 测试修复完成"
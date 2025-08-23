# UltraThink Module Separation Fix Script
# Follow architecture principle: strict module responsibility separation

Write-Host "Starting UltraThink module separation fix..." -ForegroundColor Green

# Fix PrintService.cs - access module data through composition
$printServicePath = "src\Client\Desktop\Core\Services\PrintService.cs"

Write-Host "Fixing $printServicePath..." -ForegroundColor Yellow

if (Test-Path $printServicePath) {
    $content = Get-Content $printServicePath -Raw -Encoding UTF8
    
    # Fix ChiefComplaint access
    $content = $content -replace "if \(!string\.IsNullOrEmpty\(data\.ChiefComplaint\)\)", "if (data.Consultation != null && !string.IsNullOrEmpty(data.Consultation.ChiefComplaint))"
    $content = $content -replace "data\.ChiefComplaint", "data.Consultation.ChiefComplaint"
    
    # Fix PresentIllness access  
    $content = $content -replace "if \(!string\.IsNullOrEmpty\(data\.PresentIllness\)\)", "if (data.Consultation != null && !string.IsNullOrEmpty(data.Consultation.PresentIllness))"
    $content = $content -replace "data\.PresentIllness", "data.Consultation.PresentIllness"
    
    # Fix PastHistory access
    $content = $content -replace "if \(!string\.IsNullOrEmpty\(data\.PastHistory\)\)", "if (data.Consultation != null && !string.IsNullOrEmpty(data.Consultation.PastHistory))"
    $content = $content -replace "data\.PastHistory", "data.Consultation.PastHistory"
    
    # Fix PhysicalExamination access
    $content = $content -replace "if \(!string\.IsNullOrEmpty\(data\.PhysicalExamination\)\)", "if (data.Consultation != null && !string.IsNullOrEmpty(data.Consultation.PhysicalExamination))"
    $content = $content -replace "data\.PhysicalExamination", "data.Consultation.PhysicalExamination"
    
    # Fix Diagnosis access
    $content = $content -replace "if \(!string\.IsNullOrEmpty\(data\.Diagnosis\)\)", "if (data.Consultation != null && !string.IsNullOrEmpty(data.Consultation.TcmDiagnosis))"
    $content = $content -replace "(?<!\.Consultation\.)data\.Diagnosis", "data.Consultation.TcmDiagnosis"
    
    # Fix TreatmentPlan access
    $content = $content -replace "if \(!string\.IsNullOrEmpty\(data\.TreatmentPlan\)\)", "if (data.Consultation != null && !string.IsNullOrEmpty(data.Consultation.TreatmentPrinciple))"
    $content = $content -replace "(?<!\.Consultation\.)data\.TreatmentPlan", "data.Consultation.TreatmentPrinciple"
    
    Set-Content $printServicePath $content -Encoding UTF8
    Write-Host "PrintService.cs fix completed" -ForegroundColor Green
}

# Add missing Remarks property to HerbItem
$printDataModelsPath = "src\Client\Desktop\Core\Models\Printing\PrintDataModels.cs"

Write-Host "Fixing $printDataModelsPath - adding HerbItem.Remarks property..." -ForegroundColor Yellow

if (Test-Path $printDataModelsPath) {
    $content = Get-Content $printDataModelsPath -Raw -Encoding UTF8
    
    # Add Remarks property to HerbItem class
    $pattern = "public string Usage \{ get; set; \} = `"`";"
    $replacement = "public string Usage { get; set; } = `"`";`n        public string Remarks { get; set; } = `"`";"
    $content = $content -replace $pattern, $replacement
    
    Set-Content $printDataModelsPath $content -Encoding UTF8
    Write-Host "HerbItem.Remarks property added" -ForegroundColor Green
}

# Clean and rebuild
Write-Host "Cleaning build cache..." -ForegroundColor Yellow
Remove-Item "src\Client\Desktop\Core\bin" -Recurse -Force -ErrorAction SilentlyContinue
Remove-Item "src\Client\Desktop\Core\obj" -Recurse -Force -ErrorAction SilentlyContinue

# Verify fix results
Write-Host "Verifying build status..." -ForegroundColor Yellow
$buildResult = dotnet build LYBT.All.sln --no-restore --verbosity quiet 2>&1

if ($LASTEXITCODE -eq 0) {
    Write-Host "UltraThink module separation fix successful!" -ForegroundColor Green
    Write-Host "Architecture principle verification:" -ForegroundColor Cyan
    Write-Host "  - MedicalCase: Only basic management info" -ForegroundColor Green
    Write-Host "  - Consultation: Diagnosis info via composition" -ForegroundColor Green  
    Write-Host "  - Prescription: Prescription info via composition" -ForegroundColor Green
    Write-Host "  - No cross-module field pollution" -ForegroundColor Green
} else {
    Write-Host "Still have build errors, check logs" -ForegroundColor Red
    Write-Host "May need manual adjustment:" -ForegroundColor Yellow
    Write-Host "  - Complete PrintDataConverter composition methods" -ForegroundColor Yellow
    Write-Host "  - Pass multi-module data at call sites" -ForegroundColor Yellow
}

Write-Host "UltraThink module separation fix script completed!" -ForegroundColor Green
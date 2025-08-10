# Update namespaces in Patients module
$targetPath = "D:\source\repos\LYBTZYZS\src\Frontend\Desktop\BusinessModules\Patients"

# Get all .cs and .xaml files
$files = Get-ChildItem -Path $targetPath -Include *.cs,*.xaml,*.xaml.cs -Recurse

foreach ($file in $files) {
    Write-Host "Processing: $($file.FullName)"
    
    # Read content
    $content = Get-Content -Path $file.FullName -Raw
    
    # Replace namespaces
    $content = $content -replace 'LYBT\.WPF\.Client\.Modules\.Patients', 'LYBT.WPF.Client.BusinessModules.Patients'
    
    # Write back
    Set-Content -Path $file.FullName -Value $content -NoNewline
}

Write-Host "Namespace update completed!"
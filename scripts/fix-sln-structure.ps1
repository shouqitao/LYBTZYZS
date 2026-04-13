# Fix Solution Structure - Remove orphaned "src" virtual folder

$solutionPath = "D:\source\repos\LYBTZYZS\LYBTZYZS.sln"

Write-Host "Fixing Solution structure..." -ForegroundColor Cyan

# Read file
$content = Get-Content $solutionPath -Encoding UTF8

# Find lines to remove
$toRemove = @()

for ($i = 0; $i -lt $content.Count; $i++) {
    $line = $content[$i]
    
    # Remove orphaned src folder
    if ($line -match '47B930EC-92D3-4515-99F6-87EB1293CE6B') {
        $toRemove += $i
    }
    # Remove orphaned Server folder
    elseif ($line -match '7109FD8C-3FAA-4968-BAA3-F5AED2809D2F') {
        $toRemove += $i
    }
    # Remove orphaned Core folder
    elseif ($line -match 'D914B465-E37C-4320-8000-45CC3E2B1F31') {
        $toRemove += $i
    }
}

# Filter out marked lines
$newContent = @()
for ($i = 0; $i -lt $content.Count; $i++) {
    if ($toRemove -notcontains $i) {
        $newContent += $content[$i]
    }
}

# Save fixed file
$newContent | Set-Content $solutionPath -Encoding UTF8

Write-Host "Solution structure fixed!" -ForegroundColor Green

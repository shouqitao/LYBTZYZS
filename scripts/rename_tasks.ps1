# PowerShell script to rename task files and update references
$epicPath = "D:\source\repos\LYBTZYZS\.claude\epics\prism-8x-refactor-plan"
$currentDate = (Get-Date).ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ")

# Mapping of old numbers to new issue numbers
$mapping = @{
    "001" = "748"
    "002" = "749"
    "003" = "750"
    "004" = "751"
    "005" = "752"
    "006" = "753"
    "007" = "754"
    "008" = "755"
}

# Process each mapping
foreach ($old in $mapping.Keys) {
    $new = $mapping[$old]
    $oldFile = Join-Path $epicPath "$old.md"
    $newFile = Join-Path $epicPath "$new.md"

    if (Test-Path $oldFile) {
        # Read content
        $content = Get-Content $oldFile -Raw

        # Update references to other task numbers
        foreach ($ref in $mapping.Keys) {
            if ($ref -ne $old) {
                $content = $content -replace "\b$ref\b", $mapping[$ref]
            }
        }

        # Update github field
        $githubUrl = "https://github.com/shouqitao/LYBTZYZS/issues/$new"
        $content = $content -replace "^github: .*$", "github: $githubUrl" -replace "(?m)"

        # Update updated field
        $content = $content -replace "^updated: .*$", "updated: $currentDate" -replace "(?m)"

        # Write to new file
        Set-Content -Path $newFile -Value $content -NoNewline

        # Remove old file
        Remove-Item $oldFile

        Write-Host "Renamed $old.md to $new.md"
    }
}
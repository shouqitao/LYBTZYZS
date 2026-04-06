$body = '{"userName":"sysadmin","password":"DevPass123"}'
[System.Net.ServicePointManager]::ServerCertificateValidationCallback = {$true}
try {
    $r = Invoke-WebRequest -Uri 'http://localhost:5000/api/v1/auth/login' -Method POST -ContentType 'application/json' -Body $body -UseBasicParsing -ErrorAction Stop
    Write-Output "Status: $($r.StatusCode)"
    Write-Output $r.Content
} catch {
    Write-Output "Error: $($_.Exception.Message)"
    if ($_.Exception.Response) {
        Write-Output "Response Status: $($_.Exception.Response.StatusCode)"
        $respStream = $_.Exception.Response.GetResponseStream()
        $reader = New-Object System.IO.StreamReader($respStream)
        Write-Output $reader.ReadToEnd()
        $reader.Close()
    }
}

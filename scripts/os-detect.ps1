param()
$os = (Get-CimInstance Win32_OperatingSystem | Select-Object -First 1).Caption
$shell = if ($PSVersionTable) { 'pwsh' } else { 'unknown' }
$obj = [pscustomobject]@{
  os    = 'windows'
  shell = $shell
  name  = $os
}
$obj | ConvertTo-Json -Depth 4

# Health Check Report

**Timestamp**: 2025-09-15 16:56:16  
**Endpoint**: http://localhost:8080/api/v1/health  
**Status**: 鉁?**PASSED**  
**Duration**: 0.28s  
**Retries**: 1/3

## Results Summary

| Check Type | Status | Details |
|------------|--------|---------|
| Process Check | 鉁?9 processes running | 9 dotnet processes |
| Port Check | 鉁?Port listening | Port 8080 binding status |
| TCP Connection | 鈿狅笍 Connection timeout | Direct connection test |
| HTTP Health | 鉁?HTTP 200 | Health endpoint response |

## Detailed Results

### HTTP Response
**Status Code**: 200
**Response**: 
```
{"status":"Healthy","timestamp":"2025-09-15T08:56:17.0304217Z","version":"1.0.0.0","environment":"Development"}
```

### Process Information
- **PID 5356**: "C:\Program Files\dotnet\dotnet.exe" "C:\Program Files\dotnet\sdk\9.0.305\MSBuild.dll" /noautoresponse /nologo /nodemode:1 /nodeReuse:true /low:false
- **PID 5544**: "C:\Program Files\dotnet\dotnet.exe" run --launch-profile http
- **PID 7256**: "C:\Program Files\dotnet\dotnet.exe" "C:\Program Files\dotnet\sdk\9.0.305\MSBuild.dll" /noautoresponse /nologo /nodemode:1 /nodeReuse:true /low:false
- **PID 7472**: "C:\Program Files\dotnet\dotnet.exe" "C:\Program Files\dotnet\sdk\9.0.305\MSBuild.dll" /noautoresponse /nologo /nodemode:1 /nodeReuse:true /low:false
- **PID 7704**: "C:\Program Files\dotnet\dotnet.exe" "C:\Program Files\dotnet\sdk\9.0.305\MSBuild.dll" /noautoresponse /nologo /nodemode:1 /nodeReuse:true /low:false
- **PID 8772**: "C:\Program Files\dotnet\dotnet.exe" "C:\Program Files\dotnet\sdk\9.0.305\MSBuild.dll" /noautoresponse /nologo /nodemode:1 /nodeReuse:true /low:false
- **PID 11240**: "C:\Program Files\dotnet\dotnet.exe" "C:\Program Files\dotnet\sdk\9.0.305\MSBuild.dll" /noautoresponse /nologo /nodemode:1 /nodeReuse:true /low:false
- **PID 12992**: "C:\Program Files\dotnet\dotnet.exe" "C:\Program Files\dotnet\sdk\9.0.305\MSBuild.dll" /noautoresponse /nologo /nodemode:1 /nodeReuse:true /low:false
- **PID 17224**: "C:\Program Files\dotnet\dotnet.EXE"  C:\Users\player\.serena\language_servers\static\CSharpLanguageServer\Microsoft.CodeAnalysis.LanguageServer.win-x64.5.0.0-1.25329.6\Microsoft.CodeAnalysis.LanguageServer.dll --logLevel=Information --extensionLogDirectory=C:\Users\player\.serena\language_servers\static\CSharpLanguageServer\logs --stdio

## Recommendations

鉁?**System is healthy** - WebAPI is responding correctly on port 8080

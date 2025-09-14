# Health Check Report

**Timestamp**: 2025-09-15 00:14:03  
**Endpoint**: http://localhost:8080/api/v1/health  
**Status**: 鉁?**PASSED**  
**Duration**: 1.2s  
**Retries**: 1/5

## Results Summary

| Check Type | Status | Details |
|------------|--------|---------|
| Process Check | 鉁?8 processes running | 8 dotnet processes |
| Port Check | 鉁?Port listening | Port 8080 binding status |
| TCP Connection | 鈿狅笍 Connection timeout | Direct connection test |
| HTTP Health | 鉁?HTTP 200 | Health endpoint response |

## Detailed Results

### HTTP Response
**Status Code**: 200
**Response**: 
```
{"status":"Healthy","timestamp":"2025-09-14T16:14:04.9653967Z","version":"1.0.0.0","environment":"Development"}
```

### Process Information
- **PID 4596**: "C:\Program Files\dotnet\dotnet.exe" run --urls="http://localhost:8080" --no-launch-profile --verbosity minimal 
- **PID 6872**: "C:\Program Files\dotnet\dotnet.exe" "C:\Program Files\dotnet\sdk\9.0.305\MSBuild.dll" /noautoresponse /nologo /nodemode:1 /nodeReuse:true /low:false
- **PID 7680**: "C:\Program Files\dotnet\dotnet.exe" "C:\Program Files\dotnet\sdk\9.0.305\MSBuild.dll" /noautoresponse /nologo /nodemode:1 /nodeReuse:true /low:false
- **PID 13300**: "C:\Program Files\dotnet\dotnet.exe" "C:\Program Files\dotnet\sdk\9.0.305\MSBuild.dll" /noautoresponse /nologo /nodemode:1 /nodeReuse:true /low:false
- **PID 16960**: "C:\Program Files\dotnet\dotnet.exe" "C:\Program Files\dotnet\sdk\9.0.305\MSBuild.dll" /noautoresponse /nologo /nodemode:1 /nodeReuse:true /low:false
- **PID 21144**: "C:\Program Files\dotnet\dotnet.exe" "C:\Program Files\dotnet\sdk\9.0.305\MSBuild.dll" /noautoresponse /nologo /nodemode:1 /nodeReuse:true /low:false
- **PID 23532**: "C:\Program Files\dotnet\dotnet.exe" "C:\Program Files\dotnet\sdk\9.0.305\MSBuild.dll" /noautoresponse /nologo /nodemode:1 /nodeReuse:true /low:false
- **PID 30160**: "C:\Program Files\dotnet\dotnet.exe" "C:\Program Files\dotnet\sdk\9.0.305\MSBuild.dll" /noautoresponse /nologo /nodemode:1 /nodeReuse:true /low:false

## Recommendations

鉁?**System is healthy** - WebAPI is responding correctly on port 8080

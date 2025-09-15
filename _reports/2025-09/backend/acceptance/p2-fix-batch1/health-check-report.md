# Health Check Report

**Timestamp**: 2025-09-15 18:38:44  
**Endpoint**: http://localhost:8080/api/v1/health  
**Status**: 鉁?**PASSED**  
**Duration**: 0.06s  
**Retries**: 1/5

## Results Summary

| Check Type | Status | Details |
|------------|--------|---------|
| Process Check | 鉁?2 processes running | 2 dotnet processes |
| Port Check | 鉁?Port listening | Port 8080 binding status |
| TCP Connection | 鈿狅笍 Connection timeout | Direct connection test |
| HTTP Health | 鉁?HTTP 200 | Health endpoint response |

## Detailed Results

### HTTP Response
**Status Code**: 200
**Response**: 
```
{"status":"Healthy","timestamp":"2025-09-15T10:38:44.6839094Z","version":"1.0.0.0","environment":"Development"}
```

### Process Information
- **PID 12428**: "C:\Program Files\dotnet\dotnet.exe" run --launch-profile http
- **PID 17224**: "C:\Program Files\dotnet\dotnet.EXE"  C:\Users\player\.serena\language_servers\static\CSharpLanguageServer\Microsoft.CodeAnalysis.LanguageServer.win-x64.5.0.0-1.25329.6\Microsoft.CodeAnalysis.LanguageServer.dll --logLevel=Information --extensionLogDirectory=C:\Users\player\.serena\language_servers\static\CSharpLanguageServer\logs --stdio

## Recommendations

鉁?**System is healthy** - WebAPI is responding correctly on port 8080

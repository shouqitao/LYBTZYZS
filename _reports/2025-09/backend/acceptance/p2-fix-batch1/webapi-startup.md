# WebAPI Single-process Startup Report

**Start Time**: 2025-09-15 00:09:18  
**Port**: 8080  
**Background Mode**: True  
**Process ID**: 4596

## Command Used

\\\
cd "D:\source\repos\LYBTZYZS\src\Server\Services\LYBT.WebAPI"
dotnet run --urls="http://localhost:8080" --no-launch-profile --verbosity minimal
\\\

## Environment Variables

\\\
ASPNETCORE_URLS=http://localhost:8080
ASPNETCORE_ENVIRONMENT=Development
DOTNET_ENVIRONMENT=Development
\\\

## Next Steps

1. Wait for WebAPI to fully initialize (~30 seconds)
2. Run health check: \scripts/health/check.ps1\
3. Verify endpoint: \http://localhost:8080/api/v1/health\

@echo off
echo 启动LYBT WebAPI服务器...
cd /d D:\source\repos\LYBTZYZS
dotnet run --project src/Backend/Services/LYBT.WebAPI --urls "https://localhost:7001"
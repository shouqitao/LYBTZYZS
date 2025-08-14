# 1. 用微软官方 .NET 8 运行时基础镜像
FROM mcr.microsoft.com/dotnet/aspnet:8.0

# 2. 设置容器里的工作目录
WORKDIR /app

# 3. 拷贝本地 publish 目录下所有文件到容器/app目录
COPY ./publish .

# 4. 设置默认启动命令
ENTRYPOINT ["dotnet", "LYBT.WebAPI.dll"]

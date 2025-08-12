# ================================================
# 凌隐宝堂中医诊所系统 - Docker镜像
# UltraThink Stage 5.3.3
# ================================================

# 第一阶段：构建后端
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS backend-build
WORKDIR /src

# 复制后端项目文件
COPY src/Backend/ ./Backend/
COPY src/Shared/ ./Shared/

# 还原依赖并构建
WORKDIR /src/Backend/Services/LYBT.WebAPI
RUN dotnet restore
RUN dotnet publish -c Release -o /app/backend --no-restore

# ================================================
# 第二阶段：构建前端（如果需要Web版本）
FROM node:18-alpine AS frontend-build
WORKDIR /src

# 如果有Web前端，在这里构建
# COPY src/Frontend/Web/ ./
# RUN npm install
# RUN npm run build

# ================================================
# 第三阶段：运行时镜像
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# 安装中文字体支持
RUN apt-get update && \
    apt-get install -y \
        fonts-noto-cjk \
        locales && \
    locale-gen zh_CN.UTF-8 && \
    apt-get clean && \
    rm -rf /var/lib/apt/lists/*

# 设置环境变量
ENV LANG=zh_CN.UTF-8
ENV LANGUAGE=zh_CN:zh
ENV LC_ALL=zh_CN.UTF-8
ENV ASPNETCORE_ENVIRONMENT=Production
ENV ASPNETCORE_URLS=http://+:80;https://+:443

# 复制后端应用
COPY --from=backend-build /app/backend ./

# 创建数据和日志目录
RUN mkdir -p /app/data /app/logs

# 暴露端口
EXPOSE 80
EXPOSE 443

# 健康检查
HEALTHCHECK --interval=30s --timeout=3s --start-period=5s --retries=3 \
    CMD curl -f http://localhost/health || exit 1

# 设置入口点
ENTRYPOINT ["dotnet", "LYBT.WebAPI.dll"]
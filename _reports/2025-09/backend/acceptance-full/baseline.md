# Backend Acceptance Full - Baseline Snapshot

## 时间戳
**生成时间**: 2025-09-15 17:40:00  
**时区**: UTC+8  
**执行分支**: release/backend-acceptance-smoke-full  

## 端口配置
**基线端口**: 8080  
**来源**: _reports/2025-09/backend/acceptance/p2-fix-batch1/cleanup-notes.md  
**URL**: http://localhost:8080  
**端口状态**: ✅ LISTENING (2 entries)  

## 环境变量要点
```
ASPNETCORE_URLS=http://localhost:8080
```

## 系统状态
**dotnet进程**: 2个活跃进程  
**WebAPI状态**: ✅ 正常运行  
**API基线验证**: GET /api/v1/auth → HTTP 405 (符合预期)  
**响应时间**: ~37ms  

## 关键服务状态
- **认证服务**: ✅ 可用 (返回405 Method Not Allowed)
- **数据库连接**: ✅ 正常 (根据之前验证)
- **依赖注入**: ✅ 正常启动

## 预期行为基线
- Auth模块: GET → 405, POST /login → 200
- Formula模块: GET /formulas → 200 (已修复EF Core问题)
- 其他模块: 待验证

## 环境信息
**操作系统**: Windows  
**.NET版本**: .NET 8  
**启动配置**: http profile  
**数据库**: SQL Server (localhost/LYBTDB)  

## 注意事项
- 无专用健康检查端点，使用API端点验证
- 基于P2-Fix-Batch4的修复结果
- Auth和Formula模块已通过3轮稳定性测试
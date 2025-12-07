# Tasks: consolidate-shell-architecture

## 1. 规范整合

- [x] 1.1 更新`login-ui`规范Purpose描述
- [x] 1.2 更新`shell-layout`规范Purpose描述
- [x] 1.3 添加Shell层架构概述到`shell-layout`规范

## 2. 健康检查提取

- [x] 2.1 创建`IHealthCheckCoordinator`接口
- [x] 2.2 创建`HealthCheckCoordinator`实现类
- [x] 2.3 从`MainWindowViewModel`迁移健康检查逻辑
- [x] 2.4 更新`MainWindowViewModel`依赖注入
- [x] 2.5 注册新服务到DI容器

## 3. 启动流程梳理

- [x] 3.1 审查`ApplicationBootstrapper`废弃方法
- [x] 3.2 确认`StartupPipeline`为唯一启动入口
- [x] 3.3 更新`StartupDiagnostics`日志格式

## 4. 验证与文档

- [x] 4.1 运行Shell单元测试(91个测试)
- [x] 4.2 验证编译无警告
- [x] 4.3 更新Shell README文档
- [x] 4.4 更新CHANGELOG

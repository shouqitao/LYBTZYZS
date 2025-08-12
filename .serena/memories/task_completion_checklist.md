# 任务完成检查清单

## 代码开发完成后

### 1. 代码质量检查
- [ ] 编译无错误：`scripts\build-check.bat`
- [ ] 无编译警告（或已记录）
- [ ] 代码符合命名规范
- [ ] 添加必要的XML注释
- [ ] 移除调试代码和console.log

### 2. 测试验证
- [ ] 运行单元测试：`dotnet test`
- [ ] 测试覆盖新功能
- [ ] 无测试失败
- [ ] 集成测试通过（如适用）

### 3. 代码格式化
```bash
# 格式化代码
dotnet format

# 检查异步void问题
python scripts/check_async_void.py
```

### 4. 静态代码分析
```bash
# 运行质量检查
python scripts/ultrathink_quality_checker.py

# 深度错误分析
scripts\build-analyze.bat
```

### 5. 文档更新
- [ ] 更新README（如有新功能）
- [ ] 更新API文档（如有新接口）
- [ ] 添加/更新代码注释
- [ ] 更新CHANGELOG（如适用）

### 6. Git提交前
```bash
# 检查状态
git status

# 查看差异
git diff

# 确保.gitignore正确
# 不提交：bin/, obj/, *.user, .vs/
```

### 7. 提交规范
```bash
# 提交格式
git commit -m "<type>: <subject>"

# type类型：
# feat: 新功能
# fix: 修复bug
# docs: 文档更新
# refactor: 重构
# test: 测试相关
# chore: 构建/工具相关
```

## 特定任务检查

### 添加新模块时
- [ ] 创建Module类继承IModule
- [ ] 在ServiceCollectionExtensions注册
- [ ] 添加到相应的.sln文件
- [ ] 创建接口和实现分离
- [ ] 添加AutoMapper配置

### 修改数据库时
- [ ] 在Infrastructure项目添加迁移
- [ ] 更新AppDbContext
- [ ] 运行迁移测试
- [ ] 更新种子数据（如需要）

### 修改API时
- [ ] 更新Swagger文档
- [ ] 测试API端点
- [ ] 更新前端服务调用
- [ ] 更新API测试脚本

### 前端修改时
- [ ] MVVM模式正确实现
- [ ] 数据绑定正常工作
- [ ] UI响应式设计
- [ ] 无内存泄漏

## 常见问题排查

### 编译失败
1. 检查NuGet包版本
2. 清理解决方案：`dotnet clean`
3. 删除bin/obj文件夹
4. 重新生成

### 测试失败
1. 检查测试数据
2. 验证Mock设置
3. 检查异步方法
4. 查看测试输出

### 运行时错误
1. 检查配置文件
2. 验证连接字符串
3. 检查依赖注入
4. 查看日志文件

## 性能检查
- [ ] API响应时间 < 200ms
- [ ] 无N+1查询问题
- [ ] 适当使用缓存
- [ ] 异步操作正确实现
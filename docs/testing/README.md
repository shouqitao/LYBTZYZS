# 🧪 测试文档

## 📚 文档索引

### API测试
- [API测试指南](./API_测试指南.md) - 完整的API测试指导文档
- [测试报告](./测试报告.md) - 最新测试执行报告

### 测试工具
位置：`../../testing/`
- [API测试集合](../../testing/api-tests/) - Postman测试集合
- [测试工具](../../testing/tools/) - 自动化测试脚本

## 🚀 快速开始

### 1. API测试
```bash
# 使用Postman GUI
1. 导入 testing/api-tests/LYBT_API_Tests.postman_collection.json
2. 导入环境配置
3. 运行测试集合

# 使用Newman CLI
cd testing/tools
./run_api_tests.bat
```

### 2. 查看测试报告
- 最新报告：[测试报告.md](./测试报告.md)
- HTML报告：运行Newman后生成在 `testing/tools/test_results.html`

## 📋 测试覆盖范围

### ✅ 已覆盖
- 认证系统测试
- 业务模块基础功能测试
- JWT令牌验证测试
- 健康检查测试

### 🔄 待扩展
- 性能压力测试
- 数据验证测试
- 错误处理测试
- 集成测试

## 📞 相关链接
- [开发文档](../development/) - 开发环境配置
- [API文档](../api/) - API接口文档
- [架构文档](../architecture/) - 系统架构说明
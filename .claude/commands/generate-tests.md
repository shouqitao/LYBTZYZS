# 生成测试代码 (/generate-tests)

基于现有Service/Repository代码，自动生成单元测试模板。

## 功能
- 分析类的公共方法
- 生成测试类骨架
- 生成测试方法（AAA模式）
- 配置Mock对象

## 使用
```
/generate-tests PatientService
```

## 生成内容
- 测试类文件（含命名空间）
- 构造函数测试
- 每个公共方法的测试骨架
- Mock配置示例

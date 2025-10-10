# 生成DTO类 (/generate-dto)

基于Entity实体自动生成对应的DTO类（Create/Update/Query）。

## 功能
- 读取Entity定义
- 生成HerbDto、HerbCreateDto、HerbUpdateDto、HerbQueryDto
- 添加DataAnnotations验证
- 生成Display标签

## 使用
```
/generate-dto Herb
```

## 符合标准
- 遵循 docs/architecture/dto-design-principles.md
- 继承正确的基类（StatusDto/CreateDtoBase等）
- 包含所有必要的验证属性

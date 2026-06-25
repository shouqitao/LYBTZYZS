# ADR-0011: 从 AutoMapper 迁移到 Riok.Mapperly

## 状态
Accepted

## 上下文
项目最初使用 AutoMapper 进行对象映射，但 AutoMapper 存在运行时反射开销、
配置分散、编译时无法发现映射错误等问题。

## 决策
采用 Riok.Mapperly 作为编译时对象映射库。

## 理由
- 编译时生成映射代码，无运行时反射开销
- 编译时错误检测（映射缺失字段会报编译错误）
- 23 个 Mapper 接口，全部使用 `[Mapper]` 属性标注
- .csproj 不引入 AutoMapper 包（除非需要 fallback）

## 后果
- 所有映射必须通过 Mapper 接口，不能手动映射
- 新增 DTO 字段时 Mapper 编译器会自动检查覆盖

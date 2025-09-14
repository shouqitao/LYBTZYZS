# LYBT.Entities 项目清理计划

**分析日期**: 2025-09-14  
**项目路径**: `src/Server/Core/LYBT.Entities`  
**分析范围**: 实体模型、属性、枚举、关系配置

## 📋 发现问题汇总

### 🔴 高优先级问题 (3项)

#### 1. 命名不一致 - UserModel
**位置**: `Users/UserModel.cs:26-28`  
**问题**: 属性名`Username`与数据库列名`UserName`不匹配
```csharp
[Column("UserName")]
public string Username { get; set; } = string.Empty;
```
**影响**: 造成查询混淆，数据库映射不清晰  
**修复**: 统一为`Username`属性和列名，或统一为`UserName`  
**估计工作量**: 30分钟

#### 2. 过时枚举值使用 - MedicalCaseModel
**位置**: `MedicalCase/MedicalCaseModel.cs:56`  
**问题**: 仍在使用已过时的`MedicalCaseStatus.Registered`
```csharp
public MedicalCaseStatus Status { get; set; } = MedicalCaseStatus.Registered;
```
**影响**: 与Record-Only模式不一致，产生编译警告  
**修复**: 改为`MedicalCaseStatus.Active`  
**估计工作量**: 5分钟

#### 3. 企业级配伍实体残留 - HerbCompatibilityNote
**位置**: `Compatibility/HerbCompatibilityNote.cs`  
**问题**: 整个配伍检查实体仍存在，但服务已移除
```csharp
[Table("HerbCompatibilityNotes")]
public class HerbCompatibilityNote
```
**影响**: 未使用的代码和数据表结构  
**修复**: 删除整个文件，或标记为Obsolete  
**估计工作量**: 10分钟

### 🟡 中优先级问题 (4项)

#### 4. 敏感数据属性未使用
**位置**: `Attributes/SensitiveDataAttribute.cs`  
**问题**: 定义了敏感数据特性但实体中未实际使用
```csharp
public sealed class SensitiveDataAttribute : Attribute
```
**影响**: 代码冗余，安全特性未实现  
**修复**: 要么在实体中应用，要么删除此特性  
**估计工作量**: 45分钟

#### 5. 缺少数据库索引配置
**位置**: 多个实体文件  
**问题**: 关键查询字段缺少索引，影响性能
- `User.Username` (登录查询)
- `Patient.Name` (患者搜索)  
- `User.PinYinCode` (拼音搜索)
- `MedicalCase.PatientId` (患者医案查询)
**修复**: 在Infrastructure项目的ModelConfiguration中添加索引  
**估计工作量**: 60分钟

#### 6. 魔法值分散
**位置**: 多个实体的默认值  
**问题**: 硬编码的默认状态值分散在各实体中
```csharp
public CommonStatus Status { get; set; } = CommonStatus.Enabled;
public MedicalCaseStatus Status { get; set; } = MedicalCaseStatus.Registered;
```
**修复**: 创建EntityDefaults常量类统一管理  
**估计工作量**: 30分钟

#### 7. 过时Epic引用
**位置**: `Attributes/SensitiveDataAttribute.cs:6`  
**问题**: 注释中仍引用已清理的Epic编号
```csharp
/// 敏感数据标记特性 - Epic 05-P0-03: 数据安全保障
```
**修复**: 清理过时Epic引用，更新为通用说明  
**估计工作量**: 5分钟

### 🟢 低优先级问题 (2项)

#### 8. 注释语言不一致
**位置**: 部分实体文件  
**问题**: 混用中英文注释风格  
**修复**: 统一为中文注释 + 英文技术术语  
**估计工作量**: 15分钟

#### 9. 时间字段命名不一致  
**位置**: 多个实体  
**问题**: `CreatedTime` vs `CreateTime`混用
**修复**: 统一为`CreatedTime`  
**估计工作量**: 20分钟

## ✅ 项目优势

- ✅ 实体设计符合UltraThink架构理念
- ✅ 使用统一的枚举避免魔法字符串  
- ✅ 数据注解使用规范
- ✅ 文档注释相对完整
- ✅ 领域职责分离清晰

## 📊 清理优先级建议

### Phase 1: 立即修复 (45分钟)
1. 修复UserModel命名不一致
2. 更新MedicalCaseModel过时枚举值
3. 处理HerbCompatibilityNote残留

### Phase 2: 性能优化 (90分钟)  
4. 添加数据库索引配置
5. 应用或删除SensitiveDataAttribute
6. 创建EntityDefaults常量类

### Phase 3: 细节完善 (40分钟)
7. 清理过时Epic引用
8. 统一注释和命名风格

## 🎯 预期收益

**代码质量提升**:
- 消除编译警告
- 提高命名一致性  
- 清除未使用代码

**性能优化**:
- 数据库查询性能提升
- 减少冗余配置加载

**维护性改善**:
- 降低新人理解成本
- 减少潜在bug风险

**总计工作量**: 约3小时  
**影响范围**: 仅Entities项目，对业务逻辑无影响  
**风险评估**: 低风险，主要为清理性修改
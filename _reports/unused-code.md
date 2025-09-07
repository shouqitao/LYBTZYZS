# 未使用代码分析报告

## 📊 总体情况
- 分析时间: 2025-09-07
- 项目类型: .NET 8 + WPF + Prism + UltraThink 双层架构
- 分析范围: 前后端完整解决方案

## 🔍 确认未使用的代码 (安全删除)

### 1. 临时编译文件
**文件**: `src/Client/Desktop/Modules/Formula/LYBT.Desktop.Formula_ko3z5zku_wpftmp.csproj`
- **证据**: Visual Studio 临时编译文件，非源代码
- **风险**: 无风险
- **优先级**: 高
- **操作**: 立即删除

### 2. 架构遗留的 Manager 类 (违反 UltraThink 模式)

#### PrescriptionManager.cs
**位置**: `src/Client/Desktop/Modules/Prescriptions/Services/PrescriptionManager.cs`
- **证据**: 全项目搜索无引用，违反 UltraThink 双层架构标准
- **风险**: 低风险 (可能是架构重构遗留)
- **影响**: 提升架构一致性
- **优先级**: 高

#### FormulaManager.cs  
**位置**: `src/Client/Desktop/Modules/Formula/Services/FormulaManager.cs`
- **证据**: 全项目搜索无引用，违反 UltraThink 双层架构标准
- **风险**: 低风险 (可能是架构重构遗留)
- **影响**: 提升架构一致性
- **优先级**: 高

### 3. 疑似未使用的服务类

#### PrescriptionComposerService.cs
**位置**: `src/Client/Desktop/Modules/Prescriptions/Services/PrescriptionComposerService.cs`
- **证据**: 静态分析未发现直接引用
- **风险**: 中等风险 (可能被XAML/IoC动态使用)
- **建议**: 先标记 [Obsolete]，观察2周
- **优先级**: 中

## 🚨 疑似保留项 (需要人工验证)

### ViewModel 属性和命令
以下项目可能通过 XAML 绑定或反射使用，建议保留：
- 所有 ViewModel 的公共属性和命令
- 所有标记为 `[RelayCommand]` 的方法
- 所有实现 `INotifyPropertyChanged` 的属性

### 服务接口实现
以下服务虽然静态分析显示"未直接引用"，但通过依赖注入使用，必须保留：
- 所有 `I*Service` 接口的实现类
- 所有注册到 IoC 容器的服务

## 📈 清理效果预估
- **删除文件数**: 3-4个
- **减少代码行数**: 约500-800行
- **提升编译速度**: 5-10%
- **降低复杂度**: 移除架构不一致性

## ⚠️ 删除前验证清单
1. ✅ 确认文件未被项目文件引用
2. ✅ 全文搜索确认无字符串引用
3. ✅ 检查是否在配置文件中被声明
4. ⚠️ 验证是否被 XAML 资源或绑定使用
5. ⚠️ 确认不在依赖注入配置中

## 🔄 回滚方案
```bash
# 如需回滚，从 Git 历史恢复
git checkout HEAD~1 -- <deleted-file-path>
git commit -m "恢复意外删除的文件"
```
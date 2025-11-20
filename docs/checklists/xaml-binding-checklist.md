# XAML绑定检查清单

> **适用场景**: DetailView创建或修改时，特别是Issue #2168类型的CRUD统一架构
> **目标**: 预防只读属性TwoWay绑定导致的运行时错误
> **参考**: UserDetailView.xaml（正确实现示例）

---

## 📋 检查清单

### 1. 只读属性识别

#### 1.1 检查ViewModel中的只读属性
- [ ] 列出所有只读属性（`private set` 或 无setter）
- [ ] 特别注意以下常见只读属性：
  - [ ] **PinYinCode**（拼音码 - 自动生成）
  - [ ] **Age**（年龄 - 计算属性）
  - [ ] **CreatedAt**（创建时间 - Server生成）
  - [ ] **UpdatedAt**（更新时间 - Server生成）

#### 1.2 识别只读属性的方法
```csharp
// 方式1: private set
public string PinYinCode
{
    get => _pinYinCode;
    private set => SetProperty(ref _pinYinCode, value);  // ← private set
}

// 方式2: 无setter
public int? Age
{
    get { /* 计算逻辑 */ }  // ← 只有get
}

// 方式3: 表达式属性
public bool IsReadOnly => !IsEditMode;  // ← 表达式 = 只读
```

---

### 2. XAML绑定检查

#### 2.1 单一绑定
- [ ] 所有只读属性绑定已明确指定`Mode=OneWay`
- [ ] 示例：
  ```xml
  <!-- ✅ 正确 -->
  <TextBox Text="{Binding PinYinCode, Mode=OneWay}"
           IsReadOnly="True"
           Background="#F9FAFB" />

  <!-- ❌ 错误 -->
  <TextBox Text="{Binding PinYinCode}"
           IsReadOnly="True" />
  ```

#### 2.2 MultiBinding
- [ ] MultiBinding中每个只读属性都已指定`Mode=OneWay`
- [ ] 示例：
  ```xml
  <!-- ✅ 正确 -->
  <TextBox IsReadOnly="True">
      <TextBox.Text>
          <MultiBinding StringFormat="{}{0} 岁">
              <Binding Path="Age" Mode="OneWay"/>
          </MultiBinding>
      </TextBox.Text>
  </TextBox>

  <!-- ❌ 错误 -->
  <TextBox IsReadOnly="True">
      <TextBox.Text>
          <MultiBinding StringFormat="{}{0} 岁">
              <Binding Path="Age"/>  <!-- 缺少Mode=OneWay -->
          </MultiBinding>
      </TextBox.Text>
  </TextBox>
  ```

#### 2.3 IsReadOnly不能替代绑定模式
- [ ] 确认理解：`IsReadOnly="True"`只是UI层面，不影响绑定模式
- [ ] 只读属性必须同时设置：
  - `Mode=OneWay`（绑定层面）
  - `IsReadOnly="True"`（UI层面，可选）

---

### 3. 资源引用检查

#### 3.1 转换器声明
- [ ] xmlns引用已添加：
  ```xml
  xmlns:infrastructure="clr-namespace:LYBT.Desktop.Infrastructure.Converters;assembly=LYBT.Desktop.Infrastructure"
  ```
- [ ] 转换器已在UserControl.Resources中实例化：
  ```xml
  <infrastructure:InverseBooleanConverter x:Key="InverseBooleanConverter" />
  <infrastructure:BooleanToVisibilityConverter x:Key="BooleanToVisibilityConverter" />
  ```

#### 3.2 样式定义
- [ ] 本地样式已定义（FormLabelStyle, EditableTextBoxStyle, PrimaryButtonStyle, SecondaryButtonStyle）
- [ ] 或确认引用的外部资源文件存在

#### 3.3 MergedDictionaries
- [ ] MergedDictionaries是ResourceDictionary的**最后一个子元素**
- [ ] 示例：
  ```xml
  <UserControl.Resources>
      <ResourceDictionary>
          <!-- 1. 转换器 -->
          <infrastructure:InverseBooleanConverter x:Key="..." />

          <!-- 2. 样式 -->
          <Style x:Key="FormLabelStyle" ... />

          <!-- 3. 其他资源 -->
          <DropShadowEffect x:Key="CardShadow" ... />

          <!-- 4. MergedDictionaries（必须最后） -->
          <ResourceDictionary.MergedDictionaries>
              <ResourceDictionary Source="pack://..." />
          </ResourceDictionary.MergedDictionaries>
      </ResourceDictionary>
  </UserControl.Resources>
  ```

---

### 4. 编译与运行时验证

#### 4.1 编译验证
- [ ] `dotnet build {Module}.csproj --no-restore`
- [ ] 0 个警告
- [ ] 0 个错误

#### 4.2 运行时验证
- [ ] 启动应用程序
- [ ] 导航到DetailView页面
- [ ] 测试三种模式（如适用）：
  - [ ] **Create模式**：创建新记录
  - [ ] **Edit模式**：编辑现有记录
  - [ ] **View模式**：查看只读记录
- [ ] 无XAML运行时错误

---

## 🎯 常见错误模式

### 错误1: 依赖IsReadOnly防止绑定错误
```xml
<!-- ❌ 错误：认为IsReadOnly会自动调整绑定模式 -->
<TextBox Text="{Binding PinYinCode}" IsReadOnly="True" />

<!-- ✅ 正确：明确指定绑定模式 -->
<TextBox Text="{Binding PinYinCode, Mode=OneWay}" IsReadOnly="True" />
```

**原因**: `IsReadOnly="True"`只是防止用户编辑，绑定引擎仍会尝试TwoWay绑定。

---

### 错误2: MultiBinding省略绑定模式
```xml
<!-- ❌ 错误：MultiBinding中的Binding也有默认模式 -->
<MultiBinding StringFormat="{}{0} 岁">
    <Binding Path="Age"/>
</MultiBinding>

<!-- ✅ 正确：明确指定 -->
<MultiBinding StringFormat="{}{0} 岁">
    <Binding Path="Age" Mode="OneWay"/>
</MultiBinding>
```

**原因**: MultiBinding中的每个Binding都有独立的绑定模式，默认仍是TwoWay。

---

### 错误3: 引用不存在的资源文件
```xml
<!-- ❌ 错误：文件不存在但编译通过 -->
<ResourceDictionary.MergedDictionaries>
    <ResourceDictionary Source="Resources/Styles/CommonStyles.xaml"/>
</ResourceDictionary.MergedDictionaries>

<!-- ✅ 正确：使用现有资源或本地定义 -->
<ResourceDictionary.MergedDictionaries>
    <ResourceDictionary Source="pack://application:,,,/LYBT.Desktop.Infrastructure;component/Themes/UnifiedComponents.xaml"/>
</ResourceDictionary.MergedDictionaries>
```

**原因**: XAML编译器不检查资源文件存在性，运行时才报错。

---

## 📚 参考资料

### 正确实现示例
- **UserDetailView.xaml**: 所有绑定模式正确，资源引用规范
- **UserDetailViewModel.cs**: 只读属性定义示例

### 相关Issue
- **Issue #2168**: CRUD统一架构 - DetailView三模式合一
- **Bug修复记录**:
  - commit b196d741d（资源引用错误）
  - commit 23caed8ba（PinYinCode绑定错误）
  - commit fd3de3356（Age绑定错误）

### WPF绑定模式文档
- OneWay: 源 → 目标（只读属性）
- TwoWay: 源 ↔ 目标（可编辑属性，TextBox.Text默认）
- OneWayToSource: 源 ← 目标（特殊场景）
- OneTime: 仅初始化时绑定一次

---

## ✅ 验收标准

满足以下所有条件才算通过：

1. ✅ 所有只读属性已识别并记录
2. ✅ 所有只读属性绑定已指定`Mode=OneWay`
3. ✅ MultiBinding中的Binding已检查
4. ✅ 资源引用正确（转换器、样式、MergedDictionaries）
5. ✅ 编译通过（0警告0错误）
6. ✅ 运行时测试通过（无XAML异常）
7. ✅ 三种模式（Create/Edit/View）测试通过

---

**创建时间**: 2025-11-20
**适用项目**: LYBTZYZS（凌隐宝堂中医诊所管理系统）
**维护者**: Claude Code
**版本**: v1.0

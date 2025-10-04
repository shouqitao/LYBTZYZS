# Resource Management Guidelines
# 资源管理规范

## 1. Directory Structure / 目录结构

```
src/Client/Desktop/Shell/
├── Assets/                    # Static resources / 静态资源
│   ├── Images/                # Image files / 图片文件
│   │   ├── Backgrounds/       # Background images / 背景图
│   │   ├── Illustrations/     # Illustrations / 插图
│   │   └── Logos/             # Logo images / Logo图片
│   ├── Icons/                 # Icon files / 图标文件
│   │   ├── App/               # Application icons / 应用图标
│   │   ├── Actions/           # Action icons / 操作图标
│   │   └── Status/            # Status icons / 状态图标
│   ├── Fonts/                 # Font files / 字体文件
│   ├── Audio/                 # Audio files / 音频文件
│   └── Data/                  # Static data files / 静态数据文件
├── Themes/                    # XAML style resources / XAML样式资源
│   ├── Design/                # Design system / 设计系统
│   └── Controls/              # Control templates / 控件模板
└── Resources/                 # Resource dictionaries / 资源字典
    └── Dictionaries/          # Merged dictionaries / 合并字典
```

## 2. Resource Naming Conventions / 资源命名规范

### 2.1 Icons / 图标

**Format / 格式**: `icon-{purpose}-{size}.{ext}`

**Examples / 示例**:
- `icon-save-16.png` - 16x16 save icon
- `icon-delete-24.png` - 24x24 delete icon
- `icon-status-success-32.png` - 32x32 success status icon

**Standard Sizes / 标准尺寸**:
- Small: 16x16
- Medium: 24x24
- Large: 32x32
- Extra Large: 48x48

### 2.2 Images / 图片

**Format / 格式**: `img-{module}-{description}.{ext}`

**Examples / 示例**:
- `img-login-background.jpg` - Login background image
- `img-patient-avatar-default.png` - Default patient avatar
- `img-herbs-illustration.svg` - Herbs illustration

### 2.3 Logos / Logo

**Format / 格式**: `logo-{variant}-{size}.{ext}`

**Examples / 示例**:
- `logo-main-256.png` - Main logo, 256x256
- `logo-text-horizontal.svg` - Text logo, horizontal layout
- `logo-icon-only-48.png` - Icon only logo, 48x48

### 2.4 Application Icon / 应用图标

**Multiple resolutions required / 需要多种分辨率**:
- `app.ico` - Multi-resolution ICO file (16, 32, 48, 256)
- `app-16.png` - 16x16 PNG
- `app-32.png` - 32x32 PNG
- `app-48.png` - 48x48 PNG
- `app-256.png` - 256x256 PNG

## 3. File Format Guidelines / 文件格式指南

| Use Case | Recommended Format | Reason |
|----------|-------------------|---------|
| Icons | PNG or SVG | Transparency support, scalability |
| Backgrounds | JPG | Smaller file size for photos |
| Logos | SVG (preferred) or PNG | Scalability, transparency |
| Illustrations | SVG or PNG | Vector graphics preferred |
| App Icon | ICO + PNG | Windows compatibility |

## 4. WPF Resource Usage / WPF资源使用

### 4.1 Pack URI Syntax / Pack URI语法

```xml
<!-- Absolute Pack URI (with Assembly Reference) -->
<Image Source="pack://application:,,,/LYBT.Desktop.Shell;component/Assets/Icons/App/app.ico"/>

<!-- Resource Dictionary -->
<BitmapImage x:Key="SaveIcon"
             UriSource="pack://application:,,,/LYBT.Desktop.Shell;component/Assets/Icons/Actions/icon-save-24.png"/>
```

### 4.2 Build Action Settings / 生成操作设置

All image resources should be set as **Resource** in properties:
- Build Action: `Resource`
- Copy to Output Directory: `Do not copy`

### 4.3 Using ResourcePaths Class / 使用ResourcePaths类

```csharp
// In C# code
var iconPath = ResourcePaths.Icons.Save;
var logoPath = ResourcePaths.Images.LogoMain;

// Create BitmapImage
var bitmap = new BitmapImage(new Uri(ResourcePaths.Icons.AppIcon));
```

## 5. Resource Dictionary Organization / 资源字典组织

### 5.1 Dictionary Files / 字典文件

- `IconResources.xaml` - Icon definitions / 图标定义
- `ImageResources.xaml` - Image definitions / 图片定义
- `ColorResources.xaml` - Color definitions / 颜色定义
- `FontResources.xaml` - Font definitions / 字体定义

### 5.2 Merging Resources in App.xaml / 在App.xaml中合并资源

```xml
<Application.Resources>
    <ResourceDictionary>
        <ResourceDictionary.MergedDictionaries>
            <!-- Design System -->
            <ResourceDictionary Source="Themes/Design/Colors.xaml"/>
            <ResourceDictionary Source="Themes/Design/Typography.xaml"/>
            
            <!-- Resources -->
            <ResourceDictionary Source="Resources/Dictionaries/IconResources.xaml"/>
            <ResourceDictionary Source="Resources/Dictionaries/ImageResources.xaml"/>
        </ResourceDictionary.MergedDictionaries>
    </ResourceDictionary>
</Application.Resources>
```

## 6. Vector Icons / 矢量图标

### 6.1 Using Path Geometry / 使用路径几何

```xml
<!-- Define geometry in resources -->
<Geometry x:Key="SaveIconGeometry">
    M19,12V19A2,2 0 0,1 17,21H7A2,2 0 0,1 5,19V5A2,2 0 0,1 7,3H12.5L19,9.5V12M14,3V8H19M17,12L12,17L7,12L9.5,14.5L12,12L14.5,14.5L17,12Z
</Geometry>

<!-- Use in UI -->
<Path Data="{StaticResource SaveIconGeometry}" 
      Fill="{DynamicResource PrimaryBrush}"
      Width="24" Height="24"/>
```

### 6.2 Icon Fonts / 图标字体

For scalable icons, consider using icon fonts:
- Segoe MDL2 Assets (built-in Windows)
- Custom icon fonts (imported as resources)

## 7. Performance Optimization / 性能优化

### 7.1 Image Size Guidelines / 图片大小指南

- Icons: < 10 KB per file
- Logos: < 50 KB per file
- Backgrounds: < 200 KB per file
- Use appropriate compression

### 7.2 Resource Loading / 资源加载

```csharp
// Freeze resources for better performance
var bitmap = new BitmapImage();
bitmap.BeginInit();
bitmap.UriSource = new Uri(path);
bitmap.CacheOption = BitmapCacheOption.OnLoad;
bitmap.EndInit();
bitmap.Freeze(); // Makes immutable and thread-safe
```

## 8. Adding New Resources / 添加新资源

### 8.1 Checklist / 检查清单

- [ ] Follow naming convention / 遵循命名规范
- [ ] Place in correct directory / 放置在正确目录
- [ ] Set Build Action to Resource / 设置生成操作为Resource
- [ ] Add to resource dictionary if needed / 如需要添加到资源字典
- [ ] Update ResourcePaths.cs if applicable / 如适用更新ResourcePaths.cs
- [ ] Test resource loading / 测试资源加载
- [ ] Optimize file size / 优化文件大小

### 8.2 Example: Adding a New Icon / 示例：添加新图标

1. **Save file / 保存文件**: `Shell/Assets/Icons/Actions/icon-export-24.png`
2. **Add to project / 添加到项目**: Set Build Action = Resource
3. **Add to dictionary / 添加到字典**:
   ```xml
   <BitmapImage x:Key="ExportIcon"
                UriSource="pack://application:,,,/LYBT.Desktop.Shell;component/Assets/Icons/Actions/icon-export-24.png"/>
   ```
4. **Update ResourcePaths.cs**:
   ```csharp
   public const string Export = AssetsBase + "Icons/Actions/icon-export-24.png";
   ```

## 9. Resource Licensing / 资源授权

### 9.1 License File / 授权文件

Each Assets subdirectory should contain a `LICENSE.md` file documenting:
- Source of resources / 资源来源
- License type / 授权类型
- Attribution requirements / 署名要求

### 9.2 Free Resources / 免费资源

Recommended sources for free icons and images:
- [Material Design Icons](https://materialdesignicons.com/)
- [Feather Icons](https://feathericons.com/)
- [Unsplash](https://unsplash.com/) (images)
- [Flaticon](https://www.flaticon.com/) (require attribution)

## 10. Module-Specific Resources / 模块特定资源

Modules can have their own Assets folders:
```
Modules/
└── ModuleName/
    └── Assets/
        └── Images/
```

These should follow the same conventions but are specific to the module.

## 11. Troubleshooting / 故障排除

### Common Issues / 常见问题

**Resource not found / 资源未找到**:
- Check Build Action is set to Resource
- Verify Pack URI syntax is correct
- Ensure assembly name in URI matches project

**Image not displaying / 图片不显示**:
- Check file path and name
- Verify image format is supported
- Check if resource dictionary is merged

**Performance issues / 性能问题**:
- Optimize image file sizes
- Use vector graphics for icons
- Freeze BitmapImage objects

---

**Last Updated / 最后更新**: 2025-01-31  
**Version / 版本**: 1.0.0

> 📌 **Important / 重要**: All developers must follow these guidelines when adding or modifying resources.  
> 📌 **重要**: 所有开发人员在添加或修改资源时必须遵循这些准则。
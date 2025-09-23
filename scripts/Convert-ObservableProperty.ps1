# PowerShell脚本：批量转换CommunityToolkit.Mvvm的ObservableProperty为标准Prism属性

param(
    [string]$SourcePath = "D:\source\repos\LYBTZYZS\src\Client\Desktop"
)

function Convert-ObservablePropertyFile {
    param(
        [string]$FilePath
    )

    Write-Host "Processing: $FilePath"

    # 读取文件内容
    $content = Get-Content $FilePath -Raw -Encoding UTF8

    # 替换using语句
    $content = $content -replace 'using CommunityToolkit\.Mvvm\.ComponentModel;', 'using Prism.Mvvm;'

    # 替换基类
    $content = $content -replace 'public partial class (\w+) : ObservableObject', 'public class $1 : BindableBase'

    # 使用正则表达式匹配并替换ObservableProperty模式
    $pattern = '\[ObservableProperty\]\s*private\s+(\w+(?:\??|\[\])*)\s+(\w+)(?:\s*=\s*([^;]+))?;'

    $content = [regex]::Replace($content, $pattern, {
        param($match)

        $type = $match.Groups[1].Value
        $fieldName = $match.Groups[2].Value
        $initValue = $match.Groups[3].Value

        # 生成属性名（首字母大写）
        $propertyName = [char]::ToUpper($fieldName[0]) + $fieldName.Substring(1)

        # 生成私有字段名（添加下划线前缀）
        $privateFieldName = "_$fieldName"

        # 构建新的属性代码
        $newProperty = @"
    private $type $privateFieldName$(if ($initValue) { " = $initValue" });
    public $type $propertyName
    {
        get => $privateFieldName;
        set => SetProperty(ref $privateFieldName, value);
    }
"@

        return $newProperty
    })

    # 写回文件
    Set-Content $FilePath -Value $content -Encoding UTF8
    Write-Host "Converted: $FilePath"
}

# 查找所有包含CommunityToolkit.Mvvm的C#文件
$files = Get-ChildItem -Path $SourcePath -Recurse -Filter "*.cs" |
    Where-Object { (Get-Content $_.FullName -Raw) -match "CommunityToolkit\.Mvvm" }

Write-Host "Found $($files.Count) files to convert:"

foreach ($file in $files) {
    Convert-ObservablePropertyFile -FilePath $file.FullName
}

Write-Host "Conversion completed!"
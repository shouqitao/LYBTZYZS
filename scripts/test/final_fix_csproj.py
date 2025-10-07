import os

# 需要修复的文件列表
files_to_fix = [
    r"D:\source\repos\LYBTZYZS\tests\UnitTests\Modules\Herbs.UnitTests\LYBT.Module.Herbs.Tests.csproj",
    r"D:\source\repos\LYBTZYZS\tests\UnitTests\Modules\MedicalCase.UnitTests\LYBT.Module.MedicalCase.Tests.csproj",
    r"D:\source\repos\LYBTZYZS\tests\UnitTests\Modules\Patients.UnitTests\LYBT.Module.Patients.Tests.csproj",
    r"D:\source\repos\LYBTZYZS\tests\UnitTests\Modules\Users.UnitTests\LYBT.Module.Users.Tests.csproj",
    r"D:\source\repos\LYBTZYZS\tests\UnitTests\Shared.Models.UnitTests\LYBT.Shared.Models.Tests.csproj",
    r"D:\source\repos\LYBTZYZS\tests\UnitTests\Shared\LYBT.Shared.Utilities.Tests\LYBT.Shared.Utilities.Tests.csproj"
]

# 要删除的错误行
bad_lines = [
    "    ",  # 空白行
    "      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>",
    "      <PrivateAssets>all</PrivateAssets>",
    "    </PackageReference>"
]

for file_path in files_to_fix:
    if os.path.exists(file_path):
        print(f"Fixing: {os.path.basename(file_path)}")

        with open(file_path, 'r', encoding='utf-8') as f:
            lines = f.readlines()

        # 删除有问题的行 (通常在第20-23行)
        new_lines = []
        skip_count = 0

        for i, line in enumerate(lines):
            # 检查是否是错误模式的开始
            if i < len(lines) - 3:
                # 检查是否是孤立的PackageReference结束标签组
                if (line.strip() == "" and
                    i + 1 < len(lines) and "<IncludeAssets>" in lines[i+1] and
                    i + 2 < len(lines) and "<PrivateAssets>" in lines[i+2] and
                    i + 3 < len(lines) and "</PackageReference>" in lines[i+3]):
                    skip_count = 4  # 跳过这4行

            if skip_count > 0:
                skip_count -= 1
                continue

            new_lines.append(line)

        with open(file_path, 'w', encoding='utf-8') as f:
            f.writelines(new_lines)

        print(f"  - Fixed")

print("\nAll files fixed!")
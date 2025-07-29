@echo off
echo 正在创建凌隐宝堂中医诊所WPF客户端项目结构...

REM 创建主要项目目录
mkdir "LYBT.WPF.Client.Shell\Services" 2>nul
mkdir "LYBT.WPF.Client.Shell\Resources\Icons" 2>nul
mkdir "LYBT.WPF.Client.Shell\Resources\Images" 2>nul

REM 创建Core项目结构
mkdir "LYBT.WPF.Client.Core\Models\Configuration" 2>nul
mkdir "LYBT.WPF.Client.Core\Extensions" 2>nul
mkdir "LYBT.WPF.Client.Core\Attributes" 2>nul

REM 创建Services项目结构
mkdir "LYBT.WPF.Client.Services\Authentication" 2>nul
mkdir "LYBT.WPF.Client.Services\SystemManagement\Users" 2>nul
mkdir "LYBT.WPF.Client.Services\SystemManagement\Patients" 2>nul
mkdir "LYBT.WPF.Client.Services\SystemManagement\Herbs" 2>nul
mkdir "LYBT.WPF.Client.Services\SystemManagement\PrescriptionTemplates" 2>nul
mkdir "LYBT.WPF.Client.Services\SystemManagement\Pharmacy" 2>nul
mkdir "LYBT.WPF.Client.Services\SystemManagement\Config" 2>nul
mkdir "LYBT.WPF.Client.Services\SystemManagement\Logs" 2>nul
mkdir "LYBT.WPF.Client.Services\SystemManagement\DataManagement" 2>nul
mkdir "LYBT.WPF.Client.Services\SystemManagement\Reports" 2>nul
mkdir "LYBT.WPF.Client.Services\Workflow\Registration" 2>nul
mkdir "LYBT.WPF.Client.Services\Workflow\Consultation" 2>nul
mkdir "LYBT.WPF.Client.Services\Workflow\MedicalRecords" 2>nul
mkdir "LYBT.WPF.Client.Services\Workflow\Billing" 2>nul
mkdir "LYBT.WPF.Client.Services\Workflow\Pharmacy" 2>nul
mkdir "LYBT.WPF.Client.Services\Infrastructure" 2>nul

REM 创建Infrastructure项目结构
mkdir "LYBT.WPF.Client.Infrastructure\Behaviors" 2>nul
mkdir "LYBT.WPF.Client.Infrastructure\Converters" 2>nul
mkdir "LYBT.WPF.Client.Infrastructure\Controls" 2>nul
mkdir "LYBT.WPF.Client.Infrastructure\Helpers" 2>nul
mkdir "LYBT.WPF.Client.Infrastructure\Themes" 2>nul
mkdir "LYBT.WPF.Client.Infrastructure\Templates\PrintTemplates" 2>nul
mkdir "LYBT.WPF.Client.Infrastructure\Templates\ReportTemplates" 2>nul
mkdir "LYBT.WPF.Client.Infrastructure\Validations\Rules" 2>nul
mkdir "LYBT.WPF.Client.Infrastructure\Validations\Attributes" 2>nul
mkdir "LYBT.WPF.Client.Infrastructure\Security\Encryption" 2>nul
mkdir "LYBT.WPF.Client.Infrastructure\Security\Authorization" 2>nul
mkdir "LYBT.WPF.Client.Infrastructure\Security\Audit" 2>nul

REM 创建模块项目结构
REM 认证模块
mkdir "LYBT.WPF.Client.Modules\Authentication\ViewModels" 2>nul
mkdir "LYBT.WPF.Client.Modules\Authentication\Models" 2>nul
mkdir "LYBT.WPF.Client.Modules\Authentication\Services" 2>nul

REM 系统管理模块
mkdir "LYBT.WPF.Client.Modules\SystemManagement\Users\Views" 2>nul
mkdir "LYBT.WPF.Client.Modules\SystemManagement\Users\ViewModels" 2>nul
mkdir "LYBT.WPF.Client.Modules\SystemManagement\Users\Models" 2>nul
mkdir "LYBT.WPF.Client.Modules\SystemManagement\Patients\Views" 2>nul
mkdir "LYBT.WPF.Client.Modules\SystemManagement\Patients\ViewModels" 2>nul
mkdir "LYBT.WPF.Client.Modules\SystemManagement\Patients\Models" 2>nul
mkdir "LYBT.WPF.Client.Modules\SystemManagement\Herbs\Views" 2>nul
mkdir "LYBT.WPF.Client.Modules\SystemManagement\Herbs\ViewModels" 2>nul
mkdir "LYBT.WPF.Client.Modules\SystemManagement\Herbs\Models" 2>nul

REM 前台模块
mkdir "LYBT.WPF.Client.Modules\FrontDesk\Views" 2>nul
mkdir "LYBT.WPF.Client.Modules\FrontDesk\ViewModels" 2>nul
mkdir "LYBT.WPF.Client.Modules\FrontDesk\Models" 2>nul
mkdir "LYBT.WPF.Client.Modules\FrontDesk\Services" 2>nul

REM 医生模块
mkdir "LYBT.WPF.Client.Modules\Doctor\Views" 2>nul
mkdir "LYBT.WPF.Client.Modules\Doctor\ViewModels" 2>nul
mkdir "LYBT.WPF.Client.Modules\Doctor\Models" 2>nul
mkdir "LYBT.WPF.Client.Modules\Doctor\Services" 2>nul

REM 收银员模块
mkdir "LYBT.WPF.Client.Modules\Cashier\Views" 2>nul
mkdir "LYBT.WPF.Client.Modules\Cashier\ViewModels" 2>nul
mkdir "LYBT.WPF.Client.Modules\Cashier\Models" 2>nul
mkdir "LYBT.WPF.Client.Modules\Cashier\Services" 2>nul

REM 药剂师模块
mkdir "LYBT.WPF.Client.Modules\Pharmacist\Views" 2>nul
mkdir "LYBT.WPF.Client.Modules\Pharmacist\ViewModels" 2>nul
mkdir "LYBT.WPF.Client.Modules\Pharmacist\Models" 2>nul
mkdir "LYBT.WPF.Client.Modules\Pharmacist\Services" 2>nul

REM 通用模块
mkdir "LYBT.WPF.Client.Modules\Common\Views" 2>nul
mkdir "LYBT.WPF.Client.Modules\Common\ViewModels" 2>nul
mkdir "LYBT.WPF.Client.Modules\Common\Models" 2>nul

REM 创建测试项目目录
mkdir "Tests\LYBT.WPF.Client.Tests.Unit\Services" 2>nul
mkdir "Tests\LYBT.WPF.Client.Tests.Unit\ViewModels" 2>nul
mkdir "Tests\LYBT.WPF.Client.Tests.Unit\Helpers" 2>nul
mkdir "Tests\LYBT.WPF.Client.Tests.Integration\ApiTests" 2>nul
mkdir "Tests\LYBT.WPF.Client.Tests.Integration\WorkflowTests" 2>nul
mkdir "Tests\LYBT.WPF.Client.Tests.UI\UITests" 2>nul

REM 创建文档和资源目录
mkdir "Documentation\Requirements" 2>nul
mkdir "Documentation\Development" 2>nul
mkdir "Documentation\Deployment" 2>nul
mkdir "Documentation\UserManuals" 2>nul
mkdir "Scripts\Build" 2>nul
mkdir "Scripts\Deploy" 2>nul
mkdir "Scripts\Database" 2>nul
mkdir "Resources\Images\Icons" 2>nul
mkdir "Resources\Images\Logos" 2>nul
mkdir "Resources\Images\UI" 2>nul
mkdir "Resources\Fonts" 2>nul
mkdir "Resources\Templates\Excel" 2>nul
mkdir "Resources\Templates\Word" 2>nul
mkdir "Resources\Configuration" 2>nul
mkdir "Tools\DatabaseMigration" 2>nul
mkdir "Tools\DataImport" 2>nul
mkdir "Tools\CodeGeneration" 2>nul

echo 项目结构创建完成！

echo.
echo 凌隐宝堂中医诊所WPF客户端项目结构：
echo - LYBT.WPF.Client.Shell          主壳程序
echo - LYBT.WPF.Client.Core           核心基础设施
echo - LYBT.WPF.Client.Services       服务层
echo - LYBT.WPF.Client.Infrastructure 基础设施
echo - LYBT.WPF.Client.Modules        业务模块
echo   - Authentication               认证模块
echo   - SystemManagement             系统管理模块
echo   - FrontDesk                    前台模块
echo   - Doctor                       医生模块
echo   - Cashier                      收银员模块
echo   - Pharmacist                   药剂师模块
echo   - Common                       通用模块
echo - Tests                          测试项目
echo - Documentation                  文档
echo - Resources                      资源文件

pause
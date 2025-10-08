# ============================================
# 通过API插入测试数据脚本
# 测试Herbs和Formulas模块的CREATE接口
# ============================================

$ErrorActionPreference = "Stop"

# API配置
$apiBaseUrl = "https://localhost:5001"
$username = "sysadmin"
$password = "Dev@Admin2025!"

Write-Host "==================== 开始测试API数据插入 ====================" -ForegroundColor Green
Write-Host ""

# ============================================
# 1. 登录获取Token
# ============================================

Write-Host "[步骤1] 登录系统获取Token..." -ForegroundColor Cyan

$loginBody = @{
    username = $username
    password = $password
} | ConvertTo-Json

try {
    $loginResponse = Invoke-RestMethod `
        -Uri "$apiBaseUrl/api/v1/auth/login" `
        -Method Post `
        -Body $loginBody `
        -ContentType "application/json" `
        -SkipCertificateCheck

    if ($loginResponse.success -and $loginResponse.data.token) {
        $token = $loginResponse.data.token
        Write-Host "✅ 登录成功，Token获取成功" -ForegroundColor Green
    } else {
        Write-Host "❌ 登录失败: $($loginResponse.message)" -ForegroundColor Red
        exit 1
    }
} catch {
    Write-Host "❌ 登录请求失败: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

Write-Host ""

# ============================================
# 2. 插入药材测试数据
# ============================================

Write-Host "[步骤2] 开始插入药材测试数据..." -ForegroundColor Cyan

$herbs = @(
    @{ name = "人参"; pinYinCode = "RS"; origin = "吉林"; spec = "特级"; unit = "克"; price = 15.00; effect = "大补元气，复脉固脱，补脾益肺，生津养血，安神益智"; usage = "3-9克，煎服" },
    @{ name = "黄芪"; pinYinCode = "HQ"; origin = "内蒙古"; spec = "一级"; unit = "克"; price = 0.80; effect = "补气升阳，固表止汗，利水消肿，生津养血"; usage = "9-30克，煎服" },
    @{ name = "党参"; pinYinCode = "DS"; origin = "山西"; spec = "一级"; unit = "克"; price = 1.20; effect = "健脾益肺，养血生津"; usage = "9-30克，煎服" },
    @{ name = "白术"; pinYinCode = "BZ"; origin = "浙江"; spec = "一级"; unit = "克"; price = 1.50; effect = "健脾益气，燥湿利水，止汗，安胎"; usage = "6-12克，煎服" },
    @{ name = "当归"; pinYinCode = "DG"; origin = "甘肃"; spec = "一级"; unit = "克"; price = 2.00; effect = "补血活血，调经止痛，润肠通便"; usage = "6-12克，煎服" },
    @{ name = "熟地黄"; pinYinCode = "SDH"; origin = "河南"; spec = "一级"; unit = "克"; price = 1.80; effect = "补血滋阴，益精填髓"; usage = "9-15克，煎服" },
    @{ name = "白芍"; pinYinCode = "BS"; origin = "安徽"; spec = "一级"; unit = "克"; price = 1.60; effect = "养血调经，敛阴止汗，柔肝止痛，平抑肝阳"; usage = "6-15克，煎服" },
    @{ name = "枸杞子"; pinYinCode = "GQZ"; origin = "宁夏"; spec = "特级"; unit = "克"; price = 1.00; effect = "滋补肝肾，益精明目"; usage = "6-12克，煎服" },
    @{ name = "麦冬"; pinYinCode = "MD"; origin = "浙江"; spec = "一级"; unit = "克"; price = 1.20; effect = "养阴生津，润肺清心"; usage = "6-12克，煎服" },
    @{ name = "金银花"; pinYinCode = "JYH"; origin = "河南"; spec = "一级"; unit = "克"; price = 2.00; effect = "清热解毒，疏散风热"; usage = "6-15克，煎服" },
    @{ name = "连翘"; pinYinCode = "LQ"; origin = "山西"; spec = "一级"; unit = "克"; price = 1.80; effect = "清热解毒，消肿散结，疏散风热"; usage = "6-15克，煎服" },
    @{ name = "陈皮"; pinYinCode = "CP"; origin = "广东"; spec = "一级"; unit = "克"; price = 1.50; effect = "理气健脾，燥湿化痰"; usage = "3-10克，煎服" },
    @{ name = "川芎"; pinYinCode = "CX"; origin = "四川"; spec = "一级"; unit = "克"; price = 1.80; effect = "活血行气，祛风止痛"; usage = "3-10克，煎服" },
    @{ name = "丹参"; pinYinCode = "DS2"; origin = "山东"; spec = "一级"; unit = "克"; price = 1.50; effect = "活血祛瘀，通经止痛，清心除烦，凉血消痈"; usage = "9-15克，煎服" },
    @{ name = "红花"; pinYinCode = "HH"; origin = "新疆"; spec = "一级"; unit = "克"; price = 2.50; effect = "活血通经，散瘀止痛"; usage = "3-10克，煎服" }
)

$herbsSuccessCount = 0
$herbsFailCount = 0

foreach ($herb in $herbs) {
    try {
        $herbBody = $herb | ConvertTo-Json

        $response = Invoke-RestMethod `
            -Uri "$apiBaseUrl/api/v1/herbs" `
            -Method Post `
            -Headers @{ Authorization = "Bearer $token" } `
            -Body $herbBody `
            -ContentType "application/json" `
            -SkipCertificateCheck

        if ($response.success) {
            $herbsSuccessCount++
            Write-Host "  ✅ 药材 [$($herb.name)] 插入成功" -ForegroundColor Green
        } else {
            $herbsFailCount++
            Write-Host "  ❌ 药材 [$($herb.name)] 插入失败: $($response.message)" -ForegroundColor Red
        }
    } catch {
        $herbsFailCount++
        Write-Host "  ❌ 药材 [$($herb.name)] 插入异常: $($_.Exception.Message)" -ForegroundColor Red
    }

    Start-Sleep -Milliseconds 200
}

Write-Host ""
Write-Host "📊 药材插入统计: 成功 $herbsSuccessCount 条，失败 $herbsFailCount 条" -ForegroundColor Yellow
Write-Host ""

# ============================================
# 3. 插入验方测试数据
# ============================================

Write-Host "[步骤3] 开始插入验方测试数据..." -ForegroundColor Cyan

$formulas = @(
    @{
        name = "四君子汤"
        effect = "益气健脾"
        usage = "水煎服，日一剂"
        category = "补益剂"
        formulaType = 1  # Classic
        properties = "甘温平补"
        isShared = $true
        indications = "脾胃气虚证"
    },
    @{
        name = "四物汤"
        effect = "补血调血"
        usage = "水煎服，日一剂"
        category = "补益剂"
        formulaType = 1
        properties = "补血调经"
        isShared = $true
        indications = "血虚证"
    },
    @{
        name = "银翘散"
        effect = "疏散风热，清热解毒"
        usage = "水煎服，日一剂"
        category = "解表剂"
        formulaType = 1
        properties = "辛凉平剂"
        isShared = $true
        indications = "温病初起，风热感冒"
    },
    @{
        name = "补中益气汤"
        effect = "补中益气，升阳举陷"
        usage = "水煎服，日一剂"
        category = "补益剂"
        formulaType = 1
        properties = "甘温除热"
        isShared = $true
        indications = "脾虚气陷证"
    },
    @{
        name = "逍遥散"
        effect = "疏肝解郁，养血健脾"
        usage = "水煎服，日一剂"
        category = "调和剂"
        formulaType = 1
        properties = "肝脾同调"
        isShared = $true
        indications = "肝郁脾虚证"
    }
)

$formulasSuccessCount = 0
$formulasFailCount = 0

foreach ($formula in $formulas) {
    try {
        $formulaBody = $formula | ConvertTo-Json

        $response = Invoke-RestMethod `
            -Uri "$apiBaseUrl/api/v1/formulas" `
            -Method Post `
            -Headers @{ Authorization = "Bearer $token" } `
            -Body $formulaBody `
            -ContentType "application/json" `
            -SkipCertificateCheck

        if ($response.success) {
            $formulasSuccessCount++
            Write-Host "  ✅ 验方 [$($formula.name)] 插入成功" -ForegroundColor Green
        } else {
            $formulasFailCount++
            Write-Host "  ❌ 验方 [$($formula.name)] 插入失败: $($response.message)" -ForegroundColor Red
        }
    } catch {
        $formulasFailCount++
        Write-Host "  ❌ 验方 [$($formula.name)] 插入异常: $($_.Exception.Message)" -ForegroundColor Red
    }

    Start-Sleep -Milliseconds 200
}

Write-Host ""
Write-Host "📊 验方插入统计: 成功 $formulasSuccessCount 条，失败 $formulasFailCount 条" -ForegroundColor Yellow
Write-Host ""

# ============================================
# 4. 验证数据插入
# ============================================

Write-Host "[步骤4] 验证数据插入结果..." -ForegroundColor Cyan

try {
    # 查询药材列表
    $herbsResponse = Invoke-RestMethod `
        -Uri "$apiBaseUrl/api/v1/herbs?page=1&pageSize=20" `
        -Method Get `
        -Headers @{ Authorization = "Bearer $token" } `
        -SkipCertificateCheck

    if ($herbsResponse.success) {
        Write-Host "  ✅ 药材列表查询成功，共 $($herbsResponse.data.totalCount) 条数据" -ForegroundColor Green
    }

    # 查询验方列表
    $formulasResponse = Invoke-RestMethod `
        -Uri "$apiBaseUrl/api/v1/formulas?page=1&pageSize=20" `
        -Method Get `
        -Headers @{ Authorization = "Bearer $token" } `
        -SkipCertificateCheck

    if ($formulasResponse.success) {
        Write-Host "  ✅ 验方列表查询成功，共 $($formulasResponse.data.totalCount) 条数据" -ForegroundColor Green
    }
} catch {
    Write-Host "  ⚠️  数据验证异常: $($_.Exception.Message)" -ForegroundColor Yellow
}

Write-Host ""
Write-Host "==================== 测试完成 ====================" -ForegroundColor Green
Write-Host ""
Write-Host "📋 汇总统计:" -ForegroundColor Cyan
Write-Host "  • 药材插入: 成功 $herbsSuccessCount 条，失败 $herbsFailCount 条" -ForegroundColor White
Write-Host "  • 验方插入: 成功 $formulasSuccessCount 条，失败 $formulasFailCount 条" -ForegroundColor White
Write-Host ""
Write-Host "提示：现在可以在桌面应用中测试数据加载功能了！" -ForegroundColor Yellow

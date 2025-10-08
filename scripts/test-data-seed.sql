-- ============================================
-- 测试数据种子脚本
-- 生成Herbs和Formulas测试数据
-- ============================================

USE [LYBT_DB];
GO

-- ============================================
-- 1. 插入药材测试数据 (Herbs)
-- ============================================

-- 清空现有测试数据（可选）
-- DELETE FROM Herbs WHERE Name LIKE '%测试%';

-- 插入常用中药材
INSERT INTO Herbs (Id, Name, PinYinCode, Origin, Spec, Unit, Price, CostPrice, Effect, Usage, Status, CreatedAt, UpdatedAt)
VALUES
-- 补气药
(NEWID(), '人参', 'RS', '吉林', '特级', '克', 15.00, 12.00, '大补元气，复脉固脱，补脾益肺，生津养血，安神益智', '3-9克，煎服', 1, GETDATE(), GETDATE()),
(NEWID(), '黄芪', 'HQ', '内蒙古', '一级', '克', 0.80, 0.60, '补气升阳，固表止汗，利水消肿，生津养血', '9-30克，煎服', 1, GETDATE(), GETDATE()),
(NEWID(), '党参', 'DS', '山西', '一级', '克', 1.20, 0.90, '健脾益肺，养血生津', '9-30克，煎服', 1, GETDATE(), GETDATE()),
(NEWID(), '白术', 'BZ', '浙江', '一级', '克', 1.50, 1.20, '健脾益气，燥湿利水，止汗，安胎', '6-12克，煎服', 1, GETDATE(), GETDATE()),

-- 补血药
(NEWID(), '当归', 'DG', '甘肃', '一级', '克', 2.00, 1.50, '补血活血，调经止痛，润肠通便', '6-12克，煎服', 1, GETDATE(), GETDATE()),
(NEWID(), '熟地黄', 'SDH', '河南', '一级', '克', 1.80, 1.40, '补血滋阴，益精填髓', '9-15克，煎服', 1, GETDATE(), GETDATE()),
(NEWID(), '白芍', 'BS', '安徽', '一级', '克', 1.60, 1.20, '养血调经，敛阴止汗，柔肝止痛，平抑肝阳', '6-15克，煎服', 1, GETDATE(), GETDATE()),
(NEWID(), '阿胶', 'AJ', '山东', '特级', '克', 8.00, 6.50, '补血滋阴，润燥，止血', '3-9克，烊化', 1, GETDATE(), GETDATE()),

-- 补阴药
(NEWID(), '枸杞子', 'GQZ', '宁夏', '特级', '克', 1.00, 0.70, '滋补肝肾，益精明目', '6-12克，煎服', 1, GETDATE(), GETDATE()),
(NEWID(), '麦冬', 'MD', '浙江', '一级', '克', 1.20, 0.90, '养阴生津，润肺清心', '6-12克，煎服', 1, GETDATE(), GETDATE()),
(NEWID(), '天冬', 'TD', '贵州', '一级', '克', 1.50, 1.10, '养阴润燥，清肺生津', '6-12克，煎服', 1, GETDATE(), GETDATE()),

-- 补阳药
(NEWID(), '鹿茸', 'LR', '东北', '特级', '克', 50.00, 40.00, '壮肾阳，益精血，强筋骨，调冲任，托疮毒', '1-2克，研粉吞服', 1, GETDATE(), GETDATE()),
(NEWID(), '肉苁蓉', 'RCR', '内蒙古', '一级', '克', 3.50, 2.80, '补肾阳，益精血，润肠通便', '6-10克，煎服', 1, GETDATE(), GETDATE()),
(NEWID(), '巴戟天', 'BJT', '广东', '一级', '克', 2.50, 2.00, '补肾阳，强筋骨，祛风湿', '3-10克，煎服', 1, GETDATE(), GETDATE()),

-- 清热药
(NEWID(), '金银花', 'JYH', '河南', '一级', '克', 2.00, 1.50, '清热解毒，疏散风热', '6-15克，煎服', 1, GETDATE(), GETDATE()),
(NEWID(), '连翘', 'LQ', '山西', '一级', '克', 1.80, 1.30, '清热解毒，消肿散结，疏散风热', '6-15克，煎服', 1, GETDATE(), GETDATE()),
(NEWID(), '黄芩', 'HQ2', '河北', '一级', '克', 1.50, 1.10, '清热燥湿，泻火解毒，止血，安胎', '3-10克，煎服', 1, GETDATE(), GETDATE()),
(NEWID(), '黄连', 'HL', '四川', '一级', '克', 8.00, 6.50, '清热燥湿，泻火解毒', '2-5克，煎服', 1, GETDATE(), GETDATE()),
(NEWID(), '栀子', 'ZZ', '江西', '一级', '克', 1.20, 0.90, '泻火除烦，清热利湿，凉血解毒', '6-10克，煎服', 1, GETDATE(), GETDATE()),

-- 理气药
(NEWID(), '陈皮', 'CP', '广东', '一级', '克', 1.50, 1.10, '理气健脾，燥湿化痰', '3-10克，煎服', 1, GETDATE(), GETDATE()),
(NEWID(), '枳实', 'ZS', '江西', '一级', '克', 1.80, 1.40, '破气消积，化痰散痞', '3-10克，煎服', 1, GETDATE(), GETDATE()),
(NEWID(), '木香', 'MX', '云南', '一级', '克', 3.00, 2.40, '行气止痛，健脾消食', '3-10克，煎服', 1, GETDATE(), GETDATE()),

-- 活血化瘀药
(NEWID(), '川芎', 'CX', '四川', '一级', '克', 1.80, 1.40, '活血行气，祛风止痛', '3-10克，煎服', 1, GETDATE(), GETDATE()),
(NEWID(), '丹参', 'DS2', '山东', '一级', '克', 1.50, 1.10, '活血祛瘀，通经止痛，清心除烦，凉血消痈', '9-15克，煎服', 1, GETDATE(), GETDATE()),
(NEWID(), '红花', 'HH', '新疆', '一级', '克', 2.50, 2.00, '活血通经，散瘀止痛', '3-10克，煎服', 1, GETDATE(), GETDATE()),
(NEWID(), '桃仁', 'TR', '山东', '一级', '克', 1.20, 0.90, '活血祛瘀，润肠通便，止咳平喘', '5-10克，煎服', 1, GETDATE(), GETDATE());

PRINT '已插入 25 条药材测试数据';
GO

-- ============================================
-- 2. 插入验方测试数据 (Formulas)
-- ============================================

-- 插入经典验方
DECLARE @Formula1Id UNIQUEIDENTIFIER = NEWID();
DECLARE @Formula2Id UNIQUEIDENTIFIER = NEWID();
DECLARE @Formula3Id UNIQUEIDENTIFIER = NEWID();
DECLARE @Formula4Id UNIQUEIDENTIFIER = NEWID();
DECLARE @Formula5Id UNIQUEIDENTIFIER = NEWID();

INSERT INTO Formulas (Id, Name, Effect, Usage, Category, FormulaType, Property, IsShared, Status, CreatedAt, UpdatedAt)
VALUES
(@Formula1Id, '四君子汤', '益气健脾', '水煎服，日一剂', '补益剂', 1, '甘温平补', 1, 1, GETDATE(), GETDATE()),
(@Formula2Id, '四物汤', '补血调血', '水煎服，日一剂', '补益剂', 1, '补血调经', 1, 1, GETDATE(), GETDATE()),
(@Formula3Id, '银翘散', '疏散风热，清热解毒', '水煎服，日一剂', '解表剂', 1, '辛凉平剂', 1, 1, GETDATE(), GETDATE()),
(@Formula4Id, '补中益气汤', '补中益气，升阳举陷', '水煎服，日一剂', '补益剂', 1, '甘温除热', 1, 1, GETDATE(), GETDATE()),
(@Formula5Id, '逍遥散', '疏肝解郁，养血健脾', '水煎服，日一剂', '调和剂', 1, '肝脾同调', 1, 1, GETDATE(), GETDATE());

PRINT '已插入 5 条验方测试数据';
GO

-- ============================================
-- 3. 验证插入结果
-- ============================================

PRINT '==================== 验证结果 ====================';
PRINT '药材数据统计:';
SELECT COUNT(*) AS 药材总数 FROM Herbs WHERE Status = 1;

PRINT '';
PRINT '验方数据统计:';
SELECT COUNT(*) AS 验方总数 FROM Formulas WHERE Status = 1;

PRINT '';
PRINT '药材列表（前10条）:';
SELECT TOP 10 Name AS 药材名称, PinYinCode AS 拼音码, Origin AS 产地, Price AS 单价, Unit AS 单位
FROM Herbs
WHERE Status = 1
ORDER BY CreatedAt DESC;

PRINT '';
PRINT '验方列表:';
SELECT Name AS 验方名称, Category AS 分类, Effect AS 功效,
       CASE FormulaType WHEN 1 THEN '经典方' WHEN 2 THEN '经验方' ELSE '其他' END AS 类型
FROM Formulas
WHERE Status = 1
ORDER BY CreatedAt DESC;

GO

PRINT '';
PRINT '==================== 测试数据插入完成 ====================';
PRINT '提示：现在可以在桌面应用中测试药材管理和验方管理模块的数据加载功能';
GO

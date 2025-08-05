-- 插入常用中药材数据 - 简化版本
-- 数据库：LYBTDB
-- 表名：Herbs

-- 插入解表药
INSERT INTO Herbs (Id, Name, PinYinCode, Origin, Spec, Unit, Price, Stock, BatchNo, ExpireDate, Effect, Usage, Status, CreatedAt, Specification) 
VALUES (NEWID(), '麻黄', 'mh', '内蒙古', '优质', 'g', 12.50, 500, 'MH20240101', '2026-12-31', '发汗解表，宣肺平喘，利水消肿', '煎服，3-10g', 0, GETDATE(), 1);

INSERT INTO Herbs (Id, Name, PinYinCode, Origin, Spec, Unit, Price, Stock, BatchNo, ExpireDate, Effect, Usage, Status, CreatedAt, Specification) 
VALUES (NEWID(), '桂枝', 'gz', '广西', '优质', 'g', 15.00, 300, 'GZ20240101', '2026-12-31', '发汗解肌，温经通脉，助阳化气', '煎服，3-10g', 0, GETDATE(), 1);

INSERT INTO Herbs (Id, Name, PinYinCode, Origin, Spec, Unit, Price, Stock, BatchNo, ExpireDate, Effect, Usage, Status, CreatedAt, Specification) 
VALUES (NEWID(), '防风', 'ff', '内蒙古', '优质', 'g', 18.00, 400, 'FF20240101', '2026-12-31', '祛风解表，胜湿止痛，止痉', '煎服，3-10g', 0, GETDATE(), 1);

-- 插入清热药
INSERT INTO Herbs (Id, Name, PinYinCode, Origin, Spec, Unit, Price, Stock, BatchNo, ExpireDate, Effect, Usage, Status, CreatedAt, Specification) 
VALUES (NEWID(), '金银花', 'jyh', '山东', '优质', 'g', 38.00, 200, 'JYH20240101', '2026-12-31', '清热解毒，疏散风热', '煎服，6-15g', 0, GETDATE(), 1);

INSERT INTO Herbs (Id, Name, PinYinCode, Origin, Spec, Unit, Price, Stock, BatchNo, ExpireDate, Effect, Usage, Status, CreatedAt, Specification) 
VALUES (NEWID(), '黄芩', 'hq', '山西', '优质', 'g', 28.00, 350, 'HQ20240101', '2026-12-31', '清热燥湿，泻火解毒，止血，安胎', '煎服，3-10g', 0, GETDATE(), 1);

INSERT INTO Herbs (Id, Name, PinYinCode, Origin, Spec, Unit, Price, Stock, BatchNo, ExpireDate, Effect, Usage, Status, CreatedAt, Specification) 
VALUES (NEWID(), '黄连', 'hl', '四川', '优质', 'g', 85.00, 150, 'HL20240101', '2026-12-31', '清热燥湿，泻火解毒', '煎服，2-5g', 0, GETDATE(), 1);

-- 插入补益药
INSERT INTO Herbs (Id, Name, PinYinCode, Origin, Spec, Unit, Price, Stock, BatchNo, ExpireDate, Effect, Usage, Status, CreatedAt, Specification) 
VALUES (NEWID(), '人参', 'rs', '吉林', '优质', 'g', 380.00, 50, 'RS20240101', '2026-12-31', '大补元气，补脾益肺，生津止渴，安神益智', '煎服，3-10g', 0, GETDATE(), 1);

INSERT INTO Herbs (Id, Name, PinYinCode, Origin, Spec, Unit, Price, Stock, BatchNo, ExpireDate, Effect, Usage, Status, CreatedAt, Specification) 
VALUES (NEWID(), '党参', 'ds', '山西', '优质', 'g', 45.00, 300, 'DS20240101', '2026-12-31', '补中益气，健脾益肺', '煎服，9-30g', 0, GETDATE(), 1);

INSERT INTO Herbs (Id, Name, PinYinCode, Origin, Spec, Unit, Price, Stock, BatchNo, ExpireDate, Effect, Usage, Status, CreatedAt, Specification) 
VALUES (NEWID(), '黄芪', 'hqi', '内蒙古', '优质', 'g', 55.00, 400, 'HQI20240101', '2026-12-31', '补气升阳，固表止汗，利水消肿', '煎服，9-30g', 0, GETDATE(), 1);

INSERT INTO Herbs (Id, Name, PinYinCode, Origin, Spec, Unit, Price, Stock, BatchNo, ExpireDate, Effect, Usage, Status, CreatedAt, Specification) 
VALUES (NEWID(), '白术', 'bz', '浙江', '优质', 'g', 32.00, 350, 'BZ20240101', '2026-12-31', '健脾益气，燥湿利水，止汗，安胎', '煎服，6-12g', 0, GETDATE(), 1);

INSERT INTO Herbs (Id, Name, PinYinCode, Origin, Spec, Unit, Price, Stock, BatchNo, ExpireDate, Effect, Usage, Status, CreatedAt, Specification) 
VALUES (NEWID(), '甘草', 'gc', '内蒙古', '优质', 'g', 18.00, 500, 'GC20240101', '2026-12-31', '补脾益气，清热解毒，祛痰止咳', '煎服，1.5-10g', 0, GETDATE(), 1);

-- 插入活血化瘀药
INSERT INTO Herbs (Id, Name, PinYinCode, Origin, Spec, Unit, Price, Stock, BatchNo, ExpireDate, Effect, Usage, Status, CreatedAt, Specification) 
VALUES (NEWID(), '当归', 'dg', '甘肃', '优质', 'g', 65.00, 200, 'DG20240101', '2026-12-31', '补血活血，调经止痛，润肠通便', '煎服，6-12g', 0, GETDATE(), 1);

INSERT INTO Herbs (Id, Name, PinYinCode, Origin, Spec, Unit, Price, Stock, BatchNo, ExpireDate, Effect, Usage, Status, CreatedAt, Specification) 
VALUES (NEWID(), '川芎', 'cx', '四川', '优质', 'g', 38.00, 300, 'CX20240101', '2026-12-31', '活血行气，祛风止痛', '煎服，3-10g', 0, GETDATE(), 1);

INSERT INTO Herbs (Id, Name, PinYinCode, Origin, Spec, Unit, Price, Stock, BatchNo, ExpireDate, Effect, Usage, Status, CreatedAt, Specification) 
VALUES (NEWID(), '白芍', 'bs', '安徽', '优质', 'g', 25.00, 250, 'BS20240101', '2026-12-31', '养血调经，敛阴止汗，柔肝止痛', '煎服，6-15g', 0, GETDATE(), 1);

-- 插入化痰止咳药
INSERT INTO Herbs (Id, Name, PinYinCode, Origin, Spec, Unit, Price, Stock, BatchNo, ExpireDate, Effect, Usage, Status, CreatedAt, Specification) 
VALUES (NEWID(), '半夏', 'bx', '河南', '优质', 'g', 58.00, 180, 'BX20240101', '2026-12-31', '燥湿化痰，降逆止呕，消痞散结', '煎服，3-10g', 0, GETDATE(), 1);

INSERT INTO Herbs (Id, Name, PinYinCode, Origin, Spec, Unit, Price, Stock, BatchNo, ExpireDate, Effect, Usage, Status, CreatedAt, Specification) 
VALUES (NEWID(), '陈皮', 'cp', '广东', '优质', 'g', 12.00, 400, 'CP20240101', '2026-12-31', '理气健脾，燥湿化痰', '煎服，3-10g', 0, GETDATE(), 1);

INSERT INTO Herbs (Id, Name, PinYinCode, Origin, Spec, Unit, Price, Stock, BatchNo, ExpireDate, Effect, Usage, Status, CreatedAt, Specification) 
VALUES (NEWID(), '茯苓', 'fl', '云南', '优质', 'g', 28.00, 350, 'FL20240101', '2026-12-31', '利水渗湿，健脾宁心', '煎服，9-15g', 0, GETDATE(), 1);

-- 插入理气药
INSERT INTO Herbs (Id, Name, PinYinCode, Origin, Spec, Unit, Price, Stock, BatchNo, ExpireDate, Effect, Usage, Status, CreatedAt, Specification) 
VALUES (NEWID(), '柴胡', 'ch', '山西', '优质', 'g', 48.00, 250, 'CH20240101', '2026-12-31', '疏散退热，疏肝解郁，升举阳气', '煎服，3-10g', 0, GETDATE(), 1);

INSERT INTO Herbs (Id, Name, PinYinCode, Origin, Spec, Unit, Price, Stock, BatchNo, ExpireDate, Effect, Usage, Status, CreatedAt, Specification) 
VALUES (NEWID(), '香附', 'xf', '浙江', '优质', 'g', 15.00, 350, 'XF20240101', '2026-12-31', '疏肝解郁，理气宽中，调经止痛', '煎服，6-12g', 0, GETDATE(), 1);

-- 插入其他常用药材
INSERT INTO Herbs (Id, Name, PinYinCode, Origin, Spec, Unit, Price, Stock, BatchNo, ExpireDate, Effect, Usage, Status, CreatedAt, Specification) 
VALUES (NEWID(), '大枣', 'dz', '新疆', '优质', '个', 2.00, 1000, 'DZ20240101', '2026-12-31', '补中益气，养血安神', '煎服，3-10枚', 0, GETDATE(), 1);

INSERT INTO Herbs (Id, Name, PinYinCode, Origin, Spec, Unit, Price, Stock, BatchNo, ExpireDate, Effect, Usage, Status, CreatedAt, Specification) 
VALUES (NEWID(), '生姜', 'sj', '山东', '新鲜', 'g', 5.00, 800, 'SJ20240101', '2026-12-31', '解表散寒，温中止呕，化痰止咳', '煎服，3-10g', 0, GETDATE(), 1);

-- 添加更多常用药材
INSERT INTO Herbs (Id, Name, PinYinCode, Origin, Spec, Unit, Price, Stock, BatchNo, ExpireDate, Effect, Usage, Status, CreatedAt, Specification) 
VALUES (NEWID(), '薄荷', 'bh', '江苏', '优质', 'g', 15.00, 300, 'BH20240101', '2026-12-31', '疏散风热，清利头目', '煎服，3-6g', 0, GETDATE(), 1);

INSERT INTO Herbs (Id, Name, PinYinCode, Origin, Spec, Unit, Price, Stock, BatchNo, ExpireDate, Effect, Usage, Status, CreatedAt, Specification) 
VALUES (NEWID(), '郁李仁', 'ylr', '山西', '优质', 'g', 22.00, 200, 'YLR20240101', '2026-12-31', '润肠通便，下气利水', '煎服，6-10g', 0, GETDATE(), 1);

INSERT INTO Herbs (Id, Name, PinYinCode, Origin, Spec, Unit, Price, Stock, BatchNo, ExpireDate, Effect, Usage, Status, CreatedAt, Specification) 
VALUES (NEWID(), '杏仁', 'xr', '山东', '优质', 'g', 35.00, 250, 'XR20240101', '2026-12-31', '降气止咳平喘，润肠通便', '煎服，5-10g', 0, GETDATE(), 1);

INSERT INTO Herbs (Id, Name, PinYinCode, Origin, Spec, Unit, Price, Stock, BatchNo, ExpireDate, Effect, Usage, Status, CreatedAt, Specification) 
VALUES (NEWID(), '枳实', 'zs', '江西', '优质', 'g', 20.00, 280, 'ZS20240101', '2026-12-31', '破气消积，化痰散痞', '煎服，3-10g', 0, GETDATE(), 1);

GO
PRINT '药材数据插入完成';
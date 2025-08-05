-- 插入常用中药材数据 - Unicode版本
-- 数据库：LYBTDB
-- 表名：Herbs

-- 清空测试数据
DELETE FROM Herbs WHERE Name = N'测试药材';

-- 插入解表药
INSERT INTO Herbs (Id, Name, PinYinCode, Origin, Spec, Unit, Price, Stock, BatchNo, ExpireDate, Effect, Usage, Status, CreatedAt, Specification) 
VALUES (NEWID(), N'麻黄', N'mh', N'内蒙古', N'优质', N'g', 12.50, 500, N'MH20240101', '2026-12-31', N'发汗解表，宣肺平喘，利水消肿', N'煎服，3-10g', 0, GETDATE(), 1);

INSERT INTO Herbs (Id, Name, PinYinCode, Origin, Spec, Unit, Price, Stock, BatchNo, ExpireDate, Effect, Usage, Status, CreatedAt, Specification) 
VALUES (NEWID(), N'桂枝', N'gz', N'广西', N'优质', N'g', 15.00, 300, N'GZ20240101', '2026-12-31', N'发汗解肌，温经通脉，助阳化气', N'煎服，3-10g', 0, GETDATE(), 1);

INSERT INTO Herbs (Id, Name, PinYinCode, Origin, Spec, Unit, Price, Stock, BatchNo, ExpireDate, Effect, Usage, Status, CreatedAt, Specification) 
VALUES (NEWID(), N'防风', N'ff', N'内蒙古', N'优质', N'g', 18.00, 400, N'FF20240101', '2026-12-31', N'祛风解表，胜湿止痛，止痉', N'煎服，3-10g', 0, GETDATE(), 1);

-- 插入清热药
INSERT INTO Herbs (Id, Name, PinYinCode, Origin, Spec, Unit, Price, Stock, BatchNo, ExpireDate, Effect, Usage, Status, CreatedAt, Specification) 
VALUES (NEWID(), N'金银花', N'jyh', N'山东', N'优质', N'g', 38.00, 200, N'JYH20240101', '2026-12-31', N'清热解毒，疏散风热', N'煎服，6-15g', 0, GETDATE(), 1);

INSERT INTO Herbs (Id, Name, PinYinCode, Origin, Spec, Unit, Price, Stock, BatchNo, ExpireDate, Effect, Usage, Status, CreatedAt, Specification) 
VALUES (NEWID(), N'黄芩', N'hq', N'山西', N'优质', N'g', 28.00, 350, N'HQ20240101', '2026-12-31', N'清热燥湿，泻火解毒，止血，安胎', N'煎服，3-10g', 0, GETDATE(), 1);

INSERT INTO Herbs (Id, Name, PinYinCode, Origin, Spec, Unit, Price, Stock, BatchNo, ExpireDate, Effect, Usage, Status, CreatedAt, Specification) 
VALUES (NEWID(), N'黄连', N'hl', N'四川', N'优质', N'g', 85.00, 150, N'HL20240101', '2026-12-31', N'清热燥湿，泻火解毒', N'煎服，2-5g', 0, GETDATE(), 1);

-- 插入补益药
INSERT INTO Herbs (Id, Name, PinYinCode, Origin, Spec, Unit, Price, Stock, BatchNo, ExpireDate, Effect, Usage, Status, CreatedAt, Specification) 
VALUES (NEWID(), N'人参', N'rs', N'吉林', N'优质', N'g', 380.00, 50, N'RS20240101', '2026-12-31', N'大补元气，补脾益肺，生津止渴，安神益智', N'煎服，3-10g', 0, GETDATE(), 1);

INSERT INTO Herbs (Id, Name, PinYinCode, Origin, Spec, Unit, Price, Stock, BatchNo, ExpireDate, Effect, Usage, Status, CreatedAt, Specification) 
VALUES (NEWID(), N'党参', N'ds', N'山西', N'优质', N'g', 45.00, 300, N'DS20240101', '2026-12-31', N'补中益气，健脾益肺', N'煎服，9-30g', 0, GETDATE(), 1);

INSERT INTO Herbs (Id, Name, PinYinCode, Origin, Spec, Unit, Price, Stock, BatchNo, ExpireDate, Effect, Usage, Status, CreatedAt, Specification) 
VALUES (NEWID(), N'黄芪', N'hqi', N'内蒙古', N'优质', N'g', 55.00, 400, N'HQI20240101', '2026-12-31', N'补气升阳，固表止汗，利水消肿', N'煎服，9-30g', 0, GETDATE(), 1);

INSERT INTO Herbs (Id, Name, PinYinCode, Origin, Spec, Unit, Price, Stock, BatchNo, ExpireDate, Effect, Usage, Status, CreatedAt, Specification) 
VALUES (NEWID(), N'白术', N'bz', N'浙江', N'优质', N'g', 32.00, 350, N'BZ20240101', '2026-12-31', N'健脾益气，燥湿利水，止汗，安胎', N'煎服，6-12g', 0, GETDATE(), 1);

INSERT INTO Herbs (Id, Name, PinYinCode, Origin, Spec, Unit, Price, Stock, BatchNo, ExpireDate, Effect, Usage, Status, CreatedAt, Specification) 
VALUES (NEWID(), N'甘草', N'gc', N'内蒙古', N'优质', N'g', 18.00, 500, N'GC20240101', '2026-12-31', N'补脾益气，清热解毒，祛痰止咳', N'煎服，1.5-10g', 0, GETDATE(), 1);

-- 插入活血化瘀药
INSERT INTO Herbs (Id, Name, PinYinCode, Origin, Spec, Unit, Price, Stock, BatchNo, ExpireDate, Effect, Usage, Status, CreatedAt, Specification) 
VALUES (NEWID(), N'当归', N'dg', N'甘肃', N'优质', N'g', 65.00, 200, N'DG20240101', '2026-12-31', N'补血活血，调经止痛，润肠通便', N'煎服，6-12g', 0, GETDATE(), 1);

INSERT INTO Herbs (Id, Name, PinYinCode, Origin, Spec, Unit, Price, Stock, BatchNo, ExpireDate, Effect, Usage, Status, CreatedAt, Specification) 
VALUES (NEWID(), N'川芎', N'cx', N'四川', N'优质', N'g', 38.00, 300, N'CX20240101', '2026-12-31', N'活血行气，祛风止痛', N'煎服，3-10g', 0, GETDATE(), 1);

INSERT INTO Herbs (Id, Name, PinYinCode, Origin, Spec, Unit, Price, Stock, BatchNo, ExpireDate, Effect, Usage, Status, CreatedAt, Specification) 
VALUES (NEWID(), N'白芍', N'bs', N'安徽', N'优质', N'g', 25.00, 250, N'BS20240101', '2026-12-31', N'养血调经，敛阴止汗，柔肝止痛', N'煎服，6-15g', 0, GETDATE(), 1);

-- 插入化痰止咳药
INSERT INTO Herbs (Id, Name, PinYinCode, Origin, Spec, Unit, Price, Stock, BatchNo, ExpireDate, Effect, Usage, Status, CreatedAt, Specification) 
VALUES (NEWID(), N'半夏', N'bx', N'河南', N'优质', N'g', 58.00, 180, N'BX20240101', '2026-12-31', N'燥湿化痰，降逆止呕，消痞散结', N'煎服，3-10g', 0, GETDATE(), 1);

INSERT INTO Herbs (Id, Name, PinYinCode, Origin, Spec, Unit, Price, Stock, BatchNo, ExpireDate, Effect, Usage, Status, CreatedAt, Specification) 
VALUES (NEWID(), N'陈皮', N'cp', N'广东', N'优质', N'g', 12.00, 400, N'CP20240101', '2026-12-31', N'理气健脾，燥湿化痰', N'煎服，3-10g', 0, GETDATE(), 1);

INSERT INTO Herbs (Id, Name, PinYinCode, Origin, Spec, Unit, Price, Stock, BatchNo, ExpireDate, Effect, Usage, Status, CreatedAt, Specification) 
VALUES (NEWID(), N'茯苓', N'fl', N'云南', N'优质', N'g', 28.00, 350, N'FL20240101', '2026-12-31', N'利水渗湿，健脾宁心', N'煎服，9-15g', 0, GETDATE(), 1);

-- 插入理气药
INSERT INTO Herbs (Id, Name, PinYinCode, Origin, Spec, Unit, Price, Stock, BatchNo, ExpireDate, Effect, Usage, Status, CreatedAt, Specification) 
VALUES (NEWID(), N'柴胡', N'ch', N'山西', N'优质', N'g', 48.00, 250, N'CH20240101', '2026-12-31', N'疏散退热，疏肝解郁，升举阳气', N'煎服，3-10g', 0, GETDATE(), 1);

INSERT INTO Herbs (Id, Name, PinYinCode, Origin, Spec, Unit, Price, Stock, BatchNo, ExpireDate, Effect, Usage, Status, CreatedAt, Specification) 
VALUES (NEWID(), N'香附', N'xf', N'浙江', N'优质', N'g', 15.00, 350, N'XF20240101', '2026-12-31', N'疏肝解郁，理气宽中，调经止痛', N'煎服，6-12g', 0, GETDATE(), 1);

-- 插入其他常用药材
INSERT INTO Herbs (Id, Name, PinYinCode, Origin, Spec, Unit, Price, Stock, BatchNo, ExpireDate, Effect, Usage, Status, CreatedAt, Specification) 
VALUES (NEWID(), N'大枣', N'dz', N'新疆', N'优质', N'个', 2.00, 1000, N'DZ20240101', '2026-12-31', N'补中益气，养血安神', N'煎服，3-10枚', 0, GETDATE(), 1);

INSERT INTO Herbs (Id, Name, PinYinCode, Origin, Spec, Unit, Price, Stock, BatchNo, ExpireDate, Effect, Usage, Status, CreatedAt, Specification) 
VALUES (NEWID(), N'生姜', N'sj', N'山东', N'新鲜', N'g', 5.00, 800, N'SJ20240101', '2026-12-31', N'解表散寒，温中止呕，化痰止咳', N'煎服，3-10g', 0, GETDATE(), 1);

-- 添加更多常用药材
INSERT INTO Herbs (Id, Name, PinYinCode, Origin, Spec, Unit, Price, Stock, BatchNo, ExpireDate, Effect, Usage, Status, CreatedAt, Specification) 
VALUES (NEWID(), N'薄荷', N'bh', N'江苏', N'优质', N'g', 15.00, 300, N'BH20240101', '2026-12-31', N'疏散风热，清利头目', N'煎服，3-6g', 0, GETDATE(), 1);

INSERT INTO Herbs (Id, Name, PinYinCode, Origin, Spec, Unit, Price, Stock, BatchNo, ExpireDate, Effect, Usage, Status, CreatedAt, Specification) 
VALUES (NEWID(), N'郁李仁', N'ylr', N'山西', N'优质', N'g', 22.00, 200, N'YLR20240101', '2026-12-31', N'润肠通便，下气利水', N'煎服，6-10g', 0, GETDATE(), 1);

INSERT INTO Herbs (Id, Name, PinYinCode, Origin, Spec, Unit, Price, Stock, BatchNo, ExpireDate, Effect, Usage, Status, CreatedAt, Specification) 
VALUES (NEWID(), N'杏仁', N'xr', N'山东', N'优质', N'g', 35.00, 250, N'XR20240101', '2026-12-31', N'降气止咳平喘，润肠通便', N'煎服，5-10g', 0, GETDATE(), 1);

INSERT INTO Herbs (Id, Name, PinYinCode, Origin, Spec, Unit, Price, Stock, BatchNo, ExpireDate, Effect, Usage, Status, CreatedAt, Specification) 
VALUES (NEWID(), N'枳实', N'zs', N'江西', N'优质', N'g', 20.00, 280, N'ZS20240101', '2026-12-31', N'破气消积，化痰散痞', N'煎服，3-10g', 0, GETDATE(), 1);

-- 添加更多常用药材用于验方
INSERT INTO Herbs (Id, Name, PinYinCode, Origin, Spec, Unit, Price, Stock, BatchNo, ExpireDate, Effect, Usage, Status, CreatedAt, Specification) 
VALUES (NEWID(), N'竹茹', N'zr', N'浙江', N'优质', N'g', 18.00, 200, N'ZR20240101', '2026-12-31', N'清热化痰，除烦止呕', N'煎服，6-10g', 0, GETDATE(), 1);

INSERT INTO Herbs (Id, Name, PinYinCode, Origin, Spec, Unit, Price, Stock, BatchNo, ExpireDate, Effect, Usage, Status, CreatedAt, Specification) 
VALUES (NEWID(), N'酒芍', N'js', N'安徽', N'优质', N'g', 28.00, 180, N'JS20240101', '2026-12-31', N'养血调经，柔肝止痛', N'煎服，6-12g', 0, GETDATE(), 1);

INSERT INTO Herbs (Id, Name, PinYinCode, Origin, Spec, Unit, Price, Stock, BatchNo, ExpireDate, Effect, Usage, Status, CreatedAt, Specification) 
VALUES (NEWID(), N'桔梗', N'jg', N'安徽', N'优质', N'g', 22.00, 300, N'JG20240101', '2026-12-31', N'宣肺利咽，祛痰排脓', N'煎服，3-10g', 0, GETDATE(), 1);

INSERT INTO Herbs (Id, Name, PinYinCode, Origin, Spec, Unit, Price, Stock, BatchNo, ExpireDate, Effect, Usage, Status, CreatedAt, Specification) 
VALUES (NEWID(), N'炒谷芽', N'cgy', N'江苏', N'优质', N'g', 12.00, 350, N'CGY20240101', '2026-12-31', N'消食和中，健脾开胃', N'煎服，9-15g', 0, GETDATE(), 1);

INSERT INTO Herbs (Id, Name, PinYinCode, Origin, Spec, Unit, Price, Stock, BatchNo, ExpireDate, Effect, Usage, Status, CreatedAt, Specification) 
VALUES (NEWID(), N'藿香', N'hx', N'广东', N'优质', N'g', 16.00, 280, N'HX20240101', '2026-12-31', N'化湿和中，解暑发表', N'煎服，5-10g', 0, GETDATE(), 1);

GO
PRINT N'药材数据插入完成';
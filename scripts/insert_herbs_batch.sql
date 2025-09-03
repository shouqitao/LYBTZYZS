-- 批量插入常用中药材数据
-- 数据库：LYBTDB
-- 表名：Herbs

-- 清空现有数据
DELETE FROM Herbs;

-- 批量插入药材数据
INSERT INTO Herbs (Id, Name, PinYinCode, Origin, Unit, Price, Stock, Status, CreatedAt, Specification) VALUES
-- 解表药
(NEWID(), N'麻黄', N'mh', N'内蒙古', N'g', 12.50, 500, 0, GETDATE(), 1),
(NEWID(), N'桂枝', N'gz', N'广西', N'g', 15.00, 300, 0, GETDATE(), 1),
(NEWID(), N'防风', N'ff', N'内蒙古', N'g', 18.00, 400, 0, GETDATE(), 1),
-- 清热药
(NEWID(), N'金银花', N'jyh', N'山东', N'g', 38.00, 200, 0, GETDATE(), 1),
(NEWID(), N'黄芩', N'hq', N'山西', N'g', 28.00, 350, 0, GETDATE(), 1),
(NEWID(), N'黄连', N'hl', N'四川', N'g', 85.00, 150, 0, GETDATE(), 1),
-- 补益药
(NEWID(), N'人参', N'rs', N'吉林', N'g', 380.00, 50, 0, GETDATE(), 1),
(NEWID(), N'党参', N'ds', N'山西', N'g', 45.00, 300, 0, GETDATE(), 1),
(NEWID(), N'黄芪', N'hqi', N'内蒙古', N'g', 55.00, 400, 0, GETDATE(), 1),
(NEWID(), N'白术', N'bz', N'浙江', N'g', 32.00, 350, 0, GETDATE(), 1),
(NEWID(), N'甘草', N'gc', N'内蒙古', N'g', 18.00, 500, 0, GETDATE(), 1),
-- 活血化瘀药
(NEWID(), N'当归', N'dg', N'甘肃', N'g', 65.00, 200, 0, GETDATE(), 1),
(NEWID(), N'川芎', N'cx', N'四川', N'g', 38.00, 300, 0, GETDATE(), 1),
(NEWID(), N'白芍', N'bs', N'安徽', N'g', 25.00, 250, 0, GETDATE(), 1),
(NEWID(), N'赤芍', N'cs', N'内蒙古', N'g', 25.00, 250, 0, GETDATE(), 1),
-- 化痰止咳药
(NEWID(), N'半夏', N'bx', N'河南', N'g', 58.00, 180, 0, GETDATE(), 1),
(NEWID(), N'陈皮', N'cp', N'广东', N'g', 12.00, 400, 0, GETDATE(), 1),
(NEWID(), N'茯苓', N'fl', N'云南', N'g', 28.00, 350, 0, GETDATE(), 1),
-- 理气药
(NEWID(), N'柴胡', N'ch', N'山西', N'g', 48.00, 250, 0, GETDATE(), 1),
(NEWID(), N'香附', N'xf', N'浙江', N'g', 15.00, 350, 0, GETDATE(), 1),
(NEWID(), N'枳实', N'zs', N'江西', N'g', 20.00, 280, 0, GETDATE(), 1),
-- 其他常用药材
(NEWID(), N'大枣', N'dz', N'新疆', N'个', 2.00, 1000, 0, GETDATE(), 1),
(NEWID(), N'生姜', N'sj', N'山东', N'g', 5.00, 800, 0, GETDATE(), 1),
(NEWID(), N'薄荷', N'bh', N'江苏', N'g', 15.00, 300, 0, GETDATE(), 1),
(NEWID(), N'郁李仁', N'ylr', N'山西', N'g', 22.00, 200, 0, GETDATE(), 1),
(NEWID(), N'杏仁', N'xr', N'山东', N'g', 35.00, 250, 0, GETDATE(), 1),
(NEWID(), N'竹茹', N'zr', N'浙江', N'g', 18.00, 200, 0, GETDATE(), 1),
(NEWID(), N'酒芍', N'js', N'安徽', N'g', 28.00, 180, 0, GETDATE(), 1),
(NEWID(), N'桔梗', N'jg', N'安徽', N'g', 22.00, 300, 0, GETDATE(), 1),
(NEWID(), N'炒谷芽', N'cgy', N'江苏', N'g', 12.00, 350, 0, GETDATE(), 1),
(NEWID(), N'藿香', N'hx', N'广东', N'g', 16.00, 280, 0, GETDATE(), 1),
(NEWID(), N'白芷', N'bzhi', N'河北', N'g', 20.00, 300, 0, GETDATE(), 1),
(NEWID(), N'苏叶', N'sy', N'江苏', N'g', 12.00, 400, 0, GETDATE(), 1),
(NEWID(), N'厚朴', N'hp', N'四川', N'g', 25.00, 250, 0, GETDATE(), 1),
(NEWID(), N'吴茱萸', N'wzy', N'江西', N'g', 45.00, 150, 0, GETDATE(), 1),
(NEWID(), N'炒麦芽', N'cmy', N'江苏', N'g', 10.00, 400, 0, GETDATE(), 1),
(NEWID(), N'旋覆花', N'xfh', N'河南', N'g', 18.00, 200, 0, GETDATE(), 1),
(NEWID(), N'黄柏', N'hb', N'四川', N'g', 22.00, 300, 0, GETDATE(), 1),
(NEWID(), N'腹皮', N'fp', N'广东', N'g', 15.00, 250, 0, GETDATE(), 1),
(NEWID(), N'台乌', N'tw', N'浙江', N'g', 35.00, 180, 0, GETDATE(), 1),
(NEWID(), N'川椒', N'cj', N'四川', N'g', 28.00, 220, 0, GETDATE(), 1);

PRINT N'成功插入 ' + CAST(@@ROWCOUNT AS VARCHAR(10)) + N' 条药材记录';
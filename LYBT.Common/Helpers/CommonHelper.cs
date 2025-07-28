using Microsoft.International.Converters.PinYinConverter;
using System.Collections.Concurrent;
using System.Net.NetworkInformation;
using System.Text;
using System.Text.RegularExpressions;

namespace LYBT.Common.Helpers {

    /// <summary>
    /// 通用工具类 - 性能优化版本
    /// </summary>
    public static partial class CommonHelper {

        // 预编译正则表达式以提升性能
        [GeneratedRegex(@"\n|\r|\s|\D", RegexOptions.Compiled)]
        private static partial Regex PhoneDigitsRegex();

        [GeneratedRegex(@"^\d{17}[\dXx]$", RegexOptions.Compiled)]
        private static partial Regex IdNumberRegex();

        // 身份证校验权重和校验码（避免重复计算）
        private static readonly int[] IdWeights = { 7, 9, 10, 5, 8, 4, 2, 1, 6, 3, 7, 9, 10, 5, 8, 4, 2 };
        private static readonly char[] IdCodes = "10X98765432".ToCharArray();

        // 拼音码缓存
        private static readonly ConcurrentDictionary<string, string> _pinyinCache = new();
        
        // 五笔码缓存
        private static readonly ConcurrentDictionary<string, string> _wubiCache = new();

        /// <summary>
        /// 检查网络是否可用
        /// </summary>
        public static bool IsNetworkAvailable() => NetworkInterface.GetIsNetworkAvailable();

        /// <summary>
        /// 格式化电话号码（性能优化版本）
        /// </summary>
        public static string FormatPhone(string? phone) {
            if (string.IsNullOrWhiteSpace(phone))
                return string.Empty;

            var digits = PhoneDigitsRegex().Replace(phone, string.Empty);

            return digits.Length switch {
                11 => $"{digits[..3]}-{digits[3..7]}-{digits[7..]}",
                10 => $"{digits[..3]}-{digits[3..6]}-{digits[6..]}",
                _ => digits
            };
        }

        /// <summary>
        /// 验证身份证号码（性能优化版本）
        /// </summary>
        public static bool CheckIdNumber(string? idNumber) {
            if (string.IsNullOrWhiteSpace(idNumber))
                return false;

            idNumber = idNumber.Trim();

            if (!IdNumberRegex().IsMatch(idNumber))
                return false;

            // 计算校验码
            int sum = 0;
            for (int i = 0; i < 17; i++) {
                sum += (idNumber[i] - '0') * IdWeights[i];
            }

            char expectedCode = IdCodes[sum % 11];
            return char.ToUpperInvariant(idNumber[17]) == expectedCode;
        }

        /// <summary>
        /// 根据中文名称生成拼音码（带缓存）
        /// </summary>
        public static string GetPinyinCode(string? text) {
            if (string.IsNullOrWhiteSpace(text))
                return string.Empty;

            string trimmedText = text.Trim();

            return _pinyinCache.GetOrAdd(trimmedText, static input => {
                var sb = new StringBuilder();

                foreach (var ch in input) {
                    if (ChineseChar.IsValidChar(ch)) {
                        var cc = new ChineseChar(ch);
                        var py = cc.Pinyins.FirstOrDefault(p => !string.IsNullOrWhiteSpace(p));
                        if (!string.IsNullOrEmpty(py))
                            sb.Append(char.ToUpperInvariant(py[0]));
                    } else if (char.IsLetter(ch)) {
                        sb.Append(char.ToUpperInvariant(ch));
                    }
                }

                return sb.ToString();
            });
        }

        /// <summary>
        /// 根据中文名称生成五笔码（带缓存）
        /// </summary>
        public static string GetWuBiCode(string? text) {
            if (string.IsNullOrWhiteSpace(text))
                return string.Empty;

            string trimmedText = text.Trim();

            return _wubiCache.GetOrAdd(trimmedText, static input => {
                var sb = new StringBuilder();

                // 五笔字典映射 - 这里提供一些常用字的五笔码
                var wubiDict = GetWuBiDictionary();

                foreach (var ch in input) {
                    if (wubiDict.TryGetValue(ch, out var wubiCode)) {
                        sb.Append(wubiCode);
                    } else if (char.IsLetter(ch)) {
                        sb.Append(char.ToUpperInvariant(ch));
                    }
                }

                return sb.ToString();
            });
        }

        /// <summary>
        /// 获取五笔字典（常用字映射）
        /// </summary>
        private static Dictionary<char, string> GetWuBiDictionary() {
            // 这里提供一些常用字的五笔码映射
            // 实际应用中可以从文件或数据库加载完整字典
            return new Dictionary<char, string> {
                // 常用姓氏
                {'王', "GGGG"}, {'李', "SB"}, {'张', "XTAJ"}, {'刘', "YJH"}, {'陈', "BAIY"},
                {'杨', "SNRT"}, {'赵', "FHQ"}, {'黄', "AMWU"}, {'周', "MKD"}, {'吴', "KGD"},
                {'徐', "TBTH"}, {'孙', "BBB"}, {'胡', "DEG"}, {'朱', "RII"}, {'高', "YMKF"},
                {'林', "SSY"}, {'何', "WKG"}, {'郭', "VKGK"}, {'马', "CNNG"}, {'罗', "LQG"},
                {'梁', "SSSG"}, {'宋', "PSI"}, {'郑', "UDLG"}, {'谢', "YCU"}, {'韩', "FJH"},
                {'唐', "YBG"}, {'冯', "QB"}, {'于', "GFK"}, {'董', "ATGF"}, {'萧', "AVKB"},
                {'程', "TKGG"}, {'曹', "GMAJ"}, {'袁', "FKEU"}, {'邓', "CBH"}, {'许', "TLFU"},
                {'傅', "WGEY"}, {'沈', "IPMH"}, {'曾', "ULJ"}, {'彭', "FGEE"}, {'吕', "KBH"},
                {'苏', "ALGH"}, {'卢', "HVIL"}, {'蒋', "ATGM"}, {'蔡', "AWFI"}, {'贾', "MU"},
                
                // 常用中药材名称
                {'黄', "AMWU"}, {'芪', "AQAB"}, {'当', "IVF"}, {'归', "VVG"}, {'川', "KT"},
                {'芎', "AIG"}, {'白', "RRR"}, {'芍', "AHD"}, {'茯', "AUK"}, {'苓', "AWP"},
                {'甘', "AFL"}, {'草', "AJJJ"}, {'党', "IPN"}, {'参', "CJH"}, {'麦', "GTU"},
                {'冬', "TU"}, {'地', "FBN"}, {'黄', "AMWU"}, {'连', "LPK"}, {'板', "SRC"},
                {'蓝', "AQLH"}, {'根', "SVEY"}, {'桔', "SJSG"}, {'梗', "SADK"}, {'枳', "SWI"},
                {'实', "PMHH"}, {'厚', "DJBD"}, {'朴', "SSY"}, {'陈', "BAIY"}, {'皮', "HCL"},
                {'半', "UFK"}, {'夏', "DH"}, {'生', "TG"}, {'姜', "UDU"}, {'大', "DDD"},
                {'枣', "JSU"}, {'桂', "SSG"}, {'枝', "SFCY"}, {'茵', "APD"}, {'陈', "BAIY"},
                {'蒿', "AYMU"}, {'柴', "SYG"}, {'胡', "DEG"}, {'金', "QQQ"}, {'银', "QRG"},
                {'花', "AWX"}, {'连', "LPK"}, {'翘', "ATDH"}, {'板', "SRC"}, {'蓝', "AQLH"},
                {'根', "SVEY"}, {'大', "DDD"}, {'青', "GEF"}, {'叶', "KFJ"}, {'紫', "HXI"},
                {'花', "AWX"}, {'地', "FBN"}, {'丁', "SGH"}, {'桑', "CCS"}, {'叶', "KFJ"},
                {'菊', "AJFJ"}, {'花', "AWX"}, {'薄', "AIY"}, {'荷', "AKG"}, {'决', "NWY"},
                {'明', "JEG"}, {'子', "BBB"}, {'车', "LG"}, {'前', "UJJ"}, {'草', "AJJJ"},
                {'栀', "SVIY"}, {'子', "BBB"}, {'龙', "DXV"}, {'胆', "EFN"}, {'草', "AJJJ"},
                {'夏', "DH"}, {'枯', "SFGF"}, {'草', "AJJJ"}, {'天', "GD"}, {'麻', "YSS"},
                {'钩', "QGU"}, {'藤', "ASJN"}, {'石', "DG"}, {'决', "NWY"}, {'明', "JEG"},
                {'珍', "GWET"}, {'珠', "GFIU"}, {'母', "XGU"}, {'牛', "RH"}, {'膝', "EAV"},
                {'杜', "SG"}, {'仲', "WH"}, {'续', "XF"}, {'断', "ONRH"}, {'桑', "CCS"},
                {'寄', "PDB"}, {'生', "TG"}, {'独', "QTJH"}, {'活', "IVV"}, {'防', "BL"},
                {'风', "MQI"}, {'羌', "UDJ"}, {'活', "IVV"}, {'细', "XTU"}, {'辛', "UYT"},
                {'藁', "AYMU"}, {'本', "SG"}, {'白', "RRR"}, {'芷', "AYY"}, {'苍', "AWU"},
                {'术', "WY"}, {'厚', "DJBD"}, {'朴', "SSY"}, {'枳', "SWI"}, {'壳', "KHKH"},
                {'木', "S"}, {'香', "TJ"}, {'砂', "ILN"}, {'仁', "WF"}, {'豆', "GK"},
                {'蔻', "ADJN"}, {'陈', "BAIY"}, {'皮', "HCL"}, {'青', "GEF"}, {'皮', "HCL"},
                {'竹', "TT"}, {'茹', "ATF"}, {'枇', "SSU"}, {'杷', "RCN"}, {'叶', "KFJ"},
                {'桑', "CCS"}, {'白', "RRR"}, {'皮', "HCL"}, {'葶', "AYUD"}, {'苈', "AYU"},
                {'子', "BBB"}, {'苏', "ALGH"}, {'子', "BBB"}, {'莱', "AOU"}, {'菔', "AFY"},
                {'子', "BBB"}, {'白', "RRR"}, {'芥', "AJH"}, {'子', "BBB"}, {'紫', "HXI"},
                {'苏', "ALGH"}, {'子', "BBB"}, {'杏', "SYK"}, {'仁', "WF"}, {'桃', "SIG"},
                {'仁', "WF"}, {'火', "OO"}, {'麻', "YSS"}, {'仁', "WF"}, {'郁', "DEBH"},
                {'李', "SB"}, {'仁', "WF"}, {'冬', "TU"}, {'瓜', "RCYY"}, {'仁', "WF"},
                {'薏', "AYIT"}, {'苡', "AYB"}, {'仁', "WF"}, {'白', "RRR"}, {'扁', "YNAK"},
                {'豆', "GK"}, {'赤', "FOU"}, {'小', "I"}, {'豆', "GK"}, {'绿', "XIY"},
                {'豆', "GK"}, {'黑', "LF"}, {'豆', "GK"}, {'淡', "IFN"}, {'豆', "GK"},
                {'豉', "GKUI"}, {'甘', "AFL"}, {'草', "AJJJ"}, {'节', "ABKN"}, {'浮', "IFG"},
                {'小', "I"}, {'麦', "GTU"}, {'淡', "IFN"}, {'竹', "TT"}, {'叶', "KFJ"},
                {'灯', "OSM"}, {'心', "NYN"}, {'草', "AJJJ"}, {'远', "FQP"}, {'志', "FNU"},
                {'石', "DG"}, {'菖', "AYKE"}, {'蒲', "AYGJ"}, {'茯', "AUK"}, {'神', "SJ"},
                {'龙', "DXV"}, {'骨', "MEF"}, {'牡', "TRD"}, {'蛎', "TYL"}, {'磁', "NXCI"},
                {'石', "DG"}, {'代', "WA"}, {'赭', "FOU"}, {'石', "DG"}, {'朱', "RII"},
                {'砂', "ILN"}, {'琥', "GHAG"}, {'珀', "GRUG"}, {'酸', "SGU"}, {'枣', "JSU"},
                {'仁', "WF"}, {'柏', "SG"}, {'子', "BBB"}, {'仁', "WF"}, {'夜', "YWU"},
                {'交', "UQU"}, {'藤', "ASJN"}, {'合', "WGKH"}, {'欢', "RSKC"}, {'皮', "HCL"},
                {'首', "UB"}, {'乌', "QNG"}, {'藤', "ASJN"}, {'鸡', "CY"}, {'血', "TLK"},
                {'藤', "ASJN"}, {'钩', "QGU"}, {'藤', "ASJN"}, {'忍', "QJN"}, {'冬', "TU"},
                {'藤', "ASJN"}, {'桑', "CCS"}, {'枝', "SFCY"}, {'络', "XTK"}, {'石', "DG"},
                {'英', "AMW"}, {'菊', "AJFJ"}, {'花', "AWX"}, {'野', "JLD"}, {'菊', "AJFJ"},
                {'花', "AWX"}, {'蒲', "AYGJ"}, {'公', "WCU"}, {'英', "AMW"}, {'紫', "HXI"},
                {'花', "AWX"}, {'地', "FBN"}, {'丁', "SGH"}, {'草', "AJJJ"}, {'半', "UFK"},
                {'边', "LGMH"}, {'莲', "ALPU"}, {'鱼', "QGF"}, {'腥', "EUG"}, {'草', "AJJJ"},
                {'败', "JGU"}, {'酱', "UASJ"}, {'草', "AJJJ"}, {'白', "RRR"}, {'花', "AWX"},
                {'蛇', "JPXN"}, {'舌', "TDD"}, {'草', "AJJJ"}, {'马', "CNNG"}, {'齿', "HWBJ"},
                {'苋', "AYNH"}, {'鸭', "LQYG"}, {'跖', "KHBH"}, {'草', "AJJJ"}, {'瞿', "HHWY"},
                {'麦', "GTU"}, {'委', "TVFG"}, {'陵', "BFWL"}, {'菜', "AJU"}, {'地', "FBN"},
                {'肤', "EFB"}, {'子', "BBB"}, {'茜', "AJN"}, {'草', "AJJJ"}, {'根', "SVEY"},
                {'紫', "HXI"}, {'草', "AJJJ"}, {'红', "XAG"}, {'花', "AWX"}, {'倒', "WGCJ"},
                {'扣', "RKG"}, {'草', "AJJJ"}, {'垂', "TGAF"}, {'盆', "UUMW"}, {'草', "AJJJ"},
                {'石', "DG"}, {'韦', "FNH"}, {'伸', "WJH"}, {'筋', "TLH"}, {'草', "AJJJ"},
                {'过', "FPl"}, {'路', "KHTK"}, {'黄', "AMWU"}, {'透', "TEPT"}, {'骨', "MEF"},
                {'草', "AJJJ"}, {'寻', "VFB"}, {'骨', "MEF"}, {'风', "MQI"}, {'海', "ITY"},
                {'风', "MQI"}, {'藤', "ASJN"}, {'络', "XTK"}, {'石', "DG"}, {'松', "SYG"},
                {'五', "GG"}, {'加', "LKG"}, {'皮', "HCL"}, {'桑', "CCS"}, {'寄', "PDB"},
                {'生', "TG"}, {'槲', "SWGQ"}, {'寄', "PDB"}, {'生', "TG"}, {'桑', "CCS"},
                {'枝', "SFCY"}, {'海', "ITY"}, {'桐', "SGMJ"}, {'皮', "HCL"}, {'丝', "XFF"},
                {'瓜', "RCYY"}, {'络', "XTK"}, {'路', "KHTK"}, {'路', "KHTK"}, {'通', "CEPK"}
            };
        }

        /// <summary>
        /// 验证邮箱格式
        /// </summary>
        public static bool IsValidEmail(string? email) {
            if (string.IsNullOrWhiteSpace(email))
                return false;

            try {
                var mailAddress = new System.Net.Mail.MailAddress(email);
                return mailAddress.Address == email;
            } catch {
                return false;
            }
        }

        /// <summary>
        /// 生成随机字符串
        /// </summary>
        public static string GenerateRandomString(int length, bool includeNumbers = true) {
            const string letters = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
            const string numbers = "0123456789";

            string chars = letters + letters.ToLower();
            if (includeNumbers)
                chars += numbers;

            var random = new Random();
            return new string(Enumerable.Repeat(chars, length)
                .Select(s => s[random.Next(s.Length)]).ToArray());
        }

        /// <summary>
        /// 安全地转换为整数
        /// </summary>
        public static int SafeToInt(string? value, int defaultValue = 0) {
            return int.TryParse(value, out var result) ? result : defaultValue;
        }

        /// <summary>
        /// 安全地转换为小数
        /// </summary>
        public static decimal SafeToDecimal(string? value, decimal defaultValue = 0) {
            return decimal.TryParse(value, out var result) ? result : defaultValue;
        }

        /// <summary>
        /// 安全地转换为布尔值
        /// </summary>
        public static bool SafeToBool(string? value, bool defaultValue = false) {
            return bool.TryParse(value, out var result) ? result : defaultValue;
        }

        /// <summary>
        /// 脱敏手机号
        /// </summary>
        public static string MaskPhoneNumber(string? phoneNumber) {
            if (string.IsNullOrWhiteSpace(phoneNumber) || phoneNumber.Length < 7)
                return phoneNumber ?? string.Empty;

            return phoneNumber.Length == 11
                ? $"{phoneNumber[..3]}****{phoneNumber[7..]}"
                : $"{phoneNumber[..3]}****{phoneNumber[^3..]}";
        }

        /// <summary>
        /// 脱敏身份证号
        /// </summary>
        public static string MaskIdNumber(string? idNumber) {
            if (string.IsNullOrWhiteSpace(idNumber) || idNumber.Length < 8)
                return idNumber ?? string.Empty;

            return idNumber.Length == 18
                ? $"{idNumber[..6]}********{idNumber[14..]}"
                : $"{idNumber[..3]}****{idNumber[^2..]}";
        }

        /// <summary>
        /// 清理拼音缓存
        /// </summary>
        public static void ClearPinyinCache() {
            _pinyinCache.Clear();
        }

        /// <summary>
        /// 清理五笔码缓存
        /// </summary>
        public static void ClearWuBiCache() {
            _wubiCache.Clear();
        }

        /// <summary>
        /// 清理所有缓存
        /// </summary>
        public static void ClearAllCaches() {
            _pinyinCache.Clear();
            _wubiCache.Clear();
        }

        /// <summary>
        /// 获取拼音缓存统计信息
        /// </summary>
        public static (int Count, long MemoryEstimate) GetPinyinCacheStats() {
            int count = _pinyinCache.Count;
            long memoryEstimate = _pinyinCache.Sum(kvp =>
                (kvp.Key.Length + kvp.Value.Length) * sizeof(char));

            return (count, memoryEstimate);
        }

        /// <summary>
        /// 获取五笔码缓存统计信息
        /// </summary>
        public static (int Count, long MemoryEstimate) GetWuBiCacheStats() {
            int count = _wubiCache.Count;
            long memoryEstimate = _wubiCache.Sum(kvp =>
                (kvp.Key.Length + kvp.Value.Length) * sizeof(char));

            return (count, memoryEstimate);
        }

        /// <summary>
        /// 生成唯一标识符
        /// </summary>
        public static string GenerateUniqueId() {
            return Guid.NewGuid().ToString("N");
        }

        /// <summary>
        /// 生成短ID（8位）
        /// </summary>
        public static string GenerateShortId() {
            return Guid.NewGuid().ToString("N")[..8];
        }

        /// <summary>
        /// 获取文件扩展名（包含点号）
        /// </summary>
        public static string GetFileExtension(string fileName) {
            if (string.IsNullOrWhiteSpace(fileName))
                return string.Empty;

            return Path.GetExtension(fileName).ToLower();
        }

        /// <summary>
        /// 获取文件大小的友好显示
        /// </summary>
        public static string GetFileSizeString(long fileSize) {
            string[] sizes = { "B", "KB", "MB", "GB", "TB" };
            double len = fileSize;
            int order = 0;
            while (len >= 1024 && order < sizes.Length - 1) {
                order++;
                len = len / 1024;
            }
            return $"{len:0.##} {sizes[order]}";
        }

        /// <summary>
        /// 检查文件类型是否为图片
        /// </summary>
        public static bool IsImageFile(string fileName) {
            if (string.IsNullOrWhiteSpace(fileName))
                return false;

            var extension = GetFileExtension(fileName);
            string[] imageExtensions = { ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".webp" };
            return imageExtensions.Contains(extension);
        }

        /// <summary>
        /// 检查文件类型是否为文档
        /// </summary>
        public static bool IsDocumentFile(string fileName) {
            if (string.IsNullOrWhiteSpace(fileName))
                return false;

            var extension = GetFileExtension(fileName);
            string[] docExtensions = { ".pdf", ".doc", ".docx", ".xls", ".xlsx", ".txt", ".csv", ".rtf" };
            return docExtensions.Contains(extension);
        }

        /// <summary>
        /// 清理文件名中的非法字符
        /// </summary>
        public static string SanitizeFileName(string fileName) {
            if (string.IsNullOrWhiteSpace(fileName))
                return string.Empty;

            var invalidChars = Path.GetInvalidFileNameChars();
            return string.Join("_", fileName.Split(invalidChars, StringSplitOptions.RemoveEmptyEntries));
        }

        /// <summary>
        /// 生成时间戳
        /// </summary>
        public static long GetTimestamp() {
            return DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        }

        /// <summary>
        /// 生成毫秒时间戳
        /// </summary>
        public static long GetTimestampMilliseconds() {
            return DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        }

        /// <summary>
        /// 从时间戳转换为DateTime
        /// </summary>
        public static DateTime FromTimestamp(long timestamp) {
            return DateTimeOffset.FromUnixTimeSeconds(timestamp).DateTime;
        }

        /// <summary>
        /// 从毫秒时间戳转换为DateTime
        /// </summary>
        public static DateTime FromTimestampMilliseconds(long timestamp) {
            return DateTimeOffset.FromUnixTimeMilliseconds(timestamp).DateTime;
        }
    }
}
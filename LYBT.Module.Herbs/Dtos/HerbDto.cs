namespace LYBT.Module.Herbs.Dtos {

    /// <summary>
    /// 药材列表 DTO
    /// </summary>
    public class HerbDto {

        /// <summary>药材ID</summary>
        public Guid Id { get; set; }

        /// <summary>药材名称</summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>拼音码</summary>
        public string? Pinyin { get; set; }

        /// <summary>单位</summary>
        public string? Unit { get; set; }

        /// <summary>单价</summary>
        public decimal Price { get; set; }

        public int TotalPrice { get; set; }

        public int Stock { get; set; }
        public string? BatchNo { get; set; }
        public DateTime? ExpireDate { get; set; }
    }
}
namespace LYBT.Module.Herbs.Dtos {

    /// <summary>
    /// 药材详情 DTO
    /// </summary>
    public class HerbDetailDto {

        /// <summary>药材ID</summary>
        public Guid Id { get; set; }

        /// <summary>药材名称</summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>拼音码</summary>
        public string? Pinyin { get; set; }

        /// <summary>产地</summary>
        public string? Origin { get; set; }

        /// <summary>规格</summary>
        public string? Spec { get; set; }

        /// <summary>单位</summary>
        public string? Unit { get; set; }

        /// <summary>单价</summary>
        public decimal Price { get; set; }

        public int Stock { get; set; }
        public string? BatchNo { get; set; }
        public DateTime? ExpireDate { get; set; }

        /// <summary>功效说明</summary>
        public string? Effect { get; set; }

        /// <summary>备注</summary>
        public string? Remark { get; set; }
    }
}
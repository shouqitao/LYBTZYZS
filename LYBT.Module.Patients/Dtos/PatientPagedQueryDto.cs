namespace LYBT.Module.Patients.Dtos {

    /// <summary>
    /// 病人分页与条件查询 Dto
    /// </summary>
    public class PatientPagedQueryDto {

        /// <summary>关键词（姓名/手机号/拼音码等）</summary>
        public string Keyword { get; set; } = string.Empty;

        /// <summary>页码（从1开始）</summary>
        public int Page { get; set; } = 1;

        /// <summary>每页数量</summary>
        public int PageSize { get; set; } = 20;
    }
}
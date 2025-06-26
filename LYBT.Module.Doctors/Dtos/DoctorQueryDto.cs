namespace LYBT.Module.Doctors.Dtos {

    /// <summary>
    /// 医生分页与条件查询 DTO
    /// </summary>
    public class DoctorQueryDto {

        /// <summary>关键词（姓名/拼音码/手机号）</summary>
        public string? Keyword { get; set; }

        /// <summary>在职状态筛选</summary>
        public bool? IsActive { get; set; }

        /// <summary>页码，从1开始</summary>
        public int Page { get; set; } = 1;

        /// <summary>每页数量</summary>
        public int PageSize { get; set; } = 20;
    }
}
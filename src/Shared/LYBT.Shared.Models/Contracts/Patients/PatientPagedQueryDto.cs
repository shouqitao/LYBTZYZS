using LYBT.Shared.Models.Common;
using LYBT.Shared.Models.Enums;
using System.ComponentModel;

namespace LYBT.Shared.Models.Contracts.Patients
{

    /// <summary>
    /// 患者分页查询DTO - 前后端共享API契约
    /// 用于患者档案的分页查询和筛选
    /// </summary>
    public class PatientPagedQueryDto : PaginationRequest
    {

        /// <summary>姓名关键词</summary>
        [DisplayName("姓名关键词")]
        public string? Name { get; set; }

        /// <summary>手机号关键词</summary>
        [DisplayName("手机号")]
        public string? PhoneNumber { get; set; }

        /// <summary>证件号关键词</summary>
        [DisplayName("证件号")]
        public string? IDNumber { get; set; }

        /// <summary>性别筛选</summary>
        [DisplayName("性别")]
        public Gender? Gender { get; set; }

        /// <summary>年龄范围-最小值</summary>
        [DisplayName("最小年龄")]
        public int? MinAge { get; set; }

        /// <summary>年龄范围-最大值</summary>
        [DisplayName("最大年龄")]
        public int? MaxAge { get; set; }

        /// <summary>状态筛选</summary>
        [DisplayName("状态")]
        public CommonStatus? Status { get; set; }

        /// <summary>创建日期范围-结束日期</summary>
        [DisplayName("创建结束日期")]
        public DateTime? CreateEndDate { get; set; }

        /// <summary>地址关键词</summary>
        [DisplayName("地址")]
        public string? Address { get; set; }

        /// <summary>职业关键词</summary>
        [DisplayName("职业")]
        public string? Profession { get; set; }

        /// <summary>是否包含已删除的患者</summary>
        [DisplayName("包含已删除")]
        public bool IncludeInactive { get; set; } = false;

        /// <summary>按拼音码搜索</summary>
        [DisplayName("拼音码")]
        public string? PinYinCode { get; set; }

        /// <summary>按五笔码搜索</summary>
        [DisplayName("五笔码")]
        public string? WuBiCode { get; set; }
    }
}
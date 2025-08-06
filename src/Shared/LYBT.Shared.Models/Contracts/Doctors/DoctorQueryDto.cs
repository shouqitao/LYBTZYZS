using LYBT.Shared.Models.Enums;
using System.ComponentModel;

namespace LYBT.Shared.Models.Contracts.Doctors {

    /// <summary>
    /// 医生分页与条件查询 DTO
    /// </summary>
    public class DoctorQueryDto {

        /// <summary>搜索关键词（姓名/拼音码/执业证书号/专长）</summary>
        [DisplayName("搜索关键词")]
        public string? SearchKeyword { get; set; }

        /// <summary>状态筛选</summary>
        [DisplayName("状态筛选")]
        public DoctorStatus? Status { get; set; }

        /// <summary>当前页码，从1开始</summary>
        [DisplayName("当前页码")]
        public int CurrentPage { get; set; } = 1;

        /// <summary>每页数量</summary>
        [DisplayName("每页数量")]
        public int PageSize { get; set; } = 20;

        /// <summary>排序字段</summary>
        [DisplayName("排序字段")]
        public string? OrderBy { get; set; }

        /// <summary>是否升序</summary>
        [DisplayName("是否升序")]
        public bool IsAscending { get; set; } = true;
    }
}
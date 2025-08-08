using LYBT.Shared.Models.Enums;
using System.ComponentModel;

namespace LYBT.Shared.Models.Core
{

    /// <summary>
    /// 验方基础模型 - 前后端共享核心字段
    /// 包含所有通用的验方信息字段，各层可基于此模型扩展
    /// </summary>
    public class BaseFormulaModel
    {

        /// <summary>验方唯一标识</summary>
        [DisplayName("验方ID")]
        public Guid Id { get; set; }

        /// <summary>验方名称</summary>
        [DisplayName("验方名称")]
        public string Name { get; set; } = string.Empty;

        /// <summary>功效</summary>
        [DisplayName("功效")]
        public string? Effect { get; set; }

        /// <summary>用法</summary>
        [DisplayName("用法")]
        public string? Usage { get; set; }

        /// <summary>备注</summary>
        [DisplayName("备注")]
        public string? Remark { get; set; }

        /// <summary>性味归经</summary>
        [DisplayName("性味归经")]
        public string? Property { get; set; }

        /// <summary>验方状态</summary>
        [DisplayName("状态")]
        public CommonStatus Status { get; set; } = CommonStatus.Enabled;

        /// <summary>是否共享</summary>
        [DisplayName("是否共享")]
        public bool IsShared { get; set; } = false;

        /// <summary>创建者ID</summary>
        [DisplayName("创建者ID")]
        public Guid? CreatedById { get; set; }

        /// <summary>创建时间（统一命名）</summary>
        [DisplayName("创建时间")]
        public DateTime CreateTime { get; set; } = DateTime.Now;

        /// <summary>更新时间</summary>
        [DisplayName("更新时间")]
        public DateTime? UpdateTime { get; set; }

    }
}
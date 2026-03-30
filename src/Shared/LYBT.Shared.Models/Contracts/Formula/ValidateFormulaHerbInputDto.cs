using System;

namespace LYBT.Shared.Models.Contracts.Formula
{
    /// <summary>
    /// 验证验方药材请求DTO
    /// 用于将验方中的自定义药材绑定到系统药材库
    /// </summary>
    public class ValidateFormulaHerbInputDto
    {
        /// <summary>
        /// 系统药材ID（用于替换自定义药材）
        /// </summary>
        public Guid SelectedHerbId { get; set; }
    }
}

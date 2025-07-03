using System;
using System.Collections.Generic;
using LYBT.Module.Logs.Dtos;

namespace LYBT.UI.WPF.Services.Api {
    /// <summary>
    /// 类 AddLogResponse 的说明
    /// </summary>
    public class AddLogResponse {
        /// <summary>
        /// 属性 Success 的说明
        /// </summary>
        public bool Success { get; set; }
        /// <summary>
        /// 属性 Id 的说明
        /// </summary>
        public Guid Id { get; set; }
    }

    /// <summary>
    /// 类 GetLogsResponse 的说明
    /// </summary>
    public class GetLogsResponse {
        /// <summary>
        /// 属性 Total 的说明
        /// </summary>
        public int Total { get; set; }
        /// <summary>
        /// 属性 Logs 的说明
        /// </summary>
        public List<LogDto> Logs { get; set; } = new();
    }
}

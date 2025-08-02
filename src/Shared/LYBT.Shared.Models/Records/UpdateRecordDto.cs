using System;

namespace LYBT.Shared.Models.Records
{
    /// <summary>
    /// 更新病例DTO
    /// </summary>
    public class UpdateRecordDto : CreateRecordDto
    {
        /// <summary>ID</summary>
        public Guid Id { get; set; }

        /// <summary>状态（0:草稿 1:已完成 2:已归档）</summary>
        public int Status { get; set; }
    }
}
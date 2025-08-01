using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace LYBT.Models.Users {

    /// <summary>
    /// 批量操作用户时提交的ID列表 DTO
    /// </summary>
    public class UserBatchIdsDto {

        [Required(ErrorMessage = "用户ID列表不能为空")]
        [MinLength(1, ErrorMessage = "至少需要选择一个用户")]
        [DisplayName("用户ID列表")]
        [JsonPropertyName("userIds")]
        public List<Guid> UserIds { get; set; } = new();

        // 为了向后兼容，保留旧的属性名
        [JsonPropertyName("ids")]
        public List<Guid> Ids 
        { 
            get => UserIds; 
            set => UserIds = value; 
        }
    }
}
using System.ComponentModel;
using System.Text.Json.Serialization;

namespace LYBT.Shared.Models.Enums
{
    /// <summary>
    /// 煎法枚举 - 中药材的煎煮方法
    /// </summary>
    [Description("煎法")]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum DecocteMethod
    {
        /// <summary>默认煎法（与其他药材一起煎煮）</summary>
        [Description("默认")]
        Default = 0,

        /// <summary>先煎（需要先煎煮一段时间）</summary>
        [Description("先煎")]
        PreDecoct = 1,

        /// <summary>后下（在其他药材煎好后再加入）</summary>
        [Description("后下")]
        PostAdd = 2,

        /// <summary>烊化（用热药液溶化）</summary>
        [Description("烊化")]
        MeltIn = 3,

        /// <summary>冲服（用热药液冲服）</summary>
        [Description("冲服")]
        TakeWithWater = 4,

        /// <summary>包煎（用纱布包裹后煎煮）</summary>
        [Description("包煎")]
        WrapDecoct = 5,

        /// <summary>另煎（单独煎煮）</summary>
        [Description("另煎")]
        SeparateDecoct = 6
    }
}

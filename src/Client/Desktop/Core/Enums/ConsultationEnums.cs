namespace LYBT.Desktop.Core.Enums
{

    /// <summary>
    /// 验方合并模式
    /// </summary>
    public enum FormulaMergeMode
    {

        /// <summary>替换：清空当前处方，使用验方内容</summary>
        Replace = 0,

        /// <summary>追加：在当前处方后添加验方内容</summary>
        Append = 1,

        /// <summary>合并：智能合并，相同药材合并剂量</summary>
        Merge = 2
    }
}

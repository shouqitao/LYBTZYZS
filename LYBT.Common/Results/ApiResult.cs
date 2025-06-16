namespace LYBT.Common.Results {

    /// <summary>
    /// 统一的 API 返回结果封装
    /// </summary>
    public class ApiResult<T> {
        /// <summary>是否成功</summary>
        public bool Success { get; set; }
        /// <summary>提示信息</summary>
        public string Message { get; set; } = string.Empty;
        /// <summary>返回数据</summary>
        public T? Data { get; set; }

        /// <summary>
        /// 成功返回对象
        /// </summary>
        public static ApiResult<T> Ok(T data, string message = "操作成功") =>
            new() { Success = true, Data = data, Message = message };

        /// <summary>
        /// 失败返回对象
        /// </summary>
        public static ApiResult<T> Fail(string message) =>
            new() { Success = false, Message = message };
    }
}
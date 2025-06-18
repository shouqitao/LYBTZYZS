// LYBT.Common/Responses/ApiResponse.cs
namespace LYBT.Common.Responses {
    /// <summary>
    /// 接口统一响应体
    /// </summary>
    public class ApiResponse<T> {
        public int Code { get; set; } = 200;
        public string Message { get; set; } = "操作成功";
        public T? Data { get; set; } // Marked as nullable to resolve CS8618

        public static ApiResponse<T> Success(T data, string message = "操作成功") {
            return new ApiResponse<T> { Code = 200, Message = message, Data = data };
        }

        public static ApiResponse<T> Fail(string message, int code = 500) {
            return new ApiResponse<T> { Code = code, Message = message };
        }
    }
}

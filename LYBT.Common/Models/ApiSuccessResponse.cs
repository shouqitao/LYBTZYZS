namespace LYBT.Common.Models {
    public class ApiSuccessResponse {
        public bool Success { get; set; }
        public int? Count { get; set; }
        public string? Message { get; set; } // 新增：用于传递错误信息
    }
}
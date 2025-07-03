using System;
using System.Collections.Generic;
using LYBT.Module.Logs.Dtos;

namespace LYBT.UI.WPF.Services.Api {
    public class AddLogResponse {
        public bool Success { get; set; }
        public Guid Id { get; set; }
    }

    public class GetLogsResponse {
        public int Total { get; set; }
        public List<LogDto> Logs { get; set; } = new();
    }
}

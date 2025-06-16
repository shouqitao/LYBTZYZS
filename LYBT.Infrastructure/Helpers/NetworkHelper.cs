using System.Net.NetworkInformation;

namespace LYBT.Infrastructure.Helpers {
    /// <summary>
    /// 网络状态检查工具类
    /// </summary>
    public static class NetworkHelper {
        public static bool IsNetworkAvailable() {
            return NetworkInterface.GetIsNetworkAvailable();
        }
    }
}

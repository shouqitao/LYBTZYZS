using Newtonsoft.Json;
using Refit;

namespace LYBT.WPF.Client.Infrastructure
{
    /// <summary>
    /// Refit配置
    /// </summary>
    public static class RefitConfiguration
    {
        /// <summary>
        /// 获取Refit设置
        /// </summary>
        public static RefitSettings GetRefitSettings()
        {
            return new RefitSettings
            {
                ContentSerializer = new NewtonsoftJsonContentSerializer(
                    new JsonSerializerSettings
                    {
                        ContractResolver = new Newtonsoft.Json.Serialization.CamelCasePropertyNamesContractResolver()
                    })
            };
        }
    }
}
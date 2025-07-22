using LYBT.Module.Settings.Interfaces;
using System.ComponentModel;
using System.Reflection;

namespace LYBT.Module.Settings.Services {

/// <summary>
/// 表示EnumMappingsService。
/// </summary>
    public class EnumMappingsService : IEnumMappingsService {

/// <summary>
/// 执行GetAllAsync操作。
/// </summary>
/// <returns>返回值</returns>
        public Task<Dictionary<string, Dictionary<int, string>>> GetAllAsync() {
            var result = new Dictionary<string, Dictionary<int, string>>();
            var enumTypes = Assembly.Load("LYBT.Common").GetTypes()
                .Where(t => t.IsEnum && t.Namespace == "LYBT.Common.Enums");
            foreach (var type in enumTypes) {
                var map = new Dictionary<int, string>();
                foreach (var value in Enum.GetValues(type)) {
                    var fi = type.GetField(value.ToString());
                    var desc = fi?.GetCustomAttribute<DescriptionAttribute>()?.Description ?? value.ToString();
                    map.Add((int)value, desc);
                }
                result.Add(type.Name, map);
            }
            return Task.FromResult(result);
        }
    }
}

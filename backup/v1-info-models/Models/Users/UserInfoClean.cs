using System;
using LYBT.Shared.Models.Core;

namespace LYBT.Desktop.Core.Models.Users
{
    /// <summary>
    /// 用户信息模型 - UltraThink重构后的纯数据模型
    /// Layer 4 (Info): 只包含数据，不包含UI逻辑
    /// </summary>
    public class UserInfoClean : BaseUser
    {
        // 继承自BaseUser的所有数据属性：
        // - Id, Username, RealName, PhoneNumber, Email, Role, Status
        // - CreateTime, UpdateTime, LastLoginTime, Remark
        
        // 不再包含任何UI相关属性和逻辑
        // 所有UI相关功能移至DisplayViewModel和StateViewModel
    }
}
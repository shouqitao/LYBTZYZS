using Prism.Events;

namespace LYBT.Desktop.Infrastructure.Events
{
    /// <summary>
    /// 草稿保存事件
    /// </summary>
    public class DraftSavedEvent : PubSubEvent<DraftSavedPayload>
    {
    }
}

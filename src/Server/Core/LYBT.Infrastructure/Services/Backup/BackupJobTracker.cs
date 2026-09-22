namespace LYBT.Infrastructure.Services.Backup;

/// <summary>
/// 进行中备份操作的进度快照（B-06：恢复/备份进度显示）
/// </summary>
public sealed record BackupJobSnapshot(
    string Kind,
    string Phase,
    int Percent,
    DateTime StartedAt,
    bool IsRunning,
    string? LastError);

/// <summary>
/// 备份作业进度跟踪器（单例）。
/// 备份/恢复为进程内串行操作，UI 通过状态查询接口轮询本跟踪器获取进度（不引入长连接/推送）。
/// </summary>
public sealed class BackupJobTracker
{
    private readonly object _gate = new();
    private BackupJobSnapshot? _current;

    /// <summary>当前（或最近一次）作业快照</summary>
    public BackupJobSnapshot? Current
    {
        get
        {
            lock (_gate)
            {
                return _current;
            }
        }
    }

    /// <summary>开始作业</summary>
    public void Start(string kind, string phase)
    {
        lock (_gate)
        {
            _current = new BackupJobSnapshot(kind, phase, 0, DateTime.Now, true, null);
        }
    }

    /// <summary>更新进度</summary>
    public void Report(string phase, int percent)
    {
        lock (_gate)
        {
            if (_current == null || !_current.IsRunning)
                return;

            _current = _current with
            {
                Phase = phase,
                Percent = Math.Clamp(percent, 0, 100)
            };
        }
    }

    /// <summary>作业成功结束</summary>
    public void Complete(string phase = "已完成")
    {
        lock (_gate)
        {
            if (_current == null)
                return;

            _current = _current with { Phase = phase, Percent = 100, IsRunning = false, LastError = null };
        }
    }

    /// <summary>作业失败结束</summary>
    public void Fail(string error)
    {
        lock (_gate)
        {
            if (_current == null)
                return;

            _current = _current with { Phase = "失败", IsRunning = false, LastError = error };
        }
    }
}

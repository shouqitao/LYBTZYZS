using Microsoft.Data.SqlClient;
using Respawn;
using Respawn.Graph;

namespace LYBT.Tests.Server._Infrastructure;

/// <summary>
/// Respawn 7 数据库清库（Respawner API — Respawn 6+ 已移除旧 Checkpoint 类）。
/// 首次 Reset 前必须先 EnsureCreated（Respawner.CreateAsync 需至少一张业务表）。
/// </summary>
public static class RespawnCheckpoint
{
    private static Respawner? _respawner;

    public static async Task<Respawner> CreateAsync(string connectionString)
    {
        if (_respawner is not null)
            return _respawner;

        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();

        _respawner = await Respawner.CreateAsync(connection, new RespawnerOptions
        {
            TablesToIgnore = new Table[] { "__EFMigrationsHistory" },
            WithReseed = true
        });
        return _respawner;
    }

    public static async Task ResetAsync(string connectionString)
    {
        var respawner = await CreateAsync(connectionString);

        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();
        await respawner.ResetAsync(connection);
    }
}

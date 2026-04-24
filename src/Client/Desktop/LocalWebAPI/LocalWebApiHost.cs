using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using LYBT.LocalWebAPI.Data;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Server.Kestrel.Core;

namespace LYBT.LocalWebAPI
{
    /// <summary>
    /// Manages the lifecycle of the embedded Kestrel WebAPI.
    /// Starts on demand, shuts down gracefully on app exit.
    /// </summary>
    public class LocalWebApiHost : IDisposable, IAsyncDisposable
    {
        private WebApplication? _app;
        private CancellationTokenSource? _cts;
        private Task? _runTask;
        private readonly string _dbPath;
        private readonly ILogger<LocalWebApiHost> _logger;
        public int Port { get; private set; }
        public bool IsRunning { get; private set; }

        private readonly object _lock = new object();

        public LocalWebApiHost(string dbPath, ILogger<LocalWebApiHost> logger)
        {
            _dbPath = dbPath;
            _logger = logger;
            Port = 0;
            IsRunning = false;
        }

        public async Task StartAsync(CancellationToken cancellationToken = default)
        {
            lock (_lock)
            {
                if (IsRunning)
                {
                    _logger.LogInformation("LocalWebApiHost is already running.");
                    return;
                }
            }

            try
            {
                // 1. Ensure DB directory exists
                var dbDir = Path.GetDirectoryName(_dbPath);
                if (!string.IsNullOrEmpty(dbDir))
                {
                    Directory.CreateDirectory(dbDir);
                }

                // 2. Build the WebApplication via LocalWebApiProgram
                var builder = LocalWebApiProgram.CreateBuilder(Array.Empty<string>());

                // 3. Configure Kestrel to use a dynamic port (port 0)
                builder.Services.Configure<KestrelServerOptions>(o => o.ListenAnyIP(0));

                // 4. Build the app
                _logger.LogInformation("Starting LocalWebApiHost on dynamic port with DB at {DBPath}", _dbPath);
                _app = LocalWebApiProgram.CreateApplication(builder, _dbPath);

                // 5. Initialize database (seed data)
                await LocalWebApiProgram.InitializeDatabaseAsync(_app);

                // 6. Start the server in background
                _cts = new CancellationTokenSource();
                // Start RunAsync with external cancellation token to allow external stop
                _runTask = _app.RunAsync(_cts.Token);

                // 7. Capture the actual bound port using IServerAddressesFeature, if available
                // Small delay to ensure the server has bound the address
                await Task.Delay(200, cancellationToken);
                var addressFeature = _app.ServerFeatures.Get<IServerAddressesFeature>();
                if (addressFeature != null && addressFeature.Addresses.Count > 0)
                {
                    var first = addressFeature.Addresses.First();
                    try
                    {
                        Port = new Uri(first).Port;
                    }
                    catch
                    {
                        Port = 0;
                    }
                }

                lock (_lock)
                {
                    IsRunning = true;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to start LocalWebApiHost");
                throw;
            }
        }

        public async Task StopAsync(CancellationToken cancellationToken = default)
        {
            lock (_lock)
            {
                if (!IsRunning && _runTask == null)
                {
                    return;
                }
                IsRunning = false;
            }

            try
            {
                _cts?.Cancel();
                if (_runTask != null)
                {
                    try
                    {
                        await Task.WhenAny(_runTask, Task.Delay(TimeSpan.FromSeconds(5), cancellationToken));
                    }
                    catch { /* ignore */ }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while stopping LocalWebApiHost");
            }
            finally
            {
                try
                {
                    if (_app != null)
                    {
                        await _app.DisposeAsync();
                    }
                }
                catch { /* ignore */ }

                _app = null;
                _runTask = null;
                _cts?.Dispose();
                _cts = null;
                _logger.LogInformation("LocalWebApiHost stopped.");
            }
        }

        public void Dispose()
        {
            // Synchronously stop if needed
            StopAsync().GetAwaiter().GetResult();
        }

        public async ValueTask DisposeAsync()
        {
            await StopAsync();
        }
    }
}

using LYBT.LocalWebAPI;

var builder = LocalWebApiProgram.CreateBuilder(args);

builder.WebHost.UseUrls("http://127.0.0.1:5290");

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? "Server=localhost;Database=LYBTDB_Local;Trusted_Connection=True;TrustServerCertificate=true;MultipleActiveResultSets=true";

var app = LocalWebApiProgram.CreateApplication(builder, connectionString);
await LocalWebApiProgram.InitializeDatabaseAsync(app);
await app.RunAsync();

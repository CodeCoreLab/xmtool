var builder = WebApplication.CreateBuilder(args);

// ── 监听边界：127.0.0.1 强制回环（安全红线：本服务含凭据面，禁 0.0.0.0）
var port = builder.Configuration.GetValue<int?>("RdpPanel:Port") ?? 18888;
builder.WebHost.UseUrls($"http://127.0.0.1:{port}");

builder.Services.AddControllers();

var app = builder.Build();

// ── API 路由（M2：连接 CRUD/探活/拉起；M3：安全会话）
global::RemoteDesktopPlus.Server.Api.ConnectionsApi.Map(app);
global::RemoteDesktopPlus.Server.Api.ConnectApi.Map(app);
global::RemoteDesktopPlus.Server.Api.SecurityApi.Map(app);

// ── 静态面板（wwwroot/index.html，publish 时内嵌输出目录）
app.UseDefaultFiles();
app.UseStaticFiles();

// ── 健康端点（形态 A 标准件：liveness 轻量 200）
app.MapGet("/healthz", () => Results.Ok(new { status = "alive", ts = DateTime.UtcNow }));

app.MapControllers();
app.Run();

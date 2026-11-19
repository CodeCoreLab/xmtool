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

// ── 健康端点（形态 A 标准件：liveness 轻量 200）
app.MapGet("/healthz", () => Results.Ok(new { status = "alive", ts = DateTime.UtcNow }));

// ── 占位面板（M3 换真 UI；M1 验收主路径 = 打开 200）
app.MapGet("/", () => Results.Content(
    "<!DOCTYPE html><html lang=\"zh\"><head><meta charset=\"utf-8\"><title>枢联面板</title></head>" +
    "<body><h1>枢联（B′ 服务版）</h1><p>M1 骨架占位页——API 尚未接入（M2）。</p>" +
    "<p><a href=\"/healthz\">/healthz</a></p></body></html>",
    "text/html"));

app.MapControllers();
app.Run();

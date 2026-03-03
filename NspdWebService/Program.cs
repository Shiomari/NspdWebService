var builder = WebApplication.CreateBuilder(args);

builder.WebHost.UseSetting("preventHostingStartup", "True");
builder.WebHost.ConfigureKestrel(serverOptions =>
{
    serverOptions.ListenAnyIP(5000);
    serverOptions.ListenAnyIP(5001, listenOptions =>
    {
        listenOptions.UseHttps();
    });
});

if (builder.Environment.IsDevelopment())
{
    var browserLinkEnabled = builder.Configuration.GetValue<bool>("BrowserLink:Enable", true);
    if (!browserLinkEnabled)
    {
        builder.Services.AddRazorPages();
        builder.Services.AddControllersWithViews();
    }
    else
    {
        builder.Services.AddRazorPages().AddRazorRuntimeCompilation();
    }
}
else
{
    builder.Services.AddRazorPages();
}

var allowedOrigins = builder.Configuration.GetSection("AllowedOrigins").Get<string[]>()
                     ?? new[] { "http://localhost:5001", "http://localhost:5000" };

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll",
        builder =>
        {
            builder
                .AllowAnyOrigin()
                .AllowAnyMethod()
                .AllowAnyHeader();
        });
});

builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.Name = ".Nspd.Session";
});

builder.Services.AddHttpClient();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

//app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

app.UseCors("AllowAll");
app.UseAuthorization();
app.UseSession();

app.MapRazorPages();

app.MapGet("/api/health", () => new { status = "OK", timestamp = DateTime.UtcNow });

app.MapGet("/api/network", async (HttpContext context) =>
{
    var hostName = System.Net.Dns.GetHostName();
    var ipAddresses = System.Net.Dns.GetHostAddresses(hostName)
        .Where(ip => ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
        .Select(ip => ip.ToString())
        .ToList();

    return new
    {
        hostName,
        localIp = context.Connection.LocalIpAddress?.ToString(),
        remoteIp = context.Connection.RemoteIpAddress?.ToString(),
        allIps = ipAddresses,
        port = context.Connection.LocalPort
    };
});

app.MapGet("/", () => Results.Redirect("/Map"));

app.Run();
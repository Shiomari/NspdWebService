var builder = WebApplication.CreateBuilder(args);

builder.WebHost.UseSetting("preventHostingStartup", "True");

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

app.MapGet("/", () => Results.Redirect("/Map"));

app.Run();
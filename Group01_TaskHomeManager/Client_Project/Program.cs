var builder = WebApplication.CreateBuilder(args);

// ==================================================
// ✅ Đăng ký các dịch vụ cần thiết
// ==================================================
builder.Services.AddRazorPages();

// ✅ Cho phép inject IHttpContextAccessor trong Razor
builder.Services.AddHttpContextAccessor();

var app = builder.Build();

// ==================================================
// ✅ Cấu hình pipeline HTTP
// ==================================================
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

// ✅ Khi chạy lần đầu, tự động chuyển về trang Login
app.MapGet("/", context =>
{
    context.Response.Redirect("/Auth/Login");
    return Task.CompletedTask;
});

app.MapRazorPages();

app.Run();


using DATN_GO.Service;
using DATN_GO.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddDistributedMemoryCache();
// Đăng ký Session & HttpContext
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

builder.Services.AddHttpContextAccessor();

builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();


//  Cấu hình JWT Authentication
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = builder.Configuration["Jwt:Issuer"];
        options.Audience = builder.Configuration["Jwt:Audience"];
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]))
        };
    });

// Đăng ký HttpClient cho AddressService
builder.Services.AddHttpClient("api", client =>
{
    client.BaseAddress = new Uri("https://localhost:7096/"); 
});
builder.Services.AddHttpContextAccessor();


// Đăng ký HttpClient cho các Service
builder.Services.AddScoped<AddressService>();
// Đăng ký Services (Scoped để tránh memory leak)
builder.Services.AddScoped<AuthenticationService>();
builder.Services.AddScoped<UserService>();
builder.Services.AddScoped<GoogleCloudStorageService>();
builder.Services.AddHttpClient<OcrService>();
builder.Services.AddHttpClient<BankService>();
builder.Services.AddHttpClient<StoreService>();
builder.Services.AddScoped<PriceService>();
builder.Services.AddScoped<CategoryService>();
builder.Services.AddScoped<PostService>();
builder.Services.AddScoped<StoreService>();
builder.Services.AddScoped<ProductService>();
builder.Services.AddScoped<ProductVariantService>();
builder.Services.AddScoped<VariantService>();
builder.Services.AddScoped<VariantValueService>();
builder.Services.AddScoped<VariantCompositionService>();
builder.Services.AddScoped<BlogService>();
// Đăng ký HttpClient cho các Service
builder.Services.AddHttpClient<UserTradingPaymentService>();
builder.Services.AddHttpClient<TradingPaymentService>();
builder.Services.AddHttpClient<AuthenticationService>();
builder.Services.AddHttpClient<UserService>();
builder.Services.AddHttpClient<CartService>();
builder.Services.AddHttpClient<VoucherService>();
builder.Services.AddHttpClient<CategoryService>();
builder.Services.AddHttpClient<StoreService>();
builder.Services.AddHttpClient<OrderService>();
builder.Services.AddHttpClient<DecoratesService>();
builder.Services.AddHttpClient<CartService>();
builder.Services.AddHttpClient<BlogService>();
builder.Services.AddHttpClient<ReviewService>();
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

var app = builder.Build();
app.UseCors();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}
app.UseRouting();
// Kích hoạt Session
app.UseSession();
app.UseCookiePolicy();
app.UseHttpsRedirection();
app.UseStaticFiles();



// Authentication Authorization
app.UseAuthentication();
app.UseAuthorization();

app.MapRazorPages();

app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");


app.Run();
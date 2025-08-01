using DATN_API.Data;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using DATN_API.Services.Interfaces;
using DATN_API.Services;
using DATN_API.Interfaces;


var builder = WebApplication.CreateBuilder(args);

// Cấu hình DbContext
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Thêm Authentication & Authorization
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.RequireHttpsMetadata = false;
        options.SaveToken = true;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],      // Đọc từ appsettings.json
            ValidAudience = builder.Configuration["Jwt:Audience"],  // Đọc từ appsettings.json
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]))
        };
    });

builder.Services.AddScoped<IRolesService, RolesService>();
builder.Services.AddScoped<IVouchersService, VouchersService>();
builder.Services.AddScoped<ICategoriesService, CategoriesService>();
builder.Services.AddScoped<IMessageMediasService, MessageMediasService>();
builder.Services.AddScoped<IFollowStoresService, FollowStoresService>();
builder.Services.AddScoped<IDeliveryTrackingsService, DeliveryTrackingsService>();
builder.Services.AddScoped<IDecoratesService, DecoratesService>();
builder.Services.AddScoped<ICitiesService, CitiesService>();
builder.Services.AddScoped<IProductImagesService, ProductImagesService>();
builder.Services.AddScoped<IPricesService, PricesService>();
builder.Services.AddScoped<IOrdersService, OrdersService>();
builder.Services.AddScoped<IReviewsService, ReviewsService>();
builder.Services.AddScoped<IReviewMediasService, ReviewMediasService>();
builder.Services.AddScoped<IRatingStoresService, RatingStoresService>();
builder.Services.AddScoped<IProductVouchersService, ProductVouchersService>();
builder.Services.AddScoped<IProductVariantsService, ProductVariantsService>();
builder.Services.AddScoped<IProductsService, ProductsService>();
builder.Services.AddScoped<IStoresService, StoresService>();
builder.Services.AddScoped<IShippingMethodsService, ShippingMethodsService>();
builder.Services.AddScoped<IWardsService, WardsService>();
builder.Services.AddScoped<IVariantValuesService, VariantValuesService>();
builder.Services.AddScoped<IVariantsService, VariantsService>();
builder.Services.AddScoped<IUsersService, UsersService>();
builder.Services.AddScoped<IOcrService, OcrService>();
builder.Services.AddScoped<IPostsService, PostsService>();
builder.Services.AddScoped<IVariantCompositionService, VariantCompositionService>();
builder.Services.AddScoped<ICartService, CartService>();
builder.Services.AddScoped<IOrdersService, OrdersService>();
builder.Services.AddScoped<ICategoriesService, CategoriesService>();


builder.Services.AddAuthorization();
builder.Services.AddHttpClient();

// Thêm controllers và Swagger
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();


var app = builder.Build();

// Cấu hình middleware
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage(); 
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthentication();  // xxác thực
app.UseAuthorization();   // kiem tra quyền sau

app.MapControllers();

app.Run();

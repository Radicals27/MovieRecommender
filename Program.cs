using MovieRecommender.Services;
using MovieRecommender.Config;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// Configure TMDb settings
builder.Services.Configure<MovieDbSettings>(
    builder.Configuration.GetSection("TMDb"));


// Add IMovieService as a scoped
builder.Services.AddScoped<IMovieService, TMDBMovieService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Movie}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();

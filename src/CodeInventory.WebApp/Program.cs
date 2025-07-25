using CodeInventory.WebApp.Components;
using CodeInventory.WebApp.Controllers;
using CodeInventory.WebApp.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddControllers();

// Configure HttpClient for API calls
builder.Services.AddHttpClient<IProjectService, ProjectService>(client =>
{
    // In development, the backend runs on a different port
    client.BaseAddress = new Uri(builder.Configuration.GetValue<string>("BackendUrl") ?? "http://localhost:5158");
});

var app = builder.Build();

// Configure the HTTP request pipeline.
// if (!app.Environment.IsDevelopment())
// {
//     app.UseExceptionHandler("/Error", createScopeForErrors: true);
//     // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
//     app.UseHsts();
// }
//
// app.UseHttpsRedirection();

app.MapControllers();

app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();

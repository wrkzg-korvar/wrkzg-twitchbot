using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Wrkzg.Host;

var builder = WebApplication.CreateBuilder(args);

// TODO: builder.Services.AddCoreServices();
// TODO: builder.Services.AddInfrastructure(builder.Configuration);
// TODO: builder.Services.AddApiServices();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseStaticFiles();
    app.MapFallbackToFile("index.html");
}

app.UseRouting();

app.MapGet("/", () => "Wrkzg is running!");
// TODO: app.MapHub<ChatHub>("/hubs/chat");

PhotinoHosting.Start(app);

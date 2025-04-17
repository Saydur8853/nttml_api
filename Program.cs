using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>  
{
    //c.SwaggerDoc("v1", new OpenApiInfo { Title = "NTTML API ", Version = "v1" });
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "TFL API ", Version = "v1" });
});
builder.Services.AddAuthorization();  

var app = builder.Build();

app.UseCors(policy => policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();        
    app.UseSwaggerUI(c =>
    {
        //c.SwaggerEndpoint("/swagger/v1/swagger.json", "NTTML API v1");
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "TFL API v1");
    });
}

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    //c.SwaggerEndpoint("/swagger/v1/swagger.json", "NTTML API v1");
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "TFL API v1");
});

app.UseAuthorization();

app.MapControllers();

app.Run();

using System.Text.Json.Serialization;
using FluentValidation.AspNetCore;
using MachineDataApi.BackgroundServices;
using MachineDataApi.Db;
using MachineDataApi.Extensions;
using MachineDataApi.Implementation;
using MachineDataApi.Implementation.Repositories;
using MachineDataApi.Implementation.Services;
using MachineDataApi.Implementation.WebSocketHelpers;
using MachineDataApi.Models;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.RegisterApplicationConfiguration(builder.Configuration);
//builder.Services.AddScoped<IMachineDataRepository, InMemoryMachineDataRepository>();
builder.Services.AddScoped<IMachineDataRepository, EfMachineDataRepository>();
builder.Services.AddScoped<IMachineDataService, MachineDataService>();
builder.Services.AddScoped<IWebSocketWrapper, WebSocketWrapper>();
builder.Services.AddScoped<IMachineStreamClient, MachineStreamClient>();
builder.Services.AddSingleton<IMachineStreamClientFactory, MachineStreamClientFactory>();

builder.Services.AddHostedService<MachineDataIngestHostedService>();

builder.Services.AddControllers().AddJsonOptions(o => o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddFluentValidation(o => o.RegisterValidatorsFromAssemblyContaining<PagingParamsValidator>());
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<MachineDataDbContext>(options =>
{
    options.UseNpgsql(builder.Configuration.GetConnectionString("MACHINEDATA_DB_CONNECTION"), npgOpt =>
    {
        //Add special configuring here..
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    //app.UseSwagger();
    //app.UseSwaggerUI();
}

app.UseSwagger();
app.UseSwaggerUI();

app.UseAuthorization();

app.MapControllers();


using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;

    var context = services.GetRequiredService<MachineDataDbContext>();
    context.Database.Migrate();
}

app.Run();

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
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using OpenTelemetry.Metrics;
using MachineDataApi.Instrumentation;
using Npgsql;

var builder = WebApplication.CreateBuilder(args);


//const string serviceName = "MachineDataApi";
//const string serviceVersion = "1.0.0";




//Sdk.CreateMeterProviderBuilder()
//    .AddMeter(serviceName)
//    .AddPrometheusHttpListener(
//        options => options.UriPrefixes = new string[] { "http://localhost:9464/" })
//    .Build();
builder.Services.AddOpenTelemetryTracing(tracerProviderBuilder =>
{
    tracerProviderBuilder
    .AddSource(InstrumentationConstants.AppSource)
    .SetResourceBuilder(
        ResourceBuilder.CreateDefault()
            .AddService(serviceName: InstrumentationConstants.AppSource))
    .AddJaegerExporter(o => 
    {
        //o.AgentHost = "jaeger";
        o.Protocol = OpenTelemetry.Exporter.JaegerExportProtocol.UdpCompactThrift;
        //o.ExportProcessorType = OpenTelemetry.ExportProcessorType.Simple;
    })
    .AddNpgsql()
    .AddHttpClientInstrumentation()
    .AddAspNetCoreInstrumentation(o => o.Filter = httContext => !httContext.Request.Path.Value?.Contains("/swagger", StringComparison.OrdinalIgnoreCase) == true)
    .AddSqlClientInstrumentation();
});


builder.Services.AddOpenTelemetryMetrics(builder =>
{
    builder.SetResourceBuilder(ResourceBuilder.CreateDefault().AddService(InstrumentationConstants.AppSource))
    .AddMeter(InstrumentationConstants.AppSource)
    .AddPrometheusExporter(options =>
    {
    })
    .AddAspNetCoreInstrumentation()
    //.AddAspNetCoreInstrumentation(o => o.Filter = httContext => httContext.Request.Path.Value?.Contains("/swagger", StringComparison.OrdinalIgnoreCase) == true)
    .AddHttpClientInstrumentation();
});


builder.Services.RegisterApplicationConfiguration(builder.Configuration);

builder.Services.AddDbContext<MachineDataDbContext>(options =>
{
    options.AddInterceptors(new DbConnectionInterceptor());
    options.UseNpgsql(builder.Configuration.GetConnectionString("MACHINEDATA_DB_CONNECTION"), npgOpt =>
    {
        //Add special configuring here..
    });
});

// Add services to the container.

//builder.Services.AddScoped<IMachineDataRepository, InMemoryMachineDataRepository>();
builder.Services.AddScoped<IMachineDataRepository, EfMachineDataRepository>();
builder.Services.AddScoped<IMachineDataService, MachineDataService>();
builder.Services.AddScoped<IWebSocketWrapper, WebSocketWrapper>();
builder.Services.AddScoped<IMachineStreamClient, MachineStreamClient>();
builder.Services.AddSingleton<IMachineStreamClientFactory, MachineStreamClientFactory>();

builder.Services.AddHostedService<MachineDataIngestHostedService>();

builder.Services.AddControllers().AddJsonOptions(o => o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
//builder.Services.AddFluentValidationAutoValidation().AddFluentValidationClientsideAdapters(o => o.CreateValidators())
builder.Services.AddFluentValidation(o => o.RegisterValidatorsFromAssemblyContaining<PagingParamsValidator>()).AddFluentValidationClientsideAdapters();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddSingleton(InstrumentationConstants.DefaultActivitySource);

//ActivitySource activitySource = null;

//builder.Services.AddSingleton(sp => activitySource);

var app = builder.Build();

//activitySource = new ActivitySource(serviceName, serviceVersion);

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    //app.UseSwagger();
    //app.UseSwaggerUI();
}

app.UseOpenTelemetryPrometheusScrapingEndpoint();

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

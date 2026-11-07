using System.Text.Json;
using System.Text.Json.Serialization;
using HotelResortBooking.Api.Configuration;
using HotelResortBooking.Api.Extensions;
using HotelResortBooking.Api.Middleware;
using HotelResortBooking.Api.Serialization;
using HotelResortBooking.Application;
using HotelResortBooking.Infrastructure;
using HotelResortBooking.Infrastructure.Configuration;
using Microsoft.Extensions.FileProviders;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new FlexiblePaymentMethodJsonConverter());
        options.JsonSerializerOptions.Converters.Add(new FlexibleNullablePaymentMethodJsonConverter());
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase, allowIntegerValues: true));
    });
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddApiConfiguration(builder.Configuration);
builder.Services.AddOpenApiDocumentation();

var app = builder.Build();

app.ApplyPendingMigrations();

app.UseMiddleware<CorrelationIdMiddleware>();
app.UseMiddleware<RequestLoggingMiddleware>();
app.UseMiddleware<ExceptionHandlingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseOpenApiDocumentation();
}

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseLocalImageFiles();
app.UseAuthentication();
app.UseMiddleware<AccessContextMiddleware>();
app.UseAuthorization();
app.MapControllers();

app.Run();

public partial class Program;

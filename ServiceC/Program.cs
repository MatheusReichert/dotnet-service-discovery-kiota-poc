using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddServiceDiscovery();
builder.Services.ConfigureHttpClientDefaults(http =>
{
    http.AddServiceDiscovery();
});

builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.MapGet("/api/orders", () =>
{
    return Results.Ok(new[]
    {
        new { Id = 1, CustomerId = 1, Total = 1299.98 },
        new { Id = 2, CustomerId = 2, Total = 59.98 }
    });
})
.WithName("GetOrders")
.WithOpenApi();

app.MapGet("/api/orders/{id}", (int id) =>
{
    return Results.Ok(new { Id = id, CustomerId = 1, Total = 199.99 });
})
.WithName("GetOrderById")
.WithOpenApi();

app.MapDelete("/api/orders/{id}", (int id) =>
{
    return Results.NoContent();
})
.WithName("DeleteOrder")
.WithOpenApi();

app.Run();

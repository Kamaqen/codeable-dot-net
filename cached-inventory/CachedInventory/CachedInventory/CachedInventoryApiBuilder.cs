namespace CachedInventory;

using Microsoft.AspNetCore.Mvc;

public static class CachedInventoryApiBuilder
{
  public static WebApplication Build(string[] args)
  {
    var builder = WebApplication.CreateBuilder(args);

    // Add services to the container.
    // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen();
    builder.Services.AddScoped<IWarehouseStockSystemClient, WarehouseStockSystemClient>();

    var app = builder.Build();

    // Configure the HTTP request pipeline.
    if (app.Environment.IsDevelopment())
    {
      app.UseSwagger();
      app.UseSwaggerUI();
    }

    app.UseHttpsRedirection();

    app.MapGet(
        "/stock/{productId:int}",
        async ([FromServices] IWarehouseStockSystemClient client, int productId) => await client.GetStock(productId))
      .WithName("GetStock")
      .WithOpenApi();

    app.MapPost(
        "/stock/retrieve",
        async ([FromServices] IWarehouseStockSystemClient client, [FromBody] RetrieveStockRequest req) =>
        {
          var stock = await client.GetStock(req.ProductId);
          if (stock < req.Amount)
          {
            return Results.BadRequest("Not enough stock.");
          }

          await client.UpdateStock(req.ProductId, stock - req.Amount);
          return Results.Ok();
        })
      .WithName("RetrieveStock")
      .WithOpenApi();


    app.MapPost(
        "/stock/restock",
        async ([FromServices] IWarehouseStockSystemClient client, [FromBody] RestockRequest req) =>
        {
          var stock = await client.GetStock(req.ProductId);
          await client.UpdateStock(req.ProductId, req.Amount + stock);
          return Results.Ok();
        })
      .WithName("Restock")
      .WithOpenApi();

    return app;
  }
}

public record RetrieveStockRequest(int ProductId, int Amount);

public record RestockRequest(int ProductId, int Amount);
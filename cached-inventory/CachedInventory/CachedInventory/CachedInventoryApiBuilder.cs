namespace CachedInventory;

using System.Collections.Concurrent;
using Microsoft.AspNetCore.Mvc;

public static class CachedInventoryApiBuilder
{
  public static WebApplication Build(string[] args)
  {
    var builder = WebApplication.CreateBuilder(args);
    var cache = new ConcurrentDictionary<int, int>();

    // Add services to the container.
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen();
    builder.Services.AddScoped<IWarehouseStockSystemClient, WarehouseStockSystemClient>();

    // Inject the cache object into the service container
    builder.Services.AddSingleton<ConcurrentDictionary<int, int>>(cache);

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
        async (
          [FromServices] IWarehouseStockSystemClient client,
          [FromServices] ConcurrentDictionary<int, int> cache,
          int productId
        ) =>
        {
          if (cache.TryGetValue(productId, out var cachedStock))
          {
            return Results.Ok(cachedStock);
          }

          var stock = await client.GetStock(productId);
          cache[productId] = stock;
          return Results.Ok(stock);
        }
      )
      .WithName("GetStock")
      .WithOpenApi();

    app.MapPost(
        "/stock/retrieve",
        async (
          [FromServices] IWarehouseStockSystemClient client,
          [FromServices] ConcurrentDictionary<int, int> cache,
          [FromBody] RetrieveStockRequest req
        ) =>
        {
          if (cache.TryGetValue(req.ProductId, out var cachedStock) && cachedStock >= req.Amount)
          {
            cache[req.ProductId] = cachedStock - req.Amount;
            _ = Task.Run(() => client.UpdateStock(req.ProductId, cachedStock - req.Amount)); // Update stock in background
            return Results.Ok();
          }

          var stock = await client.GetStock(req.ProductId);
          if (stock < req.Amount)
          {
            return Results.BadRequest("Not enough stock.");
          }

          await client.UpdateStock(req.ProductId, stock - req.Amount);
          cache[req.ProductId] = stock - req.Amount;
          return Results.Ok();
        }
      )
      .WithName("RetrieveStock")
      .WithOpenApi();

    app.MapPost(
        "/stock/restock",
        async (
          [FromServices] IWarehouseStockSystemClient client,
          [FromServices] ConcurrentDictionary<int, int> cache,
          [FromBody] RestockRequest req
        ) =>
        {
          var stock = await client.GetStock(req.ProductId);
          await client.UpdateStock(req.ProductId, req.Amount + stock);
          cache[req.ProductId] = req.Amount + stock;
          return Results.Ok();
        }
      )
      .WithName("Restock")
      .WithOpenApi();

    return app;
  }
}

public record RetrieveStockRequest(int ProductId, int Amount);

public record RestockRequest(int ProductId, int Amount);

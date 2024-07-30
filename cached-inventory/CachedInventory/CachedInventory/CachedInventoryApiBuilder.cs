namespace CachedInventory;

using System.Collections.Concurrent;
using Microsoft.AspNetCore.Mvc;

public static class CachedInventoryApiBuilder
{
  public static WebApplication Build(string[] args)
  {
    var builder = WebApplication.CreateBuilder(args);
    var cache = new ConcurrentDictionary<int, int>();
    var timers = new ConcurrentDictionary<int, Timer>();

    // Add services to the container.
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen();
    builder.Services.AddScoped<IWarehouseStockSystemClient, WarehouseStockSystemClient>();

    // Inject the cache object and other dictionaries into the service container
    builder.Services.AddSingleton(cache);
    builder.Services.AddSingleton(timers);

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
          [FromServices] ConcurrentDictionary<int, Timer> timers,
          [FromBody] RetrieveStockRequest req
        ) =>
        {
          try
          {
            if (cache.TryGetValue(req.ProductId, out var cachedStock) && cachedStock >= req.Amount)
            {
              cache[req.ProductId] = cachedStock - req.Amount;
              ResetTimer(req.ProductId, client, cache, timers);
              return Results.Ok();
            }

            var stock = await client.GetStock(req.ProductId);
            if (stock < req.Amount)
            {
              return Results.BadRequest("Not enough stock.");
            }

            cache[req.ProductId] = stock - req.Amount;
            ResetTimer(req.ProductId, client, cache, timers);
            return Results.Ok();
          }
          finally
          {}
        }
      )
      .WithName("RetrieveStock")
      .WithOpenApi();

    app.MapPost(
        "/stock/restock",
        async (
          [FromServices] IWarehouseStockSystemClient client,
          [FromServices] ConcurrentDictionary<int, int> cache,
          [FromServices] ConcurrentDictionary<int, Timer> timers,
          [FromBody] RestockRequest req
        ) =>
        {
          try
          {
            var stock = await client.GetStock(req.ProductId);
            cache[req.ProductId] = req.Amount + stock;
            ResetTimer(req.ProductId, client, cache, timers);
            return Results.Ok();
          }
          finally
          {}
        }
      )
      .WithName("Restock")
      .WithOpenApi();

    return app;
  }

  private static void ResetTimer(
    int productId,
    IWarehouseStockSystemClient client,
    ConcurrentDictionary<int, int> cache,
    ConcurrentDictionary<int, Timer> timers
  )
  {
    if (timers.TryGetValue(productId, out var existingTimer))
    {
      existingTimer.Change(2500, Timeout.Infinite);
    }
    else
    {
      var newTimer = new Timer(
        async state =>
        {
          if (state != null)
          {
            var pid = (int)state;
            if (cache.TryGetValue(pid, out var stock))
            {
              await client.UpdateStock(pid, stock);
            }
          }
        },
        productId,
        2500,
        Timeout.Infinite
      );
      timers[productId] = newTimer;
    }
  }
}

public record RetrieveStockRequest(int ProductId, int Amount);

public record RestockRequest(int ProductId, int Amount);

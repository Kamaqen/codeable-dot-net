# CachedInventory API

CachedInventory is a web API built with .NET 8 that provides functionalities to manage product inventory in a warehouse, including stock querying, retrieval, and restocking using an in-memory cache system.

## Prerequisites

.NET SDK 8.0 or higher
Visual Studio or Visual Studio Code (optional, for development)
Any HTTP client tool (e.g., Postman, cURL)
Installation

1. Clone the repository

```bash
git clone <REPOSITORY_URL>
cd <PROJECT_NAME>
```

2. Restore dependencies

In the project's root directory, run:

```bash
dotnet restore
```

3. Build the project

```bash
dotnet build
```

Running the Application
To run the API in a development environment, use the following command:

```bash
dotnet run
```

The API will be available by default at https://localhost:5001. You can access the automatically generated API documentation (Swagger UI) at https://localhost:5001/swagger.

## Endpoints

1. Get Product Stock
   GET /stock/{productId}

Description: Retrieves the current stock level of a specific product.
Parameters:
productId (int): The ID of the product.
Responses:
200 OK: Returns the available stock.
404 Not Found: If the product does not exist.

2. Retrieve Product Stock
   POST /stock/retrieve

Description: Retrieves a specified amount of stock from a product.
Request Body:
productId (int): The ID of the product.
amount (int): The amount to retrieve.
Responses:
200 OK: Successful retrieval.
400 Bad Request: Not enough stock available.

3. Restock Product
   POST /stock/restock

Description: Restocks a specified amount of stock for a product.
Request Body:
productId (int): The ID of the product.
amount (int): The amount to restock.
Responses:
200 OK: Successful restock.

## Technologies and Packages Used

.NET 8.0: Framework for building the application.
Swagger & Swashbuckle: For generating API documentation and Swagger UI.
ConcurrentDictionary, Timer, SemaphoreSlim: Used for synchronization and in-memory caching.

## Contributors to this project

- Claudia Berríos
- Jorge Capcha

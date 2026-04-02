namespace Inventory.Contract;

public enum StoreTypeDto
{
    Warehouse = 1,
    Store = 2
}

public enum TransactionTypeDto
{
    Purchase = 1,
    Sale = 2,
    Transfer = 3
}

public sealed record ServiceResult(bool Success, string? Error)
{
    public static ServiceResult Ok() => new(true, null);
    public static ServiceResult Fail(string error) => new(false, error);
}

public sealed record ServiceResult<T>(bool Success, string? Error, T? Data)
{
    public static ServiceResult<T> Ok(T data) => new(true, null, data);
    public static ServiceResult<T> Fail(string error) => new(false, error, default);
}

public sealed record ProductDto(int Id, string Name, decimal Price);

public sealed record StoreDto(int Id, string Name, string Location, StoreTypeDto Type);

public sealed record ProductStockDto(int ProductId, int StoreId, int Quantity);

public sealed record ProductStockDetailsDto(
    int StoreId,
    string StoreName,
    StoreTypeDto StoreType,
    int ProductId,
    string ProductName,
    int Quantity);

public sealed record StockTransactionDto(
    int Id,
    int ProductId,
    string ProductName,
    int? FromStoreId,
    string? FromStoreName,
    int? ToStoreId,
    string? ToStoreName,
    int Quantity,
    TransactionTypeDto TransactionType,
    DateTime TransactionDate);

public sealed record CreateProductRequest(string Name, decimal Price);
public sealed record UpdateProductRequest(int Id, string Name, decimal Price);

public sealed record CreateStoreRequest(string Name, string Location, StoreTypeDto Type);
public sealed record UpdateStoreRequest(int Id, string Name, string Location, StoreTypeDto Type);

public sealed record PurchaseRequest(int ProductId, int Quantity);
public sealed record SaleRequest(int StoreId, int ProductId, int Quantity);
public sealed record TransferRequest(int ProductId, int ToStoreId, int Quantity);

public sealed record TransactionFilter(int? StoreId, int? ProductId, DateTime? FromUtc, DateTime? ToUtc);

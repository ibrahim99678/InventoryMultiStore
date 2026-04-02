namespace Inventory.Domain;

public enum StoreType
{
    Warehouse = 1,
    Store = 2
}

public enum TransactionType
{
    Purchase = 1,
    Sale = 2,
    Transfer = 3
}

public sealed class Product
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public decimal Price { get; set; }

    public ICollection<ProductStock> ProductStocks { get; set; } = new List<ProductStock>();
    public ICollection<StockTransaction> StockTransactions { get; set; } = new List<StockTransaction>();
}

public sealed class Store
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Location { get; set; } = "";
    public StoreType Type { get; set; }

    public ICollection<ProductStock> ProductStocks { get; set; } = new List<ProductStock>();
    public ICollection<StockTransaction> FromTransactions { get; set; } = new List<StockTransaction>();
    public ICollection<StockTransaction> ToTransactions { get; set; } = new List<StockTransaction>();
}

public sealed class ProductStock
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public int StoreId { get; set; }
    public int Quantity { get; set; }

    public Product? Product { get; set; }
    public Store? Store { get; set; }
}

public sealed class StockTransaction
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public int? FromStoreId { get; set; }
    public int? ToStoreId { get; set; }
    public int Quantity { get; set; }
    public TransactionType TransactionType { get; set; }
    public DateTime TransactionDate { get; set; }

    public Product? Product { get; set; }
    public Store? FromStore { get; set; }
    public Store? ToStore { get; set; }
}

using Inventory.Contract;
using Inventory.DAL;
using Inventory.Domain;
using Microsoft.EntityFrameworkCore;

namespace Inventory.BLL;

public interface IProductService
{
    Task<List<ProductDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<ServiceResult<ProductDto>> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<ServiceResult<ProductDto>> CreateAsync(CreateProductRequest request, CancellationToken cancellationToken = default);
    Task<ServiceResult> UpdateAsync(UpdateProductRequest request, CancellationToken cancellationToken = default);
    Task<ServiceResult> DeleteAsync(int id, CancellationToken cancellationToken = default);
}

public interface IStoreService
{
    Task<List<StoreDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<ServiceResult<StoreDto>> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<ServiceResult<StoreDto>> CreateAsync(CreateStoreRequest request, CancellationToken cancellationToken = default);
    Task<ServiceResult> UpdateAsync(UpdateStoreRequest request, CancellationToken cancellationToken = default);
    Task<ServiceResult> DeleteAsync(int id, CancellationToken cancellationToken = default);
    Task<ServiceResult<StoreDto>> GetWarehouseAsync(CancellationToken cancellationToken = default);
}

public interface IStockQueryService
{
    Task<int> GetStockAsync(int productId, int storeId, CancellationToken cancellationToken = default);
    Task<List<ProductStockDto>> GetStockByStoreAsync(int storeId, CancellationToken cancellationToken = default);
    Task<List<ProductStockDetailsDto>> GetStockDetailsByStoreAsync(int storeId, CancellationToken cancellationToken = default);
}

public interface IPurchaseService
{
    Task<ServiceResult> PurchaseAsync(PurchaseRequest request, CancellationToken cancellationToken = default);
}

public interface ISalesService
{
    Task<ServiceResult> SellAsync(SaleRequest request, CancellationToken cancellationToken = default);
}

public interface ITransferService
{
    Task<ServiceResult> TransferAsync(TransferRequest request, CancellationToken cancellationToken = default);
}

public interface ITransactionService
{
    Task<List<StockTransactionDto>> GetTransactionsAsync(TransactionFilter filter, CancellationToken cancellationToken = default);
}

public sealed class ProductService(IUnitOfWork uow) : IProductService
{
    public async Task<List<ProductDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var products = await uow.Products.Query()
            .AsNoTracking()
            .OrderBy(x => x.Name)
            .Select(x => new ProductDto(x.Id, x.Name, x.Price))
            .ToListAsync(cancellationToken);

        return products;
    }

    public async Task<ServiceResult<ProductDto>> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var product = await uow.Products.Query().AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (product is null)
        {
            return ServiceResult<ProductDto>.Fail("Product not found.");
        }

        return ServiceResult<ProductDto>.Ok(new ProductDto(product.Id, product.Name, product.Price));
    }

    public async Task<ServiceResult<ProductDto>> CreateAsync(CreateProductRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return ServiceResult<ProductDto>.Fail("Product name is required.");
        }

        if (request.Price < 0)
        {
            return ServiceResult<ProductDto>.Fail("Price must be zero or positive.");
        }

        var entity = new Product
        {
            Name = request.Name.Trim(),
            Price = request.Price
        };

        await uow.Products.AddAsync(entity, cancellationToken);
        await uow.SaveChangesAsync(cancellationToken);

        return ServiceResult<ProductDto>.Ok(new ProductDto(entity.Id, entity.Name, entity.Price));
    }

    public async Task<ServiceResult> UpdateAsync(UpdateProductRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return ServiceResult.Fail("Product name is required.");
        }

        if (request.Price < 0)
        {
            return ServiceResult.Fail("Price must be zero or positive.");
        }

        var entity = await uow.Products.GetByIdAsync(request.Id, cancellationToken);
        if (entity is null)
        {
            return ServiceResult.Fail("Product not found.");
        }

        entity.Name = request.Name.Trim();
        entity.Price = request.Price;

        uow.Products.Update(entity);
        await uow.SaveChangesAsync(cancellationToken);

        return ServiceResult.Ok();
    }

    public async Task<ServiceResult> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await uow.Products.GetByIdAsync(id, cancellationToken);
        if (entity is null)
        {
            return ServiceResult.Fail("Product not found.");
        }

        uow.Products.Remove(entity);
        await uow.SaveChangesAsync(cancellationToken);

        return ServiceResult.Ok();
    }
}

public sealed class StoreService(IUnitOfWork uow) : IStoreService
{
    public async Task<List<StoreDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var stores = await uow.Stores.Query()
            .AsNoTracking()
            .OrderByDescending(x => x.Type == StoreType.Warehouse)
            .ThenBy(x => x.Name)
            .Select(x => new StoreDto(x.Id, x.Name, x.Location, Map(x.Type)))
            .ToListAsync(cancellationToken);

        return stores;
    }

    public async Task<ServiceResult<StoreDto>> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var store = await uow.Stores.Query().AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (store is null)
        {
            return ServiceResult<StoreDto>.Fail("Store not found.");
        }

        return ServiceResult<StoreDto>.Ok(new StoreDto(store.Id, store.Name, store.Location, Map(store.Type)));
    }

    public async Task<ServiceResult<StoreDto>> CreateAsync(CreateStoreRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return ServiceResult<StoreDto>.Fail("Store name is required.");
        }

        var type = Map(request.Type);
        if (type == StoreType.Warehouse)
        {
            var warehouseExists = await uow.Stores.Query().AsNoTracking().AnyAsync(x => x.Type == StoreType.Warehouse, cancellationToken);
            if (warehouseExists)
            {
                return ServiceResult<StoreDto>.Fail("Warehouse already exists.");
            }
        }

        var entity = new Store
        {
            Name = request.Name.Trim(),
            Location = request.Location?.Trim() ?? "",
            Type = type
        };

        await uow.Stores.AddAsync(entity, cancellationToken);
        await uow.SaveChangesAsync(cancellationToken);

        return ServiceResult<StoreDto>.Ok(new StoreDto(entity.Id, entity.Name, entity.Location, Map(entity.Type)));
    }

    public async Task<ServiceResult> UpdateAsync(UpdateStoreRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return ServiceResult.Fail("Store name is required.");
        }

        var entity = await uow.Stores.GetByIdAsync(request.Id, cancellationToken);
        if (entity is null)
        {
            return ServiceResult.Fail("Store not found.");
        }

        var newType = Map(request.Type);
        if (newType == StoreType.Warehouse)
        {
            var otherWarehouseExists = await uow.Stores.Query().AsNoTracking()
                .AnyAsync(x => x.Type == StoreType.Warehouse && x.Id != entity.Id, cancellationToken);
            if (otherWarehouseExists)
            {
                return ServiceResult.Fail("Another Warehouse already exists.");
            }
        }

        entity.Name = request.Name.Trim();
        entity.Location = request.Location?.Trim() ?? "";
        entity.Type = newType;

        uow.Stores.Update(entity);
        await uow.SaveChangesAsync(cancellationToken);

        return ServiceResult.Ok();
    }

    public async Task<ServiceResult> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await uow.Stores.GetByIdAsync(id, cancellationToken);
        if (entity is null)
        {
            return ServiceResult.Fail("Store not found.");
        }

        if (entity.Type == StoreType.Warehouse)
        {
            return ServiceResult.Fail("Warehouse cannot be deleted.");
        }

        uow.Stores.Remove(entity);
        await uow.SaveChangesAsync(cancellationToken);

        return ServiceResult.Ok();
    }

    public async Task<ServiceResult<StoreDto>> GetWarehouseAsync(CancellationToken cancellationToken = default)
    {
        var warehouse = await uow.Stores.Query().AsNoTracking().FirstOrDefaultAsync(x => x.Type == StoreType.Warehouse, cancellationToken);
        if (warehouse is null)
        {
            return ServiceResult<StoreDto>.Fail("Warehouse store is missing.");
        }

        return ServiceResult<StoreDto>.Ok(new StoreDto(warehouse.Id, warehouse.Name, warehouse.Location, Map(warehouse.Type)));
    }

    private static StoreTypeDto Map(StoreType type)
        => type switch
        {
            StoreType.Warehouse => StoreTypeDto.Warehouse,
            StoreType.Store => StoreTypeDto.Store,
            _ => StoreTypeDto.Store
        };

    private static StoreType Map(StoreTypeDto type)
        => type switch
        {
            StoreTypeDto.Warehouse => StoreType.Warehouse,
            StoreTypeDto.Store => StoreType.Store,
            _ => StoreType.Store
        };
}

public sealed class StockQueryService(IUnitOfWork uow) : IStockQueryService
{
    public async Task<int> GetStockAsync(int productId, int storeId, CancellationToken cancellationToken = default)
    {
        var stock = await uow.ProductStocks.GetByProductAndStoreAsync(productId, storeId, cancellationToken);
        return stock?.Quantity ?? 0;
    }

    public Task<List<ProductStockDto>> GetStockByStoreAsync(int storeId, CancellationToken cancellationToken = default)
        => uow.ProductStocks.Query()
            .AsNoTracking()
            .Where(x => x.StoreId == storeId)
            .OrderByDescending(x => x.Quantity)
            .Select(x => new ProductStockDto(x.ProductId, x.StoreId, x.Quantity))
            .ToListAsync(cancellationToken);

    public Task<List<ProductStockDetailsDto>> GetStockDetailsByStoreAsync(int storeId, CancellationToken cancellationToken = default)
        => (from ps in uow.ProductStocks.Query().AsNoTracking()
            join p in uow.Products.Query().AsNoTracking() on ps.ProductId equals p.Id
            join s in uow.Stores.Query().AsNoTracking() on ps.StoreId equals s.Id
            where ps.StoreId == storeId
            orderby ps.Quantity descending, p.Name
            select new ProductStockDetailsDto(
                s.Id,
                s.Name,
                Map(s.Type),
                p.Id,
                p.Name,
                ps.Quantity))
        .ToListAsync(cancellationToken);

    private static StoreTypeDto Map(StoreType type)
        => type switch
        {
            StoreType.Warehouse => StoreTypeDto.Warehouse,
            StoreType.Store => StoreTypeDto.Store,
            _ => StoreTypeDto.Store
        };
}

public sealed class PurchaseService(IUnitOfWork uow, IStoreService stores) : IPurchaseService
{
    public async Task<ServiceResult> PurchaseAsync(PurchaseRequest request, CancellationToken cancellationToken = default)
    {
        if (request.Quantity <= 0)
        {
            return ServiceResult.Fail("Quantity must be greater than zero.");
        }

        var productExists = await uow.Products.Query().AsNoTracking().AnyAsync(x => x.Id == request.ProductId, cancellationToken);
        if (!productExists)
        {
            return ServiceResult.Fail("Product not found.");
        }

        var warehouseResult = await stores.GetWarehouseAsync(cancellationToken);
        if (!warehouseResult.Success || warehouseResult.Data is null)
        {
            return ServiceResult.Fail(warehouseResult.Error ?? "Warehouse not found.");
        }

        var warehouseId = warehouseResult.Data.Id;
        var stock = await GetOrCreateStockAsync(request.ProductId, warehouseId, cancellationToken);
        stock.Quantity += request.Quantity;

        uow.ProductStocks.Update(stock);
        await uow.StockTransactions.AddAsync(new StockTransaction
        {
            ProductId = request.ProductId,
            FromStoreId = null,
            ToStoreId = warehouseId,
            Quantity = request.Quantity,
            TransactionType = TransactionType.Purchase,
            TransactionDate = DateTime.UtcNow
        }, cancellationToken);

        await uow.SaveChangesAsync(cancellationToken);
        return ServiceResult.Ok();
    }

    private async Task<ProductStock> GetOrCreateStockAsync(int productId, int storeId, CancellationToken cancellationToken)
    {
        var stock = await uow.ProductStocks.GetByProductAndStoreAsync(productId, storeId, cancellationToken);
        if (stock is not null)
        {
            return stock;
        }

        var newStock = new ProductStock
        {
            ProductId = productId,
            StoreId = storeId,
            Quantity = 0
        };

        await uow.ProductStocks.AddAsync(newStock, cancellationToken);
        await uow.SaveChangesAsync(cancellationToken);
        return newStock;
    }
}

public sealed class SalesService(IUnitOfWork uow) : ISalesService
{
    public async Task<ServiceResult> SellAsync(SaleRequest request, CancellationToken cancellationToken = default)
    {
        if (request.Quantity <= 0)
        {
            return ServiceResult.Fail("Quantity must be greater than zero.");
        }

        var store = await uow.Stores.Query().AsNoTracking().FirstOrDefaultAsync(x => x.Id == request.StoreId, cancellationToken);
        if (store is null)
        {
            return ServiceResult.Fail("Store not found.");
        }

        if (store.Type != StoreType.Store)
        {
            return ServiceResult.Fail("Sales can only be recorded from a Store.");
        }

        var productExists = await uow.Products.Query().AsNoTracking().AnyAsync(x => x.Id == request.ProductId, cancellationToken);
        if (!productExists)
        {
            return ServiceResult.Fail("Product not found.");
        }

        var stock = await uow.ProductStocks.GetByProductAndStoreAsync(request.ProductId, request.StoreId, cancellationToken);
        if (stock is null || stock.Quantity < request.Quantity)
        {
            return ServiceResult.Fail("Insufficient stock.");
        }

        stock.Quantity -= request.Quantity;
        uow.ProductStocks.Update(stock);

        await uow.StockTransactions.AddAsync(new StockTransaction
        {
            ProductId = request.ProductId,
            FromStoreId = request.StoreId,
            ToStoreId = null,
            Quantity = request.Quantity,
            TransactionType = TransactionType.Sale,
            TransactionDate = DateTime.UtcNow
        }, cancellationToken);

        await uow.SaveChangesAsync(cancellationToken);
        return ServiceResult.Ok();
    }
}

public sealed class TransferService(IUnitOfWork uow, IStoreService stores) : ITransferService
{
    public async Task<ServiceResult> TransferAsync(TransferRequest request, CancellationToken cancellationToken = default)
    {
        if (request.Quantity <= 0)
        {
            return ServiceResult.Fail("Quantity must be greater than zero.");
        }

        var productExists = await uow.Products.Query().AsNoTracking().AnyAsync(x => x.Id == request.ProductId, cancellationToken);
        if (!productExists)
        {
            return ServiceResult.Fail("Product not found.");
        }

        var toStore = await uow.Stores.Query().AsNoTracking().FirstOrDefaultAsync(x => x.Id == request.ToStoreId, cancellationToken);
        if (toStore is null)
        {
            return ServiceResult.Fail("Destination store not found.");
        }

        if (toStore.Type != StoreType.Store)
        {
            return ServiceResult.Fail("Destination must be a Store.");
        }

        var warehouseResult = await stores.GetWarehouseAsync(cancellationToken);
        if (!warehouseResult.Success || warehouseResult.Data is null)
        {
            return ServiceResult.Fail(warehouseResult.Error ?? "Warehouse not found.");
        }

        var warehouseId = warehouseResult.Data.Id;
        var warehouseStock = await uow.ProductStocks.GetByProductAndStoreAsync(request.ProductId, warehouseId, cancellationToken);
        if (warehouseStock is null || warehouseStock.Quantity < request.Quantity)
        {
            return ServiceResult.Fail("Insufficient stock in Warehouse.");
        }

        warehouseStock.Quantity -= request.Quantity;
        uow.ProductStocks.Update(warehouseStock);

        var toStock = await uow.ProductStocks.GetByProductAndStoreAsync(request.ProductId, request.ToStoreId, cancellationToken);
        if (toStock is null)
        {
            toStock = new ProductStock { ProductId = request.ProductId, StoreId = request.ToStoreId, Quantity = request.Quantity };
            await uow.ProductStocks.AddAsync(toStock, cancellationToken);
        }
        else
        {
            toStock.Quantity += request.Quantity;
            uow.ProductStocks.Update(toStock);
        }

        await uow.StockTransactions.AddAsync(new StockTransaction
        {
            ProductId = request.ProductId,
            FromStoreId = warehouseId,
            ToStoreId = request.ToStoreId,
            Quantity = request.Quantity,
            TransactionType = TransactionType.Transfer,
            TransactionDate = DateTime.UtcNow
        }, cancellationToken);

        await uow.SaveChangesAsync(cancellationToken);
        return ServiceResult.Ok();
    }
}

public sealed class TransactionService(IUnitOfWork uow) : ITransactionService
{
    public async Task<List<StockTransactionDto>> GetTransactionsAsync(TransactionFilter filter, CancellationToken cancellationToken = default)
    {
        var q = uow.StockTransactions.QueryWithDetails();

        if (filter.StoreId.HasValue)
        {
            var storeId = filter.StoreId.Value;
            q = q.Where(x => x.FromStoreId == storeId || x.ToStoreId == storeId);
        }

        if (filter.ProductId.HasValue)
        {
            q = q.Where(x => x.ProductId == filter.ProductId.Value);
        }

        if (filter.FromUtc.HasValue)
        {
            q = q.Where(x => x.TransactionDate >= filter.FromUtc.Value);
        }

        if (filter.ToUtc.HasValue)
        {
            q = q.Where(x => x.TransactionDate <= filter.ToUtc.Value);
        }

        var list = await q
            .OrderByDescending(x => x.TransactionDate)
            .Select(x => new StockTransactionDto(
                x.Id,
                x.ProductId,
                x.Product != null ? x.Product.Name : "",
                x.FromStoreId,
                x.FromStore != null ? x.FromStore.Name : null,
                x.ToStoreId,
                x.ToStore != null ? x.ToStore.Name : null,
                x.Quantity,
                Map(x.TransactionType),
                x.TransactionDate))
            .ToListAsync(cancellationToken);

        return list;
    }

    private static TransactionTypeDto Map(TransactionType type)
        => type switch
        {
            TransactionType.Purchase => TransactionTypeDto.Purchase,
            TransactionType.Sale => TransactionTypeDto.Sale,
            TransactionType.Transfer => TransactionTypeDto.Transfer,
            _ => TransactionTypeDto.Transfer
        };
}

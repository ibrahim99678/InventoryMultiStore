using Inventory.Domain;
using Microsoft.EntityFrameworkCore;

namespace Inventory.DAL;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Store> Stores => Set<Store>();
    public DbSet<ProductStock> ProductStocks => Set<ProductStock>();
    public DbSet<StockTransaction> StockTransactions => Set<StockTransaction>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Product>(b =>
        {
            b.Property(x => x.Name).HasMaxLength(200).IsRequired();
            b.Property(x => x.Price).HasColumnType("decimal(18,2)");
        });

        modelBuilder.Entity<Store>(b =>
        {
            b.Property(x => x.Name).HasMaxLength(200).IsRequired();
            b.Property(x => x.Location).HasMaxLength(300);
            b.Property(x => x.Type).HasConversion<int>();
        });

        modelBuilder.Entity<ProductStock>(b =>
        {
            b.HasIndex(x => new { x.ProductId, x.StoreId }).IsUnique();
            b.HasOne(x => x.Product)
                .WithMany(x => x.ProductStocks)
                .HasForeignKey(x => x.ProductId)
                .OnDelete(DeleteBehavior.Cascade);
            b.HasOne(x => x.Store)
                .WithMany(x => x.ProductStocks)
                .HasForeignKey(x => x.StoreId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<StockTransaction>(b =>
        {
            b.Property(x => x.TransactionType).HasConversion<int>();
            b.HasOne(x => x.Product)
                .WithMany(x => x.StockTransactions)
                .HasForeignKey(x => x.ProductId)
                .OnDelete(DeleteBehavior.Cascade);
            b.HasOne(x => x.FromStore)
                .WithMany(x => x.FromTransactions)
                .HasForeignKey(x => x.FromStoreId)
                .OnDelete(DeleteBehavior.Restrict);
            b.HasOne(x => x.ToStore)
                .WithMany(x => x.ToTransactions)
                .HasForeignKey(x => x.ToStoreId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}

public interface IGenericRepository<T> where T : class
{
    Task<T?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<List<T>> GetAllAsync(CancellationToken cancellationToken = default);
    IQueryable<T> Query();
    Task AddAsync(T entity, CancellationToken cancellationToken = default);
    void Update(T entity);
    void Remove(T entity);
}

public class GenericRepository<T>(AppDbContext db) : IGenericRepository<T> where T : class
{
    private readonly DbSet<T> _set = db.Set<T>();

    public Task<T?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        => _set.FindAsync([id], cancellationToken).AsTask();

    public Task<List<T>> GetAllAsync(CancellationToken cancellationToken = default)
        => _set.AsNoTracking().ToListAsync(cancellationToken);

    public IQueryable<T> Query() => _set.AsQueryable();

    public Task AddAsync(T entity, CancellationToken cancellationToken = default)
        => _set.AddAsync(entity, cancellationToken).AsTask();

    public void Update(T entity) => _set.Update(entity);

    public void Remove(T entity) => _set.Remove(entity);
}

public interface IProductStockRepository : IGenericRepository<ProductStock>
{
    Task<ProductStock?> GetByProductAndStoreAsync(int productId, int storeId, CancellationToken cancellationToken = default);
}

public sealed class ProductStockRepository(AppDbContext db) : GenericRepository<ProductStock>(db), IProductStockRepository
{
    private readonly AppDbContext _db = db;

    public Task<ProductStock?> GetByProductAndStoreAsync(int productId, int storeId, CancellationToken cancellationToken = default)
        => _db.ProductStocks.FirstOrDefaultAsync(x => x.ProductId == productId && x.StoreId == storeId, cancellationToken);
}

public interface IStockTransactionRepository : IGenericRepository<StockTransaction>
{
    IQueryable<StockTransaction> QueryWithDetails();
}

public sealed class StockTransactionRepository(AppDbContext db) : GenericRepository<StockTransaction>(db), IStockTransactionRepository
{
    private readonly AppDbContext _db = db;

    public IQueryable<StockTransaction> QueryWithDetails()
        => _db.StockTransactions
            .AsNoTracking()
            .Include(x => x.Product)
            .Include(x => x.FromStore)
            .Include(x => x.ToStore);
}

public interface IUnitOfWork
{
    IGenericRepository<Product> Products { get; }
    IGenericRepository<Store> Stores { get; }
    IProductStockRepository ProductStocks { get; }
    IStockTransactionRepository StockTransactions { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}

public sealed class UnitOfWork(AppDbContext db) : IUnitOfWork
{
    private readonly AppDbContext _db = db;

    private GenericRepository<Product>? _products;
    private GenericRepository<Store>? _stores;
    private ProductStockRepository? _productStocks;
    private StockTransactionRepository? _stockTransactions;

    public IGenericRepository<Product> Products => _products ??= new GenericRepository<Product>(_db);
    public IGenericRepository<Store> Stores => _stores ??= new GenericRepository<Store>(_db);
    public IProductStockRepository ProductStocks => _productStocks ??= new ProductStockRepository(_db);
    public IStockTransactionRepository StockTransactions => _stockTransactions ??= new StockTransactionRepository(_db);

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        => _db.SaveChangesAsync(cancellationToken);
}

public static class DbInitializer
{
    public static async Task InitializeAsync(AppDbContext db, CancellationToken cancellationToken = default)
    {
        var applied = await db.Database.GetAppliedMigrationsAsync(cancellationToken);
        var pending = await db.Database.GetPendingMigrationsAsync(cancellationToken);
        if (applied.Any() || pending.Any())
        {
            await db.Database.MigrateAsync(cancellationToken);
        }
        else
        {
            await db.Database.EnsureCreatedAsync(cancellationToken);
        }

        var warehouseExists = await db.Stores.AsNoTracking().AnyAsync(x => x.Type == StoreType.Warehouse, cancellationToken);
        if (!warehouseExists)
        {
            db.Stores.Add(new Store
            {
                Name = "Central Warehouse",
                Location = "N/A",
                Type = StoreType.Warehouse
            });
            await db.SaveChangesAsync(cancellationToken);
        }
    }
}

using System.ComponentModel.DataAnnotations;
using Inventory.Contract;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Inventory.Web.ViewModels;

public sealed class ProductCreateEditVm
{
    public int Id { get; set; }

    [Required]
    [StringLength(200)]
    public string Name { get; set; } = "";

    [Range(0, double.MaxValue)]
    public decimal Price { get; set; }
}

public sealed class StoreCreateEditVm
{
    public int Id { get; set; }

    [Required]
    [StringLength(200)]
    public string Name { get; set; } = "";

    [StringLength(300)]
    public string Location { get; set; } = "";

    [Required]
    public StoreTypeDto Type { get; set; } = StoreTypeDto.Store;
}

public sealed class PurchaseVm
{
    [Required]
    public int? ProductId { get; set; }

    [Range(1, int.MaxValue)]
    public int Quantity { get; set; } = 1;

    public List<SelectListItem> Products { get; set; } = [];
}

public sealed class SaleVm
{
    [Required]
    public int? StoreId { get; set; }

    [Required]
    public int? ProductId { get; set; }

    [Range(1, int.MaxValue)]
    public int Quantity { get; set; } = 1;

    public List<SelectListItem> Stores { get; set; } = [];
    public List<SelectListItem> Products { get; set; } = [];
}

public sealed class TransferVm
{
    [Required]
    public int? ProductId { get; set; }

    [Required]
    public int? ToStoreId { get; set; }

    [Range(1, int.MaxValue)]
    public int Quantity { get; set; } = 1;

    public List<SelectListItem> Products { get; set; } = [];
    public List<SelectListItem> Stores { get; set; } = [];
}

public sealed class TransactionIndexVm
{
    public int? StoreId { get; set; }
    public int? ProductId { get; set; }
    public List<SelectListItem> Stores { get; set; } = [];
    public List<SelectListItem> Products { get; set; } = [];
    public List<StockTransactionDto> Transactions { get; set; } = [];
}

public sealed class StockIndexVm
{
    public int? StoreId { get; set; }
    public List<SelectListItem> Stores { get; set; } = [];
    public List<ProductStockDetailsDto> Stocks { get; set; } = [];
}

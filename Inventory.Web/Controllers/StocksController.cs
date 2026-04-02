using Inventory.BLL;
using Inventory.Web.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Inventory.Web.Controllers;

public sealed class StocksController(IStoreService stores, IStockQueryService stockQuery) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(int? storeId, CancellationToken cancellationToken)
    {
        var storeList = await stores.GetAllAsync(cancellationToken);
        var selectedStoreId = storeId;

        if (!selectedStoreId.HasValue)
        {
            selectedStoreId = storeList.FirstOrDefault(x => x.Type == Inventory.Contract.StoreTypeDto.Warehouse)?.Id
                ?? storeList.FirstOrDefault()?.Id;
        }

        var items = new List<SelectListItem> { new() { Value = "", Text = "Select a store", Selected = !selectedStoreId.HasValue } };
        items.AddRange(storeList.Select(x => new SelectListItem(x.Name, x.Id.ToString(), selectedStoreId == x.Id)));

        var stocks = selectedStoreId.HasValue
            ? await stockQuery.GetStockDetailsByStoreAsync(selectedStoreId.Value, cancellationToken)
            : [];

        return View(new StockIndexVm
        {
            StoreId = selectedStoreId,
            Stores = items,
            Stocks = stocks
        });
    }
}

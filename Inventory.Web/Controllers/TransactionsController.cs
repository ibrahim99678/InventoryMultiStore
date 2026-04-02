using Inventory.BLL;
using Inventory.Contract;
using Inventory.Web.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Inventory.Web.Controllers;

public sealed class TransactionsController(ITransactionService transactions, IStoreService stores, IProductService products) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(int? storeId, int? productId, CancellationToken cancellationToken)
    {
        var tx = await transactions.GetTransactionsAsync(new TransactionFilter(storeId, productId, null, null), cancellationToken);

        var storeItems = await BuildStoreListAsync(storeId, cancellationToken);
        var productItems = await BuildProductListAsync(productId, cancellationToken);

        return View(new TransactionIndexVm
        {
            StoreId = storeId,
            ProductId = productId,
            Stores = storeItems,
            Products = productItems,
            Transactions = tx
        });
    }

    private async Task<List<SelectListItem>> BuildProductListAsync(int? selectedProductId, CancellationToken cancellationToken)
    {
        var list = await products.GetAllAsync(cancellationToken);
        var items = new List<SelectListItem> { new() { Value = "", Text = "All products", Selected = !selectedProductId.HasValue } };
        items.AddRange(list.Select(x => new SelectListItem(x.Name, x.Id.ToString(), selectedProductId == x.Id)));
        return items;
    }

    private async Task<List<SelectListItem>> BuildStoreListAsync(int? selectedStoreId, CancellationToken cancellationToken)
    {
        var list = await stores.GetAllAsync(cancellationToken);
        var items = new List<SelectListItem> { new() { Value = "", Text = "All stores", Selected = !selectedStoreId.HasValue } };
        items.AddRange(list.Select(x => new SelectListItem(x.Name, x.Id.ToString(), selectedStoreId == x.Id)));
        return items;
    }
}

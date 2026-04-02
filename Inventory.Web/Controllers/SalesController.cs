using Inventory.BLL;
using Inventory.Contract;
using Inventory.Web.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Inventory.Web.Controllers;

public sealed class SalesController(IProductService products, IStoreService stores, ISalesService sales) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Create(CancellationToken cancellationToken)
    {
        var model = new SaleVm
        {
            Stores = await BuildStoreListAsync(cancellationToken),
            Products = await BuildProductListAsync(cancellationToken)
        };
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(SaleVm model, CancellationToken cancellationToken)
    {
        model.Stores = await BuildStoreListAsync(cancellationToken);
        model.Products = await BuildProductListAsync(cancellationToken);

        if (!ModelState.IsValid || model.StoreId is null || model.ProductId is null)
        {
            return View(model);
        }

        var result = await sales.SellAsync(new SaleRequest(model.StoreId.Value, model.ProductId.Value, model.Quantity), cancellationToken);
        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.Error ?? "Sale failed.");
            return View(model);
        }

        TempData["Success"] = "Sale saved.";
        return RedirectToAction(nameof(Create));
    }

    private async Task<List<SelectListItem>> BuildProductListAsync(CancellationToken cancellationToken)
    {
        var list = await products.GetAllAsync(cancellationToken);
        var items = new List<SelectListItem> { new() { Value = "", Text = "Select a product" } };
        items.AddRange(list.Select(x => new SelectListItem(x.Name, x.Id.ToString())));
        return items;
    }

    private async Task<List<SelectListItem>> BuildStoreListAsync(CancellationToken cancellationToken)
    {
        var list = await stores.GetAllAsync(cancellationToken);
        var items = new List<SelectListItem> { new() { Value = "", Text = "Select a store" } };
        items.AddRange(list.Where(x => x.Type == StoreTypeDto.Store).Select(x => new SelectListItem(x.Name, x.Id.ToString())));
        return items;
    }
}

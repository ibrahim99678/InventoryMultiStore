using Inventory.BLL;
using Inventory.Contract;
using Inventory.Web.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Inventory.Web.Controllers;

public sealed class TransferController(IProductService products, IStoreService stores, ITransferService transfers) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Create(CancellationToken cancellationToken)
    {
        var model = new TransferVm
        {
            Products = await BuildProductListAsync(cancellationToken),
            Stores = await BuildStoreListAsync(cancellationToken)
        };
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(TransferVm model, CancellationToken cancellationToken)
    {
        model.Products = await BuildProductListAsync(cancellationToken);
        model.Stores = await BuildStoreListAsync(cancellationToken);

        if (!ModelState.IsValid || model.ProductId is null || model.ToStoreId is null)
        {
            return View(model);
        }

        var result = await transfers.TransferAsync(new TransferRequest(model.ProductId.Value, model.ToStoreId.Value, model.Quantity), cancellationToken);
        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.Error ?? "Transfer failed.");
            return View(model);
        }

        TempData["Success"] = "Transfer saved.";
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

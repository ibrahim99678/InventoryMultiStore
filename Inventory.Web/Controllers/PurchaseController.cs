using Inventory.BLL;
using Inventory.Contract;
using Inventory.Web.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Inventory.Web.Controllers;

public sealed class PurchaseController(IProductService products, IPurchaseService purchases) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Create(CancellationToken cancellationToken)
    {
        var model = new PurchaseVm
        {
            Products = await BuildProductListAsync(cancellationToken)
        };
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(PurchaseVm model, CancellationToken cancellationToken)
    {
        model.Products = await BuildProductListAsync(cancellationToken);

        if (!ModelState.IsValid || model.ProductId is null)
        {
            return View(model);
        }

        var result = await purchases.PurchaseAsync(new PurchaseRequest(model.ProductId.Value, model.Quantity), cancellationToken);
        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.Error ?? "Purchase failed.");
            return View(model);
        }

        TempData["Success"] = "Purchase saved.";
        return RedirectToAction(nameof(Create));
    }

    private async Task<List<SelectListItem>> BuildProductListAsync(CancellationToken cancellationToken)
    {
        var list = await products.GetAllAsync(cancellationToken);
        var items = new List<SelectListItem> { new() { Value = "", Text = "Select a product" } };
        items.AddRange(list.Select(x => new SelectListItem(x.Name, x.Id.ToString())));
        return items;
    }
}

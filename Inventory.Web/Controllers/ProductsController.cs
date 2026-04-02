using Inventory.BLL;
using Inventory.Contract;
using Inventory.Web.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace Inventory.Web.Controllers;

public sealed class ProductsController(IProductService products) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var list = await products.GetAllAsync(cancellationToken);
        return View(list);
    }

    [HttpGet]
    public IActionResult Create()
    {
        return View(new ProductCreateEditVm());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ProductCreateEditVm model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var result = await products.CreateAsync(new CreateProductRequest(model.Name, model.Price), cancellationToken);
        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.Error ?? "Failed to create product.");
            return View(model);
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken)
    {
        var result = await products.GetByIdAsync(id, cancellationToken);
        if (!result.Success || result.Data is null)
        {
            return NotFound();
        }

        return View(new ProductCreateEditVm
        {
            Id = result.Data.Id,
            Name = result.Data.Name,
            Price = result.Data.Price
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(ProductCreateEditVm model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var result = await products.UpdateAsync(new UpdateProductRequest(model.Id, model.Name, model.Price), cancellationToken);
        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.Error ?? "Failed to update product.");
            return View(model);
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        await products.DeleteAsync(id, cancellationToken);
        return RedirectToAction(nameof(Index));
    }
}

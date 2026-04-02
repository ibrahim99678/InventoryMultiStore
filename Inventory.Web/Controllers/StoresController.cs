using Inventory.BLL;
using Inventory.Contract;
using Inventory.Web.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace Inventory.Web.Controllers;

public sealed class StoresController(IStoreService stores) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var list = await stores.GetAllAsync(cancellationToken);
        return View(list);
    }

    [HttpGet]
    public IActionResult Create()
    {
        return View(new StoreCreateEditVm());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(StoreCreateEditVm model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var result = await stores.CreateAsync(new CreateStoreRequest(model.Name, model.Location, model.Type), cancellationToken);
        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.Error ?? "Failed to create store.");
            return View(model);
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken)
    {
        var result = await stores.GetByIdAsync(id, cancellationToken);
        if (!result.Success || result.Data is null)
        {
            return NotFound();
        }

        return View(new StoreCreateEditVm
        {
            Id = result.Data.Id,
            Name = result.Data.Name,
            Location = result.Data.Location,
            Type = result.Data.Type
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(StoreCreateEditVm model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var result = await stores.UpdateAsync(new UpdateStoreRequest(model.Id, model.Name, model.Location, model.Type), cancellationToken);
        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.Error ?? "Failed to update store.");
            return View(model);
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        await stores.DeleteAsync(id, cancellationToken);
        return RedirectToAction(nameof(Index));
    }
}

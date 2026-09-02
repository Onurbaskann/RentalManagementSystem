using KiraTakip.Authorization;
using KiraTakip.Infrastructure.Exceptions;
using KiraTakip.Models.Common;
using KiraTakip.Models.Dtos.Store;
using KiraTakip.Models.ViewModels;
using KiraTakip.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KiraTakip.Controllers;

[Authorize]
[Route("Admin/Store")]
public class AdminStoreController(IStoreService storeService) : Controller
{
    [HttpGet("")]
    [Authorize(Policy = PermissionCatalog.Store.Module)]
    public async Task<IActionResult> Index([FromQuery] TableQuery query)
    {
        ViewBag.Query = query;
        return View(await storeService.GetPagedListAsync(query));
    }

    [HttpGet("Create")]
    [Authorize(Policy = PermissionCatalog.Store.Module)]
    public IActionResult Create() => View(new StoreFormViewModel());

    [HttpPost("Create")]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = PermissionCatalog.Store.Create)]
    public async Task<IActionResult> Create(StoreFormViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        try
        {
            var id = await storeService.CreateAsync(
                new CreateStoreInput(model.Name, model.Description, model.IsActive));
            return RedirectToAction(nameof(Edit), new { id });
        }
        catch (BusinessValidationException exception)
        {
            ModelState.AddModelError(exception.Field, exception.Message);
            return View(model);
        }
    }

    [HttpGet("Edit/{id}")]
    [Authorize(Policy = PermissionCatalog.Store.Module)]
    public async Task<IActionResult> Edit(int id)
    {
        var viewModel = await BuildEditViewModelAsync(id);
        return viewModel == null ? NotFound() : View(viewModel);
    }

    [HttpPost("Edit/{id}")]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = PermissionCatalog.Store.Edit)]
    public async Task<IActionResult> Edit(int id, StoreFormViewModel model)
    {
        if (id != model.Id) return BadRequest();
        if (!ModelState.IsValid)
            return await EditViewAsync(id);

        try
        {
            await storeService.UpdateAsync(
                id,
                new UpdateStoreInput(model.Name, model.Description, model.IsActive));
        }
        catch (BusinessValidationException exception)
        {
            ModelState.AddModelError(exception.Field, exception.Message);
            return await EditViewAsync(id);
        }

        return RedirectToAction(nameof(Edit), new { id });
    }

    [HttpPost("ToggleStatus/{id}")]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = PermissionCatalog.Store.Edit)]
    public async Task<IActionResult> ToggleStatus(int id)
    {
        await storeService.ToggleStatusAsync(id);
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("ReplaceAccount/{id}")]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = PermissionCatalog.Store.Account)]
    public async Task<IActionResult> ReplaceAccount(int id, StoreAccountFormViewModel model)
    {
        if (id != model.StoreId) return BadRequest();
        if (!ModelState.IsValid)
            return await EditViewAsync(id, model, clearPassword: true);

        try
        {
            await storeService.ReplaceAccountAsync(new CreateStoreAccountVersionInput(
                id,
                model.ProviderCode,
                model.Currency,
                model.MerchantId,
                model.MerchantUser,
                model.MerchantPassword));
        }
        catch (BusinessValidationException exception)
        {
            ModelState.AddModelError(exception.Field, exception.Message);
            return await EditViewAsync(id, model, clearPassword: true);
        }

        return RedirectToAction(nameof(Edit), new { id });
    }

    [HttpPost("DeactivateAccount/{id}/{accountId}")]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = PermissionCatalog.Store.Account)]
    public async Task<IActionResult> DeactivateAccount(int id, int accountId)
    {
        await storeService.DeactivateAccountAsync(id, accountId);
        return RedirectToAction(nameof(Edit), new { id });
    }

    private async Task<StoreEditViewModel?> BuildEditViewModelAsync(
        int id,
        StoreAccountFormViewModel? accountForm = null,
        bool clearPassword = false)
    {
        var store = await storeService.GetDetailAsync(id);
        if (store == null) return null;

        accountForm ??= new StoreAccountFormViewModel { StoreId = id };
        accountForm.StoreId = id;
        if (clearPassword) accountForm.MerchantPassword = string.Empty;

        return new StoreEditViewModel
        {
            Store = store,
            AccountForm = accountForm
        };
    }

    private async Task<IActionResult> EditViewAsync(
        int id,
        StoreAccountFormViewModel? accountForm = null,
        bool clearPassword = false)
    {
        var viewModel = await BuildEditViewModelAsync(id, accountForm, clearPassword);
        return viewModel == null ? NotFound() : View(nameof(Edit), viewModel);
    }
}

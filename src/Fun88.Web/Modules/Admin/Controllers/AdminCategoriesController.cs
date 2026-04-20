namespace Fun88.Web.Modules.Admin.Controllers;

using Fun88.Web.Infrastructure.Data.Entities;
using Fun88.Web.Modules.Admin.ViewModels;
using Fun88.Web.Modules.Categories.Repositories;
using Fun88.Web.Shared.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Supabase;

[Area("Admin")]
[Route("admin/categories")]
[Authorize(Policy = PolicyNames.AdminOnly)]
public class AdminCategoriesController(
    ICategoryRepository categoryRepo,
    Client supabaseClient
) : Controller
{
    [HttpGet("")]
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        ViewData["Title"] = "Categories";
        var cats = await categoryRepo.GetAllCategoriesAsync(LanguageCode.English, ct);
        return View(cats);
    }

    [HttpGet("create")]
    public IActionResult Create()
    {
        ViewData["Title"] = "Add Category";
        return View("Form", new AdminCategoryFormViewModel { IsActive = true });
    }

    [HttpPost("create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(AdminCategoryFormViewModel model, CancellationToken ct)
    {
        if (!ModelState.IsValid) { ViewData["Title"] = "Add Category"; return View("Form", model); }

        var cat = new Category { Slug = model.Slug, Icon = model.Icon, SortOrder = model.SortOrder, IsActive = model.IsActive };
        await supabaseClient.From<Category>().Insert(cat, cancellationToken: ct);

        TempData["Success"] = "Category created.";
        return RedirectToAction("Index");
    }

    [HttpGet("edit/{id:int}")]
    public async Task<IActionResult> Edit(int id, CancellationToken ct)
    {
        var cat = await categoryRepo.GetByIdAsync(id, ct);
        if (cat is null) return NotFound();
        ViewData["Title"] = "Edit Category";
        return View("Form", new AdminCategoryFormViewModel
        {
            Id = cat.Id,
            Slug = cat.Slug,
            Icon = cat.Icon,
            SortOrder = cat.SortOrder,
            IsActive = cat.IsActive
        });
    }

    [HttpPost("edit/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, AdminCategoryFormViewModel model, CancellationToken ct)
    {
        if (!ModelState.IsValid) { ViewData["Title"] = "Edit Category"; return View("Form", model); }

        var cat = await categoryRepo.GetByIdAsync(id, ct);
        if (cat is null) return NotFound();

        cat.Slug = model.Slug;
        cat.Icon = model.Icon;
        cat.SortOrder = model.SortOrder;
        cat.IsActive = model.IsActive;
        await supabaseClient.From<Category>().Update(cat, cancellationToken: ct);

        TempData["Success"] = "Category updated.";
        return RedirectToAction("Index");
    }
}

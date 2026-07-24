using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using OrderHub.Core.Services;
using OrderHub.Web.ViewModels;

namespace OrderHub.Web.Controllers;

public class ProductsController : Controller
{
    private readonly IProductService _productService;

    public ProductsController(IProductService productService)
    {
        _productService = productService;
    }

    public async Task<IActionResult> Index()
    {
        var products = await _productService.GetAllAsync();

        var vm = new ProductListViewModel
        {
            Products = products.Select(p => new ProductRowViewModel
            {
                Sku = p.Sku,
                Name = p.Name,
                UnitPrice = p.UnitPrice,
                StockQuantity = p.StockQuantity,
                IsActive = p.IsActive
            }).ToList()
        };

        return View(vm);
    }

    public async Task<IActionResult> LowStock(
        [Range(1, int.MaxValue, ErrorMessage = "門檻必須大於 0")] int threshold = 10)
    {
        var vm = new LowStockViewModel { Threshold = threshold };

        if (!ModelState.IsValid)
            return View(vm);

        var products = await _productService.GetLowStockAsync(threshold);
        vm.Products = products.Select(p => new LowStockProductRowViewModel
        {
            Sku = p.Sku,
            Name = p.Name,
            StockQuantity = p.StockQuantity,
            SoldQuantityLast30Days = p.SoldQuantityLast30Days
        }).ToList();

        return View(vm);
    }
}


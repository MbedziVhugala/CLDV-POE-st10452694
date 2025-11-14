using ABCRetailers.Models;
using ABCRetailers.Services;
using ABCRetailers.Functions.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;

namespace ABCRetailers.Controllers
{
    public class ProductController : Controller
    {
        private readonly IAzureStorageService _storageService;
        private readonly IFunctionsService _functionsService;
        private readonly IConfiguration _configuration;
        private readonly ILogger<ProductController> _logger;
        private readonly bool _useFunctions;

        public ProductController(IAzureStorageService storageService, IFunctionsService functionsService,
                        IConfiguration configuration, ILogger<ProductController> logger)
        {
            _storageService = storageService;
            _functionsService = functionsService;
            _configuration = configuration;
            _logger = logger;
            _useFunctions = _configuration.GetValue<bool>("UseFunctions", false);

            // ADD THIS DEBUGGING:
            Console.WriteLine($"=== PRODUCT CONTROLLER INITIALIZED ===");
            Console.WriteLine($"UseFunctions: {_useFunctions}");
            Console.WriteLine($"FunctionsService is null: {_functionsService == null}");
        }

        // GET: Product/Index
        public async Task<IActionResult> Index()
        {
            try
            {
                if (_useFunctions)
                {
                    var response = await _functionsService.GetProductsAsync();
                    if (response.Success)
                    {
                        var products = response.Data?.Select(apiModel => new Product
                        {
                            RowKey = apiModel.ProductId,
                            ProductName = apiModel.ProductName,
                            Description = apiModel.Description,
                            Price = apiModel.Price,
                            StockAvailable = apiModel.StockAvailable,
                            ImageUrl = apiModel.ImageUrl
                        }).ToList() ?? new List<Product>();

                        return View(products);
                    }
                }

                var directProducts = await _storageService.GetAllEntitiesAsync<Product>();
                return View(directProducts);
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error retrieving products: {ex.Message}";
                return View(new List<Product>());
            }
        }

        // GET: Product/Create
        public IActionResult Create()
        {
            Console.WriteLine("=== CREATE PRODUCT PAGE LOADED ===");
            return View();
        }

        // POST: Product/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Product product, IFormFile? imageFile)
        {
            Console.WriteLine("=== CREATE PRODUCT STARTED ===");
            Console.WriteLine($"Product Name: {product.ProductName}");
            Console.WriteLine($"Product Price: {product.Price}");
            Console.WriteLine($"Product Stock: {product.StockAvailable}");
            Console.WriteLine($"Image File: {imageFile?.FileName ?? "No image"}");

            try
            {
                // Check ModelState before validation
                Console.WriteLine("Checking ModelState...");
                foreach (var state in ModelState)
                {
                    Console.WriteLine($"Key: {state.Key}, Value: {state.Value.RawValue}, IsValid: {state.Value.ValidationState}");
                    foreach (var error in state.Value.Errors)
                    {
                        Console.WriteLine($"  Error: {error.ErrorMessage}");
                    }
                }

                if (ModelState.IsValid)
                {
                    Console.WriteLine("ModelState is valid, proceeding...");

                    if (product.Price <= 0)
                    {
                        Console.WriteLine("Price validation failed");
                        ModelState.AddModelError("Price", "Price must be greater than $0.00");
                        return View(product);
                    }

                    Console.WriteLine("Price validation passed");

                    // Upload image if provided
                    string imageUrl = string.Empty;
                    if (imageFile != null && imageFile.Length > 0)
                    {
                        Console.WriteLine("Starting image upload...");
                        try
                        {
                            imageUrl = await _storageService.UploadImageAsync(imageFile, "product-images");
                            Console.WriteLine($"Image uploaded successfully: {imageUrl}");
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Image upload failed: {ex.Message}");
                            throw;
                        }
                    }
                    else
                    {
                        Console.WriteLine("No image provided");
                    }

                    product.ImageUrl = imageUrl;

                    if (_useFunctions)
                    {
                        Console.WriteLine("Using Functions...");
                        var apiModel = new ProductApiModel
                        {
                            ProductId = product.RowKey,
                            ProductName = product.ProductName,
                            Description = product.Description,
                            Price = product.Price,
                            StockAvailable = product.StockAvailable,
                            ImageUrl = imageUrl
                        };

                        var response = await _functionsService.CreateProductAsync(apiModel);
                        if (response.Success)
                        {
                            TempData["Success"] = $"Product '{product.ProductName}' created successfully via Functions!";
                            return RedirectToAction(nameof(Index));
                        }
                        throw new Exception(response.Message);
                    }
                    else
                    {
                        Console.WriteLine("Using direct storage...");
                        try
                        {
                            Console.WriteLine("Calling AddEntityAsync...");
                            await _storageService.AddEntityAsync(product);
                            Console.WriteLine("Product saved to storage successfully!");

                            TempData["Success"] = $"Product '{product.ProductName}' created successfully!";
                            return RedirectToAction(nameof(Index));
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"AddEntityAsync failed: {ex.Message}");
                            Console.WriteLine($"Stack trace: {ex.StackTrace}");
                            throw;
                        }
                    }
                }
                else
                {
                    Console.WriteLine("ModelState is invalid");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"FATAL ERROR: {ex.Message}");
                Console.WriteLine($"FULL STACK TRACE: {ex.StackTrace}");
                _logger.LogError(ex, "Error creating product");
                ModelState.AddModelError("", $"Error creating product: {ex.Message}");
            }

            Console.WriteLine("Returning to Create view");
            return View(product);
        }

        // GET: Product/Edit/{id}
        public async Task<IActionResult> Edit(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return NotFound();
            }

            try
            {
                Product product;

                if (_useFunctions)
                {
                    var response = await _functionsService.GetProductAsync(id);
                    if (!response.Success || response.Data == null)
                    {
                        return NotFound();
                    }
                    var apiModel = response.Data;
                    product = new Product
                    {
                        RowKey = apiModel.ProductId,
                        ProductName = apiModel.ProductName,
                        Description = apiModel.Description,
                        Price = apiModel.Price,
                        StockAvailable = apiModel.StockAvailable,
                        ImageUrl = apiModel.ImageUrl
                    };
                }
                else
                {
                    product = await _storageService.GetEntityAsync<Product>("Product", id);
                    if (product == null)
                    {
                        return NotFound();
                    }
                }

                return View(product);
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error retrieving product: {ex.Message}";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: Product/Edit/{id}
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Product product, IFormFile? imageFile)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    // Upload new image if provided
                    if (imageFile != null && imageFile.Length > 0)
                    {
                        var imageUrl = await _storageService.UploadImageAsync(imageFile, "product-images");
                        product.ImageUrl = imageUrl;
                    }

                    if (_useFunctions)
                    {
                        var apiModel = new ProductApiModel
                        {
                            ProductId = product.RowKey,
                            ProductName = product.ProductName,
                            Description = product.Description,
                            Price = product.Price,
                            StockAvailable = product.StockAvailable,
                            ImageUrl = product.ImageUrl
                        };

                        var response = await _functionsService.UpdateProductAsync(product.RowKey, apiModel);
                        if (response.Success)
                        {
                            TempData["Success"] = "Product updated successfully via Functions!";
                            return RedirectToAction(nameof(Index));
                        }
                        throw new Exception(response.Message);
                    }
                    else
                    {
                        await _storageService.UpdateEntityAsync(product);
                        TempData["Success"] = "Product updated successfully!";
                        return RedirectToAction(nameof(Index));
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error updating product: {Message}", ex.Message);
                    ModelState.AddModelError("", $"Error updating product: {ex.Message}");
                }
            }
            return View(product);
        }

        // GET: Product/Details/{id}
        public async Task<IActionResult> Details(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return NotFound();
            }

            try
            {
                Product product;

                if (_useFunctions)
                {
                    var response = await _functionsService.GetProductAsync(id);
                    if (!response.Success || response.Data == null)
                    {
                        return NotFound();
                    }
                    var apiModel = response.Data;
                    product = new Product
                    {
                        RowKey = apiModel.ProductId,
                        ProductName = apiModel.ProductName,
                        Description = apiModel.Description,
                        Price = apiModel.Price,
                        StockAvailable = apiModel.StockAvailable,
                        ImageUrl = apiModel.ImageUrl
                    };
                }
                else
                {
                    product = await _storageService.GetEntityAsync<Product>("Product", id);
                    if (product == null)
                    {
                        return NotFound();
                    }
                }

                return View(product);
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error retrieving product: {ex.Message}";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: Product/Delete/{id}
        [HttpPost]
        public async Task<IActionResult> Delete(string id)
        {
            try
            {
                if (_useFunctions)
                {
                    var response = await _functionsService.DeleteProductAsync(id);
                    if (response.Success)
                    {
                        TempData["Success"] = "Product deleted successfully via Functions!";
                    }
                    else
                    {
                        TempData["Error"] = $"Error deleting product via Functions: {response.Message}";
                    }
                }
                else
                {
                    await _storageService.DeleteEntityAsync<Product>("Product", id);
                    TempData["Success"] = "Product deleted successfully!";
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error deleting product: {ex.Message}";
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
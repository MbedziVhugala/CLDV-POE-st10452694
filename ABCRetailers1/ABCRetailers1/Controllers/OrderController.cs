using ABCRetailers.Models;
using ABCRetailers.Models.ViewModels;
using ABCRetailers.Services;
using ABCRetailers.Functions.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System.Text.Json;

namespace ABCRetailers.Controllers
{
    public class OrderController : Controller
    {
        private readonly IAzureStorageService _storageService;
        private readonly IFunctionsService _functionsService;
        private readonly IConfiguration _configuration;
        private readonly bool _useFunctions;

        public OrderController(IAzureStorageService storageService, IFunctionsService functionsService, IConfiguration configuration)
        {
            _storageService = storageService;
            _functionsService = functionsService;
            _configuration = configuration;
            _useFunctions = _configuration.GetValue<bool>("UseFunctions", false);
        }

        // GET: Order/Index
        public async Task<IActionResult> Index()
        {
            try
            {
                if (_useFunctions)
                {
                    var response = await _functionsService.GetOrdersAsync();
                    if (response.Success)
                    {
                        var orders = response.Data?.Select(apiModel => new Order
                        {
                            RowKey = apiModel.OrderId,
                            CustomerId = apiModel.CustomerId,
                            Username = apiModel.Username,
                            ProductId = apiModel.ProductId,
                            ProductName = apiModel.ProductName,
                            OrderDate = apiModel.OrderDate,
                            Quantity = apiModel.Quantity,
                            UnitPrice = apiModel.UnitPrice,
                            TotalPrice = apiModel.TotalPrice,
                            Status = apiModel.Status
                        }).ToList() ?? new List<Order>();

                        return View(orders);
                    }
                }

                var directOrders = await _storageService.GetAllEntitiesAsync<Order>();
                return View(directOrders);
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error retrieving orders: {ex.Message}";
                return View(new List<Order>());
            }
        }

        // GET: Order/Create
        public async Task<IActionResult> Create()
        {
            var customers = await _storageService.GetAllEntitiesAsync<Customer>();
            var products = await _storageService.GetAllEntitiesAsync<Product>();

            var viewModel = new OrderCreateViewModel
            {
                Customers = customers,
                Products = products
            };

            return View(viewModel);
        }

        // POST: Order/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(OrderCreateViewModel model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    // Get customer and product details
                    var customer = await _storageService.GetEntityAsync<Customer>("Customer", model.CustomerId);
                    var product = await _storageService.GetEntityAsync<Product>("Product", model.ProductId);

                    if (customer == null || product == null)
                    {
                        ModelState.AddModelError("", "Invalid customer or product selected.");
                        await PopulateDropdowns(model);
                        return View(model);
                    }

                    // Check stock availability
                    if (product.StockAvailable < model.Quantity)
                    {
                        ModelState.AddModelError("Quantity", $"Insufficient stock. Available: {product.StockAvailable}");
                        await PopulateDropdowns(model);
                        return View(model);
                    }

                    if (_useFunctions)
                    {
                        var orderRequest = new CreateOrderRequest
                        {
                            CustomerId = model.CustomerId,
                            ProductId = model.ProductId,
                            Quantity = model.Quantity
                        };

                        var response = await _functionsService.CreateOrderAsync(orderRequest);
                        if (response.Success)
                        {
                            TempData["Success"] = "Order created successfully via Functions!";
                            return RedirectToAction(nameof(Index));
                        }
                        throw new Exception(response.Message);
                    }
                    else
                    {
                        // Create order using direct storage
                        var order = new Order
                        {
                            CustomerId = model.CustomerId,
                            Username = customer.Username,
                            ProductId = model.ProductId,
                            ProductName = product.ProductName,
                            OrderDate = model.OrderDate,
                            Quantity = model.Quantity,
                            UnitPrice = product.Price,
                            TotalPrice = product.Price * model.Quantity,
                            Status = "Submitted"
                        };

                        await _storageService.AddEntityAsync(order);

                        // Update product stock
                        product.StockAvailable -= model.Quantity;
                        await _storageService.UpdateEntityAsync(product);

                        TempData["Success"] = "Order created successfully!";
                        return RedirectToAction(nameof(Index));
                    }
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", $"Error creating order: {ex.Message}");
                }
            }

            await PopulateDropdowns(model);
            return View(model);
        }

        // GET: Order/Details/{id}
        public async Task<IActionResult> Details(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return NotFound();
            }

            try
            {
                Order order;

                if (_useFunctions)
                {
                    var response = await _functionsService.GetOrderAsync(id);
                    if (!response.Success || response.Data == null)
                    {
                        return NotFound();
                    }
                    var apiModel = response.Data;
                    order = new Order
                    {
                        RowKey = apiModel.OrderId,
                        CustomerId = apiModel.CustomerId,
                        Username = apiModel.Username,
                        ProductId = apiModel.ProductId,
                        ProductName = apiModel.ProductName,
                        OrderDate = apiModel.OrderDate,
                        Quantity = apiModel.Quantity,
                        UnitPrice = apiModel.UnitPrice,
                        TotalPrice = apiModel.TotalPrice,
                        Status = apiModel.Status
                    };
                }
                else
                {
                    order = await _storageService.GetEntityAsync<Order>("Order", id);
                    if (order == null)
                    {
                        return NotFound();
                    }
                }

                return View(order);
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error retrieving order: {ex.Message}";
                return RedirectToAction(nameof(Index));
            }
        }

        // GET: Order/Edit/{id}
        public async Task<IActionResult> Edit(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return NotFound();
            }

            try
            {
                var order = await _storageService.GetEntityAsync<Order>("Order", id);
                if (order == null)
                {
                    return NotFound();
                }

                return View(order);
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error retrieving order: {ex.Message}";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: Order/Edit/{id}
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Order order)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    if (_useFunctions)
                    {
                        var response = await _functionsService.UpdateOrderStatusAsync(order.RowKey, order.Status);
                        if (response.Success)
                        {
                            TempData["Success"] = "Order status updated successfully via Functions!";
                            return RedirectToAction(nameof(Index));
                        }
                        throw new Exception(response.Message);
                    }
                    else
                    {
                        await _storageService.UpdateEntityAsync(order);
                        TempData["Success"] = "Order updated successfully!";
                        return RedirectToAction(nameof(Index));
                    }
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", $"Error updating order: {ex.Message}");
                }
            }
            return View(order);
        }

        // POST: Order/Delete/{id}
        [HttpPost]
        public async Task<IActionResult> Delete(string id)
        {
            try
            {
                if (_useFunctions)
                {
                    var response = await _functionsService.DeleteOrderAsync(id);
                    if (response.Success)
                    {
                        TempData["Success"] = "Order deleted successfully via Functions!";
                    }
                    else
                    {
                        TempData["Error"] = $"Error deleting order via Functions: {response.Message}";
                    }
                }
                else
                {
                    await _storageService.DeleteEntityAsync<Order>("Order", id);
                    TempData["Success"] = "Order deleted successfully!";
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error deleting order: {ex.Message}";
            }
            return RedirectToAction(nameof(Index));
        }

        // AJAX method for real-time price/stock checking
        [HttpGet]
        public async Task<JsonResult> GetProductPrice(string productId)
        {
            try
            {
                var product = await _storageService.GetEntityAsync<Product>("Product", productId);
                if (product != null)
                {
                    return Json(new
                    {
                        success = true,
                        price = product.Price,
                        stock = product.StockAvailable,
                        productName = product.ProductName
                    });
                }
                return Json(new { success = false });
            }
            catch
            {
                return Json(new { success = false });
            }
        }

        [HttpPost]
        public async Task<JsonResult> UpdateOrderStatus(string id, string newStatus)
        {
            try
            {
                var order = await _storageService.GetEntityAsync<Order>("Order", id);
                if (order == null)
                {
                    return Json(new { success = false, message = "Order not found" });
                }

                var previousStatus = order.Status;
                order.Status = newStatus;

                if (_useFunctions)
                {
                    var response = await _functionsService.UpdateOrderStatusAsync(id, newStatus);
                    if (!response.Success)
                    {
                        return Json(new { success = false, message = response.Message });
                    }
                }
                else
                {
                    await _storageService.UpdateEntityAsync(order);
                }

                return Json(new { success = true, message = $"Order status updated to {newStatus}" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        private async Task PopulateDropdowns(OrderCreateViewModel model)
        {
            model.Customers = await _storageService.GetAllEntitiesAsync<Customer>();
            model.Products = await _storageService.GetAllEntitiesAsync<Product>();
        }
    }
}
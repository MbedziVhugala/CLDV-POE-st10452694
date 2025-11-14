using ABCRetailers.Models;
using ABCRetailers.Services;
using ABCRetailers.Functions.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

namespace ABCRetailers.Controllers
{
    public class CustomerController : Controller
    {
        private readonly IAzureStorageService _storageService;
        private readonly IFunctionsService _functionsService;
        private readonly IConfiguration _configuration;
        private readonly bool _useFunctions;

        public CustomerController(IAzureStorageService storageService, IFunctionsService functionsService, IConfiguration configuration)
        {
            _storageService = storageService;
            _functionsService = functionsService;
            _configuration = configuration;
            _useFunctions = _configuration.GetValue<bool>("UseFunctions", false);
        }

        public async Task<IActionResult> Index()
        {
            try
            {
                if (_useFunctions)
                {
                    var response = await _functionsService.GetCustomersAsync();
                    if (response.Success)
                    {
                        // Convert API models to your entity models
                        var customers = response.Data?.Select(apiModel => new Customer
                        {
                            RowKey = apiModel.CustomerId,
                            Name = apiModel.Name,
                            Surname = apiModel.Surname,
                            Username = apiModel.Username,
                            Email = apiModel.Email,
                            ShippingAddress = apiModel.ShippingAddress
                        }).ToList() ?? new List<Customer>();

                        return View(customers);
                    }
                    // Fall back to direct storage if Functions fail
                }

                // Use direct storage
                var directCustomers = await _storageService.GetAllEntitiesAsync<Customer>();
                return View(directCustomers);
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error retrieving customers: {ex.Message}";
                return View(new List<Customer>());
            }
        }

        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Customer customer)
        {
            Console.WriteLine("=== CREATE CUSTOMER STARTED ===");
            Console.WriteLine($"UseFunctions: {_useFunctions}");

            if (ModelState.IsValid)
            {
                try
                {
                    if (_useFunctions)
                    {
                        Console.WriteLine("Calling Functions API...");

                        var apiModel = new CustomerApiModel
                        {
                            CustomerId = customer.RowKey,
                            Name = customer.Name,
                            Surname = customer.Surname,
                            Username = customer.Username,
                            Email = customer.Email,
                            ShippingAddress = customer.ShippingAddress
                        };

                        var response = await _functionsService.CreateCustomerAsync(apiModel);
                        if (response.Success)
                        {
                            TempData["Success"] = "Customer created successfully via Functions!";
                            return RedirectToAction(nameof(Index));
                        }
                        throw new Exception(response.Message);
                    }
                    else
                    {
                        Console.WriteLine("Using direct storage...");
                        await _storageService.AddEntityAsync(customer);
                        Console.WriteLine("Customer created via direct storage");
                        TempData["Success"] = "Customer created successfully!";
                        return RedirectToAction(nameof(Index));
                    }
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", $"Error creating customer: {ex.Message}");
                }
            }

            // RETURN THIS LINE WAS MISSING - this fixes the error
            return View(customer);
        }

        public async Task<IActionResult> Edit(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return NotFound();
            }

            try
            {
                Customer customer;

                if (_useFunctions)
                {
                    var response = await _functionsService.GetCustomerAsync(id);
                    if (!response.Success || response.Data == null)
                    {
                        return NotFound();
                    }
                    var apiModel = response.Data;
                    customer = new Customer
                    {
                        RowKey = apiModel.CustomerId,
                        Name = apiModel.Name,
                        Surname = apiModel.Surname,
                        Username = apiModel.Username,
                        Email = apiModel.Email,
                        ShippingAddress = apiModel.ShippingAddress
                    };
                }
                else
                {
                    customer = await _storageService.GetEntityAsync<Customer>("Customer", id);
                    if (customer == null)
                    {
                        return NotFound();
                    }
                }

                return View(customer);
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error retrieving customer: {ex.Message}";
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Customer customer)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    if (_useFunctions)
                    {
                        var apiModel = new CustomerApiModel
                        {
                            CustomerId = customer.RowKey,
                            Name = customer.Name,
                            Surname = customer.Surname,
                            Username = customer.Username,
                            Email = customer.Email,
                            ShippingAddress = customer.ShippingAddress
                        };

                        var response = await _functionsService.UpdateCustomerAsync(customer.RowKey, apiModel);
                        if (response.Success)
                        {
                            TempData["Success"] = "Customer updated successfully via Functions!";
                            return RedirectToAction(nameof(Index));
                        }
                        throw new Exception(response.Message);
                    }
                    else
                    {
                        await _storageService.UpdateEntityAsync(customer);
                        TempData["Success"] = "Customer updated successfully!";
                        return RedirectToAction(nameof(Index));
                    }
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", $"Error updating customer: {ex.Message}");
                }
            }
            return View(customer);
        }

        [HttpPost]
        public async Task<IActionResult> Delete(string id)
        {
            try
            {
                if (_useFunctions)
                {
                    var response = await _functionsService.DeleteCustomerAsync(id);
                    if (response.Success)
                    {
                        TempData["Success"] = "Customer deleted successfully via Functions!";
                    }
                    else
                    {
                        TempData["Error"] = $"Error deleting customer via Functions: {response.Message}";
                    }
                }
                else
                {
                    await _storageService.DeleteEntityAsync<Customer>("Customer", id);
                    TempData["Success"] = "Customer deleted successfully!";
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error deleting customer: {ex.Message}";
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
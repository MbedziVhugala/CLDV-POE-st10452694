using ABCRetailers.Functions.Entities;
using ABCRetailers.Functions.Models;

namespace ABCRetailers.Functions.Helpers
{
    public static class Mapper
    {
        public static CustomerApiModel ToApiModel(this CustomerEntity entity)
        {
            return new CustomerApiModel
            {
                CustomerId = entity.RowKey,
                Name = entity.Name,
                Surname = entity.Surname,
                Username = entity.Username,
                Email = entity.Email,
                ShippingAddress = entity.ShippingAddress
            };
        }

        public static ProductApiModel ToApiModel(this ProductEntity entity)
        {
            return new ProductApiModel
            {
                ProductId = entity.RowKey,
                ProductName = entity.ProductName,
                Description = entity.Description,
                Price = entity.Price,
                StockAvailable = entity.StockAvailable,
                ImageUrl = entity.ImageUrl
            };
        }

        public static OrderApiModel ToApiModel(this OrderEntity entity)
        {
            return new OrderApiModel
            {
                OrderId = entity.RowKey,
                CustomerId = entity.CustomerId,
                Username = entity.Username,
                ProductId = entity.ProductId,
                ProductName = entity.ProductName,
                OrderDate = entity.OrderDate,
                Quantity = entity.Quantity,
                UnitPrice = entity.UnitPrice,
                TotalPrice = entity.TotalPrice,
                Status = entity.Status
            };
        }

        public static CustomerEntity ToEntity(this CustomerApiModel model)
        {
            return new CustomerEntity
            {
                RowKey = model.CustomerId,
                Name = model.Name,
                Surname = model.Surname,
                Username = model.Username,
                Email = model.Email,
                ShippingAddress = model.ShippingAddress
            };
        }

        public static ProductEntity ToEntity(this ProductApiModel model)
        {
            return new ProductEntity
            {
                RowKey = model.ProductId,
                ProductName = model.ProductName,
                Description = model.Description,
                PriceString = model.Price.ToString("F2"),
                StockAvailable = model.StockAvailable,
                ImageUrl = model.ImageUrl
            };
        }

        public static OrderEntity ToEntity(this OrderApiModel model)
        {
            return new OrderEntity
            {
                RowKey = model.OrderId,
                CustomerId = model.CustomerId,
                Username = model.Username,
                ProductId = model.ProductId,
                ProductName = model.ProductName,
                OrderDate = model.OrderDate,
                Quantity = model.Quantity,
                UnitPrice = model.UnitPrice,
                TotalPrice = model.TotalPrice,
                Status = model.Status
            };
        }
    }
}
using Mapster;
using MapsterMapper;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using RefactoringChallenge.Entities;
using RefactoringChallenge.Models;
using RefactoringChallenge.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace RefactoringChallenge.Tests.Services
{
    public class OrdersServiceTests
    {
        #region Variables

        public readonly NorthwindDbContext _dbContext;
        public readonly Mapper _mapper;
        public readonly OrdersService _ordersService;

        #endregion

        #region Constructor 
        public OrdersServiceTests()
        {
            // Arrange
            // GetOrder - GetOrderById 
            // Implement the in memory database instead of the real time data and store the data from json file.

            var options = new DbContextOptionsBuilder<NorthwindDbContext>()
                .UseInMemoryDatabase(databaseName: "NorthwindTest")
                .Options;
            _dbContext = new NorthwindDbContext(options);
            _dbContext.Orders.AddRange(GetOrdersLists("Orders.json"));
            _dbContext.SaveChangesAsync();

            TypeAdapterConfig<Order, OrderResponse>.NewConfig();
            _mapper = new Mapper(TypeAdapterConfig.GlobalSettings);

            _ordersService = new OrdersService(_dbContext, _mapper);
        }
        #endregion

        #region Test Cases

        [Fact]
        public async Task GetOrder_ReturnsOrders_WhenSkipAndTakeNotSpecified()
        {
            // Act
            var result = await _ordersService.GetOrder();

            // Assert
            Assert.NotNull(result);
            Assert.Equal(832, result.Count());
        }

        [Fact]
        public async Task GetOrder_ReturnsOrders_WhenSkipAndTake()
        {
            // Arrange
            var skip = 11;
            var take = 200;

            // Act
            var result = await _ordersService.GetOrder(skip, take);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(200, result.Count());
            Assert.Equal("ERNSH", result.First().CustomerId);
        }

        [Fact]
        public async Task GetOrder_ShouldThrowException_WhenSkipOrTakeIsNegative()
        {
            // Arrange
            var skip = -1;
            var take = -1;

            // Act & Assert
            var result = await _ordersService.GetOrder(skip, take);

            try
            {
                var teat = result.First().CustomerId;
            }
            catch (Exception ex)
            {
                Assert.Equal("Sequence contains no elements", ex.Message);
            }
        }

        [Fact]
        public async Task GetOrderById_ReturnsOrders_ExistingOrderId()
        {
            // Arrange
            var orderId = 10248;

            // Act
            var result = await _ordersService.GetOrderById(orderId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("VINET", result.CustomerId);
            Assert.Equal("Vins et alcools Chevalier", result.ShipName);
            Assert.Equal("59 rue de l'Abbaye", result.ShipAddress);
        }

        [Fact]
        public async Task GetOrderById_ReturnsOrders_NonExistingOrderId()
        {
            // Act
            var result = await _ordersService.GetOrderById(0);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task CreateOrder_ValidOrderRequest_ReturnsOrderResponse()
        {
            // Arrange
            var optionsManipulation = new DbContextOptionsBuilder<NorthwindDbContext>()
                .UseInMemoryDatabase(databaseName: "CreateOrder")
                .Options;
            var _dbContextManipulation = new NorthwindDbContext(optionsManipulation);
            _dbContextManipulation.SaveChangesAsync();

            TypeAdapterConfig<Order, OrderResponse>.NewConfig();
            var _mapperManipulation = new Mapper(TypeAdapterConfig.GlobalSettings);

            var _ordersServiceManipulation = new OrdersService(_dbContextManipulation, _mapperManipulation);


            var orderRequest = new OrderRequest
            {
                customerId = "ALFKI",
                employeeId = 1,
                requiredDate = new DateTime(2023, 3, 30),
                shipVia = 1,
                freight = 12.34m,
                shipName = "Ship Name",
                shipAddress = "Ship Address",
                shipCity = "Ship City",
                shipRegion = "Ship Region",
                shipPostalCode = "12345",
                shipCountry = "Ship Country",
                orderDetails = new List<OrderDetailRequest>
                {
                    new OrderDetailRequest
                    {
                        ProductId = 1,
                        Discount = 0,
                        Quantity = 10,
                        UnitPrice = 9.99m,
                    }
                }
            };

            // Act
            var result = await _ordersServiceManipulation.CreateOrder(orderRequest);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(orderRequest.customerId, result.CustomerId);
            Assert.Equal(orderRequest.employeeId, result.EmployeeId);
            Assert.Equal(orderRequest.requiredDate, result.RequiredDate);
            Assert.Equal(orderRequest.shipVia, result.ShipVia);
            Assert.Equal(orderRequest.freight, result.Freight);
            Assert.Equal(orderRequest.shipName, result.ShipName);
            Assert.Equal(orderRequest.shipAddress, result.ShipAddress);
            Assert.Equal(orderRequest.shipCity, result.ShipCity);
            Assert.Equal(orderRequest.shipRegion, result.ShipRegion);
            Assert.Equal(orderRequest.shipPostalCode, result.ShipPostalCode);
            Assert.Equal(orderRequest.shipCountry, result.ShipCountry);
            Assert.Single(result.OrderDetails);
        }

        [Fact]
        public async Task AddProductsToOrder_ExistingOrderIdAndValidOrderDetails_OrderDetailsAddedToOrder()
        {
            // Arrange
            var optionsManipulation = new DbContextOptionsBuilder<NorthwindDbContext>()
                .UseInMemoryDatabase(databaseName: "AddProductsToOrder")
                .Options;
            var _dbContextManipulation = new NorthwindDbContext(optionsManipulation);
            _dbContextManipulation.Orders.AddRange(GetOrdersLists("SingleOrder.json"));
            _dbContextManipulation.SaveChangesAsync();

            TypeAdapterConfig<Order, OrderResponse>.NewConfig();
            var _mapperManipulation = new Mapper(TypeAdapterConfig.GlobalSettings);

            var _ordersServiceManipulation = new OrdersService(_dbContextManipulation, _mapperManipulation);

            var existingOrderId = 1;
            var orderDetails = new List<OrderDetailRequest>
            {
                new OrderDetailRequest
                {
                    ProductId = 3,
                    Discount = 0,
                    Quantity = 10,
                    UnitPrice = 9.99m,
                },
                new OrderDetailRequest
                {
                    ProductId = 4,
                    Discount = 2,
                    Quantity = 20,
                    UnitPrice = 19.99m,
                },
            };

            // Act
            var result = await _ordersServiceManipulation.AddProductsToOrder(existingOrderId, orderDetails);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(existingOrderId, result.FirstOrDefault(x => x.OrderId == 1)?.OrderId);
            Assert.Equal(2, result.Count());
            Assert.Equal(orderDetails[0].ProductId, result.FirstOrDefault(x => x.ProductId == 3)?.ProductId);
            Assert.Equal(orderDetails[0].Discount, result.FirstOrDefault(x => x.Discount == 0)?.Discount);
            Assert.Equal(orderDetails[0].Quantity, result.FirstOrDefault(x => x.Quantity == 10)?.Quantity);
            Assert.Equal(orderDetails[0].UnitPrice, result.FirstOrDefault(x => x.UnitPrice == 9.99m)?.UnitPrice);
        }

        [Fact]
        public async void DeleteOrder_ExistingOrderId_DeletesOrderAndOrderDetails()
        {
            // Arrange
            var optionsManipulation = new DbContextOptionsBuilder<NorthwindDbContext>()
                .UseInMemoryDatabase(databaseName: "DeleteOrder")
                .Options;
            var _dbContextManipulation = new NorthwindDbContext(optionsManipulation);
            _dbContextManipulation.Orders.AddRange(GetOrdersLists("SingleOrder.json"));
            _dbContextManipulation.SaveChangesAsync();

            TypeAdapterConfig<Order, OrderResponse>.NewConfig();
            var _mapperManipulation = new Mapper(TypeAdapterConfig.GlobalSettings);

            var _ordersServiceManipulation = new OrdersService(_dbContextManipulation, _mapperManipulation);

            var orderIdToDelete = 1;
            var order = _dbContextManipulation.Orders.FirstOrDefault(o => o.OrderId == orderIdToDelete);
            Assert.NotNull(order);

            // Act
            var result = await _ordersServiceManipulation.DeleteOrder(orderIdToDelete);

            // Assert
            Assert.True(Convert.ToBoolean(result));
        }

        [Fact]
        public async void DeleteOrder_NonExistingOrderId_ReturnsFalse()
        {
            // Arrange
            var nonExistingOrderId = 999;

            // Act
            var result = await _ordersService.DeleteOrder(nonExistingOrderId);

            // Assert
            Assert.False(Convert.ToBoolean(result));
        }
        #endregion

        #region GetOrdersLists
        private static List<Order> GetOrdersLists(string FileName)
        {
            string jsonString = File.ReadAllText(Path.Combine(Directory.GetCurrentDirectory(), FileName));
            List<Order> ordersList = JsonConvert.DeserializeObject<List<Order>>(jsonString);
            return ordersList;
        }
        #endregion

    }
}

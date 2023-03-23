using Mapster;
using MapsterMapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using RefactoringChallenge.Controllers;
using RefactoringChallenge.Entities;
using RefactoringChallenge.Models;
using RefactoringChallenge.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace RefactoringChallenge.Tests.Controllers
{
    public class OrdersControllerTests
    {
        #region Variables

        public readonly NorthwindDbContext _dbContext;
        public readonly Mapper _mapper;
        public readonly OrdersService _ordersService;
        public readonly OrdersController _ordersController;

        #endregion

        #region Constructor 
        public OrdersControllerTests()
        {
            // Arrange
            // GetOrder - GetOrderById

            var options = new DbContextOptionsBuilder<NorthwindDbContext>()
                .UseInMemoryDatabase(databaseName: "NorthwindTest")
                .Options;
            _dbContext = new NorthwindDbContext(options);
            _dbContext.Orders.AddRange(GetOrdersLists("Orders.json"));
            _dbContext.SaveChangesAsync();

            TypeAdapterConfig<Order, OrderResponse>.NewConfig();
            _mapper = new Mapper(TypeAdapterConfig.GlobalSettings);

            _ordersService = new OrdersService(_dbContext, _mapper);

            _ordersController = new OrdersController(_ordersService);
        }
        #endregion

        #region Test Cases
        [Fact]
        public async Task GetOrder_ReturnsOrders_WhenSkipAndTakeNotSpecified()
        {
            // Act
            var result = await _ordersController.Get() as OkObjectResult;
            var items = Assert.IsType<List<OrderResponse>>(result.Value);

            // Assert
            Assert.NotNull(result);
            Assert.IsType<OkObjectResult>(result as OkObjectResult);
            Assert.Equal(832, items.Count());
        }

        [Fact]
        public async Task GetOrder_ReturnsOrders_WhenSkipAndTake()
        {
            // Arrange
            var skip = 11;
            var take = 200;

            // Act
            var result = await _ordersController.Get(skip, take) as OkObjectResult;
            var items = Assert.IsType<List<OrderResponse>>(result.Value);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(200, items.Count());
            Assert.Equal("ERNSH", items.First().CustomerId);
        }

        [Fact]
        public async Task GetOrder_ShouldThrowException_WhenSkipOrTakeIsNegative()
        {
            // Arrange
            var skip = -1;
            var take = -1;

            // Act
            var result = await _ordersController.Get(skip, take) as OkObjectResult;
            var items = Assert.IsType<List<OrderResponse>>(result.Value);

            try
            {
                var teat = items.First().CustomerId;
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
            var result = await _ordersController.GetById(orderId) as OkObjectResult;
            var items = Assert.IsType<OrderResponse>(result.Value);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("VINET", items.CustomerId);
            Assert.Equal("Vins et alcools Chevalier", items.ShipName);
            Assert.Equal("59 rue de l'Abbaye", items.ShipAddress);
        }

        [Fact]
        public async Task GetOrderById_ReturnsOrders_NonExistingOrderId()
        {
            // Act
            var result = await _ordersController.GetById(0) as OkObjectResult;

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
            //_dbContextManipulation.Orders.AddRange(GetOrdersLists("SingleOrder.json"));
            _dbContextManipulation.SaveChangesAsync();

            TypeAdapterConfig<Order, OrderResponse>.NewConfig();
            var _mapperManipulation = new Mapper(TypeAdapterConfig.GlobalSettings);

            var _ordersServiceManipulation = new OrdersService(_dbContextManipulation, _mapperManipulation);
            var _ordersControllerManipulation = new OrdersController(_ordersServiceManipulation);

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
            var result = await _ordersControllerManipulation.Create(orderRequest) as CreatedAtActionResult;
            var items = Assert.IsType<OrderResponse>(result.Value);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(orderRequest.customerId, items.CustomerId);
            Assert.Equal(orderRequest.employeeId, items.EmployeeId);
            Assert.Equal(orderRequest.requiredDate, items.RequiredDate);
            Assert.Equal(orderRequest.shipVia, items.ShipVia);
            Assert.Equal(orderRequest.freight, items.Freight);
            Assert.Equal(orderRequest.shipName, items.ShipName);
            Assert.Equal(orderRequest.shipAddress, items.ShipAddress);
            Assert.Equal(orderRequest.shipCity, items.ShipCity);
            Assert.Equal(orderRequest.shipRegion, items.ShipRegion);
            Assert.Equal(orderRequest.shipPostalCode, items.ShipPostalCode);
            Assert.Equal(orderRequest.shipCountry, items.ShipCountry);
            Assert.Single(items.OrderDetails);
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
            var _ordersControllerManipulation = new OrdersController(_ordersServiceManipulation);

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
            var result = await _ordersControllerManipulation.AddProductsToOrder(existingOrderId, orderDetails);

            // Assert
            Assert.NotNull(result);
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
            var _ordersControllerManipulation = new OrdersController(_ordersServiceManipulation);

            var orderIdToDelete = 1;
            var order = _dbContextManipulation.Orders.FirstOrDefault(o => o.OrderId == orderIdToDelete);
            Assert.NotNull(order);

            // Act
            var result = await _ordersControllerManipulation.Delete(orderIdToDelete);
            
            // Assert
            Assert.IsType<OkResult>(result as OkResult);
        }

        [Fact]
        public async void DeleteOrder_NonExistingOrderId_ReturnsFalse()
        {
            // Arrange
            var nonExistingOrderId = 999;

            // Act
            var result = await _ordersController.Delete(nonExistingOrderId);

            // Assert
            Assert.IsType<NotFoundResult>(result as NotFoundResult);
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

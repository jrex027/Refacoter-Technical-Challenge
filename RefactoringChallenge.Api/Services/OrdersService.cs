using Mapster;
using MapsterMapper;
using Microsoft.EntityFrameworkCore;
using RefactoringChallenge.Entities;
using RefactoringChallenge.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RefactoringChallenge.Services
{
    #region IOrdersService
    public interface IOrdersService
    {
        Task<IEnumerable<OrderResponse>> GetOrder(int? skip = null, int? take = null);
        Task<OrderResponse> GetOrderById(int orderId);
        Task<OrderResponse> CreateOrder(OrderRequest orderRequest);
        Task<IEnumerable<OrderDetailResponse>> AddProductsToOrder(int orderId, IEnumerable<OrderDetailRequest> orderDetails);
        Task<bool> DeleteOrder(int orderId);
    }
    #endregion

    #region OrdersService
    
    public class OrdersService : IOrdersService
    {
        private readonly NorthwindDbContext _northwindDbContext;
        private readonly IMapper _mapper;

        public OrdersService(NorthwindDbContext northwindDbContext, IMapper mapper)
        {
            _northwindDbContext = northwindDbContext;
            _mapper = mapper;
        }

        public async Task<IEnumerable<OrderResponse>> GetOrder(int? skip = null, int? take = null)
        {
            // The AsQueryable method is called on the _northwindDbContext.Orders property
            var query = _northwindDbContext.Orders.AsQueryable();
            if (skip != null)
            {
                query = query.Skip(skip.Value);
            }
            if (take != null)
            {
                query = query.Take(take.Value);
            }
            // Map the response to the related model class 
            var result = await _mapper.From(query).ProjectToType<OrderResponse>().ToListAsync();
            return result;
        }

        public async Task<OrderResponse> GetOrderById(int orderId)
        {
            var result = _mapper.From(_northwindDbContext.Orders).ProjectToType<OrderResponse>().FirstOrDefault(o => o.OrderId == orderId);
            return await Task.FromResult(result);
        }

        public async Task<OrderResponse> CreateOrder(OrderRequest orderRequest)
        {
            // Use LinQ instead of ForLoop 
            var newOrderDetails = orderRequest.orderDetails.Select(od => new OrderDetail
            {
                ProductId = od.ProductId,
                Discount = od.Discount,
                Quantity = od.Quantity,
                UnitPrice = od.UnitPrice,
            }).ToList();

            var newOrder = new Order
            {
                CustomerId = orderRequest.customerId,
                EmployeeId = orderRequest.employeeId,
                OrderDate = DateTime.Now,
                RequiredDate = orderRequest.requiredDate,
                ShipVia = orderRequest.shipVia,
                Freight = orderRequest.freight,
                ShipName = orderRequest.shipName,
                ShipAddress = orderRequest.shipAddress,
                ShipCity = orderRequest.shipCity,
                ShipRegion = orderRequest.shipRegion,
                ShipPostalCode = orderRequest.shipPostalCode,
                ShipCountry = orderRequest.shipCountry,
                OrderDetails = newOrderDetails,
            };
            _northwindDbContext.Orders.Add(newOrder);
            _northwindDbContext.SaveChanges();

            return await Task.FromResult(newOrder.Adapt<OrderResponse>());
        }

        public async Task<IEnumerable<OrderDetailResponse>> AddProductsToOrder(int orderId, IEnumerable<OrderDetailRequest> orderDetails)
        {
            var order = _northwindDbContext.Orders.FirstOrDefault(o => o.OrderId == orderId);
            if (order == null)
                return null;

            var newOrderDetails = new List<OrderDetail>();
            foreach (var orderDetail in orderDetails)
            {
                newOrderDetails.Add(new OrderDetail
                {
                    OrderId = orderId,
                    ProductId = orderDetail.ProductId,
                    Discount = orderDetail.Discount,
                    Quantity = orderDetail.Quantity,
                    UnitPrice = orderDetail.UnitPrice,
                });
            }

            _northwindDbContext.OrderDetails.AddRange(newOrderDetails);
            _northwindDbContext.SaveChanges();

            return await Task.FromResult(newOrderDetails.Select(od => od.Adapt<OrderDetailResponse>()).ToList());
        }

        public async Task<bool> DeleteOrder(int orderId)
        {
            var order = await _northwindDbContext.Orders.FirstOrDefaultAsync(o => o.OrderId == orderId);

            if (order == null)
                return false;

            var orderDetails = _northwindDbContext.OrderDetails.Where(od => od.OrderId == orderId);
            _northwindDbContext.OrderDetails.RemoveRange(orderDetails);
            _northwindDbContext.Orders.Remove(order);
            _northwindDbContext.SaveChanges();

            return true;
        }
    }
    #endregion
}

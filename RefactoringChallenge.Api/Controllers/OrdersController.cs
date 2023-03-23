using Microsoft.AspNetCore.Mvc;
using RefactoringChallenge.Models;
using RefactoringChallenge.Services;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace RefactoringChallenge.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class OrdersController : Controller
    {
        // Implement the Service Layer
        private readonly IOrdersService _ordersService;

        public OrdersController(IOrdersService ordersService)
        {
            _ordersService = ordersService;
        }

        [HttpGet]
        public async Task<IActionResult> Get(int? skip = null, int? take = null)
        {
            try
            {
                var orders = await _ordersService.GetOrder(skip, take);
                return Ok(orders);
            }
            catch (Exception ex) 
            {
                // Send the error response with 500 status code
                return StatusCode(500, ex.Message);
            }
        }


        [HttpGet("{orderId}")]
        public async Task<IActionResult> GetById([FromRoute] int orderId)
        {
            try
            {
                var order = await _ordersService.GetOrderById(orderId);
                if (order == null)
                    return NotFound();
                // Implement Http status code instead of JsonResult
                return Ok(order);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        [HttpPost("[action]")]
        public async Task<IActionResult> Create(OrderRequest orderRequest)
        {
            try
            {
                var order = await _ordersService.CreateOrder(orderRequest);
                return CreatedAtAction(nameof(GetById), new { orderId = order.OrderId }, order);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        [HttpPost("{orderId}/[action]")]
        public async Task<IActionResult> AddProductsToOrder([FromRoute] int orderId, IEnumerable<OrderDetailRequest> orderDetails)
        {
            try
            {
                var orderDetailsResponses = await _ordersService.AddProductsToOrder(orderId, orderDetails);
                return Ok(orderDetailsResponses);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        [HttpDelete("{orderId}/[action]")]
        public async Task<IActionResult> Delete([FromRoute] int orderId)
        {
            try
            {
                var result = await _ordersService.DeleteOrder(orderId);
                if (result)
                    return Ok();
                else
                    return NotFound();
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }
    }
}

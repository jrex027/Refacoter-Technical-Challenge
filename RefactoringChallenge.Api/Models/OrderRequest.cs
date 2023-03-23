using System;
using System.Collections.Generic;

namespace RefactoringChallenge.Models
{
    public class OrderRequest
    {
        public string customerId { get; set; }
        public int? employeeId { get; set; }
        public DateTime? requiredDate { get; set; }
        public int? shipVia { get; set; }
        public decimal? freight { get; set; }
        public string shipName { get; set; }
        public string shipAddress { get; set; }
        public string shipCity { get; set; }
        public string shipRegion { get; set; }
        public string shipPostalCode { get; set; }
        public string shipCountry { get; set; }
        public IEnumerable<OrderDetailRequest> orderDetails { get; set; }
    }
}

using FoodEmolite.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FoodEmolite.Application.DTOs.Order
{
    public class UpdatePaymentStatusRequestDto
    {
        public PaymentStatus NewStatus { get; set; }
        public string? ChangedNote { get; set; }
    }
}

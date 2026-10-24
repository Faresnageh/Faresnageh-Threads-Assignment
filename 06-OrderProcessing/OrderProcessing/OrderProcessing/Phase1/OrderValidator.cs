using OrderProcessing.Shared.Models;

namespace OrderProcessing.Phase1
{
    public class OrderValidator
    {
        public async Task ValidateOrderAsync(Order order, CancellationToken ct)
        {
            CustomerRepository customerRepository = new();
            Customer? customer = await customerRepository.GetByIdAsync(order.CustomerId, ct);
            if (customer is null)
            {
                order.Status = OrderStatus.Rejected;
                order.FailureReason = $"Customer #{order.CustomerId} not found";
                return;
            }

            if (customer.IsActive == false)
            {
                order.Status = OrderStatus.Rejected;
                order.FailureReason = "Customer is inactive";
                return;
            }


            ProductRepository productRepository = new ProductRepository();
            foreach (var item in order.Items)
            {
                Product? product = await productRepository.GetByIdAsync(item.ProductId, ct);
                if (product is null)
                {
                    order.Status = OrderStatus.Rejected;
                    order.FailureReason = $"Product #{item.ProductId} not found";
                    return;
                }
            }



            foreach (var item in order.Items)
            {
                Product? product = await productRepository.GetByIdAsync(item.ProductId, ct);
                if (product is not null && product.StockQuantity < item.Quantity)
                {
                    order.Status = OrderStatus.Rejected;
                    order.FailureReason = $"Insufficient stock for '{product.Name}'";
                    return;
                }
            }


            CouponRepository couponRepository = new();
            if (order.CouponCode is not null)
            {
                Coupon? coupon = await couponRepository.GetByCodeAsync(order.CouponCode, ct);
                if (coupon is null)
                {
                    order.Status = OrderStatus.Rejected;
                    order.FailureReason = $"Coupon '{order.CouponCode}' does not exist";
                    return;
                }
                if (coupon.ExpiryDate < DateTime.Today)
                {
                    order.Status = OrderStatus.Rejected;
                    order.FailureReason = $"Coupon '{coupon.Code}' has expired";
                    return;
                }
            }


            order.Status = OrderStatus.Validated;
            order.FailureReason = null;
        }
    }
}

using OrderProcessing.Shared.Models;

namespace OrderProcessing.Phase1
{
    public class SyncOrderValidator
    {
        public void ValidateOrderSync(Order order)
        {
            SyncCustomerRepository syncCustomerRepository = new();
            Customer? customer = syncCustomerRepository.GetByIdSync(order.CustomerId);
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


            SyncProductRepository SyncProductRepository = new ();
            foreach (var item in order.Items)
            {
                Product? product = SyncProductRepository.GetByIdSync(item.ProductId);
                if (product is null)
                {
                    order.Status = OrderStatus.Rejected;
                    order.FailureReason = $"Product #{item.ProductId} not found";
                    return;
                }
            }



            foreach (var item in order.Items)
            {
                Product? product = SyncProductRepository.GetByIdSync(item.ProductId);
                if (product is not null && product.StockQuantity < item.Quantity)
                {
                    order.Status = OrderStatus.Rejected;
                    order.FailureReason = $"Insufficient stock for '{product.Name}'";
                    return;
                }
            }


            SyncCouponRepository syncCouponRepository = new();
            if (order.CouponCode is not null)
            {
                Coupon? coupon = syncCouponRepository.GetByCodeSync(order.CouponCode);
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

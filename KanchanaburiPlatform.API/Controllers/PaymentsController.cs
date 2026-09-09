using API.DTOs;
using Stripe;


namespace API.Controllers;

[ApiController]
[Route("api/payments")]
public sealed class PaymentsController(IUnitOfWork unit, IConfiguration configuration) : ControllerBase
{
    [HttpPost("orders/{orderId:guid}/intent"), Authorize]
    public async Task<ActionResult<CreatePaymentIntentResponse>> CreateIntent(Guid orderId)
    {
        var order = await unit.Repository<Order>().GetByIdAsync(orderId);
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (order == null || order.UserId != userId) return NotFound();
        if (order.PaymentStatus == "Paid") return BadRequest("Order is already paid");

        var secretKey = configuration["Stripe:SecretKey"];
        if (string.IsNullOrWhiteSpace(secretKey))
            return Problem("Stripe:SecretKey is not configured", statusCode: StatusCodes.Status503ServiceUnavailable);
        StripeConfiguration.ApiKey = secretKey;
        var service = new PaymentIntentService();
        var intent = await service.CreateAsync(new PaymentIntentCreateOptions
        {
            Amount = (long)Math.Round(order.TotalAmount * 100, MidpointRounding.AwayFromZero),
            Currency = "thb",
            AutomaticPaymentMethods = new PaymentIntentAutomaticPaymentMethodsOptions { Enabled = true },
            Metadata = new Dictionary<string, string> { ["orderId"] = order.OrderId.ToString() }
        });

        var payment = new Payment { PaymentId = Guid.NewGuid(), OrderId = order.OrderId, PaymentMethod = "Stripe", TransactionRef = intent.Id, Amount = order.TotalAmount, PaymentStatus = "Pending", CreatedAt = DateTime.UtcNow };
        unit.Repository<Payment>().Add(payment);
        await unit.Complete();
        return Ok(new CreatePaymentIntentResponse(intent.ClientSecret, intent.Id));
    }

    [HttpPost("webhook")]
    public async Task<IActionResult> Webhook()
    {
        var json = await new StreamReader(HttpContext.Request.Body).ReadToEndAsync();
        Event stripeEvent;
        var webhookSecret = configuration["Stripe:WebhookSecret"];
        if (string.IsNullOrWhiteSpace(webhookSecret)) return Problem("Stripe:WebhookSecret is not configured", statusCode: StatusCodes.Status503ServiceUnavailable);
        try { stripeEvent = EventUtility.ConstructEvent(json, Request.Headers["Stripe-Signature"], webhookSecret); }
        catch (StripeException) { return BadRequest(); }

        if (stripeEvent.Type == "payment_intent.payment_failed")
        {
            var failedIntent = stripeEvent.Data.Object as PaymentIntent;
            var failedPayment = failedIntent == null ? null : (await unit.Repository<Payment>().ListAllAsync()).FirstOrDefault(x => x.TransactionRef == failedIntent.Id);
            if (failedPayment != null && failedPayment.PaymentStatus == "Pending")
            {
                failedPayment.PaymentStatus = "Failed";
                unit.Repository<Payment>().Update(failedPayment);
                await unit.Complete();
            }
            return Ok();
        }

        if (stripeEvent.Type != "payment_intent.succeeded") return Ok();
        var intent = stripeEvent.Data.Object as PaymentIntent;
        if (intent == null) return Ok();
        var payment = (await unit.Repository<Payment>().ListAllAsync()).FirstOrDefault(x => x.TransactionRef == intent.Id);
        if (payment == null || payment.PaymentStatus == "Paid") return Ok();
        var order = await unit.Repository<Order>().GetByIdAsync(payment.OrderId);
        if (order == null) return Ok();
        payment.PaymentStatus = "Paid"; payment.PaidAt = DateTime.UtcNow; order.PaymentStatus = "Paid";
        unit.Repository<Payment>().Update(payment); unit.Repository<Order>().Update(order);
        await unit.Complete();
        return Ok();
    }

    [HttpPost("orders/{orderId:guid}/sync"), Authorize]
    public async Task<IActionResult> SyncPayment(Guid orderId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var order = await unit.Repository<Order>().GetByIdAsync(orderId);
        if (order == null || order.UserId != userId) return NotFound();
        if (order.PaymentStatus == "Paid") return Ok();

        var payment = (await unit.Repository<Payment>().ListAllAsync())
            .Where(x => x.OrderId == orderId && x.PaymentMethod == "Stripe")
            .OrderByDescending(x => x.CreatedAt)
            .FirstOrDefault();
        if (payment == null || string.IsNullOrWhiteSpace(payment.TransactionRef)) return BadRequest("Stripe payment was not found");

        var secretKey = configuration["Stripe:SecretKey"];
        if (string.IsNullOrWhiteSpace(secretKey)) return Problem("Stripe:SecretKey is not configured", statusCode: StatusCodes.Status503ServiceUnavailable);
        StripeConfiguration.ApiKey = secretKey;
        var intent = await new PaymentIntentService().GetAsync(payment.TransactionRef);
        if (intent.Status != "succeeded") return BadRequest("Payment is not complete");

        payment.PaymentStatus = "Paid";
        payment.PaidAt = DateTime.UtcNow;
        order.PaymentStatus = "Paid";
        unit.Repository<Payment>().Update(payment);
        unit.Repository<Order>().Update(order);
        return await unit.Complete() ? Ok() : BadRequest("Unable to update payment status");
    }

    [HttpGet("orders/summary"), Authorize]
    public async Task<ActionResult<BatchOrderSummaryDto>> GetOrdersSummary([FromQuery] string orderIds)
    {
        if (string.IsNullOrWhiteSpace(orderIds)) return BadRequest("orderIds is required");
        var ids = orderIds.Split(',').Select(s => Guid.TryParse(s.Trim(), out var g) ? g : Guid.Empty).Where(g => g != Guid.Empty).ToList();
        if (ids.Count == 0) return BadRequest("Invalid orderIds");

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var orders = (await unit.Repository<Order>().ListAllAsync())
            .Where(x => ids.Contains(x.OrderId) && x.UserId == userId)
            .ToList();

        if (orders.Count == 0) return NotFound();

        var shopIds = orders.Select(x => x.ShopId).ToHashSet();
        var allShops = (await unit.Repository<Shop>().ListAllAsync()).Where(x => shopIds.Contains(x.ShopId)).ToDictionary(x => x.ShopId);

        var items = orders.Select(o => new OrderSummaryItemDto
        {
            OrderId = o.OrderId,
            OrderNumber = o.OrderNumber,
            ShopId = o.ShopId,
            ShopName = allShops.TryGetValue(o.ShopId, out var shop) ? shop.ShopName : null,
            Subtotal = o.Subtotal,
            ShippingFee = o.ShippingFee,
            TotalAmount = o.TotalAmount,
            PaymentStatus = o.PaymentStatus
        }).ToList();

        return Ok(new BatchOrderSummaryDto
        {
            Orders = items,
            TotalAmount = items.Sum(x => x.TotalAmount),
            IsAllPaid = items.All(x => x.PaymentStatus == "Paid")
        });
    }

    [HttpPost("batch/intent"), Authorize]
    public async Task<ActionResult<CreatePaymentIntentResponse>> CreateBatchIntent(BatchPaymentRequestDto dto)
    {
        if (dto.OrderIds == null || dto.OrderIds.Count == 0) return BadRequest("No order IDs provided");

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var allOrders = (await unit.Repository<Order>().ListAllAsync())
            .Where(x => dto.OrderIds.Contains(x.OrderId) && x.UserId == userId)
            .ToList();

        if (allOrders.Count != dto.OrderIds.Distinct().Count())
            return BadRequest("One or more orders were not found");

        var unpaidOrders = allOrders.Where(x => x.PaymentStatus != "Paid").ToList();
        if (unpaidOrders.Count == 0) return BadRequest("All orders are already paid");

        var totalAmount = unpaidOrders.Sum(x => x.TotalAmount);
        var orderIdsStr = string.Join(",", unpaidOrders.Select(x => x.OrderId));

        var secretKey = configuration["Stripe:SecretKey"];
        if (string.IsNullOrWhiteSpace(secretKey))
            return Problem("Stripe:SecretKey is not configured", statusCode: StatusCodes.Status503ServiceUnavailable);

        StripeConfiguration.ApiKey = secretKey;
        var service = new PaymentIntentService();
        var intent = await service.CreateAsync(new PaymentIntentCreateOptions
        {
            Amount = (long)Math.Round(totalAmount * 100, MidpointRounding.AwayFromZero),
            Currency = "thb",
            AutomaticPaymentMethods = new PaymentIntentAutomaticPaymentMethodsOptions { Enabled = true },
            Metadata = new Dictionary<string, string> { ["orderIds"] = orderIdsStr }
        });

        foreach (var order in unpaidOrders)
        {
            var payment = new Payment
            {
                PaymentId = Guid.NewGuid(),
                OrderId = order.OrderId,
                PaymentMethod = "Stripe",
                TransactionRef = intent.Id,
                Amount = order.TotalAmount,
                PaymentStatus = "Pending",
                CreatedAt = DateTime.UtcNow
            };
            unit.Repository<Payment>().Add(payment);
        }

        await unit.Complete();
        return Ok(new CreatePaymentIntentResponse(intent.ClientSecret, intent.Id));
    }

    [HttpPost("batch/sync"), Authorize]
    public async Task<IActionResult> SyncBatchPayment(BatchPaymentRequestDto dto)
    {
        if (dto.OrderIds == null || dto.OrderIds.Count == 0) return BadRequest("No order IDs provided");

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var orders = (await unit.Repository<Order>().ListAllAsync())
            .Where(x => dto.OrderIds.Contains(x.OrderId) && x.UserId == userId)
            .ToList();

        if (orders.Count == 0) return NotFound("Orders not found");
        var unpaidOrders = orders.Where(x => x.PaymentStatus != "Paid").ToList();
        if (unpaidOrders.Count == 0) return Ok();

        var unpaidOrderIds = unpaidOrders.Select(x => x.OrderId).ToHashSet();
        var payments = (await unit.Repository<Payment>().ListAllAsync())
            .Where(x => unpaidOrderIds.Contains(x.OrderId) && x.PaymentMethod == "Stripe")
            .OrderByDescending(x => x.CreatedAt)
            .ToList();

        var paymentWithRef = payments.FirstOrDefault(x => !string.IsNullOrWhiteSpace(x.TransactionRef));
        if (paymentWithRef == null) return BadRequest("Stripe payment transaction reference not found");

        var secretKey = configuration["Stripe:SecretKey"];
        if (string.IsNullOrWhiteSpace(secretKey)) return Problem("Stripe:SecretKey is not configured", statusCode: StatusCodes.Status503ServiceUnavailable);

        StripeConfiguration.ApiKey = secretKey;
        var intent = await new PaymentIntentService().GetAsync(paymentWithRef.TransactionRef);
        if (intent.Status != "succeeded") return BadRequest("Payment is not complete");

        var now = DateTime.UtcNow;
        foreach (var order in unpaidOrders)
        {
            order.PaymentStatus = "Paid";
            unit.Repository<Order>().Update(order);
        }

        var relatedPayments = (await unit.Repository<Payment>().ListAllAsync())
            .Where(x => x.TransactionRef == paymentWithRef.TransactionRef)
            .ToList();

        foreach (var p in relatedPayments)
        {
            p.PaymentStatus = "Paid";
            p.PaidAt = now;
            unit.Repository<Payment>().Update(p);
        }

        return await unit.Complete() ? Ok() : BadRequest("Unable to update payment status");
    }
}

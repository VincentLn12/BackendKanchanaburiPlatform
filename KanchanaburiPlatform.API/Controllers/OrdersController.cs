using API.DTOs;

namespace API.Controllers;

[ApiController, Authorize]
[Route("api/orders")]
public sealed class OrdersController(IUnitOfWork unit) : ControllerBase
{
    [HttpPost("checkout")]
    public async Task<ActionResult<IReadOnlyList<OrderDto>>> Checkout(CheckoutDto dto)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
        var cart = (await unit.Repository<Cart>().ListAllAsync()).FirstOrDefault(x => x.UserId == userId);
        if (cart == null) return BadRequest("Cart not found");
        var cartItems = (await unit.Repository<CartItem>().ListAllAsync()).Where(x => x.CartId == cart.CartId).ToList();
        if (cartItems.Count == 0) return BadRequest("Cart is empty");

        if (dto.ShippingMethod is not ("Delivery" or "Pickup")) return BadRequest("Invalid shipping method");
        if (dto.ShippingMethod == "Delivery" && (string.IsNullOrWhiteSpace(dto.ReceiverName) || string.IsNullOrWhiteSpace(dto.ReceiverPhone) || string.IsNullOrWhiteSpace(dto.ShippingAddress)))
            return BadRequest("Receiver name, phone, and shipping address are required for delivery");

        var products = new Dictionary<Guid, Product>();
        foreach (var item in cartItems)
        {
            var product = await unit.Repository<Product>().GetByIdAsync(item.ProductId);
            if (product == null || product.Status != "Active")
                return BadRequest("Cart contains an unavailable product");
            products[item.ProductId] = product;
        }

        foreach (var item in cartItems)
        {
            if (products[item.ProductId].Quantity < item.Quantity)
                return BadRequest($"Insufficient stock for product: {products[item.ProductId].ProductName}");
        }

        var itemsByShop = cartItems.GroupBy(x => products[x.ProductId].ShopId).ToList();
        var allShops = (await unit.Repository<Shop>().ListAllAsync()).ToDictionary(x => x.ShopId);

        foreach (var shopGroup in itemsByShop)
        {
            if (!allShops.TryGetValue(shopGroup.Key, out var shop) || shop.Status != "Active")
                return BadRequest("Cart contains a product from an unavailable shop");
        }

        var createdOrders = new List<Order>();

        await unit.BeginTransactionAsync();
        try
        {
            var baseTimestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
            var shopIndex = 0;

            foreach (var shopGroup in itemsByShop)
            {
                shopIndex++;
                var shopId = shopGroup.Key;
                var shopCartItems = shopGroup.ToList();
                var subtotal = shopCartItems.Sum(x => products[x.ProductId].Price * x.Quantity);
                var shippingFee = dto.ShippingMethod == "Pickup" ? 0 : (dto.ShippingFee > 0 ? dto.ShippingFee : 50m);
                var orderNumber = $"ORD-{baseTimestamp}-{Random.Shared.Next(1000, 9999)}{(itemsByShop.Count > 1 ? $"-{shopIndex}" : "")}";

                var order = new Order
                {
                    OrderId = Guid.NewGuid(),
                    UserId = userId,
                    ShopId = shopId,
                    OrderNumber = orderNumber,
                    Subtotal = subtotal,
                    ShippingFee = shippingFee,
                    TotalAmount = subtotal + shippingFee,
                    ShippingMethod = dto.ShippingMethod,
                    ReceiverName = dto.ReceiverName,
                    ReceiverPhone = dto.ReceiverPhone,
                    ShippingAddress = dto.ShippingAddress,
                    CreatedAt = DateTime.UtcNow
                };
                unit.Repository<Order>().Add(order);

                foreach (var item in shopCartItems)
                {
                    var product = products[item.ProductId];
                    product.Quantity -= item.Quantity;
                    product.UpdatedAt = DateTime.UtcNow;
                    unit.Repository<Product>().Update(product);

                    var unitPrice = product.Price;
                    unit.Repository<OrderItem>().Add(new OrderItem
                    {
                        OrderItemId = Guid.NewGuid(),
                        OrderId = order.OrderId,
                        ProductId = item.ProductId,
                        ProductName = product.ProductName,
                        Quantity = item.Quantity,
                        UnitPrice = unitPrice,
                        TotalPrice = unitPrice * item.Quantity
                    });
                    unit.Repository<CartItem>().Remove(item);
                }

                createdOrders.Add(order);
            }

            if (!await unit.Complete())
            {
                await unit.RollbackTransactionAsync();
                return BadRequest("Checkout failed");
            }

            await unit.CommitTransactionAsync();

            var orderDtos = createdOrders.Select(o =>
            {
                var dto = ToDto(o);
                if (allShops.TryGetValue(o.ShopId, out var shop))
                {
                    dto.ShopName = shop.ShopName;
                    dto.ShopLogoUrl = shop.CoverImageUrl;
                }
                return dto;
            }).ToList();

            return Ok(orderDtos);
        }
        catch
        {
            await unit.RollbackTransactionAsync();
            throw;
        }
    }

    [HttpGet("mine")]
    public async Task<IReadOnlyList<OrderDto>> MyOrders()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
        var orders = (await unit.Repository<Order>().ListAllAsync())
            .Where(x => x.UserId == userId)
            .OrderByDescending(x => x.CreatedAt)
            .ToList();

        if (orders.Count == 0) return [];

        var orderIds = orders.Select(x => x.OrderId).ToHashSet();
        var shopIds = orders.Select(x => x.ShopId).ToHashSet();
        var allItems = (await unit.Repository<OrderItem>().ListAllAsync()).Where(x => orderIds.Contains(x.OrderId)).ToList();
        var allShipments = (await unit.Repository<Shipment>().ListAllAsync()).Where(x => orderIds.Contains(x.OrderId)).ToDictionary(x => x.OrderId);
        var allShops = (await unit.Repository<Shop>().ListAllAsync()).Where(x => shopIds.Contains(x.ShopId)).ToDictionary(x => x.ShopId);
        var productIds = allItems.Select(x => x.ProductId).ToHashSet();
        var allProducts = (await unit.Repository<Product>().ListAllAsync()).Where(x => productIds.Contains(x.ProductId)).ToDictionary(x => x.ProductId);

        return orders.Select(o =>
        {
            var dto = ToDto(o);
            if (allShops.TryGetValue(o.ShopId, out var shop))
            {
                dto.ShopName = shop.ShopName;
                dto.ShopLogoUrl = shop.CoverImageUrl;
            }
            if (allShipments.TryGetValue(o.OrderId, out var shipment))
            {
                dto.Shipment = new ShipmentDto
                {
                    ShippingProvider = shipment.ShippingProvider,
                    TrackingNumber = shipment.TrackingNumber,
                    ShippingStatus = shipment.ShippingStatus,
                    ShippedAt = shipment.ShippedAt,
                    DeliveredAt = shipment.DeliveredAt
                };
            }
            dto.Items = allItems.Where(i => i.OrderId == o.OrderId).Select(i => new OrderItemDto
            {
                OrderItemId = i.OrderItemId,
                ProductId = i.ProductId,
                ProductName = i.ProductName,
                Quantity = i.Quantity,
                UnitPrice = i.UnitPrice,
                TotalPrice = i.TotalPrice,
                ImageUrl = allProducts.TryGetValue(i.ProductId, out var prod) ? prod.ImageUrl : null
            }).ToList();
            return dto;
        }).ToList();
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<OrderDetailDto>> GetOrder(Guid id)
    {
        var order = await unit.Repository<Order>().GetByIdAsync(id);
        if (order == null) return NotFound();
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var shop = await unit.Repository<Shop>().GetByIdAsync(order.ShopId);
        var canView = order.UserId == userId || User.IsInRole("Admin") || shop?.OwnerUserId == userId;
        if (!canView) return Forbid();
        
        var orderItems = (await unit.Repository<OrderItem>().ListAllAsync()).Where(x => x.OrderId == id).ToList();
        var productIds = orderItems.Select(x => x.ProductId).ToHashSet();
        var products = (await unit.Repository<Product>().ListAllAsync()).Where(x => productIds.Contains(x.ProductId)).ToDictionary(x => x.ProductId);

        var items = orderItems.Select(x => new OrderItemDto
        {
            OrderItemId = x.OrderItemId,
            ProductId = x.ProductId,
            ProductName = x.ProductName,
            Quantity = x.Quantity,
            UnitPrice = x.UnitPrice,
            TotalPrice = x.TotalPrice,
            ImageUrl = products.TryGetValue(x.ProductId, out var prod) ? prod.ImageUrl : null
        }).ToList();

        var shipment = (await unit.Repository<Shipment>().ListAllAsync()).FirstOrDefault(x => x.OrderId == id);
        return Ok(new OrderDetailDto
        {
            OrderId = order.OrderId,
            ShopId = order.ShopId,
            ShopName = shop?.ShopName,
            ShopLogoUrl = shop?.CoverImageUrl,
            OrderNumber = order.OrderNumber,
            Subtotal = order.Subtotal,
            ShippingFee = order.ShippingFee,
            TotalAmount = order.TotalAmount,
            OrderStatus = order.OrderStatus,
            PaymentStatus = order.PaymentStatus,
            CreatedAt = order.CreatedAt,
            ReceiverName = order.ReceiverName,
            ReceiverPhone = order.ReceiverPhone,
            ShippingAddress = order.ShippingAddress,
            ShippingMethod = order.ShippingMethod,
            Items = items,
            Shipment = shipment == null ? null : new ShipmentDto
            {
                ShippingProvider = shipment.ShippingProvider,
                TrackingNumber = shipment.TrackingNumber,
                ShippingStatus = shipment.ShippingStatus,
                ShippedAt = shipment.ShippedAt,
                DeliveredAt = shipment.DeliveredAt
            }
        });
    }

    [HttpGet("shop/mine")]
    public async Task<ActionResult<IReadOnlyList<OrderDto>>> MyShopOrders(
        [FromQuery] string? status,
        [FromQuery] string? search,
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
        var shop = (await unit.Repository<Shop>().ListAllAsync()).FirstOrDefault(x => x.OwnerUserId == userId);
        if (shop == null) return NotFound("Shop not found");

        var query = (await unit.Repository<Order>().ListAllAsync()).Where(x => x.ShopId == shop.ShopId);

        if (!string.IsNullOrWhiteSpace(status) && status != "all")
        {
            if (status == "paid")
                query = query.Where(o => o.PaymentStatus == "Paid");
            else if (status == "pending_payment")
                query = query.Where(o => o.PaymentStatus != "Paid");
            else
                query = query.Where(o => o.OrderStatus == status);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var q = search.Trim().ToLower();
            query = query.Where(o => o.OrderNumber.ToLower().Contains(q));
        }

        if (startDate.HasValue)
        {
            query = query.Where(o => o.CreatedAt >= startDate.Value.Date);
        }

        if (endDate.HasValue)
        {
            var endOfDay = endDate.Value.Date.AddDays(1).AddTicks(-1);
            query = query.Where(o => o.CreatedAt <= endOfDay);
        }

        return Ok(query.OrderByDescending(x => x.CreatedAt).Select(ToDto).ToList());
    }

    [HttpPatch("{id:guid}/status")]
    public async Task<IActionResult> UpdateStatus(Guid id, UpdateOrderStatusDto dto)
    {
        var order = await unit.Repository<Order>().GetByIdAsync(id);
        if (order == null) return NotFound();
        var shop = await unit.Repository<Shop>().GetByIdAsync(order.ShopId);
        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var isOwner = shop != null && shop.OwnerUserId == currentUserId;
        var isAdmin = User.IsInRole("Admin");
        var isCustomer = order.UserId == currentUserId;

        if (!isOwner && !isAdmin && !isCustomer) return Forbid();

        if (!string.IsNullOrWhiteSpace(dto.Status))
        {
            var allowed = new[] { "Pending", "Confirmed", "Shipped", "Completed", "Cancelled" };
            if (!allowed.Contains(dto.Status)) return BadRequest("Invalid order status");
            if (dto.Status != "Cancelled" && dto.Status != "Pending" && order.PaymentStatus != "Paid")
                return BadRequest("Order must be paid before processing");
            order.OrderStatus = dto.Status;
        }

        if (!string.IsNullOrWhiteSpace(dto.PaymentStatus))
        {
            var allowedPayments = new[] { "Pending", "PendingVerification", "Paid", "Failed" };
            if (!allowedPayments.Contains(dto.PaymentStatus)) return BadRequest("Invalid payment status");
            order.PaymentStatus = dto.PaymentStatus;
        }

        if (!string.IsNullOrWhiteSpace(dto.SlipImageUrl))
        {
            order.SlipImageUrl = dto.SlipImageUrl;
        }

        if (dto.SlipUploadedAt.HasValue)
        {
            order.SlipUploadedAt = dto.SlipUploadedAt.Value;
        }

        unit.Repository<Order>().Update(order);
        return await unit.Complete() ? NoContent() : BadRequest();
    }

    [HttpPut("{id:guid}/shipment")]
    public async Task<ActionResult<ShipmentDto>> UpdateShipment(Guid id, UpdateShipmentDto dto)
    {
        var order = await unit.Repository<Order>().GetByIdAsync(id);
        if (order == null) return NotFound();
        var shop = await unit.Repository<Shop>().GetByIdAsync(order.ShopId);
        if (shop == null || (!User.IsInRole("Admin") && shop.OwnerUserId != User.FindFirstValue(ClaimTypes.NameIdentifier))) return Forbid();
        if (order.PaymentStatus != "Paid") return BadRequest("Order must be paid before shipping");
        if (order.ShippingMethod == "Pickup") return BadRequest("Pickup order does not require shipment");
        if (string.IsNullOrWhiteSpace(dto.ShippingProvider) || string.IsNullOrWhiteSpace(dto.TrackingNumber)) return BadRequest("Shipping provider and tracking number are required");
        var shipment = (await unit.Repository<Shipment>().ListAllAsync()).FirstOrDefault(x => x.OrderId == id);
        if (shipment == null) { shipment = new Shipment { ShipmentId = Guid.NewGuid(), OrderId = id }; unit.Repository<Shipment>().Add(shipment); }
        shipment.ShippingProvider = dto.ShippingProvider; shipment.TrackingNumber = dto.TrackingNumber; shipment.ShippingStatus = "Shipped"; shipment.ShippedAt = DateTime.UtcNow;
        order.OrderStatus = "Shipped"; unit.Repository<Order>().Update(order);
        return await unit.Complete() ? Ok(new ShipmentDto { ShippingProvider = shipment.ShippingProvider, TrackingNumber = shipment.TrackingNumber, ShippingStatus = shipment.ShippingStatus, ShippedAt = shipment.ShippedAt, DeliveredAt = shipment.DeliveredAt }) : BadRequest();
    }

    [HttpPatch("{id:guid}/shipment/status")]
    public async Task<IActionResult> UpdateShipmentStatus(Guid id, UpdateShipmentStatusDto dto)
    {
        var order = await unit.Repository<Order>().GetByIdAsync(id);
        if (order == null) return NotFound();
        var shop = await unit.Repository<Shop>().GetByIdAsync(order.ShopId);
        if (shop == null || (!User.IsInRole("Admin") && shop.OwnerUserId != User.FindFirstValue(ClaimTypes.NameIdentifier))) return Forbid();
        if (dto.Status is not ("Delivered" or "Completed")) return BadRequest("Invalid shipment status");
        var shipment = (await unit.Repository<Shipment>().ListAllAsync()).FirstOrDefault(x => x.OrderId == id);
        if (shipment == null) return BadRequest("Shipment not found");
        shipment.ShippingStatus = "Delivered";
        shipment.DeliveredAt = DateTime.UtcNow;
        order.OrderStatus = "Completed";
        unit.Repository<Shipment>().Update(shipment);
        unit.Repository<Order>().Update(order);
        return await unit.Complete() ? NoContent() : BadRequest();
    }

    [HttpGet("admin/pending-slips"), Authorize(Roles = "Admin")]
    public async Task<ActionResult<IReadOnlyList<AdminPendingSlipGroupDto>>> GetAdminPendingSlips()
    {
        var orders = (await unit.Repository<Order>().ListAllAsync())
            .Where(x => x.PaymentStatus == "PendingVerification" || (!string.IsNullOrEmpty(x.SlipImageUrl) && x.PaymentStatus != "Paid"))
            .OrderByDescending(x => x.SlipUploadedAt ?? x.CreatedAt)
            .ToList();

        if (orders.Count == 0) return Ok(Array.Empty<AdminPendingSlipGroupDto>());

        var shopIds = orders.Select(x => x.ShopId).ToHashSet();
        var allShops = (await unit.Repository<Shop>().ListAllAsync()).Where(x => shopIds.Contains(x.ShopId)).ToDictionary(x => x.ShopId);
        var orderIds = orders.Select(x => x.OrderId).ToHashSet();
        var allItems = (await unit.Repository<OrderItem>().ListAllAsync()).Where(x => orderIds.Contains(x.OrderId)).ToList();

        var grouped = orders.GroupBy(x => string.IsNullOrWhiteSpace(x.SlipImageUrl) ? x.OrderId.ToString() : x.SlipImageUrl);

        var result = new List<AdminPendingSlipGroupDto>();
        foreach (var g in grouped)
        {
            var groupOrders = g.ToList();
            var first = groupOrders.First();
            var orderDtos = groupOrders.Select(o =>
            {
                var dto = ToDto(o);
                if (allShops.TryGetValue(o.ShopId, out var shop))
                {
                    dto.ShopName = shop.ShopName;
                    dto.ShopLogoUrl = shop.CoverImageUrl;
                }
                dto.Items = allItems.Where(i => i.OrderId == o.OrderId).Select(i => new OrderItemDto
                {
                    OrderItemId = i.OrderItemId,
                    ProductId = i.ProductId,
                    ProductName = i.ProductName,
                    Quantity = i.Quantity,
                    UnitPrice = i.UnitPrice,
                    TotalPrice = i.TotalPrice
                }).ToList();
                return dto;
            }).ToList();

            result.Add(new AdminPendingSlipGroupDto
            {
                SlipImageUrl = first.SlipImageUrl ?? string.Empty,
                SlipUploadedAt = first.SlipUploadedAt,
                TotalGroupAmount = groupOrders.Sum(x => x.TotalAmount),
                BuyerName = first.ReceiverName,
                BuyerPhone = first.ReceiverPhone,
                Orders = orderDtos
            });
        }

        return Ok(result);
    }

    [HttpPost("admin/verify-slip"), Authorize(Roles = "Admin")]
    public async Task<IActionResult> VerifySlip(VerifySlipDto dto)
    {
        if (dto.OrderIds == null || dto.OrderIds.Count == 0) return BadRequest("OrderIds is required");

        var orders = (await unit.Repository<Order>().ListAllAsync())
            .Where(x => dto.OrderIds.Contains(x.OrderId))
            .ToList();

        if (orders.Count == 0) return NotFound("Orders not found");

        var isApprove = dto.Action.Equals("Approve", StringComparison.OrdinalIgnoreCase);

        foreach (var order in orders)
        {
            if (isApprove)
            {
                order.PaymentStatus = "Paid";
                if (order.OrderStatus == "Pending")
                {
                    order.OrderStatus = "Processing";
                }
            }
            else
            {
                order.PaymentStatus = "Failed";
            }
            unit.Repository<Order>().Update(order);
        }

        return await unit.Complete() ? Ok() : BadRequest("Unable to update order verification status");
    }

    [HttpGet("admin/payouts"), Authorize(Roles = "Admin")]
    public async Task<ActionResult<IReadOnlyList<AdminMerchantPayoutGroupDto>>> GetAdminPayouts()
    {
        var allShops = (await unit.Repository<Shop>().ListAllAsync()).ToDictionary(x => x.ShopId);
        var paidOrders = (await unit.Repository<Order>().ListAllAsync())
            .Where(x => x.PaymentStatus == "Paid" && x.PayoutStatus != "Settled")
            .OrderByDescending(x => x.CreatedAt)
            .ToList();

        var orderIds = paidOrders.Select(x => x.OrderId).ToHashSet();
        var allItems = (await unit.Repository<OrderItem>().ListAllAsync()).Where(x => orderIds.Contains(x.OrderId)).ToList();

        var grouped = paidOrders.GroupBy(x => x.ShopId);

        var result = new List<AdminMerchantPayoutGroupDto>();
        foreach (var shopKvp in allShops)
        {
            var shop = shopKvp.Value;
            var ordersInShop = grouped.FirstOrDefault(g => g.Key == shop.ShopId)?.ToList() ?? [];
            if (ordersInShop.Count == 0) continue;

            var orderDtos = ordersInShop.Select(o =>
            {
                var dto = ToDto(o);
                dto.ShopName = shop.ShopName;
                dto.ShopLogoUrl = shop.CoverImageUrl;
                dto.Items = allItems.Where(i => i.OrderId == o.OrderId).Select(i => new OrderItemDto
                {
                    OrderItemId = i.OrderItemId,
                    ProductId = i.ProductId,
                    ProductName = i.ProductName,
                    Quantity = i.Quantity,
                    UnitPrice = i.  UnitPrice,
                    TotalPrice = i.TotalPrice
                }).ToList();
                return dto;
            }).ToList();

            result.Add(new AdminMerchantPayoutGroupDto
            {
                ShopId = shop.ShopId,
                ShopName = shop.ShopName,
                ShopLogoUrl = shop.CoverImageUrl,
                BankName = shop.BankName,
                BankAccountName = shop.BankAccountName,
                BankAccountNumber = shop.BankAccountNumber,
                PromptPay = shop.PromptPay,
                TotalSalesAmount = ordersInShop.Sum(x => x.TotalAmount),
                PaidOrdersCount = ordersInShop.Count,
                Orders = orderDtos
            });
        }

        return Ok(result.OrderByDescending(x => x.TotalSalesAmount).ToList());
    }

    [HttpPost("admin/payouts/confirm"), Authorize(Roles = "Admin")]
    public async Task<ActionResult<MerchantPayoutRecordDto>> ConfirmPayout(
        [FromForm] AdminConfirmPayoutFormDto dto,
        [FromServices] IWebHostEnvironment environment)
    {
        var ids = (dto.OrderIds ?? "").Split(',').Select(s => Guid.TryParse(s.Trim(), out var g) ? g : Guid.Empty).Where(g => g != Guid.Empty).ToList();
        if (ids.Count == 0) return BadRequest("OrderIds is required");

        var shop = await unit.Repository<Shop>().GetByIdAsync(dto.ShopId);
        if (shop == null) return NotFound("Shop not found");

        var orders = (await unit.Repository<Order>().ListAllAsync())
            .Where(x => x.ShopId == dto.ShopId && ids.Contains(x.OrderId) && x.PaymentStatus == "Paid")
            .ToList();

        if (orders.Count == 0) return BadRequest("No valid paid orders found for this payout");

        string? slipUrl = null;
        if (dto.SlipFile != null && dto.SlipFile.Length > 0)
        {
            var allowedTypes = new[] { "image/jpeg", "image/png", "image/webp" };
            if (allowedTypes.Contains(dto.SlipFile.ContentType.ToLowerInvariant()))
            {
                var extension = Path.GetExtension(dto.SlipFile.FileName).ToLowerInvariant();
                var folder = Path.Combine(environment.WebRootPath ?? Path.Combine(environment.ContentRootPath, "wwwroot"), "uploads", "payouts");
                Directory.CreateDirectory(folder);
                var fileName = $"{Guid.NewGuid():N}{extension}";
                await using var stream = System.IO.File.Create(Path.Combine(folder, fileName));
                await dto.SlipFile.CopyToAsync(stream);
                slipUrl = $"/uploads/payouts/{fileName}";
            }
        }

        var payout = new MerchantPayout
        {
            PayoutId = Guid.NewGuid(),
            ShopId = dto.ShopId,
            TotalAmount = orders.Sum(x => x.TotalAmount),
            SlipImageUrl = slipUrl,
            TransactionRef = dto.TransactionRef,
            Note = dto.Note,
            Status = "Completed",
            CreatedAt = DateTime.UtcNow
        };
        unit.Repository<MerchantPayout>().Add(payout);

        foreach (var o in orders)
        {
            o.PayoutStatus = "Settled";
            o.MerchantPayoutId = payout.PayoutId;
            unit.Repository<Order>().Update(o);
        }

        if (!await unit.Complete()) return BadRequest("Failed to confirm merchant payout");

        return Ok(new MerchantPayoutRecordDto
        {
            PayoutId = payout.PayoutId,
            ShopId = shop.ShopId,
            ShopName = shop.ShopName,
            ShopLogoUrl = shop.CoverImageUrl,
            TotalAmount = payout.TotalAmount,
            SlipImageUrl = payout.SlipImageUrl,
            TransactionRef = payout.TransactionRef,
            Note = payout.Note,
            Status = payout.Status,
            CreatedAt = payout.CreatedAt,
            OrdersCount = orders.Count,
            Orders = orders.Select(ToDto).ToList()
        });
    }

    [HttpGet("admin/payouts/history"), Authorize(Roles = "Admin")]
    public async Task<ActionResult<IReadOnlyList<MerchantPayoutRecordDto>>> GetAdminPayoutHistory()
    {
        var payouts = (await unit.Repository<MerchantPayout>().ListAllAsync())
            .OrderByDescending(x => x.CreatedAt)
            .ToList();

        if (payouts.Count == 0) return Ok(Array.Empty<MerchantPayoutRecordDto>());

        var shopIds = payouts.Select(x => x.ShopId).ToHashSet();
        var payoutIds = payouts.Select(x => x.PayoutId).ToHashSet();

        var allShops = (await unit.Repository<Shop>().ListAllAsync()).Where(x => shopIds.Contains(x.ShopId)).ToDictionary(x => x.ShopId);
        var allOrders = (await unit.Repository<Order>().ListAllAsync()).Where(x => x.MerchantPayoutId.HasValue && payoutIds.Contains(x.MerchantPayoutId.Value)).ToList();

        return Ok(payouts.Select(p =>
        {
            var shop = allShops.TryGetValue(p.ShopId, out var s) ? s : null;
            var relatedOrders = allOrders.Where(o => o.MerchantPayoutId == p.PayoutId).ToList();
            return new MerchantPayoutRecordDto
            {
                PayoutId = p.PayoutId,
                ShopId = p.ShopId,
                ShopName = shop?.ShopName,
                ShopLogoUrl = shop?.CoverImageUrl,
                TotalAmount = p.TotalAmount,
                SlipImageUrl = p.SlipImageUrl,
                TransactionRef = p.TransactionRef,
                Note = p.Note,
                Status = p.Status,
                CreatedAt = p.CreatedAt,
                OrdersCount = relatedOrders.Count,
                Orders = relatedOrders.Select(ToDto).ToList()
            };
        }).ToList());
    }

    [HttpGet("merchant/payouts"), HttpGet("/api/shops/mine/payouts"), Authorize]
    public async Task<ActionResult<object>> GetMerchantPayouts()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
        var shop = (await unit.Repository<Shop>().ListAllAsync()).FirstOrDefault(x => x.OwnerUserId == userId);
        if (shop == null) return NotFound("Shop not found");

        var shopOrders = (await unit.Repository<Order>().ListAllAsync())
            .Where(x => x.ShopId == shop.ShopId)
            .ToList();

        var pendingPayoutAmount = shopOrders
            .Where(x => x.PaymentStatus == "Paid" && x.PayoutStatus != "Settled")
            .Sum(x => x.TotalAmount);

        var payouts = (await unit.Repository<MerchantPayout>().ListAllAsync())
            .Where(x => x.ShopId == shop.ShopId)
            .OrderByDescending(x => x.CreatedAt)
            .ToList();

        var totalPaidOutAmount = payouts.Sum(x => x.TotalAmount);
        var payoutIds = payouts.Select(x => x.PayoutId).ToHashSet();
        var payoutOrders = shopOrders.Where(x => x.MerchantPayoutId.HasValue && payoutIds.Contains(x.MerchantPayoutId.Value)).ToList();

        var history = payouts.Select(p =>
        {
            var relatedOrders = payoutOrders.Where(o => o.MerchantPayoutId == p.PayoutId).ToList();
            return new MerchantPayoutRecordDto
            {
                PayoutId = p.PayoutId,
                ShopId = p.ShopId,
                ShopName = shop.ShopName,
                ShopLogoUrl = shop.CoverImageUrl,
                TotalAmount = p.TotalAmount,
                SlipImageUrl = p.SlipImageUrl,
                TransactionRef = p.TransactionRef,
                Note = p.Note,
                Status = p.Status,
                CreatedAt = p.CreatedAt,
                OrdersCount = relatedOrders.Count,
                Orders = relatedOrders.Select(ToDto).ToList()
            };
        }).ToList();

        return Ok(new
        {
            PendingPayoutAmount = pendingPayoutAmount,
            TotalPaidOutAmount = totalPaidOutAmount,
            History = history
        });
    }

    private static OrderDto ToDto(Order order) => new()
    {
        OrderId = order.OrderId, ShopId = order.ShopId, OrderNumber = order.OrderNumber,
        Subtotal = order.Subtotal, ShippingFee = order.ShippingFee, TotalAmount = order.TotalAmount,
        OrderStatus = order.OrderStatus, PaymentStatus = order.PaymentStatus, PayoutStatus = order.PayoutStatus, CreatedAt = order.CreatedAt,
        ReceiverName = order.ReceiverName, ReceiverPhone = order.ReceiverPhone, ShippingAddress = order.ShippingAddress, ShippingMethod = order.ShippingMethod,
        SlipImageUrl = order.SlipImageUrl, SlipUploadedAt = order.SlipUploadedAt
    };
}

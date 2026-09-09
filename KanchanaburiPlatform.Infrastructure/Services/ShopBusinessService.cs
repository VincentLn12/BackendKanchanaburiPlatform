
using KanchanaburiPlatform.Application.Specifications.Commerce;
using KanchanaburiPlatform.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Services;

public sealed class ShopBusinessService(StoreContext context, IGenericRepository<Shop> shops)
{
    public async Task ValidateCreateAsync(Shop shop)
    {
        if (await shops.CountAsync(new ShopSearchSpecification(ownerUserId: shop.OwnerUserId)) > 0)
            throw new InvalidOperationException("User already has a shop.");
        await ValidateLocationAsync(shop);
    }

    public Task ValidateUpdateAsync(Shop shop) => ValidateLocationAsync(shop);

    public static void EnsureOwner(Shop shop, string userId, bool isAdmin)
    {
        if (!isAdmin && shop.OwnerUserId != userId)
            throw new UnauthorizedAccessException("You do not own this shop.");
    }

    private async Task ValidateLocationAsync(Shop shop)
    {
        if (!await context.ShopCategories.AnyAsync(x => x.ShopCategoryId == shop.ShopCategoryId)) throw new InvalidOperationException("Shop category not found.");
        if (!await context.Districts.AnyAsync(x => x.DistrictId == shop.DistrictId)) throw new InvalidOperationException("District not found.");
        if (!await context.SubDistricts.AnyAsync(x => x.SubDistrictId == shop.SubDistrictId && x.DistrictId == shop.DistrictId)) throw new InvalidOperationException("Sub-district does not belong to district.");
    }
}

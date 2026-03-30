using RaidLoop.Core;
using RaidLoop.Core.Contracts;

namespace RaidLoop.Client;

public sealed record ShopStock(ShopOfferSnapshot Offer, Item Item)
{
    public int Price => Offer.Price;
    public int Stock => Offer.Stock;
    public int ItemDefId => Offer.ItemDefId;
}

using System.Text.Json.Serialization;

namespace DigitalBoardGameList.App.Enrichment;

public class PriceData
{
    public static readonly PriceData Zero = new(0);

    public decimal Value { get; init; }
    public decimal? DiscountPct { get; init; }

    [JsonConstructor]
    public PriceData()
    {
    }

    public PriceData(decimal value, decimal? discountPct = null)
    {
        Value = value;

        var discountValue = discountPct.GetValueOrDefault();
        if (discountValue != 0)
        {
            discountValue = Math.Abs(discountPct.GetValueOrDefault());

            if (discountValue > 100)
            {
                throw new ArgumentOutOfRangeException($"Invalid discount percentage: {discountValue}%");
            }

            DiscountPct = discountValue;
        }
    }
}
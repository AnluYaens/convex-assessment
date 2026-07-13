using System.Net.Http.Json;

namespace ConvexEnergy.Web;

public sealed class DayAheadApiClient(HttpClient http)
{
    public sealed record DayAheadPoint(
        DateTimeOffset StartCet,
        DateOnly DeliveryDate,
        int Period,
        decimal PriceEsEurMwh,
        decimal PricePtEurMwh);

    public async Task<List<DayAheadPoint>> GetDayAsync(DateOnly day, CancellationToken ct = default)
        => await http.GetFromJsonAsync<List<DayAheadPoint>>(
               $"/api/day-ahead-prices?from={day:yyyy-MM-dd}&to={day:yyyy-MM-dd}", ct) ?? [];
}
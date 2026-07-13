using Microsoft.AspNetCore.SignalR;

namespace IndiaGoldRates.Api.Hubs;

/// <summary>
/// Public, unauthenticated hub — broadcasts live rate updates to all connected clients.
/// No client-invoked methods or per-city groups in v1: there is no per-city data to filter on yet
/// (see RateSnapshot's design note), so a per-city subscription model would add complexity for no benefit.
/// </summary>
public class RatesHub : Hub;

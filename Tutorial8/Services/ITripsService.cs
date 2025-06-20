using Tutorial8.Models.DTOs;

namespace Tutorial8.Services;

public interface ITripsService
{
    Task<List<TripDTO>> GetTrips();
    Task<List<TripForClientDTO>> GetClientTrips(int clientId);
    Task<int> CreateClientAsync(ClientCreateDTO dto);
    Task<bool> RegisterClientToTripAsync(int clientId, int tripId);
    Task<bool> UnregisterClientFromTripAsync(int clientId, int tripId);

}
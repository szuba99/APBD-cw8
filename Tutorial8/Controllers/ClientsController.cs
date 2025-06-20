using Microsoft.AspNetCore.Mvc;
using Tutorial8.Models.DTOs;
using Tutorial8.Services;

namespace Tutorial8.Controllers;

[ApiController]
[Route("api/clients")]
public class ClientsController : ControllerBase
{
    private readonly ITripsService _tripsService;

    public ClientsController(ITripsService tripsService)
    {
        _tripsService = tripsService;
    }

    [HttpGet("{id}/trips")]
    public async Task<IActionResult> GetClientTrips(int id)
    {
        var trips = await _tripsService.GetClientTrips(id);
        if (!trips.Any())
        {
            return NotFound($"Klient o ID {id} nie ma przypisanych wycieczek.");
        }

        return Ok(trips);
    }
    
    [HttpPost]
    public async Task<IActionResult> CreateClient([FromBody] ClientCreateDTO dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var newId = await _tripsService.CreateClientAsync(dto);

        return CreatedAtAction(nameof(GetClientTrips), new { id = newId }, new { id = newId });
    }
    
    [HttpPut("{clientId}/trips/{tripId}")]
    public async Task<IActionResult> RegisterClientToTrip(int clientId, int tripId)
    {
        try
        {
            await _tripsService.RegisterClientToTripAsync(clientId, tripId);
            return Ok("Client registered to trip.");
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    
    [HttpDelete("{clientId}/trips/{tripId}")]
    public async Task<IActionResult> UnregisterClientFromTrip(int clientId, int tripId)
    {
        var result = await _tripsService.UnregisterClientFromTripAsync(clientId, tripId);
        if (!result)
            return NotFound("Nie znaleziono rejestracji klienta na tę wycieczkę.");

        return NoContent(); // 204
    }


}
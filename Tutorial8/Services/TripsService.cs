using Microsoft.Data.SqlClient;
using Tutorial8.Models.DTOs;

namespace Tutorial8.Services;

public class TripsService : ITripsService
{
    private readonly string _connectionString =
        "Data Source=db-mssql;Initial Catalog=2019SBD;Integrated Security=True;Trust Server Certificate=True";

    public async Task<List<TripDTO>> GetTrips()
    {
        var trips = new List<TripDTO>();

        string command = "SELECT IdTrip, Name FROM Trip";

        using (SqlConnection conn = new SqlConnection(_connectionString))
        using (SqlCommand cmd = new SqlCommand(command, conn))
        {
            await conn.OpenAsync();

            using (var reader = await cmd.ExecuteReaderAsync())
            {
                while (await reader.ReadAsync())
                {
                    int idOrdinal = reader.GetOrdinal("IdTrip");
                    trips.Add(new TripDTO()
                    {
                        Id = reader.GetInt32(idOrdinal),
                        Name = reader.GetString(1),
                    });
                }
            }
        }


        return trips;
    }

    public async Task<List<TripForClientDTO>> GetClientTrips(int clientId)
    {
        var trips = new List<TripForClientDTO>();

        using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();

        var command = @"
        SELECT T.IdTrip, T.Name, T.Description, T.DateFrom, T.DateTo, T.MaxPeople,
               CT.RegisteredAt, CT.PaymentDate,
               C.Name AS CountryName
        FROM Trip T
        JOIN Client_Trip CT ON T.IdTrip = CT.IdTrip
        JOIN Country_Trip CTR ON T.IdTrip = CTR.IdTrip
        JOIN Country C ON CTR.IdCountry = C.IdCountry
        WHERE CT.IdClient = @IdClient
        ORDER BY T.IdTrip;
    ";

        using var cmd = new SqlCommand(command, connection);
        cmd.Parameters.AddWithValue("@IdClient", clientId);

        using var reader = await cmd.ExecuteReaderAsync();

        TripForClientDTO? currentTrip = null;
        int? lastTripId = null;

        while (await reader.ReadAsync())
        {
            int tripId = reader.GetInt32(reader.GetOrdinal("IdTrip"));

            if (tripId != lastTripId)
            {
                currentTrip = new TripForClientDTO
                {
                    IdTrip = tripId,
                    Name = reader.GetString(reader.GetOrdinal("Name")),
                    Description = reader.GetString(reader.GetOrdinal("Description")),
                    DateFrom = reader.GetDateTime(reader.GetOrdinal("DateFrom")),
                    DateTo = reader.GetDateTime(reader.GetOrdinal("DateTo")),
                    MaxPeople = reader.GetInt32(reader.GetOrdinal("MaxPeople")),
                    RegisteredAt = reader.GetInt32(reader.GetOrdinal("RegisteredAt")),
                    PaymentDate = reader.IsDBNull(reader.GetOrdinal("PaymentDate"))
                        ? null
                        : reader.GetInt32(reader.GetOrdinal("PaymentDate")),
                    Countries = new List<string>()
                };

                trips.Add(currentTrip);
                lastTripId = tripId;
            }

            var country = reader.GetString(reader.GetOrdinal("CountryName"));
            currentTrip?.Countries.Add(country);
        }

        return trips;
    }

    public async Task<int> CreateClientAsync(ClientCreateDTO dto)
    {
        using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();

        var query = @"
        INSERT INTO Client (FirstName, LastName, Email, Telephone, Pesel)
        OUTPUT INSERTED.IdClient
        VALUES (@FirstName, @LastName, @Email, @Telephone, @Pesel);
    ";

        using var cmd = new SqlCommand(query, connection);
        cmd.Parameters.AddWithValue("@FirstName", dto.FirstName);
        cmd.Parameters.AddWithValue("@LastName", dto.LastName);
        cmd.Parameters.AddWithValue("@Email", dto.Email);
        cmd.Parameters.AddWithValue("@Telephone", (object?)dto.Telephone ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@Pesel", (object?)dto.Pesel ?? DBNull.Value);

        var newId = (int)await cmd.ExecuteScalarAsync();
        return newId;
    }

    public async Task<bool> RegisterClientToTripAsync(int clientId, int tripId)
    {
        using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();

        // Sprawdzenie istnienia klienta
        var clientCmd = new SqlCommand("SELECT COUNT(*) FROM Client WHERE IdClient = @Id", connection);
        clientCmd.Parameters.AddWithValue("@Id", clientId);
        var clientExists = (int)await clientCmd.ExecuteScalarAsync() > 0;
        if (!clientExists) throw new Exception("Client not found");

        // Sprawdzenie istnienia wycieczki
        var tripCmd = new SqlCommand("SELECT MaxPeople FROM Trip WHERE IdTrip = @Id", connection);
        tripCmd.Parameters.AddWithValue("@Id", tripId);
        var maxPeopleObj = await tripCmd.ExecuteScalarAsync();
        if (maxPeopleObj == null) throw new Exception("Trip not found");
        int maxPeople = (int)maxPeopleObj;

        // Sprawdzenie liczby zapisanych osób
        var countCmd = new SqlCommand("SELECT COUNT(*) FROM Client_Trip WHERE IdTrip = @Id", connection);
        countCmd.Parameters.AddWithValue("@Id", tripId);
        int currentCount = (int)await countCmd.ExecuteScalarAsync();
        if (currentCount >= maxPeople) throw new Exception("Trip is full");

        // Sprawdzenie duplikatu
        var existsCmd = new SqlCommand("SELECT COUNT(*) FROM Client_Trip WHERE IdTrip = @Trip AND IdClient = @Client",
            connection);
        existsCmd.Parameters.AddWithValue("@Trip", tripId);
        existsCmd.Parameters.AddWithValue("@Client", clientId);
        var exists = (int)await existsCmd.ExecuteScalarAsync() > 0;
        if (exists) throw new Exception("Client already registered");

        // Rejestracja
        var insertCmd = new SqlCommand(@"
        INSERT INTO Client_Trip (IdClient, IdTrip, RegisteredAt, PaymentDate)
        VALUES (@ClientId, @TripId, @RegisteredAt, NULL)", connection);
        insertCmd.Parameters.AddWithValue("@ClientId", clientId);
        insertCmd.Parameters.AddWithValue("@TripId", tripId);

        int dateInt = int.Parse(DateTime.Now.ToString("yyyyMMdd"));
        insertCmd.Parameters.AddWithValue("@RegisteredAt", dateInt);

        await insertCmd.ExecuteNonQueryAsync();

        return true;
    }


    public async Task<bool> UnregisterClientFromTripAsync(int clientId, int tripId)
    {
        using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();

        // Sprawdź czy zapis istnieje
        var checkCmd = new SqlCommand(@"
        SELECT COUNT(*) FROM Client_Trip
        WHERE IdClient = @ClientId AND IdTrip = @TripId", connection);
        checkCmd.Parameters.AddWithValue("@ClientId", clientId);
        checkCmd.Parameters.AddWithValue("@TripId", tripId);

        int exists = (int)await checkCmd.ExecuteScalarAsync();
        if (exists == 0)
            return false;

        // Usuń zapis
        var deleteCmd = new SqlCommand(@"
        DELETE FROM Client_Trip
        WHERE IdClient = @ClientId AND IdTrip = @TripId", connection);
        deleteCmd.Parameters.AddWithValue("@ClientId", clientId);
        deleteCmd.Parameters.AddWithValue("@TripId", tripId);

        await deleteCmd.ExecuteNonQueryAsync();
        return true;
    }
}
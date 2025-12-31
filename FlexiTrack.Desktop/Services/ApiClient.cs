using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using FlexiTrack.Desktop.Models;

namespace FlexiTrack.Desktop.Services;

public class ApiClient : IApiClient
{
    private readonly HttpClient _httpClient;
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    public ApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<LoginResponse> LoginAsync(string email, string password)
    {
        try
        {
            var request = new LoginRequest(email, password);
            var response = await _httpClient.PostAsJsonAsync("/api/users/login", request, _jsonOptions);

            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<LoginResponse>(_jsonOptions)
                    ?? new LoginResponse(false, Error: "Invalid response");
            }

            var errorContent = await response.Content.ReadAsStringAsync();
            return new LoginResponse(false, Error: "Login failed");
        }
        catch (Exception ex)
        {
            return new LoginResponse(false, Error: $"Connection error: {ex.Message}");
        }
    }

    public async Task<UserProfileDto?> GetProfileAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync("/api/users/profile");
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<UserProfileDto>(_jsonOptions);
            }
            return null;
        }
        catch
        {
            return null;
        }
    }

    public async Task<IEnumerable<TaskLogDto>> GetTaskLogsAsync()
    {
        try
        {
            var response = await _httpClient.GetFromJsonAsync<TaskLogsResponse>("/api/tasks/logs", _jsonOptions);
            return response?.TaskLogs ?? [];
        }
        catch
        {
            return [];
        }
    }

    public async Task<IEnumerable<string>> GetClientsAsync()
    {
        try
        {
            var response = await _httpClient.GetFromJsonAsync<ClientsResponse>("/api/tasks/clients", _jsonOptions);
            return response?.Clients ?? [];
        }
        catch
        {
            return [];
        }
    }

    public async Task<ApiResponse> CreateTaskAsync(CreateTaskRequest request)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("/api/tasks", request, _jsonOptions);

            if (response.IsSuccessStatusCode)
            {
                return new ApiResponse(true);
            }

            var errorContent = await response.Content.ReadAsStringAsync();
            try
            {
                var error = JsonSerializer.Deserialize<ErrorResponse>(errorContent, _jsonOptions);
                return new ApiResponse(false, error?.Error ?? "Failed to create task");
            }
            catch
            {
                return new ApiResponse(false, "Failed to create task");
            }
        }
        catch (Exception ex)
        {
            return new ApiResponse(false, $"Connection error: {ex.Message}");
        }
    }

    public async Task<ApiResponse> UpdateTaskAsync(int id, UpdateTaskRequest request)
    {
        try
        {
            var response = await _httpClient.PutAsJsonAsync($"/api/tasks/{id}", request, _jsonOptions);

            if (response.IsSuccessStatusCode)
            {
                return new ApiResponse(true);
            }

            var errorContent = await response.Content.ReadAsStringAsync();
            try
            {
                var error = JsonSerializer.Deserialize<ErrorResponse>(errorContent, _jsonOptions);
                return new ApiResponse(false, error?.Error ?? "Failed to update task");
            }
            catch
            {
                return new ApiResponse(false, "Failed to update task");
            }
        }
        catch (Exception ex)
        {
            return new ApiResponse(false, $"Connection error: {ex.Message}");
        }
    }

    public async Task<ApiResponse> DeleteTaskAsync(int id)
    {
        try
        {
            var response = await _httpClient.DeleteAsync($"/api/tasks/{id}");

            if (response.IsSuccessStatusCode)
            {
                return new ApiResponse(true);
            }

            var errorContent = await response.Content.ReadAsStringAsync();
            try
            {
                var error = JsonSerializer.Deserialize<ErrorResponse>(errorContent, _jsonOptions);
                return new ApiResponse(false, error?.Error ?? "Failed to delete task");
            }
            catch
            {
                return new ApiResponse(false, "Failed to delete task");
            }
        }
        catch (Exception ex)
        {
            return new ApiResponse(false, $"Connection error: {ex.Message}");
        }
    }

    private record ErrorResponse(string? Error);
}

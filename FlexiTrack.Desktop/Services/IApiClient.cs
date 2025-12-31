using FlexiTrack.Desktop.Models;

namespace FlexiTrack.Desktop.Services;

public interface IApiClient
{
    Task<LoginResponse> LoginAsync(string email, string password);
    Task<UserProfileDto?> GetProfileAsync();
    Task<IEnumerable<TaskLogDto>> GetTaskLogsAsync();
    Task<IEnumerable<string>> GetClientsAsync();
    Task<ApiResponse> CreateTaskAsync(CreateTaskRequest request);
    Task<ApiResponse> UpdateTaskAsync(int id, UpdateTaskRequest request);
    Task<ApiResponse> DeleteTaskAsync(int id);
}

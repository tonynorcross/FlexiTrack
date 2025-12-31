namespace FlexiTrack.Desktop.Models;

public record LoginRequest(string Email, string Password);

public record LoginResponse(bool Success, string? Token = null, string? Error = null);

namespace UltimaAPI.Services;

internal sealed class UltimaSdkUnavailableException(string message) : InvalidOperationException(message);

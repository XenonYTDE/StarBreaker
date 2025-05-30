﻿using System.Text.Json.Serialization;

namespace StarBreaker.Sandbox;

public record LoginData
{
    [JsonPropertyName("username")] public string Username { get; init; } = null!;

    [JsonPropertyName("token")] public string Token { get; init; } = null!;

    [JsonPropertyName("auth_token")] public string AuthToken { get; init; } = null!;

    [JsonPropertyName("star_network")] public StarNetwork StarNetwork { get; init; } = null!;
}
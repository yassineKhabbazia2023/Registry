// <copyright file="TokenModel.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

namespace WebApi.Configurations.Models;

/// <summary>
/// Configuration du token d'authentification.
/// </summary>
public class TokenModel
{
    public string Token {  get; set; }

    public string? HeaderToken { get; set; }
}

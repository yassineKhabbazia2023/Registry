// <copyright file="AkuiteoAuthenticationTechnicalException.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

namespace Application.Exceptions;

/// <summary>
/// Represents a technical failure while authenticating Akuiteo requests.
/// </summary>
public class AkuiteoAuthenticationTechnicalException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AkuiteoAuthenticationTechnicalException"/> class.
    /// </summary>
    /// <param name="message">The exception message.</param>
    public AkuiteoAuthenticationTechnicalException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="AkuiteoAuthenticationTechnicalException"/> class.
    /// </summary>
    /// <param name="message">The exception message.</param>
    /// <param name="innerException">The inner exception.</param>
    public AkuiteoAuthenticationTechnicalException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}

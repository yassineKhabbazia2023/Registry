// <copyright file="AkuiteoResponseDeserializationTechnicalException.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

namespace Application.Exceptions;

/// <summary>
/// Represents a malformed response returned by Akuiteo.
/// </summary>
public class AkuiteoResponseDeserializationTechnicalException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AkuiteoResponseDeserializationTechnicalException"/> class.
    /// </summary>
    /// <param name="message">The technical error message.</param>
    /// <param name="innerException">The JSON exception raised while reading the response.</param>
    public AkuiteoResponseDeserializationTechnicalException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}

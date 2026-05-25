// <copyright file="AkuiteoAccountSearchTechnicalException.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

namespace Application.Exceptions;

/// <summary>
/// Represents a technical failure while searching an account in Akuiteo.
/// </summary>
public class AkuiteoAccountSearchTechnicalException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AkuiteoAccountSearchTechnicalException"/> class.
    /// </summary>
    /// <param name="message">The exception message.</param>
    public AkuiteoAccountSearchTechnicalException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="AkuiteoAccountSearchTechnicalException"/> class.
    /// </summary>
    /// <param name="message">The exception message.</param>
    /// <param name="innerException">The inner exception.</param>
    public AkuiteoAccountSearchTechnicalException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}

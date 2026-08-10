// <copyright file="AkuiteoContactSearchTechnicalException.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

namespace Application.Exceptions;

/// <summary>
/// Represents a technical failure while searching contacts in Akuiteo.
/// </summary>
public class AkuiteoContactSearchTechnicalException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AkuiteoContactSearchTechnicalException"/> class.
    /// </summary>
    /// <param name="message">The technical error message.</param>
    public AkuiteoContactSearchTechnicalException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="AkuiteoContactSearchTechnicalException"/> class.
    /// </summary>
    /// <param name="message">The technical error message.</param>
    /// <param name="innerException">The underlying technical exception.</param>
    public AkuiteoContactSearchTechnicalException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}

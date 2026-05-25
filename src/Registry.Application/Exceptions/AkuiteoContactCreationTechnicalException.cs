// <copyright file="AkuiteoContactCreationTechnicalException.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

namespace Application.Exceptions;

/// <summary>
/// Represents a technical failure while creating a contact in Akuiteo.
/// </summary>
public class AkuiteoContactCreationTechnicalException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AkuiteoContactCreationTechnicalException"/> class.
    /// </summary>
    /// <param name="message">The exception message.</param>
    public AkuiteoContactCreationTechnicalException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="AkuiteoContactCreationTechnicalException"/> class.
    /// </summary>
    /// <param name="message">The exception message.</param>
    /// <param name="innerException">The inner exception.</param>
    public AkuiteoContactCreationTechnicalException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}

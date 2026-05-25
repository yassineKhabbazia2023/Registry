// <copyright file="SiretValidationHelper.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

using Application.Exceptions;
using Kpmg.ExceptionMiddleware.AdvancedException;

namespace Application.Helpers;

/// <summary>
/// Provides reusable SIRET validation helpers.
/// </summary>
public static class SiretValidationHelper
{
    private const int SiretLength = 14;

    /// <summary>
    /// Normalizes a SIRET by trimming surrounding spaces.
    /// </summary>
    /// <param name="siret">The SIRET value to normalize.</param>
    /// <returns>The trimmed SIRET value when provided; otherwise <see langword="null"/>.</returns>
    public static string? Normalize(string? siret)
    {
        return siret?.Trim();
    }

    /// <summary>
    /// Validates that the SIRET is present, contains exactly 14 digits, and satisfies the Luhn checksum.
    /// </summary>
    /// <param name="siret">The normalized SIRET value to validate.</param>
    public static void ValidateRequiredSiret(string? siret)
    {
        if (string.IsNullOrWhiteSpace(siret))
        {
            throw new BadRequestException(Errors.InvalidSiretCode, "The siret query parameter is required.");
        }

        var requiredSiret = siret;

        if (siret.Length != SiretLength)
        {
            throw new BadRequestException(Errors.InvalidSiretCode, "The siret query parameter must contain exactly 14 characters.");
        }

        if (!requiredSiret.All(char.IsDigit))
        {
            throw new BadRequestException(Errors.InvalidSiretCode, "The siret query parameter must contain digits only.");
        }

        if (!HasValidLuhnChecksum(requiredSiret))
        {
            throw new BadRequestException(Errors.InvalidSiretCode, "The siret query parameter is not a valid SIRET.");
        }
    }

    /// <summary>
    /// Determines whether the SIRET satisfies the Luhn checksum algorithm.
    /// </summary>
    /// <param name="siret">The normalized SIRET value to evaluate.</param>
    /// <returns><see langword="true"/> when the checksum is valid; otherwise <see langword="false"/>.</returns>
    private static bool HasValidLuhnChecksum(string siret)
    {
        var sum = 0;
        var shouldDouble = false;

        for (var index = siret.Length - 1; index >= 0; index--)
        {
            var digit = siret[index] - '0';

            if (shouldDouble)
            {
                digit *= 2;
                if (digit > 9)
                {
                    digit -= 9;
                }
            }

            sum += digit;
            shouldDouble = !shouldDouble;
        }

        return sum % 10 == 0;
    }
}

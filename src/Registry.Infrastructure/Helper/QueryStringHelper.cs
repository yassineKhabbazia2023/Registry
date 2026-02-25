// <copyright file="QueryStringHelper.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

using System.Text.RegularExpressions;

namespace Infrastructure.Helper;

/// <summary>
/// Helper class for query string operations.
/// </summary>
public static partial class QueryStringHelper
{
    /// <summary>
    /// Extracts and decodes a parameter from a raw query string.
    /// Uses Uri.UnescapeDataString to preserve '+' characters that ASP.NET Core would otherwise convert to spaces.
    /// </summary>
    /// <param name="rawQueryString">The raw query string (e.g., "?param1=value1&amp;param2=value2").</param>
    /// <param name="parameterName">The name of the parameter to extract.</param>
    /// <returns>The decoded parameter value, or null if not present.</returns>
    public static string? GetDecodedParameter(string? rawQueryString, string parameterName)
    {
        if (string.IsNullOrEmpty(rawQueryString) || string.IsNullOrEmpty(parameterName))
        {
            return null;
        }

        try
        {
            var pattern = $@"[?&]{Regex.Escape(parameterName)}=([^&]*)";
            var match = Regex.Match(rawQueryString, pattern, RegexOptions.IgnoreCase, TimeSpan.FromMilliseconds(100));

            if (!match.Success)
            {
                return null;
            }

            var rawValue = match.Groups[1].Value;
            return string.IsNullOrEmpty(rawValue) ? null : Uri.UnescapeDataString(rawValue);
        }
        catch (RegexMatchTimeoutException)
        {
            return null;
        }
        catch
        {
            return null;
        }
    }
}

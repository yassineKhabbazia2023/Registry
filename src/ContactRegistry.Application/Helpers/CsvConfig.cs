// <copyright file="CsvConfig.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

using System.Text;

namespace Application.Helpers;

public static class CsvConfig
{
    private const char Separator = ';';

    public static bool IsValidCsvFormat(string data, out string messageError)
    {
        messageError = ".";

        if (string.IsNullOrWhiteSpace(data))
        {
            messageError = ": message cannot be null or empty.";
            return false;
        }

        var lines = data.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries).ToList();

        if (!lines.Any())
        {
            return false;
        }

        var columnCount = lines[0].Split(Separator).Length;
        var linesOnError = new StringBuilder();

        foreach (var line in lines)
        {
            if (!line.Contains(Separator) || line.Split(Separator).Length != columnCount)
            {
                linesOnError.Append((lines.IndexOf(line) + 1) + ", ");
            }
        }

        if (!string.IsNullOrEmpty(linesOnError.ToString()))
        {
            messageError = ": errors on lines " + linesOnError.Remove(linesOnError.Length - 2, 2);
            return false;
        }

        return true;
    }
}

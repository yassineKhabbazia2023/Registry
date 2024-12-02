// <copyright file="CsvConfig.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

using Application.Models;
using System.Text;

namespace Application.Helpers;

public static class CsvConfig
{
    private const char Separator = ';';
    private const char Wrapper = '"';

    public static bool IsValidCsvFormat(string data, Type classType, out string messageError)
    {
        messageError = string.Empty;

        if (string.IsNullOrWhiteSpace(data))
        {
            messageError = "The input data cannot be null or empty.";
            return false;
        }

        if (data.Contains(Wrapper))
        {
            data = data.Trim(Wrapper);
        }

        var lines = data.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries).ToList();

        if (!lines.Any())
        {
            messageError = "The input data does not contain any lines.";
            return false;
        }

        // Récupération des noms des propriétés de la classe passée en paramètre
        var expectedColumns = classType.GetProperties()
                                        .Select(p => p.Name)
                                        .ToList();

        var header = lines.First();
        var headerColumns = header.Split(Separator).Select(c => c.Trim()).ToList();

        var errors = new List<string>();

        // Vérification de la correspondance entre les noms des colonnes dans l'en-tête et les noms des propriétés de la classe
        foreach (var column in headerColumns)
        {
            if (!expectedColumns.Contains(column))
            {
                errors.Add($"Invalid column name '{column}' in header. Expected columns are: {string.Join(", ", expectedColumns)}.");
            }
        }

        // Vérification de la correspondance du nombre de colonnes
        var columnCount = expectedColumns.Count;
        if (headerColumns.Count != columnCount)
        {
            var missingColumns = expectedColumns.Where(c => !headerColumns.Contains(c)).ToList();
            var extraColumns = headerColumns.Where(c => !expectedColumns.Contains(c)).ToList();

            if (missingColumns.Any())
            {
                errors.Add($"Missing columns in header: {string.Join(", ", missingColumns)}.");
            }

            if (extraColumns.Any())
            {
                errors.Add($"Extra columns in header: {string.Join(", ", extraColumns)}.");
            }

            errors.Add($"Header column count mismatch. Expected {columnCount}, but got {headerColumns.Count}.");
        }

        // Vérification du format des lignes (s'assurer que chaque ligne a le bon nombre de colonnes)
        for (var i = 1; i < lines.Count; i++) // Start from 1 to skip the header line
        {
            var line = lines[i];
            var values = line.Split(Separator);

            if (values.Length != columnCount)
            {
                errors.Add($"Line {i + 1}: Column count mismatch. Expected {columnCount}, but got {values.Length}.");
                continue; // Ignore further checks for this line if column count doesn't match
            }
        }

        // Ajouter les erreurs au message d'erreur global
        if (errors.Any())
        {
            messageError = string.Join(" ", errors);
            return false;
        }

        return true;
    }
}

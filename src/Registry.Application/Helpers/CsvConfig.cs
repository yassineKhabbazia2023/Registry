// <copyright file="CsvConfig.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

using Application.Models;

namespace Application.Helpers;

public static class CsvConfig
{
    public static bool IsValidCsvFormat<T>(List<(T, int, string[])> data, Type classType, out string messageError)
    {
        messageError = string.Empty;

        var lines = data.Select(d=>(d.Item1,d.Item2)).ToList();
        if (!lines.Any())
        {
            messageError = "The input data does not contain any lines.";
            return false;
        }
        // Récupération des noms des propriétés de la classe passée en paramètre
        var expectedColumns = classType.GetProperties()
                                       .Select(p => p.Name)
                                       .ToList();

        var headerColumns = data.First().Item3;

        var errors = new List<string>();

        // Vérification du format des lignes (s'assurer que chaque ligne a le bon nombre de colonnes)
        foreach (var line in lines)
        {
            if (line.Item2 != headerColumns.Length)
            {
                errors.Add($"Column count mismatch. Expected {headerColumns.Length}, but got {line.Item2}.");
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

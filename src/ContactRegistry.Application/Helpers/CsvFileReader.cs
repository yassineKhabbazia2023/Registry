// <copyright file="CsvFileReader.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

using CsvHelper.Configuration;
using CsvHelper;
using System.Globalization;
using System.Text;
using Microsoft.Extensions.Logging;

namespace Application.Helpers
{
    /// <summary>
    /// CsvFileReader.
    /// </summary>
    public static class CsvFileReader
    {
        private static async Task<IEnumerable<T>> ReadStreamAsync<T>(Stream stream)
        {
            using (var reader = new StreamReader(stream, Encoding.GetEncoding("ISO-8859-1")))
            {
                using (var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture)
                {
                    Delimiter = ";",
                    HasHeaderRecord = true,
                    Encoding = Encoding.GetEncoding("ISO-8859-1"),
                    BadDataFound = args =>
                    {
                    },
                    MissingFieldFound = args =>
                    {
                    }
                }))
                {
                    var records = new List<T>();
                    await foreach (var record in csv.GetRecordsAsync<T>())
                    {
                        records.Add(record);
                    }

                    return records;
                }
            }
        }

        public static async Task<IEnumerable<T>> ReadCsvAsync<T>(Stream stream)
        {
            return await ReadStreamAsync<T>(stream);
        }
    }
}

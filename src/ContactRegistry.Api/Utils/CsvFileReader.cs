// <copyright file="CsvFileReader.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

using CsvHelper.Configuration;
using CsvHelper;
using System.Globalization;
using System.Text;

namespace Application.Utils
{
    /// <summary>
    /// CsvFileReader.
    /// </summary>
    public static class CsvFileReader
    {
        /// <summary>
        /// ReadCsvAsync.
        /// </summary>
        /// <typeparam name="T">T.</typeparam>
        /// <param name="file">file.</param>
        /// <returns></returns>
        public static async Task<IEnumerable<T>> ReadCsvAsync<T>(IFormFile file)
        {
            using (var reader = new StreamReader(file.OpenReadStream(), Encoding.GetEncoding("ISO-8859-1")))
            {
                using (var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture)
                {
                    Delimiter = ";",
                    HasHeaderRecord = true,
                    Encoding = Encoding.GetEncoding("ISO-8859-1")
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
    }
}

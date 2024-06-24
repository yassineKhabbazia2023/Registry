// <copyright file="CsvFileReader.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

using CsvHelper.Configuration;
using CsvHelper;
using System.Globalization;
using System.Text;
using Microsoft.Extensions.Logging;
using System.Formats.Asn1;

namespace Application.Helpers
{
    /// <summary>
    /// CsvFileReader.
    /// </summary>
    public static class CsvFileReader
    {
        private static async Task<IEnumerable<T>> ReadStreamAsync<T>(Stream stream)
        {
            using (var reader = new StreamReader(stream, Encoding.GetEncoding("utf-8")))
            {
                using (var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture)
                {
                    Delimiter = ";",
                    Quote = '"', // Use double quotes as the quote character
                    Escape = '"', // Use double quotes as the escape character
                    Mode = CsvMode.Escape,
                    HasHeaderRecord = true,
                    TrimOptions = TrimOptions.Trim,

                Encoding = Encoding.GetEncoding("utf-8"),
                    BadDataFound = args =>
                    {
                        Console.WriteLine(string.Format("BadDataFound: Bad entry found at field {0}, \n : {1}", args.Field, args.RawRecord.Replace("\"", "'")));
                    },
                    MissingFieldFound = args =>
                    {
                        Console.WriteLine(string.Format("missing field  index : {0}", args.Context.Parser.RawRecord));
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

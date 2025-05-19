// <copyright file="CsvFileReader.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

using CsvHelper.Configuration;
using CsvHelper;
using System.Globalization;
using System.Text;

namespace Application.Helpers;

/// <summary>
/// CsvFileReader.
/// </summary>
public static class CsvFileReader
{
    private static readonly CsvConfiguration csvConfiguration = new CsvConfiguration(CultureInfo.InvariantCulture)
    {
        Quote = '"', // Use double quotes as the quote character
        Escape = '"', // Use double quotes as the escape character
        Mode = CsvMode.RFC4180,
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
    };

    public static IEnumerable<(T, int, string[])> ReadStreamAsync<T>(Stream stream, string? delimeter = ";")
    {
        using (var reader = new StreamReader(stream, Encoding.GetEncoding("utf-8")))
        {
            csvConfiguration.Delimiter = delimeter ?? ";";
            using (var csv = new CsvReader(reader, csvConfiguration))
            {
                foreach (var record in csv.GetRecords<T>())
                {
                    yield return (record, csv.Parser.Count, csv.HeaderRecord);
                }
            }
        }
    }

}

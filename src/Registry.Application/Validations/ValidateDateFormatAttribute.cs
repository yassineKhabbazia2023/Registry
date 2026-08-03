// <copyright file="ValidateDateFormatAttribute.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

using System.ComponentModel.DataAnnotations;
using System.Globalization;

namespace Application.Validations
{
    public class ValidateDateFormatAttribute : ValidationAttribute
    {
        private readonly string _format;

        public ValidateDateFormatAttribute(string format)
        {
            _format = format;
        }

        public override bool IsValid(object? value)
        {
            // Emptiness is carried by [Required] so the attributes compose cleanly.
            if (value is not string text || string.IsNullOrWhiteSpace(text))
            {
                return true;
            }

            return DateTime.TryParseExact(text, _format, CultureInfo.InvariantCulture, DateTimeStyles.None, out _);
        }

        public override string FormatErrorMessage(string name)
        {
            return $"Invalid date format ({_format})";
        }
    }
}

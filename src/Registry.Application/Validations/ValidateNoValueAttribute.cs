using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Validations
{
    public class ValidateNoValueAttribute : ValidationAttribute
    {

        public override bool IsValid(object? value)
        {
            if (value == null) return false;

            string? noValue = value as string;

            if(string.IsNullOrEmpty(noValue) || noValue.ToLower().Equals("no_value"))  return false;

            return true;
        }
        public override string FormatErrorMessage(string name)
        {
            return $"{name} can not be null or NO_VALUE";
        }
    }
}

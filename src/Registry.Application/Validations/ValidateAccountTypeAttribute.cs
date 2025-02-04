using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Validations
{
    public class ValidateAccountTypeAttribute : ValidationAttribute
    {
        public override bool IsValid(object? value)
        {
            if(value == null) return false;

            string? accountType = value as string;

            if(string.IsNullOrEmpty(accountType) || !accountType.ToLower().Equals("client")) return false;

            return true;
        }

        public override string FormatErrorMessage(string name)
        {
            return $"Account Type {name} is not equal to CLIENT!";
        }
    }
}

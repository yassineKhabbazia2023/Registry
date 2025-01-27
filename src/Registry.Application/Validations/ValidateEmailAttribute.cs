using System.ComponentModel.DataAnnotations;

namespace Application.Validations
{
    public class ValidateEmailAttribute : ValidationAttribute
    {
        public override bool IsValid(object value)
        {
            if (value == null) return false;

            string? email = value.ToString();

            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                return addr.Address == email;
            }
            catch
            {
                return false;
            }
        }

        public override string FormatErrorMessage(string name)
        {
            return $"{name} must be a valid email address";
        }
    }
}

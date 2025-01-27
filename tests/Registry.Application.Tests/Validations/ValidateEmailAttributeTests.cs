using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Registry.Application.Tests.Validations
{
    public class ValidateEmailAttributeTests
    {
        [Fact]
        public void NullOrEmptyEmail_ShouldReturn_EmailRequired()
        {
            var emailValidationAttr = new EmailAddressAttribute();
            string email = string.Empty;
            ValidationContext context= new ValidationContext(email);
            var validationResult = emailValidationAttr.GetValidationResult(email, context);
            Assert.NotNull(validationResult);
            validationResult?.ErrorMessage?.Equals("Email is required");
        }

        [Fact]
        public void InvalidEmail_ShouldReturn_NotValidEmailAddress()
        {
            var emailValidationAttr = new EmailAddressAttribute();
            string email = "notValidEmail1";
            ValidationContext context = new ValidationContext(email);
            var validationResult = emailValidationAttr.GetValidationResult(email, context);
            Assert.NotNull(validationResult);
            validationResult?.ErrorMessage?.Equals("Email must be valid email address");
        }

        [Fact]
        public void ValidEmail_ShouldReturn_Success()
        {
            var emailValidationAttr = new EmailAddressAttribute();
            string email = "valid_email@rydge.fr";
            ValidationContext context = new ValidationContext(email);
            var validationResult = emailValidationAttr.GetValidationResult(email, context);
            var isValid = emailValidationAttr.IsValid(email);
            Assert.Null(validationResult);
            Assert.True(isValid);
        }
    }
}

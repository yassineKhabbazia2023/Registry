using Application.Models;
using System.ComponentModel.DataAnnotations;

namespace Registry.Application.Tests.Models
{
    public class RefContactCsvTests
    {
        [Fact]
        public void FirstName_LengthExceeds255_ReturnsFailure()
        {
            // Arrange
            var refContactCsv = new RefContactCsv
            {
                FirstName = new string('a', 256),
                Operation = string.Empty,
                Email = new string('a', 256),
            };

            // Act
            var validationContext = new ValidationContext(refContactCsv);
            var results = new List<ValidationResult>();
            var isValid = Validator.TryValidateObject(refContactCsv, validationContext, results, true);

            // Assert
            Assert.False(isValid);
            Assert.Contains(results, r => r.ErrorMessage == "First Name should not exceed 255 characters");
        }

        [Fact]
        public void LastName_LengthExceeds255_ReturnsFailure()
        {
            // Arrange
            var refContactCsv = new RefContactCsv
            {
                LastName = new string('a', 256),
                Operation = string.Empty,
                Email = new string('a', 256),
            };

            // Act
            var validationContext = new ValidationContext(refContactCsv);
            var results = new List<ValidationResult>();
            var isValid = Validator.TryValidateObject(refContactCsv, validationContext, results, true);

            // Assert
            Assert.False(isValid);
            Assert.Contains(results, r => r.ErrorMessage == "Last Name should not exceed 255 characters");
        }

        [Fact]
        public void LandPhone_LengthExceeds255_ReturnsFailure()
        {
            // Arrange
            var refContactCsv = new RefContactCsv
            {
                LandPhone = new string('a', 256),
                Operation = string.Empty,
                Email = new string('a', 256),
            };

            // Act
            var validationContext = new ValidationContext(refContactCsv);
            var results = new List<ValidationResult>();
            var isValid = Validator.TryValidateObject(refContactCsv, validationContext, results, true);

            // Assert
            Assert.False(isValid);
            Assert.Contains(results, r => r.ErrorMessage == "Land Phone should not exceed 255 characters");
        }

        [Fact]
        public void MobilePhone_LengthExceeds255_ReturnsFailure()
        {
            // Arrange
            var refContactCsv = new RefContactCsv
            {
                MobilePhone = new string('a', 256),
                Operation = string.Empty,
                Email = new string('a', 256),
            };

            // Act
            var validationContext = new ValidationContext(refContactCsv);
            var results = new List<ValidationResult>();
            var isValid = Validator.TryValidateObject(refContactCsv, validationContext, results, true);

            // Assert
            Assert.False(isValid);
            Assert.Contains(results, r => r.ErrorMessage == "Mobile Phone should not exceed 255 characters");
        }

        [Fact]
        public void Email_LengthExceeds255_ReturnsFailure()
        {
            // Arrange
            var refContactCsv = new RefContactCsv
            {
                Email = new string('a', 256),
                Operation = string.Empty
            };

            // Act
            var validationContext = new ValidationContext(refContactCsv);
            var results = new List<ValidationResult>();
            var isValid = Validator.TryValidateObject(refContactCsv, validationContext, results, true);

            // Assert
            Assert.False(isValid);
            Assert.Contains(results, r => r.ErrorMessage == "Email should not exceed 255 characters");
        }

        [Fact]
        public void OfficeCode_LengthExceeds255_ReturnsFailure()
        {
            // Arrange
            var refContactCsv = new RefContactCsv
            {
                Email = string.Empty,
                Operation = string.Empty,
                OfficeCode = new string('a', 256)
            };

            // Act
            var validationContext = new ValidationContext(refContactCsv);
            var results = new List<ValidationResult>();
            var isValid = Validator.TryValidateObject(refContactCsv, validationContext, results, true);

            // Assert
            Assert.False(isValid);
            Assert.Contains(results, r => r.ErrorMessage == "Office Code should not exceed 255 characters");
        }

        [Fact]
        public void ValidateOfficeCodeAttribute_OfficeCodeIsNull_ReturnsFailure()
        {
            // Arrange
            var refContactCsv = new RefContactCsv() { Email = "validEmail@gmail.com", Operation = "INSERT", OfficeCode = string.Empty, IsCustomer = false };
            var validationContext = new ValidationContext(refContactCsv);

            // Act
            var results = new List<ValidationResult>();
            var isValid = Validator.TryValidateObject(refContactCsv, validationContext, results, true);

            // Assert
            Assert.False(isValid);
            Assert.Contains(results, r => r.ErrorMessage == "Office Code is required when contact type is collaborator");
        }


        [Fact]
        public void ValidateEmailAttribute_EmailIsInvalid_ReturnsFailure()
        {
            // Arrange
            var refContactCsv = new RefContactCsv() { Email = "invalidEmail", Operation = string.Empty, IsCustomer = false };
            var validationContext = new ValidationContext(refContactCsv);

            // Act
            var results = new List<ValidationResult>();
            var isValid = Validator.TryValidateObject(refContactCsv, validationContext, results, true);

            // Assert
            Assert.Contains(results, r => r.ErrorMessage == "Email must be a valid email address");
        }


        [Fact]
        public void ValidateOperationAttribute_OperationTypeIsNull_ReturnsFailure()
        {
            // Arrange
            var refContactCsv = new RefContactCsv() { Email = string.Empty, Operation = null };
            var validationContext = new ValidationContext(refContactCsv);

            // Act
            var results = new List<ValidationResult>();
            var isValid = Validator.TryValidateObject(refContactCsv, validationContext, results, true);

            // Assert
            Assert.False(isValid);
            Assert.Contains(results, r => r.ErrorMessage == "Operation type can not be empty!");
        }

        [Fact]
        public void ValidateOperationAttribute_OperationTypeIsUnknown_ReturnsFailure()
        {
            // Arrange
            var refContactCsv = new RefContactCsv() { Email = string.Empty, Operation = "Hakouna Matata"};
            var validationContext = new ValidationContext(refContactCsv);

            // Act
            var results = new List<ValidationResult>();
            var isValid = Validator.TryValidateObject(refContactCsv, validationContext, results, true);

            // Assert
            Assert.False(isValid);
            Assert.Contains(results, r => r.ErrorMessage == "Operation type not known!");
        }

        [Fact]
        public void ValidateOperationAttribute_ValidInstance_ShouldReturnSuccess()
        {
            // Arrange
            var refContactCsv = new RefContactCsv() { Email = "validEmail@rydge.fr", Operation = "UPDATE", OfficeCode = "ABCD123", IsCustomer = false, FirstName = "Mark" , LastName = "Something" };
            var validationContext = new ValidationContext(refContactCsv);

            // Act
            var results = new List<ValidationResult>();
            var isValid = Validator.TryValidateObject(refContactCsv, validationContext, results, true);

            // Assert
            Assert.True(isValid);
            Assert.Empty(results);
        }

    }
}
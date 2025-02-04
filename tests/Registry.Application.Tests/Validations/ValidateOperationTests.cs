
using Application.Validations;
using System.ComponentModel.DataAnnotations;

namespace Registry.Application.Tests.Validations
{
    public class ValidateOperationTests
    {
        [Fact]
        public void NullableOrEmptyOperation_ShouldBeNotValid()
        {
            var model = new { Operation = string.Empty };
            var context = new ValidationContext(model);
            var validateOperationAttr = new ValidateOperationAttribute("");
            var validationResults = validateOperationAttr.GetValidationResult(model, context);
            Assert.NotNull(validationResults);
            validationResults?.ErrorMessage?.Equals("Operation type can not be empty!");
        }

        [Fact]
        public void UnacceptedValuesOperation_ShouldBeNotValid()
        {
            var model = new { Operation = "Hakouna Matata" };
            var context = new ValidationContext(model);
            var validateOperationAttr = new ValidateOperationAttribute("HAKOUNA|MATATA");
            var validationResults = validateOperationAttr.GetValidationResult(model, context);
            Assert.NotNull(validationResults);
            validationResults?.ErrorMessage?.Equals("Operation type not known!");
        }

        [Fact]
        public void InsertUpdateDeleteOperationValue_ShouldBeValid()
        {
            var insertModel = new { Operation = "INSERT" };
            var deleteModel = new { Operation = "DELETE" };
            var updateModel = new { Operation = "UPDATE" };

            var insertContext = new ValidationContext(insertModel);
            var deleteContext = new ValidationContext(deleteModel);
            var updateContext = new ValidationContext(updateModel);

            var validateOperationAttr = new ValidateOperationAttribute("INSERT|DELETE|UPDATE");

            var insertValidationResult = validateOperationAttr.GetValidationResult(insertModel, insertContext);
            var deleteValidationResult = validateOperationAttr.GetValidationResult(deleteModel, deleteContext);
            var updateValidationResult = validateOperationAttr.GetValidationResult(updateModel, updateContext);

            Assert.Null(insertValidationResult);
            Assert.Null(deleteValidationResult);
            Assert.Null(updateValidationResult);
        }
    }
}

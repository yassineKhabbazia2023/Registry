using Application.Interfaces;
using Application.Models;
using System.ComponentModel.DataAnnotations;

namespace Application.Helpers
{
    public class ValidationHelper<T> : IValidationHelper<T>
        where T : class
    {
        private IEnumerable<ValidationResult> ValidateInstance(T model)
        {
            var validationResults = new List<ValidationResult>();
            var context = new ValidationContext(model);
            Validator.TryValidateObject(model, context, validationResults, true);
            return validationResults;
        }

        public IEnumerable<LightValidationResult> ValidateInstanceList(IEnumerable<T> contacts)
        {
            int lineNumber = 1;
            List<LightValidationResult> result = new List<LightValidationResult>();
            foreach (var contact in contacts)
            {
                IEnumerable<ValidationResult> validationResults = ValidateInstance(contact);
                if (validationResults.Any())
                {
                    LightValidationResult lightValidationResult = new LightValidationResult() { LineNumber = lineNumber, Errors = validationResults?.Select(x => x?.ErrorMessage) };
                    yield return lightValidationResult;
                }
                lineNumber++;
            }
        }
    }

   
}

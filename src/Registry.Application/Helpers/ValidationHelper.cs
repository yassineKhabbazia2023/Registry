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

        public LightValidationResult<T> Validate(IEnumerable<T> models)
        {
            int lineNumber = 1;
            LightValidationResult<T> result = new LightValidationResult<T>();
            foreach (var model in models)
            {
                IEnumerable<ValidationResult> validationResults = ValidateInstance(model);
                if (validationResults.Any())
                {
                    LightValidationError lightValidationResult = new() { LineNumber = lineNumber, Errors = validationResults?.Select(x => x?.ErrorMessage)! };
                    result.Errors.Add(lightValidationResult);
                }
                else
                {
                    result.ValidateModels.Add(model);
                }
                lineNumber++;
            }

            return result;
        }

    }

   
}

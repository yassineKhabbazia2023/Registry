namespace Application.Models
{
    public class LightValidationResult<T>
    {
        public List<T> ValidateModels = [];
        public List<LightValidationError> Errors = [];
    }

    public class LightValidationError
    {
        public int LineNumber;
        public IEnumerable<string> Errors = [];
    }
}

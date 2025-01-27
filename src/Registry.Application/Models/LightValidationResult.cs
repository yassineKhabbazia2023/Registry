using System.ComponentModel.DataAnnotations;

namespace Application.Models
{
    public class LightValidationResult
    {
        public int LineNumber;
        public IEnumerable<string> Errors = new List<string>();
    }
}

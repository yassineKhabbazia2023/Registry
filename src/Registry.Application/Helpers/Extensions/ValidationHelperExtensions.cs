using Application.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Helpers.Extensions
{
    public static class ValidationHelperExtensions
    {
        /// <summary>
        /// After the DataAnnotations pass, apply
        /// “Collaborator passed as customer” rules filter on contacts.
        /// </summary>
        public static LightValidationResult<RefContactCsv> ValidateCollabRules(
            this LightValidationResult<RefContactCsv> result)
        {

            var contactsToSkip = result.ValidateModels.Where(c => c.IsCustomer == true && c.Email.Contains("@rydge"))
                .ToList();

            result.ValidateModels = result.ValidateModels.Except(contactsToSkip).ToList();

            if (contactsToSkip.Any())
            {
                result.Errors.Add(new LightValidationError()
                {
                    LineNumber = 0,
                    Errors = new List<string>
                    {
                        $"Collaborators cannot be customers. Please check this list of addresses: [{string.Join(",",contactsToSkip.Select(c =>c.Email))}]"
                    }
                });

            }
            return result;
        }
    }
}

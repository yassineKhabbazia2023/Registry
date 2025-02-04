
using Application.Models;

namespace Application.Interfaces;
public interface IContactRegistryProvider
{
    Task<HttpResponseMessage> CreateContactAsync(Application.Models.ContactRegistry contactRegistry);

    Task<HttpResponseMessage> UpdateContactAsync(Application.Models.ContactRegistry contactRegistry);
}



using Application.Models;

namespace Application.Interfaces;
public interface IContactRegistryProvider
{
    Task<HttpResponseMessage> CreateContactAsync(ContactRegistry contactRegistry);

    Task<HttpResponseMessage> UpdateContactAsync(ContactRegistry contactRegistry);
}


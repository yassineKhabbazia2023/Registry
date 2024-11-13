
using Application.Models;

namespace Application.Interfaces;
public interface IContactRegistryProvider
{
    Task CreateContactAsync(ContactRegistry contactRegistry);

    Task UpdateContactAsync(ContactRegistry contactRegistry);
}


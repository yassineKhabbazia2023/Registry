using Application.Interfaces;
using Application.Models;
using Newtonsoft.Json;
using System.Text;

namespace Infrastructure.Providers
{
    public class ContactRegistryProvider : IContactRegistryProvider
    {
        private readonly IHttpClientFactory factory;

        public ContactRegistryProvider(IHttpClientFactory factory)
        {
            this.factory = factory;
        }

        public async Task<HttpResponseMessage> CreateContactAsync(ContactRegistry contactRegistry)
        {
            var url = "contacts";
            var json = JsonConvert.SerializeObject(contactRegistry);
            var httpClient = factory.CreateClient("RegistryApi");
            var content = new StringContent(json, encoding: Encoding.UTF8, mediaType: "application/json");

            return await httpClient.PostAsync(url, content);
        }

        public async Task<HttpResponseMessage> UpdateContactAsync(ContactRegistry contactRegistry)
        {
            var url = "contacts/pulse";
            var json = JsonConvert.SerializeObject(contactRegistry);
            var httpClient = factory.CreateClient("RegistryApi");
            var content = new StringContent(json, encoding: Encoding.UTF8, mediaType: "application/json");

            return await httpClient.PutAsync(url, content);
        }

      

    }
}

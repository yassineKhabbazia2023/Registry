using Application.Interfaces;
using Application.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System.Text;

namespace Application.Providers
{
    // TODO: Legacy provider currently has no active call path. Document its historical
    // RegistryApi dependency, then confirm whether it can be removed safely.
    public class ContactRegistryProvider : IContactRegistryProvider
    {
        private readonly IHttpClientFactory factory;

        public ContactRegistryProvider(IHttpClientFactory factory)
        {
            this.factory = factory;
        }

        public async Task<HttpResponseMessage> CreateContactAsync(Application.Models.ContactRegistry contactRegistry)
        {
            var url = "contacts";
            var json = JsonConvert.SerializeObject(contactRegistry, new JsonSerializerSettings
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver()
            });
            var httpClient = factory.CreateClient("RegistryApi");
            var content = new StringContent(json, encoding: Encoding.UTF8, mediaType: "application/json");

            return await httpClient.PostAsync(url, content);
        }

        public async Task<HttpResponseMessage> UpdateContactAsync(Application.Models.ContactRegistry contactRegistry)
        {
            var url = "contacts/pulse";
            var json = JsonConvert.SerializeObject(contactRegistry, new JsonSerializerSettings
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver()
            });
            var httpClient = factory.CreateClient("RegistryApi");
            var content = new StringContent(json, encoding: null, mediaType: "application/json");
            return await httpClient.PutAsync(url, content);
        }




    }
}

using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using BuurtApplicatie.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;

namespace BuurtApplicatie.Helpers
{
    public class AddressValidator : IAddressValidator
    {
        private const string BaseUri = "https://geodata.nationaalgeoregister.nl/locatieserver/v3/free";
        private readonly ILogger<AddressValidator> _logger;
        private readonly IConfiguration _configuration;
        private HttpClient Client { get; }

        public AddressValidator(IConfiguration configuration, 
            ILogger<AddressValidator> logger)
        {
            _logger = logger;
            _configuration = configuration;
            Client = new HttpClient
            {
                BaseAddress = new Uri(BaseUri)
            };
            Client.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json"));
        }
        
        public async Task<AddressResult> ValidateAddressAsync(Address address)
        {
            var (result, addressInfo) = await FetchAddressInfo(address);

            if (!result.Succeeded)
                return result;

            if (!addressInfo.District.Contains(_configuration["AddressValidation:District"]))
            {
                result.Error = $"{_configuration["AddressValidation:ErrorMessages:AddressNotInDistrict"]} {_configuration["AddressValidation:District"]}.";
                return result;
            }

            if (EqualAddresses(addressInfo, address))
                result.Valid = true;
            
            result.Error = _configuration["AddressValidation:ErrorMessages:InputDoesntMatchResponse"];
            return result;
        }

        private static bool EqualAddresses(AddressInfo response, Address input)
        {
            return response.City == input.City &&
                   response.Street == input.StreetName &&
                   response.PostalCode == input.PostCode &&
                   response.HouseNo == input.HouseNr;
        }
        
        private async Task<(AddressResult result, AddressInfo addressInfo)> FetchAddressInfo(Address address)
        {
            var result = new AddressResult {Succeeded = true};
            var response = await Client.GetAsync($"?fq=postcode:{address.PostCode}&fq=huisnummer:{address.HouseNr}&rows=1");
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError($"Address validation API returned with status code {response.StatusCode.ToString()}");
                result.Error = _configuration["AddressValidation:ErrorMessages:BadRequest"];
                result.Succeeded = false;
                return (result, null);
            }
            
            var responseBody = await response.Content.ReadAsStringAsync();
            var jsonResponse = JObject.Parse(responseBody).First.First; // properly destructure the response

            var resultsFound = (int) jsonResponse["numFound"];
            if (resultsFound >= 1)
            {
                var unparsedResponse = jsonResponse["docs"][0];
                return (result, ConvertJsonResponseToAddress(unparsedResponse));
            }
            
            result.Error = _configuration["AddressValidation:ErrorMessages:AddressNotFound"];
            result.Succeeded = false;
            return (result, null);
        }

        private static AddressInfo ConvertJsonResponseToAddress(JToken response)
        {
            return new AddressInfo
            {
                City = response["woonplaatsnaam"].ToString(),
                Street = response["straatnaam"].ToString(),
                PostalCode = response["postcode"].ToString(),
                HouseNo = (int) response["huisnummer"],
                District = response["buurtnaam"].ToString()
            };
        }
        
        private class AddressInfo
        {
            public string City { get; set; }
            public string Street { get; set; }
            public string PostalCode { get; set; }
            public string District { get; set; }
            public int HouseNo { get; set; }
        }
    }
}

public class AddressResult
{
    public bool Valid { get; set; }
            
    public string Error { get; set; }
    
    public bool Succeeded { get; set; }
}
using System.Threading.Tasks;
using BuurtApplicatie.Models;

namespace BuurtApplicatie.Helpers
{
    public interface IAddressValidator
    {
        Task<AddressResult> ValidateAddressAsync(Address address);
    }
}
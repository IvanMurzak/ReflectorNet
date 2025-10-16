// Complex class: Address with multiple properties
namespace com.IvanMurzak.ReflectorNet.OuterAssembly.Model
{
    public class Address
    {
        public string Street { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string? Zip { get; set; }
        public string Country { get; set; } = string.Empty;
    }
}

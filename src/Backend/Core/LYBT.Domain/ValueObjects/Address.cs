using System.Collections.Generic;
using LYBT.Domain.Common;

namespace LYBT.Domain.ValueObjects
{
    /// <summary>
    /// 地址值对象
    /// </summary>
    public class Address : ValueObject
    {
        public string Province { get; private set; }
        public string City { get; private set; }
        public string District { get; private set; }
        public string Street { get; private set; }
        public string ZipCode { get; private set; }

        protected Address() { }

        public Address(string province, string city, string district, string street, string zipCode = null)
        {
            Province = province;
            City = city;
            District = district;
            Street = street;
            ZipCode = zipCode;
        }

        public string GetFullAddress()
        {
            return $"{Province}{City}{District}{Street}";
        }

        protected override IEnumerable<object> GetEqualityComponents()
        {
            yield return Province;
            yield return City;
            yield return District;
            yield return Street;
            yield return ZipCode;
        }
    }

    /// <summary>
    /// 联系信息值对象
    /// </summary>
    public class ContactInfo : ValueObject
    {
        public string Phone { get; private set; }
        public string Email { get; private set; }
        public string EmergencyContact { get; private set; }

        protected ContactInfo() { }

        public ContactInfo(string phone, string email, string emergencyContact)
        {
            Phone = phone;
            Email = email;
            EmergencyContact = emergencyContact;
        }

        protected override IEnumerable<object> GetEqualityComponents()
        {
            yield return Phone;
            yield return Email;
            yield return EmergencyContact;
        }
    }
}
using System.Security.Cryptography;
using System.Text;
using Business.Abstract;
using Microsoft.Extensions.Configuration;

namespace Business.Concrete
{
    public class HashService : IHashService
    {
        private readonly byte[] _key;

        public HashService(IConfiguration config)
        {
            var secret = config["HashSecret"] ?? "TimeCapsule-Change-This-Secret";
            _key = Encoding.UTF8.GetBytes(secret);
        }

        public byte[] ComputeHash(string value)
        {
            using var hmac = new HMACSHA256(_key);
            return hmac.ComputeHash(Encoding.UTF8.GetBytes(value ?? string.Empty));
        }
    }
}

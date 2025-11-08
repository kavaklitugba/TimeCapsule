using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Business.Abstract
{
    public interface ICryptoService
    {
        (byte[] cipherText, byte[] iv) Encrypt(string plainText);
        string Decrypt(byte[] cipherText, byte[] iv);
        byte[] HashToken(string token);
    }
}

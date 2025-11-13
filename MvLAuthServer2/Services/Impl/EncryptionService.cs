using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Encodings;
using Org.BouncyCastle.Crypto.Engines;
using Org.BouncyCastle.OpenSsl;
using System;
using System.IO;
using System.Text;

namespace MvLAuthServer2.Services.Impl
{
    public class EncryptionService : IEncryptionService
    {
        private Pkcs1Encoding rsaEncrypter, rsaDecrypter;

        public EncryptionService()
        {
            // RSA keys
            using var rsaKeyStream = File.OpenText(Path.Combine(AppContext.BaseDirectory, "private.rsa.pem"));
            var rsaReader = new PemReader(rsaKeyStream);
            AsymmetricCipherKeyPair rsaKeyPair = (AsymmetricCipherKeyPair)rsaReader.ReadObject();

            rsaEncrypter = new Pkcs1Encoding(new RsaEngine());
            rsaEncrypter.Init(true, rsaKeyPair.Private);
            rsaDecrypter = new Pkcs1Encoding(new RsaEngine());
            rsaDecrypter.Init(false, rsaKeyPair.Public);
        }

        public string EncryptToBase64(string input)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(input);
            return Utils.ToBase64(rsaEncrypter.ProcessBlock(bytes, 0, bytes.Length));
        }

        public string DecryptFromBase64(string input)
        {
            byte[] bytes = Utils.FromBase64(input);
            return Encoding.UTF8.GetString(rsaDecrypter.ProcessBlock(bytes, 0, bytes.Length));
        }
    }
}

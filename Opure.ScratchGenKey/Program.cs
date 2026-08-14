using System;
using System.Security.Cryptography;

class Program
{
    static void Main()
    {
        using var ecdsa = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        var publicKey = ecdsa.ExportSubjectPublicKeyInfo();
        var privateKey = ecdsa.ExportPkcs8PrivateKey();
        Console.WriteLine("PublicKeyInfo: " + Convert.ToBase64String(publicKey));
        Console.WriteLine("PrivateKeyPkcs8: " + Convert.ToBase64String(privateKey));
    }
}

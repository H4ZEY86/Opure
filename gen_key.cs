using System;
using System.Security.Cryptography;

class Program
{
    static void Main()
    {
        var ed25519 = Ed25519.Create();
        var publicKey = ed25519.ExportSubjectPublicKeyInfo();
        var privateKey = ed25519.ExportPkcs8PrivateKey();
        Console.WriteLine("PublicKeyInfo: " + Convert.ToBase64String(publicKey));
        Console.WriteLine("PrivateKeyPkcs8: " + Convert.ToBase64String(privateKey));
    }
}

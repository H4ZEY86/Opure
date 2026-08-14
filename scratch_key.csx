using System;
using System.Security.Cryptography;

using var ecdsa = ECDsa.Create();
var pubKey = Convert.ToBase64String(ecdsa.ExportSubjectPublicKeyInfo());
var privKey = Convert.ToBase64String(ecdsa.ExportPkcs8PrivateKey());

Console.WriteLine($"Public Key: {pubKey}");
Console.WriteLine($"Private Key: {privKey}");

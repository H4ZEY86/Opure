using System;
using System.IO;
using System.Threading.Tasks;

namespace Opure.Desktop.GatewayClient;

public static class LicenseGatewayClient
{
    public static async Task<bool> ApplyLicenseAsync(string licenseKey)
    {
        try
        {
            // The MSIX Desktop app runs with runFullTrust, meaning it shares the same %LOCALAPPDATA%
            // as the CLI daemon. We write the license key exactly as the CLI does to avoid spinning up processes.
            string appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string licensePath = Path.Combine(appData, "Opure", "license.dat");
            
            Directory.CreateDirectory(Path.GetDirectoryName(licensePath)!);
            await File.WriteAllTextAsync(licensePath, licenseKey);
            
            return true;
        }
        catch
        {
            return false;
        }
    }
}

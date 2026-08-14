using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace Opure.Desktop.Contracts;

public sealed class DesktopLicenseViewModel : INotifyPropertyChanged
{
    private string licenseKey = string.Empty;
    private string feedbackMessage = string.Empty;
    private bool isApplying;
    private bool hasValidLicense;

    public DesktopLicenseViewModel()
    {
    }

    public string LicenseKey
    {
        get => licenseKey;
        set => SetProperty(ref licenseKey, value);
    }

    public string FeedbackMessage
    {
        get => feedbackMessage;
        set => SetProperty(ref feedbackMessage, value);
    }

    public bool IsApplying
    {
        get => isApplying;
        private set => SetProperty(ref isApplying, value);
    }

    public bool HasValidLicense
    {
        get => hasValidLicense;
        set => SetProperty(ref hasValidLicense, value);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (!Equals(field, value))
        {
            field = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public async Task ApplyLicenseAsync(Func<string, Task<bool>> applyAction)
    {
        if (string.IsNullOrWhiteSpace(LicenseKey))
        {
            FeedbackMessage = "Please enter a valid license key.";
            return;
        }

        IsApplying = true;
        FeedbackMessage = "Applying license...";

        try
        {
            bool success = await applyAction(LicenseKey);
            if (success)
            {
                FeedbackMessage = "License applied successfully! Features unlocked.";
                HasValidLicense = true;
                LicenseKey = string.Empty;
            }
            else
            {
                FeedbackMessage = "Failed to apply license. Please check the key.";
            }
        }
        catch (Exception ex)
        {
            FeedbackMessage = $"Error: {ex.Message}";
        }
        finally
        {
            IsApplying = false;
        }
    }
}

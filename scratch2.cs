using System;

class Program {
    static void Main() {
        var env = Opure.Bootstrap.Windows.BootstrapChildEnvironment.Create(
            Opure.Bootstrap.Windows.BootstrapChannel.Test,
            ""C:\\root"",
            new Opure.Bootstrap.Windows.BootstrapSession(""0123456789abcdef0123456789abcdef"", ""0123456789abcdef0123456789abcdef0123456789a""),
            123,
            DateTimeOffset.UtcNow);
        Console.WriteLine(""OPURE_CHANNEL="" + env[""OPURE_CHANNEL""]);
    }
}

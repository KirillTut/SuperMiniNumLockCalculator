// Program.cs — NumLock Calculator 4.0 entry point
internal static class Program
{
    private const string MutexName = "NLCalc2-SingleInstance-8F3A1B2C";

    [STAThread]
    static void Main()
    {
        using var mutex = new System.Threading.Mutex(true, MutexName, out bool createdNew);
        if (!createdNew)
        {
            // Another instance is running — signal it to show itself via a named event
            try
            {
                using var evt = new System.Threading.EventWaitHandle(
                    false, System.Threading.EventResetMode.AutoReset,
                    "NLCalc2-ShowWindow-8F3A1B2C");
                evt.Set();
            }
            catch { }
            return;
        }

        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        Application.SetColorMode(SystemColorMode.System);
        Application.Run(new MainForm());
    }
}


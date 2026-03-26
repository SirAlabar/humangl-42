using System;

namespace HumanGL
{
    internal static class Program
    {
        private static int Main()
        {
            try
            {
                using App app = new App();
                app.Run();
                return 0;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Fatal: {ex.Message}");
                return 1;
            }
        }
    }
}

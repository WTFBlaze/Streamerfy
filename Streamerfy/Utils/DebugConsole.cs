using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Streamerfy.Utils
{
    public static class DebugConsole
    {
        [DllImport("kernel32.dll")]
        private static extern bool AllocConsole();

        [DllImport("kernel32.dll")]
        private static extern bool FreeConsole();

        private static bool _isConsoleAllocated = false;

        public static void Init()
        {
#if DEBUG
            if (Debugger.IsAttached && !_isConsoleAllocated)
            {
                _isConsoleAllocated = AllocConsole();
                Console.WriteLine("Console initialized for debugging.");
            }
#endif
        }

        public static void Shutdown()
        {
#if DEBUG
            if (_isConsoleAllocated)
            {
                Console.WriteLine("Cleaning up console...");
                FreeConsole();
                _isConsoleAllocated = false;
            }
#endif
        }
    }
}

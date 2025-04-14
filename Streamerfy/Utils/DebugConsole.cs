using Streamerfy.Services;
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
            }
#endif
        }

        public static void Shutdown()
        {
#if DEBUG
            if (_isConsoleAllocated)
            {
                FreeConsole();
                _isConsoleAllocated = false;
            }
#endif
        }
    }
}

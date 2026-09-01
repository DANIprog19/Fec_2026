using System;
using System.Threading;
using Avalonia;
using Sistema; 

namespace Sistema
{
    internal class Program
    {
        private static Mutex? _mutex;
        private const string MutexName = "ProjFec_Sistema_Unico_Mutex";

        [STAThread]
        public static void Main(string[] args)
        {
            bool criadoNovo;
            _mutex = new Mutex(true, MutexName, out criadoNovo);

            if (!criadoNovo)
            {
                Console.WriteLine("[SISTEMA] ⚠️ Já existe uma instância do aplicativo rodando.");
                return;
            }

            try
            {
                BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
            }
            finally
            {
                _mutex.ReleaseMutex();
            }
        }

        public static AppBuilder BuildAvaloniaApp()
            => AppBuilder.Configure<App>() 
                .UsePlatformDetect()
                .WithInterFont()
                .LogToTrace();
    }
}
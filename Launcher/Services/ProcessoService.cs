using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace Launcher.Services;

public class ProcessoService
{
    public async Task<bool> IniciarTudoAsync(
        Action<string>? atualizarStatus = null)
    {
        try
        {
            atualizarStatus?.Invoke(
                "Localizando Sistema..."
            );

            await Task.Delay(1000);

            DirectoryInfo? pastaAtual = new DirectoryInfo(AppContext.BaseDirectory);
            DirectoryInfo? pastaProjFec = null;
            string pastaSistema = string.Empty;

            while (pastaAtual != null)
            {
                string testeSistema = Path.Combine(pastaAtual.FullName, "Sistema");
                if (Directory.Exists(testeSistema))
                {
                    pastaProjFec = pastaAtual;
                    pastaSistema = testeSistema;
                    break;
                }
                pastaAtual = pastaAtual.Parent;
            }

            if (pastaProjFec == null || string.IsNullOrEmpty(pastaSistema))
            {
                atualizarStatus?.Invoke(
                    "Não foi possível localizar a pasta do Sistema."
                );

                Console.WriteLine(
                    "[LAUNCHER] ❌ Pasta Sistema não encontrada."
                );

                return false;
            }

            Console.WriteLine(
                $"[LAUNCHER] 📁 Sistema encontrado em: {pastaSistema}"
            );

            string executavelDebug =
                Path.Combine(
                    pastaSistema,
                    "bin",
                    "Debug",
                    "net10.0",
                    "Sistema"
                );

            string executavelRelease =
                Path.Combine(
                    pastaSistema,
                    "bin",
                    "Release",
                    "net10.0",
                    "Sistema"
                );

            string dllDebug =
                Path.Combine(
                    pastaSistema,
                    "bin",
                    "Debug",
                    "net10.0",
                    "Sistema.dll"
                );

            string dllRelease =
                Path.Combine(
                    pastaSistema,
                    "bin",
                    "Release",
                    "net10.0",
                    "Sistema.dll"
                );

            string? executavel = null;
            bool usarDotnet = false;

            if (File.Exists(executavelDebug))
            {
                executavel = executavelDebug;
            }
            else if (File.Exists(executavelRelease))
            {
                executavel = executavelRelease;
            }
            else if (File.Exists(dllDebug))
            {
                executavel = dllDebug;
                usarDotnet = true;
            }
            else if (File.Exists(dllRelease))
            {
                executavel = dllRelease;
                usarDotnet = true;
            }

            if (executavel == null)
            {
                atualizarStatus?.Invoke(
                    "Executável do Sistema não encontrado."
                );

                Console.WriteLine(
                    "[LAUNCHER] ❌ Compile o projeto Sistema primeiro."
                );

                return false;
            }

            Console.WriteLine(
                $"[LAUNCHER] 🚀 Executando: {executavel}"
            );

            atualizarStatus?.Invoke(
                "Iniciando sistema..."
            );
            
            await Task.Delay(3500);

            ProcessStartInfo psi;

            if (usarDotnet)
            {
                psi = new ProcessStartInfo
                {
                    FileName = "dotnet",
                    UseShellExecute = false,
                    WorkingDirectory = pastaSistema
                };

                psi.ArgumentList.Add(executavel);
            }
            else
            {
                psi = new ProcessStartInfo
                {
                    FileName = executavel,
                    UseShellExecute = false,
                    WorkingDirectory = pastaSistema
                };
            }

            Process? processo =
                Process.Start(psi);

            if (processo == null)
            {
                atualizarStatus?.Invoke(
                    "Não foi possível iniciar o Sistema."
                );

                return false;
            }

            Console.WriteLine(
                $"[LAUNCHER] ✅ Sistema iniciado. PID: {processo.Id}"
            );

            atualizarStatus?.Invoke(
                "Sistema iniciado!"
            );

            await Task.Delay(1500);

            return true;
        }
        catch (Exception ex)
        
        {
            Console.WriteLine(
                $"[LAUNCHER] ❌ Erro ao iniciar Sistema: {ex}"
            );

            atualizarStatus?.Invoke(
                $"Erro: {ex.Message}"
            );

            return false;
        }
    }

}
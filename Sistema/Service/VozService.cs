using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Sistema.Services
{
    public class VozService
    {
        private static Process? _processoTtsAtual;
        private static Process? _processoMpvAtual;

        private static CancellationTokenSource?
            _cancelamentoAtual;

        private static readonly object _lock =
            new object();

        public async Task FalarRespostaDoOllamaAsync(
    string texto)
{
    if (string.IsNullOrWhiteSpace(texto))
        return;

    CancellationTokenSource cts;

    lock (_lock)
    {
        PararVozInterno();

        _cancelamentoAtual =
            new CancellationTokenSource();

        cts = _cancelamentoAtual;
    }

    CancellationToken token =
        cts.Token;

    string arquivoAudio =
        Path.Combine(
            AppContext.BaseDirectory,
            $"voz_{Guid.NewGuid():N}.mp3"
        );

    try
    {


        var tts =
            new ProcessStartInfo
            {
                FileName = "edge-tts",
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };

        tts.ArgumentList.Add("--voice");
        tts.ArgumentList.Add(
            "pt-BR-FranciscaNeural"
        );

        tts.ArgumentList.Add("--text");
        tts.ArgumentList.Add(texto);

        tts.ArgumentList.Add("--write-media");
        tts.ArgumentList.Add(arquivoAudio);

        var processoTts =
            Process.Start(tts);

        if (processoTts == null)
        {
            Console.WriteLine(
                "[VOZ] ❌ Não foi possível iniciar Edge-TTS."
            );

            return;
        }

        lock (_lock)
        {
            _processoTtsAtual =
                processoTts;
        }

        Console.WriteLine(
            "[VOZ] 🎙️ Edge-TTS iniciado."
        );

        await processoTts.WaitForExitAsync(token);

        token.ThrowIfCancellationRequested();

        if (processoTts.ExitCode != 0)
        {
            string erro =
                await processoTts.StandardError.ReadToEndAsync();

            Console.WriteLine(
                $"[VOZ] ❌ Edge-TTS falhou. ExitCode: {processoTts.ExitCode}"
            );

            if (!string.IsNullOrWhiteSpace(erro))
            {
                Console.WriteLine(
                    $"[VOZ] ❌ Edge-TTS: {erro}"
                );
            }

            return;
        }

        if (!File.Exists(arquivoAudio))
        {
            Console.WriteLine(
                "[VOZ] ❌ Edge-TTS não criou o arquivo."
            );

            return;
        }

        FileInfo informacoesAudio =
            new FileInfo(arquivoAudio);

        Console.WriteLine(
            $"[VOZ] 🎵 Áudio gerado: {informacoesAudio.Length} bytes"
        );

        if (informacoesAudio.Length < 1000)
        {
            Console.WriteLine(
                "[VOZ] ❌ Arquivo de áudio parece inválido."
            );

            return;
        }

        var mpv =
            new ProcessStartInfo
            {
                FileName = "mpv",
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };

        mpv.ArgumentList.Add("--no-video");
        mpv.ArgumentList.Add("--really-quiet");
        mpv.ArgumentList.Add(arquivoAudio);

        var processoMpv =
            Process.Start(mpv);

        if (processoMpv == null)
        {
            Console.WriteLine(
                "[VOZ] ❌ Não foi possível iniciar MPV."
            );

            return;
        }

        lock (_lock)
        {
            _processoMpvAtual =
                processoMpv;
        }

        Console.WriteLine(
            "[VOZ] 🔊 MPV iniciado."
        );

        await processoMpv.WaitForExitAsync(token);

        token.ThrowIfCancellationRequested();

        if (processoMpv.ExitCode != 0)
        {
            string erroMpv =
                await processoMpv.StandardError.ReadToEndAsync();

            Console.WriteLine(
                $"[VOZ] ❌ MPV falhou. ExitCode: {processoMpv.ExitCode}"
            );

            if (!string.IsNullOrWhiteSpace(erroMpv))
            {
                Console.WriteLine(
                    $"[VOZ] ❌ MPV: {erroMpv}"
                );
            }

            return;
        }

        Console.WriteLine(
            "[VOZ] ✅ Fala concluída."
        );
    }
    catch (OperationCanceledException)
    {
        Console.WriteLine(
            "[VOZ] ⛔ Fala cancelada."
        );
    }
    catch (Exception ex)
    {
        Console.WriteLine(
            $"[VOZ] ❌ Erro: {ex}"
        );
    }
    finally
    {
        lock (_lock)
        {
            if (
                _processoTtsAtual != null &&
                _processoTtsAtual.HasExited
            )
            {
                _processoTtsAtual.Dispose();
                _processoTtsAtual = null;
            }

            if (
                _processoMpvAtual != null &&
                _processoMpvAtual.HasExited
            )
            {
                _processoMpvAtual.Dispose();
                _processoMpvAtual = null;
            }

            if (
                ReferenceEquals(
                    _cancelamentoAtual,
                    cts
                )
            )
            {
                _cancelamentoAtual = null;
            }
        }

        try
        {
            if (File.Exists(arquivoAudio))
                File.Delete(arquivoAudio);
        }
        catch
        {
            // Não interrompe o programa
        }
    }
}


        public static void PararVoz()
        {
            lock (_lock)
            {
                PararVozInterno();
            }
        }

        private static void PararVozInterno()
        {
            try
            {


                if (_cancelamentoAtual != null)
                {
                    _cancelamentoAtual.Cancel();
                    _cancelamentoAtual.Dispose();
                    _cancelamentoAtual = null;
                }

                if (
                    _processoTtsAtual != null &&
                    !_processoTtsAtual.HasExited
                )
                {
                    try
                    {
                        _processoTtsAtual.Kill(
                            entireProcessTree: true
                        );
                    }
                    catch
                    {
                    }

                    Console.WriteLine(
                        "[VOZ] 🛑 Edge-TTS parado."
                    );
                }

                _processoTtsAtual?.Dispose();
                _processoTtsAtual = null;


                if (
                    _processoMpvAtual != null &&
                    !_processoMpvAtual.HasExited
                )
                {
                    try
                    {
                        _processoMpvAtual.Kill(
                            entireProcessTree: true
                        );
                    }
                    catch
                    {
            
                    }

                    Console.WriteLine(
                        "[VOZ] 🔇 MPV parado."
                    );
                }

                _processoMpvAtual?.Dispose();
                _processoMpvAtual = null;
            }
            catch (Exception ex)
            {
                Console.WriteLine(
                    $"[VOZ] ❌ Erro ao parar voz: {ex.Message}"
                );
            }
        }
    }
}
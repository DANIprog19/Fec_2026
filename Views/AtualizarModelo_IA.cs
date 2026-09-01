using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
using System;
using System.Diagnostics; 
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Sistema.Services;

namespace Sistema.Views
{
    public partial class JanelaPrincipal : UserControl
    {
        private static readonly HttpClient httpClient = new HttpClient()
        {
            Timeout = TimeSpan.FromMinutes(3)
        };

        private string NomeUsuarioAtual = "convidado"; 
        private bool _apresentacaoIAConcluida = false;


        public void DefinirUsuarioAtual(string nome)
{
    if (!string.IsNullOrWhiteSpace(nome))
    {
        NomeUsuarioAtual = nome.Trim();

        Console.WriteLine(
            $"[SESSAO] Usuário definido na JanelaPrincipal: {NomeUsuarioAtual}"
        );
    }
}

public async Task InicializarApresentacaoIA()
{
    BloquearEntradaDica();

    string mensagem =
        "Olá! eu sou a Inside, sua assistente pessoal nessa apresentação. " +
        "Aperte no botão de foto para capturar a imagem do objeto e ver ele ser renderizado em forma de jogo!";

    if (TextoEstadoCentral != null)
        TextoEstadoCentral.Text = "Inside está falando...";

    try
    {
        var vozService =
            new Sistema.Services.VozService();

        Console.WriteLine(
            "[INSIDE] 🎙️ Iniciando apresentação..."
        );

        await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
        {
            IniciarOndaVoz();
        });

        await vozService.FalarRespostaDoOllamaAsync(
            mensagem
        );

        Console.WriteLine(
            "[INSIDE] ✅ Apresentação terminou."
        );
    }
    catch (Exception ex)
    {
        Console.WriteLine(
            $"[INSIDE] ❌ Erro na apresentação: {ex.Message}"
        );
    }
    finally
    {
        await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
        {
            PararOndaVoz();

            LiberarEntradaDica();

            if (TextoEstadoCentral != null)
                TextoEstadoCentral.Text = "";
        });
    }
}

        private async void CapturarImagem_Clique(
    object sender,
    RoutedEventArgs e)
{
    if (TextoEstadoCentral != null)
        TextoEstadoCentral.Text =
            "Procurando câmera disponível...";

    try
    {

        string caminhoCompletoDestino =
            await Task.Run(() =>
            {
                string pastaDestino =
                    Path.Combine(
                        Directory.GetCurrentDirectory(),
                        "foto_capturada"
                    );

                if (!Directory.Exists(pastaDestino))
                {
                    Directory.CreateDirectory(
                        pastaDestino
                    );
                }

                string nomeArquivoFinal =
                    $"foto_{NomeUsuarioAtual}.jpg";

                string caminhoDestino =
                    Path.Combine(
                        pastaDestino,
                        nomeArquivoFinal
                    );

                if (File.Exists(caminhoDestino))
                {
                    try
                    {
                        File.Delete(caminhoDestino);
                    }
                    catch
                    {
                        Console.WriteLine(
                            "[CAMERA] ⚠️ Não foi possível apagar foto anterior."
                        );
                    }
                }

                Console.WriteLine(
                    "[CAMERA] 🔎 Procurando uma câmera disponível..."
                );

                string fotoCapturada =
                    CapturarFotoDeQualquerCamera(
                        caminhoDestino
                    );

                if (!File.Exists(fotoCapturada))
                {
                    throw new Exception(
                        "O FFmpeg não conseguiu gravar " +
                        "o arquivo da câmera."
                    );
                }

                Console.WriteLine(
                    $"[CAMERA] ✅ Foto capturada: {fotoCapturada}"
                );

                return fotoCapturada;
            });

        if (
            File.Exists(caminhoCompletoDestino) &&
            ExibicaoImagemCapturada != null
        )
        {
            await using var fs =
                new FileStream(
                    caminhoCompletoDestino,
                    FileMode.Open,
                    FileAccess.Read,
                    FileShare.Read
                );

            ExibicaoImagemCapturada.Source =
                new Bitmap(fs);
        }

        if (TextoEstadoCentral != null)
        {
            TextoEstadoCentral.Text =
                "Foto capturada! Pronto para a IA.";
        }

        Console.WriteLine(
            "=========================================="
        );

        Console.WriteLine(
            "[CAMERA] 📷 Foto capturada:"
        );

        Console.WriteLine(
            caminhoCompletoDestino
        );

        Console.WriteLine(
            "=========================================="
        );

        if (TextoObjetoIdentificado != null)
        {
            TextoObjetoIdentificado.Text =
                $"ANALISANDO...";
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine(
            $"[CAMERA] ❌ Erro: {ex}"
        );

        if (TextoEstadoCentral != null)
        {
            TextoEstadoCentral.Text =
                $"Erro ao capturar da câmera: {ex.Message}";
        }
    }
}
private void ExcluirImagem_Clique(object sender, RoutedEventArgs e)
{
    PararScannerCompleto();

    if (ExibicaoImagemCapturada != null)
        ExibicaoImagemCapturada.Source = null;

    if (TextoObjetoIdentificado != null)
        TextoObjetoIdentificado.Text = "STATUS: PRONTO";

    if (TextoEstadoCentral != null)
        TextoEstadoCentral.Text =
            "Imagem removida. Aguardando nova seleção...";

    Console.WriteLine("[IMAGEM] 🗑️ Imagem removida e scanner parado.");
}

private async void AnalisarImagem_Clique(
    object sender,
    RoutedEventArgs e)
{
 

    if (!_apresentacaoIAConcluida)
    {
        if (TextoEstadoCentral != null)
        {
            TextoEstadoCentral.Text =
                "Aguarde a apresentação da Inside terminar.";
        }

        return;
    }


    IniciarScannerVisual();

    if (BotaoAnalisarImagem != null)
        BotaoAnalisarImagem.IsEnabled = false;

    if (TextoEstadoCentral != null)
        TextoEstadoCentral.Text =
            "🔍 SCANEANDO OBJETO...";

    try
    {
       

        string objetoDetectado =
            await ChamarOllamaParaIdentificarObjetoAsync();

        if (string.IsNullOrWhiteSpace(objetoDetectado))
        {
            if (TextoEstadoCentral != null)
            {
                TextoEstadoCentral.Text =
                    "Não consegui identificar o objeto. " +
                    "Digite o nome abaixo e envie.";
            }

            return;
        }

        string palavraFinal =
            objetoDetectado
                .Trim()
                .ToUpperInvariant();


        string emojiFinal =
            EmojiObjetoService.ObterEmojiCorrespondente(
                palavraFinal
            );

        Console.WriteLine(
            $"[IA] Objeto identificado: {palavraFinal}"
        );

        Console.WriteLine(
            $"[IA] Emoji mapeado: {emojiFinal}"
        );
        PararScannerCompleto();

  
        await FinalizarAnaliseEAbrirJogoAsync(
            palavraFinal,
            emojiFinal
        );


        if (TextoEstadoCentral != null)
        {
            TextoEstadoCentral.Text =
                $"{emojiFinal} Objeto identificado: {palavraFinal}";
        }

        Console.WriteLine(
            "[IA] ✅ Jogo carregado com sucesso."
        );
    }
    catch (Exception ex)
    {
        if (TextoEstadoCentral != null)
        {
            TextoEstadoCentral.Text =
                "Não consegui analisar a imagem. " +
                "Digite o objeto manualmente.";
        }

        Console.WriteLine(
            $"[IA] ❌ Erro na análise: {ex}"
        );
    }
    finally
{
    PararScannerCompleto();

    if (BotaoAnalisarImagem != null)
        BotaoAnalisarImagem.IsEnabled =
            _apresentacaoIAConcluida;
}
}
       
private async Task<string> ChamarOllamaParaIdentificarObjetoAsync()
{
    try
    {
        

        string pastaProjeto = Directory.GetCurrentDirectory();

      
        string script = Path.Combine(
            pastaProjeto,
            "scripts",
            "ConsultarListaImagens.py"
        );

        Console.WriteLine("=================================");
        Console.WriteLine("ANÁLISE COM QWEN3-VL");
        Console.WriteLine($"Pasta projeto: {pastaProjeto}");
        Console.WriteLine($"Script: {script}");
        Console.WriteLine($"Script existe: {File.Exists(script)}");
        Console.WriteLine("=================================");

        if (!File.Exists(script))
        {
            Console.WriteLine(
                $"[QWEN] ❌ Script não encontrado: {script}"
            );

            return "";
        }

       

        string pastaDestino = Path.Combine(
            pastaProjeto,
            "foto_capturada"
        );

        string caminhoArquivo = Path.Combine(
            pastaDestino,
            $"foto_{NomeUsuarioAtual}.jpg"
        );

        Console.WriteLine(
            $"[QWEN] Foto: {caminhoArquivo}"
        );

        Console.WriteLine(
            $"[QWEN] Foto existe: {File.Exists(caminhoArquivo)}"
        );

        if (!File.Exists(caminhoArquivo))
        {
            Console.WriteLine(
                "[QWEN] ❌ Foto não encontrada."
            );

            return "";
        }

      

        string python = Path.Combine(
            pastaProjeto,
            ".venv",
            "bin",
            "python3"
        );

        if (!File.Exists(python))
        {
            Console.WriteLine(
                "[QWEN] ⚠️ Python da venv não encontrado."
            );

            python = "python3";
        }

        Console.WriteLine(
            $"[QWEN] Python: {python}"
        );

        var psi = new ProcessStartInfo
        {
            FileName = python,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };

        psi.ArgumentList.Add(script);
        psi.ArgumentList.Add(caminhoArquivo);

        Console.WriteLine(
            "[QWEN] 🚀 Executando análise..."
        );

        var cronometro =
            System.Diagnostics.Stopwatch.StartNew();

        using var processo = Process.Start(psi);

        if (processo == null)
        {
            Console.WriteLine(
                "[QWEN] ❌ Não foi possível iniciar o Python."
            );

            return "";
        }

        string saida =
            await processo.StandardOutput.ReadToEndAsync();

        string erro =
            await processo.StandardError.ReadToEndAsync();

        await processo.WaitForExitAsync();

        cronometro.Stop();

        Console.WriteLine(
            $"[QWEN] ⏱️ Tempo total: " +
            $"{cronometro.Elapsed.TotalSeconds:F2}s"
        );

        Console.WriteLine("=================================");
        Console.WriteLine("[QWEN] SAÍDA DO PYTHON:");
        Console.WriteLine(saida);

        if (!string.IsNullOrWhiteSpace(erro))
        {
            Console.WriteLine("=================================");
            Console.WriteLine("[QWEN] ERRO DO PYTHON:");
            Console.WriteLine(erro);
        }

        Console.WriteLine(
            $"[QWEN] ExitCode: {processo.ExitCode}"
        );

        Console.WriteLine("=================================");

        
        string prefixo = "OBJETO_FINAL=";

        foreach (string linha in saida.Split(
            new[] { '\r', '\n' },
            StringSplitOptions.RemoveEmptyEntries))
        {
            string linhaLimpa = linha.Trim();

            if (linhaLimpa.StartsWith(
                prefixo,
                StringComparison.OrdinalIgnoreCase))
            {
                string objeto = linhaLimpa
                    .Substring(prefixo.Length)
                    .Trim();

                if (string.IsNullOrWhiteSpace(objeto))
                {
                    Console.WriteLine(
                        "[QWEN] ⚠️ OBJETO_FINAL está vazio."
                    );

                    return "";
                }

                objeto = objeto.ToUpperInvariant();

                Console.WriteLine(
                    $"[QWEN] ✅ OBJETO FINAL = {objeto}"
                );

                return objeto;
            }
        }

        Console.WriteLine(
            "[QWEN] ❌ Não encontrei OBJETO_FINAL na saída."
        );

        return "";
    }
    catch (Exception ex)
    {
        Console.WriteLine(
            $"[QWEN] ❌ EXCEÇÃO: {ex}"
        );

        return "";
    }

}        private void DispararInicioJogoComObjeto(string nomeObjeto)
        {
           
        }

        
        private void DesenharOndasEcocardiograma()
        {
            if (CanvasOndasVoz == null) return;

            CanvasOndasVoz.Children.Clear();
            
            var onda = new Avalonia.Controls.Shapes.Polyline
            {
                Stroke = Brushes.Cyan,
                StrokeThickness = 2
            };

            var pontos = new Avalonia.Collections.AvaloniaList<Avalonia.Point>
            {
                new Avalonia.Point(0, 50),
                new Avalonia.Point(30, 50),
                new Avalonia.Point(45, 15),
                new Avalonia.Point(60, 85),
                new Avalonia.Point(75, 50),
                new Avalonia.Point(120, 50),
                new Avalonia.Point(135, 20),
                new Avalonia.Point(150, 80),
                new Avalonia.Point(165, 50),
                new Avalonia.Point(350, 50)
            };

            onda.Points = pontos;
            CanvasOndasVoz.Children.Add(onda);
        }
    }
}
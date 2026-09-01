using System;
using System.IO;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Threading;
using System.Threading;
using Avalonia.Collections;
using Sistema.Models;
using System.Collections.ObjectModel;
using Avalonia.Layout;
using Avalonia.Media.Imaging;
using Sistema.Services;
using System.Linq;

namespace Sistema.Views
{
    public partial class JanelaPrincipal : UserControl
    {
        public event EventHandler? VoltarParaLoginSolicitado;
        private Process? _scannerAudioProcess;
        private CancellationTokenSource? _scannerAudioCancellation;
        private Task? _scannerAudioTask;
        private DispatcherTimer? _timerRadar;
        private DispatcherTimer? _timerHardware;
        private long _prevIdleTime = 0;
        private long _prevTotalTime = 0;
        private double _posicaoXNave = -60;
        private Random _random = new Random();
        private string _estadoRadar = "MOVENDO"; 
        private int _contadorPausa = 0;
        private DispatcherTimer? _timerScanner;
        private double _posicaoScanner;
        private bool _scannerAtivo;
        private const int TempoPausaMaximo = 130; 
        private List<string> _listaCaminhosMusicas = new List<string>();
        private List<string> _listaNomesMusicas = new List<string>();
        private int _indiceMusicaAtual = 0;
        private bool _estaReproduzindo = true;
        private Process? _processoAudio = null;
        private double _posicaoAtualSegundos = 0;
        private DateTime _tempoInicioReproducao;
        private bool _estaMutado = false;
        private DispatcherTimer? _timerOndaVoz;

        public JanelaPrincipal()
{
    InitializeComponent(); 

    CarregarDadosHardware();
    IniciarAnimacaoRadar();
    IniciarMonitoramentoHardware();
    CarregarMusicasDaPasta();
    AtualizarRankingPontuacoes(5);

    DataContext = this;


    _timerOndaVoz = new DispatcherTimer
    {
        Interval = TimeSpan.FromMilliseconds(50)
    };

    _timerOndaVoz.Tick += AnimarOndaVoz;

    this.Loaded += JanelaPrincipal_Loaded;
}
protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == IsVisibleProperty && change.NewValue is bool visivel && visivel)
        {
            AtualizarRankingAutomatico();
        }
    }
       private async void JanelaPrincipal_Loaded(
    object? sender,
    RoutedEventArgs e)
{
    this.Loaded -= JanelaPrincipal_Loaded;

    BotaoAnalisarImagem.IsEnabled = false;

    await InicializarApresentacaoIA();

    BotaoAnalisarImagem.IsEnabled = true;
}

        private void IniciarOndaVoz()
        {
            _timerOndaVoz?.Start();
        }


        private void CarregarDadosHardware()
        {
            string processador = Environment.GetEnvironmentVariable("PROCESSOR_IDENTIFIER") ?? "Processador Desconhecido";
            int nucleos = Environment.ProcessorCount;
            if (TextoEspecificacoesHardware != null)
            {
                TextoEspecificacoesHardware.Text = $"{processador} ({nucleos} Núcleos)";
            }
        }
        public void AtualizarRankingAutomatico()
{
    int limite = 5;
    if (NumRankingPontuacoes != null && NumRankingPontuacoes.Value.HasValue)
    {
        limite = (int)NumRankingPontuacoes.Value.Value;
    }
    AtualizarRankingPontuacoes(limite);
}


        private void BotaoVoltarLoginPrincipal_Clique(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            PararAudioAtual();
            VoltarParaLoginSolicitado?.Invoke(this, EventArgs.Empty);
        }

        private void IniciarAnimacaoRadar()
        {
            _timerRadar = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(16) 
            };

            _timerRadar.Tick += (s, e) =>
            {
                double centroTelaX = 110; 

                if (_estadoRadar == "MOVENDO")
                {
                    _posicaoXNave += 3.5; 

                    if (_posicaoXNave >= centroTelaX && _posicaoXNave <= (centroTelaX + 3))
                    {
                        _posicaoXNave = centroTelaX; 
                        _estadoRadar = "PAUSADO";    
                        _contadorPausa = 0;          
                    }

                    if (_posicaoXNave > 320)
                    {
                        _posicaoXNave = -70; 
                    }
                }
                else if (_estadoRadar == "PAUSADO")
                {
                    _contadorPausa++;
                    if (_contadorPausa >= TempoPausaMaximo)
                    {
                        _estadoRadar = "MOVENDO";
                    }
                }

                Canvas.SetLeft(NaveEspacialMovel, _posicaoXNave);

                double centroXNave = _posicaoXNave + 25; 
                Canvas.SetLeft(AlvoExterno, centroXNave - 40); 
                Canvas.SetTop(AlvoExterno, 18); 

                Canvas.SetLeft(AlvoInterno, centroXNave - 20); 
                Canvas.SetTop(AlvoInterno, 38); 
            };

            _timerRadar.Start();
        }
        private async Task TocarSomScannerAsync()
{
    try
    {
        string caminhoSom = Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory,
            "SomJogo",
            "Scanner.mp3"
        );

        if (!File.Exists(caminhoSom))
        {
            Console.WriteLine(
                $"[SCANNER] ⚠️ Som não encontrado: {caminhoSom}"
            );

            return;
        }

        _scannerAudioCancellation?.Cancel();

        _scannerAudioCancellation =
            new CancellationTokenSource();

        CancellationToken token =
            _scannerAudioCancellation.Token;

        Console.WriteLine(
            "[SCANNER] 🔊 Loop de som iniciado."
        );

        while (_scannerAtivo && !token.IsCancellationRequested)
        {
            try
            {
                _scannerAudioProcess = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "mpv",
                        Arguments =
                            $"--no-video " +
                            $"--really-quiet " +
                            $"\"{caminhoSom}\"",

                        UseShellExecute = false,
                        CreateNoWindow = true
                    }
                };

                _scannerAudioProcess.Start();

                Console.WriteLine(
                    "[SCANNER] 🔊 Som iniciado."
                );

                // Espera o som terminar
                await _scannerAudioProcess
                    .WaitForExitAsync(token);

                _scannerAudioProcess.Dispose();
                _scannerAudioProcess = null;

               
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                Console.WriteLine(
                    $"[SCANNER] ❌ Erro no áudio: {ex.Message}"
                );

                break;
            }
        }

        Console.WriteLine(
            "[SCANNER] 🔇 Loop de som encerrado."
        );
    }
    catch (Exception ex)
    {
        Console.WriteLine(
            $"[SCANNER] ❌ Erro no loop de áudio: {ex.Message}"
        );
    }
}

        private void IniciarMonitoramentoHardware()
        {
            _timerHardware = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(2) 
            };

            _timerHardware.Tick += (s, e) =>
            {
                AtualizarUsoMemoria();
                AtualizarUsoCpuSimuladoOuReal();
            };

            _timerHardware.Start();
        }

        private void AtualizarUsoMemoria()
        {
            try
            {
                if (File.Exists("/proc/meminfo"))
                {
                    long totalKb = 0, livreKb = 0, buffersKb = 0, cachedKb = 0; 

                    foreach (var linha in File.ReadLines("/proc/meminfo"))
                    {
                        var partes = linha.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                        if (partes.Length >= 2)
                        {
                            if (partes[0] == "MemTotal:") long.TryParse(partes[1], out totalKb);
                            if (partes[0] == "MemFree:") long.TryParse(partes[1], out livreKb);
                            if (partes[0] == "Buffers:") long.TryParse(partes[1], out buffersKb);
                            if (partes[0] == "Cached:") long.TryParse(partes[1], out cachedKb);
                        }
                    }

                    if (totalKb > 0)
                    {
                        double totalGb = totalKb / 1024.0 / 1024.0;
                        double usadoGb = (totalKb - (livreKb + buffersKb + cachedKb)) / 1024.0 / 1024.0;
                        double porcentagem = (usadoGb / totalGb) * 100;

                        if (TxtMemoriaValor != null)
                            TxtMemoriaValor.Text = $"{usadoGb:F1} / {totalGb:F1} GB";
                        
                        if (BarraMemoria != null)
                            BarraMemoria.Value = Math.Clamp(porcentagem, 0, 100);
                    }
                }
            }
            catch { }
        }

      private string CapturarFotoDeQualquerCamera(string caminhoDestino)
{
    Console.WriteLine(
        "[CAMERA] 🔎 Procurando câmeras..."
    );

    string[] camerasUsb =
    {
        "/dev/video2",
        "/dev/video3"
    };

    foreach (string camera in camerasUsb)
    {
        if (!File.Exists(camera))
            continue;

        Console.WriteLine(
            $"[CAMERA] 🔌 Testando câmera USB: {camera}"
        );

        if (CapturarFoto(camera, caminhoDestino))
        {
            Console.WriteLine(
                $"[CAMERA] ✅ USB selecionada: {camera}"
            );

            return caminhoDestino;
        }
    }


    string[] camerasInternas =
    {
        "/dev/video0",
        "/dev/video1"
    };

    foreach (string camera in camerasInternas)
    {
        if (!File.Exists(camera))
            continue;

        Console.WriteLine(
            $"[CAMERA] 💻 Testando webcam interna: {camera}"
        );

        if (CapturarFoto(camera, caminhoDestino))
        {
            Console.WriteLine(
                $"[CAMERA] ✅ Webcam selecionada: {camera}"
            );

            return caminhoDestino;
        }
    }

    throw new Exception(
        "Nenhuma câmera conseguiu capturar uma imagem."
    );
}
private bool CapturarFoto(
    string camera,
    string caminhoDestino)
{
    try
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = "ffmpeg",

            Arguments =
                $"-f v4l2 " +
                $"-i {camera} " +
                $"-frames:v 1 " +
                $"-y " +
                $"\"{caminhoDestino}\"",

            RedirectStandardOutput = true,
            RedirectStandardError = true,

            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process =
            Process.Start(startInfo);

        if (process == null)
            return false;

        process.WaitForExit(4000);

        if (process.ExitCode != 0)
        {
            Console.WriteLine(
                $"[CAMERA] ❌ Falhou: {camera}"
            );

            return false;
        }

        if (!File.Exists(caminhoDestino))
        {
            Console.WriteLine(
                $"[CAMERA] ❌ Nenhuma foto criada: {camera}"
            );

            return false;
        }

        return true;
    }
    catch (Exception ex)
    {
        Console.WriteLine(
            $"[CAMERA] ❌ Erro em {camera}: {ex.Message}"
        );

        return false;
    }
}

        private void AtualizarUsoCpuSimuladoOuReal()
{
    try
    {
        if (File.Exists("/proc/stat"))
        {
            string primeiraLinha = File.ReadLines("/proc/stat").FirstOrDefault();
            if (string.IsNullOrWhiteSpace(primeiraLinha)) return;

            var partes = primeiraLinha.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (partes.Length < 5) return;

            long user = long.Parse(partes[1]);
            long nice = long.Parse(partes[2]);
            long system = long.Parse(partes[3]);
            long idle = long.Parse(partes[4]);
            long iowait = partes.Length > 5 ? long.Parse(partes[5]) : 0;
            long irq = partes.Length > 6 ? long.Parse(partes[6]) : 0;
            long softirq = partes.Length > 7 ? long.Parse(partes[7]) : 0;
            long steal = partes.Length > 8 ? long.Parse(partes[8]) : 0;

            long idleTotal = idle + iowait;
            long nonIdle = user + nice + system + irq + softirq + steal;
            long totalTime = idleTotal + nonIdle;

            if (_prevTotalTime == 0)
            {
                _prevIdleTime = idleTotal;
                _prevTotalTime = totalTime;
                return;
            }

            long totalDiferenca = totalTime - _prevTotalTime;
            long idleDiferenca = idleTotal - _prevIdleTime;

            double usoCpu = 0;
            if (totalDiferenca > 0)
            {
                usoCpu = (double)(totalDiferenca - idleDiferenca) / totalDiferenca * 100.0;
            }

            _prevIdleTime = idleTotal;
            _prevTotalTime = totalTime;

            usoCpu = Math.Clamp(usoCpu, 0, 100);

            Dispatcher.UIThread.Post(() =>
            {
                if (TxtCpuValor != null)
                    TxtCpuValor.Text = $"{usoCpu:F0}%";

                if (BarraCpu != null)
                    BarraCpu.Value = usoCpu;
            });
        }
    }
    catch (Exception ex)
    {
        System.Diagnostics.Debug.WriteLine($"Erro ao ler CPU real: {ex.Message}");
    }
}

        private void CarregarMusicasDaPasta()
        {
            string pastaMusicas = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "musicas");

            if (!Directory.Exists(pastaMusicas))
            {
                Directory.CreateDirectory(pastaMusicas);
            }

            var arquivosMp3 = Directory.GetFiles(pastaMusicas, "*.mp3");
            var arquivosMp4 = Directory.GetFiles(pastaMusicas, "*.mp4");
            
            var arquivosEncontrados = new List<string>();
            arquivosEncontrados.AddRange(arquivosMp3);
            arquivosEncontrados.AddRange(arquivosMp4);

            _listaCaminhosMusicas.Clear();
            _listaNomesMusicas.Clear();

            foreach (var arquivo in arquivosEncontrados)
            {
                _listaCaminhosMusicas.Add(arquivo);
                _listaNomesMusicas.Add(System.IO.Path.GetFileNameWithoutExtension(arquivo));
            }

            AtualizarInterfacePlaylist();
        }

        private void AtualizarInterfacePlaylist()
{
    if (PainelListaMusicas == null) return;

    PainelListaMusicas.Children.Clear();

    if (_listaNomesMusicas.Count == 0)
    {
        var aviso = new TextBlock
        {
            Text = "Nenhuma música encontrada.",
            Foreground = Brushes.Gray,
            FontSize = 11,
            Margin = new Thickness(5)
        };

        PainelListaMusicas.Children.Add(aviso);
        return;
    }

    string caminhoSeta = Path.Combine(
        AppContext.BaseDirectory,
        "Assets",
        "seta.png"
    );

    for (int i = 0; i < _listaNomesMusicas.Count; i++)
    {
        bool eAtual = (i == _indiceMusicaAtual);

        var borderCard = new Border
        {
            Background = eAtual
                ? Brush.Parse("#1A1430")
                : Brush.Parse("#0F0C1B"),

            CornerRadius = new CornerRadius(8),
            Padding = new Thickness(10),

            BorderBrush = eAtual
                ? Brush.Parse("#00F0FF")
                : Brush.Parse("#362963"),

            BorderThickness = new Thickness(1)
        };

        var stack = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 8,
            VerticalAlignment = VerticalAlignment.Center
        };

        // Texto da música
        var txtTitulo = new TextBlock
        {
            Text = $"{i + 1}. {_listaNomesMusicas[i]}",
            Foreground = eAtual
                ? Brush.Parse("#00F0FF")
                : Brush.Parse("#EFEFFF"),

            FontSize = 12,
            FontWeight = FontWeight.Bold,
            VerticalAlignment = VerticalAlignment.Center
        };

        stack.Children.Add(txtTitulo);

        // Adiciona a seta somente na música atual
        if (eAtual && File.Exists(caminhoSeta))
        {
            var seta = new Image
            {
                Source = new Bitmap(caminhoSeta),
                Width = 20,
                Height = 20,
                Stretch = Stretch.Uniform,
                VerticalAlignment = VerticalAlignment.Center
            };

            stack.Children.Add(seta);
        }

        borderCard.Child = stack;

        PainelListaMusicas.Children.Add(borderCard);
    }
}

        private void BotaoProxima_Clique(
    object? sender,
    RoutedEventArgs e)
{
    if (_listaCaminhosMusicas.Count == 0)
        return;

    _indiceMusicaAtual =
        (_indiceMusicaAtual + 1)
        % _listaCaminhosMusicas.Count;

    _posicaoAtualSegundos = 0;

    TocarMusicaAtual(0);

    AtualizarInterfacePlaylist();
}

        private void PararAudioAtual()
{
    try
    {
        if (_processoAudio != null)
        {
            if (!_processoAudio.HasExited)
            {
                _processoAudio.Kill(
                    entireProcessTree: true
                );

                Console.WriteLine(
                    "[MUSICA] 🛑 MPV encerrado."
                );
            }

            _processoAudio.Dispose();
            _processoAudio = null;
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine(
            $"[MUSICA] Erro ao parar áudio: {ex.Message}"
        );

        _processoAudio = null;
    }
}

        protected override void OnUnloaded(
    RoutedEventArgs e)
{
    Console.WriteLine(
        "[JANELA PRINCIPAL] ⛔ Tela sendo encerrada."
    );
    PararScannerVisual();

    VozService.PararVoz();

    base.OnUnloaded(e);

    PararAudioAtual();

    _timerRadar?.Stop();
    _timerHardware?.Stop();
    _timerOndaVoz?.Stop();
}

        private void BotaoAnterior_Clique(
    object? sender,
    RoutedEventArgs e)
{
    if (_listaCaminhosMusicas.Count == 0)
        return;

    _indiceMusicaAtual =
        (_indiceMusicaAtual - 1 +
         _listaCaminhosMusicas.Count)
        % _listaCaminhosMusicas.Count;

    _posicaoAtualSegundos = 0;

    TocarMusicaAtual(0);

    AtualizarInterfacePlaylist();
}

        private void BotaoMute_Clique(
    object? sender,
    RoutedEventArgs e)
{
    _estaMutado = !_estaMutado;

    if (sender is Button btn)
    {
        btn.Content =
            _estaMutado
                ? "🔇"
                : "🔊";
    }

    if (_listaCaminhosMusicas.Count == 0)
        return;

    if (!_estaReproduzindo)
    {
        Console.WriteLine(
            $"[MUSICA] 🔇 Estado de mute alterado durante pausa: " +
            $"{_estaMutado}"
        );

        return;
    }

    double segundosDecorridos =
        (DateTime.Now - _tempoInicioReproducao)
        .TotalSeconds;

    _posicaoAtualSegundos +=
        segundosDecorridos;

    Console.WriteLine(
        $"[MUSICA] 🔊 Alterando mute em " +
        $"{_posicaoAtualSegundos:F2}s"
    );

    TocarMusicaAtual(
        _posicaoAtualSegundos
    );
}

        private void TocarMusicaAtual(double posicaoInicio = 0)
{
    if (_listaCaminhosMusicas.Count == 0)
        return;

    if (posicaoInicio < 0)
        posicaoInicio = 0;

    string caminhoArquivo =
        _listaCaminhosMusicas[_indiceMusicaAtual];

    try
    {
        PararAudioAtual();

        string muteArg =
            _estaMutado
                ? "--mute=yes"
                : "--mute=no";

        string startArg =
            posicaoInicio > 0
                ? $"--start={posicaoInicio.ToString(
                    System.Globalization.CultureInfo.InvariantCulture)}"
                : "";

        Console.WriteLine(
            $"[MUSICA] ▶ Iniciando: " +
            $"{_listaNomesMusicas[_indiceMusicaAtual]}"
        );

        _processoAudio = Process.Start(
            new ProcessStartInfo
            {
                FileName = "mpv",
                Arguments =
                    $"--no-video " +
                    $"{muteArg} " +
                    $"{startArg} " +
                    $"\"{caminhoArquivo}\"",

                UseShellExecute = false,
                CreateNoWindow = true
            }
        );

        if (_processoAudio == null)
        {
            Console.WriteLine(
                "[MUSICA] ❌ Não foi possível iniciar MPV."
            );

            _estaReproduzindo = false;
            return;
        }

        _posicaoAtualSegundos = posicaoInicio;
        _tempoInicioReproducao = DateTime.Now;
        _estaReproduzindo = true;

        if (BotaoPlayPause != null)
            BotaoPlayPause.Content = "⏸";

        Console.WriteLine(
            "[MUSICA] ✅ Reprodução iniciada."
        );

        _ = MonitorarFimDaMusicaAsync(_processoAudio);
    }
    catch (Exception ex)
    {
        Console.WriteLine(
            $"[MUSICA] ❌ Erro ao reproduzir áudio: {ex}"
        );

        _estaReproduzindo = false;
    }
}
private async Task MonitorarFimDaMusicaAsync(Process processo)
{
    try
    {
        await processo.WaitForExitAsync();

        if (_processoAudio != processo)
            return;

        Console.WriteLine(
            "[MUSICA] 🎵 Música terminou."
        );

        _estaReproduzindo = false;
        _posicaoAtualSegundos = 0;

        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            if (_listaCaminhosMusicas.Count == 0)
                return;

            _indiceMusicaAtual =
                (_indiceMusicaAtual + 1)
                % _listaCaminhosMusicas.Count;

            Console.WriteLine(
                $"[MUSICA] ⏭ Próxima música: " +
                $"{_listaNomesMusicas[_indiceMusicaAtual]}"
            );

            AtualizarInterfacePlaylist();

            TocarMusicaAtual(0);
        });
    }
    catch (Exception ex)
    {
        Console.WriteLine(
            $"[MUSICA] ❌ Erro monitorando música: {ex.Message}"
        );
    }
}
private void GarantirMusicaTocando()
{
    if (_listaCaminhosMusicas.Count == 0)
    {
        Console.WriteLine("[MUSICA] ⚠️ Nenhuma música disponível.");
        return;
    }

    if (_estaReproduzindo &&
        _processoAudio != null &&
        !_processoAudio.HasExited)
    {
        Console.WriteLine("[MUSICA] 🎵 Música já está tocando.");
        return;
    }

    Console.WriteLine("[MUSICA] ▶ Música não está tocando. Iniciando...");

    TocarMusicaAtual(_posicaoAtualSegundos);
}

        private void BotaoPlayPause_Clique(
    object? sender,
    RoutedEventArgs e)
{
    if (_listaCaminhosMusicas.Count == 0)
        return;

    if (_estaReproduzindo)
    {
        double segundosDecorridos =
            (DateTime.Now - _tempoInicioReproducao)
            .TotalSeconds;

        _posicaoAtualSegundos +=
            segundosDecorridos;

        Console.WriteLine(
            $"[MUSICA] ⏸ Pausando em " +
            $"{_posicaoAtualSegundos:F2}s"
        );

        PararAudioAtual();

        _estaReproduzindo = false;

        if (BotaoPlayPause != null)
            BotaoPlayPause.Content = "▶";
    }
    else
    {
        Console.WriteLine(
            $"[MUSICA] ▶ Retomando em " +
            $"{_posicaoAtualSegundos:F2}s"
        );

        TocarMusicaAtual(
            _posicaoAtualSegundos
        );
    }
}

        public ObservableCollection<UsuarioPontuacaoModel> ListaPontuacoesUsuarios { get; set; } = new();

        private void AtualizarRankingPontuacoes(int limiteExibicao)
        {
            ListaPontuacoesUsuarios.Clear();
            try
            {
                var bancoService = new Sistema.Services.BancoService();
                var listaTemporaria = bancoService.ObterRanking(limiteExibicao);

                for (int i = 0; i < listaTemporaria.Count; i++)
                {
                    var usuario = listaTemporaria[i];
                    usuario.IsEmpatado = (i > 0 && listaTemporaria[i - 1].Pontos == usuario.Pontos) ||
                                         (i < listaTemporaria.Count - 1 && listaTemporaria[i + 1].Pontos == usuario.Pontos);
                    usuario.CorDestaque = (i == 0 || usuario.Pontos == listaTemporaria[0].Pontos) ? Brush.Parse("#2ECC71") : Brush.Parse("#3D3278");
                    ListaPontuacoesUsuarios.Add(usuario);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erro ao carregar ranking: {ex.Message}");
            }
        }

        

        private void NumRankingPontuacoes_ValorChanged(object? sender, NumericUpDownValueChangedEventArgs e)
        {
            if (e.NewValue.HasValue) AtualizarRankingPontuacoes((int)e.NewValue.Value);
        }

private void AnimarOndaVoz(object? sender, EventArgs e)
{
    if (DesenhoOndaVozIA == null) return;

    int p1 = _random.Next(15, 75);
    int p2 = _random.Next(10, 80);
    int p3 = _random.Next(20, 70);

    DesenhoOndaVozIA.Data = Geometry.Parse(
        $"M 0,45 L 50,{p1} L 100,45 L 150,{p2} L 200,45 L 250,{p3} L 300,45 L 350,{p1} L 400,45 L 450,{p2} L 500,45"
    );
}

private void PararOndaVoz()
{
    _timerOndaVoz?.Stop();
    if (DesenhoOndaVozIA != null)
    {
        DesenhoOndaVozIA.Data = Geometry.Parse("M 0,45 L 500,45");
    }
}
private async Task FinalizarAnaliseEAbrirJogoAsync(
    string nomeObjeto,
    string emojiObjeto)
{
    string usuarioAtual = SessaoUsuario.Nome; 

    Console.WriteLine(
        $"[JOGO] Iniciando abertura do jogo para o usuário: {usuarioAtual}"
    );

    string pastaDestino = Path.Combine(
        Directory.GetCurrentDirectory(),
        "foto_capturada"
    );

    string caminhoFotoSalva = Path.Combine(
        pastaDestino,
        $"foto_{usuarioAtual}.jpg"
    );

    var telaJogo = new Jogo();

    telaJogo.ConfigurarOndaVoz(
        IniciarOndaVoz,
        PararOndaVoz
    );

    telaJogo.ConfigurarDadosPartida(
        usuarioAtual,
        caminhoFotoSalva
    );
    GarantirMusicaTocando();

    await telaJogo.ConfigurarObjetoParaPartidaAsync(
        nomeObjeto,
        caminhoFotoSalva
    );

    telaJogo.DefinirEmojiObjeto(emojiObjeto);
    telaJogo.DefinirObjetoAlvo(nomeObjeto);

    if (AreaConteudoJogo != null)
    {
        AreaConteudoJogo.Content = telaJogo;
    }

    await telaJogo.IniciarPartidaAsync();
}
private async void EnviarDicaManual_Clique(
    object? sender,
    RoutedEventArgs e)
{
    Console.WriteLine(
        $"[DICA] Apresentação concluída? {_apresentacaoIAConcluida}"
    );

    if (!_apresentacaoIAConcluida)
    {
        if (TextoEstadoCentral != null)
        {
            TextoEstadoCentral.Text =
                "Aguarde a apresentação da IA terminar.";
        }

        return;
    }

    string? dicaDigitada =
        CampoDicaContexto?.Text?.Trim();

    if (string.IsNullOrWhiteSpace(dicaDigitada))
        return;

    try
    {
        if (BotaoEnviarDica != null)
            BotaoEnviarDica.IsEnabled = false;

        if (CampoDicaContexto != null)
            CampoDicaContexto.IsEnabled = false;

        if (TextoEstadoCentral != null)
        {
            TextoEstadoCentral.Text =
                $"Objeto informado: {dicaDigitada}. Iniciando jogo...";
        }

        string objetoManual =
            dicaDigitada.Trim().ToUpperInvariant();

        string emojiManual =
            EmojiObjetoService.ObterEmojiCorrespondente(
                objetoManual
            );

        await FinalizarAnaliseEAbrirJogoAsync(
            objetoManual,
            emojiManual
        );
        CampoDicaContexto?.Clear();

        if (TextoEstadoCentral != null)
        {
            TextoEstadoCentral.Text =
                $"Jogo iniciado com o objeto: {dicaDigitada.ToUpper()}";
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine(
            $"[DICA] Erro: {ex}"
        );

        if (TextoEstadoCentral != null)
        {
            TextoEstadoCentral.Text =
                $"Erro ao iniciar jogo: {ex.Message}";
        }

        LiberarEntradaDica();
    }
}
private void LiberarEntradaDica()
{
    _apresentacaoIAConcluida = true;

    if (CampoDicaContexto != null)
        CampoDicaContexto.IsEnabled = true;

    if (BotaoEnviarDica != null)
        BotaoEnviarDica.IsEnabled = true;

    Console.WriteLine("[INSIDE] ✅ Entrada de dica LIBERADA.");
}
private void BloquearEntradaDica()
{
    _apresentacaoIAConcluida = false;

    if (CampoDicaContexto != null)
        CampoDicaContexto.IsEnabled = false;

    if (BotaoEnviarDica != null)
        BotaoEnviarDica.IsEnabled = false;

    Console.WriteLine("[INSIDE] 🔒 Entrada de dica BLOQUEADA.");
}
private void IniciarScannerVisual()
{
    if (FaixaScanner == null ||
        FaixaScannerGlow == null ||
        CanvasScanner == null)
    {
        Console.WriteLine(
            "[SCANNER] ❌ Elementos do scanner não encontrados."
        );

        return;
    }

    _scannerAtivo = true;
    _posicaoScanner = 0;

    double largura = CanvasScanner.Bounds.Width;

    if (largura <= 0)
        largura = 306;

    FaixaScanner.Width = largura;
    FaixaScannerGlow.Width = largura;

    FaixaScanner.IsVisible = true;
    FaixaScannerGlow.IsVisible = true;

    Canvas.SetLeft(FaixaScanner, 0);
    Canvas.SetLeft(FaixaScannerGlow, 0);

    Canvas.SetTop(FaixaScanner, 0);
    Canvas.SetTop(FaixaScannerGlow, -9);

    _timerScanner?.Stop();

    _timerScanner = new DispatcherTimer
    {
        Interval = TimeSpan.FromMilliseconds(16)
    };

    _timerScanner.Tick += ExecutarScannerVisual;

    _timerScanner.Start();

    Console.WriteLine(
        $"[SCANNER] 🟢 Scanner visual iniciado! Largura: {largura}"
    );

    _ = TocarSomScannerAsync();
}
private void ExecutarScannerVisual(
    object? sender,
    EventArgs e)
{
    if (!_scannerAtivo)
        return;

    if (CanvasScanner == null ||
        FaixaScanner == null ||
        FaixaScannerGlow == null)
        return;

    double altura = CanvasScanner.Bounds.Height;

    if (altura <= 0)
        altura = 220;

    double largura = CanvasScanner.Bounds.Width;

    if (largura > 0)
    {
        FaixaScanner.Width = largura;
        FaixaScannerGlow.Width = largura;
    }

    _posicaoScanner += 3.0;

    if (_posicaoScanner > altura)
        _posicaoScanner = -4;

    Canvas.SetTop(
        FaixaScanner,
        _posicaoScanner
    );

    Canvas.SetTop(
        FaixaScannerGlow,
        _posicaoScanner - 9
    );
}
private void PararScannerVisual()
{
    _scannerAtivo = false;

    _timerScanner?.Stop();
    _timerScanner = null;

    _scannerAudioCancellation?.Cancel();

    try
    {
        if (_scannerAudioProcess != null)
        {
            if (!_scannerAudioProcess.HasExited)
            {
                _scannerAudioProcess.Kill(
                    entireProcessTree: true
                );

                Console.WriteLine(
                    "[SCANNER] 🔇 MPV do scanner encerrado."
                );
            }

            _scannerAudioProcess.Dispose();
            _scannerAudioProcess = null;
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine(
            $"[SCANNER] ⚠️ Erro ao parar áudio: {ex.Message}"
        );
    }

    if (FaixaScanner != null)
        FaixaScanner.IsVisible = false;

    if (FaixaScannerGlow != null)
        FaixaScannerGlow.IsVisible = false;

    Console.WriteLine(
        "[SCANNER] 🔴 Scanner encerrado."
    );
}
private void PararScannerCompleto()
{
    Console.WriteLine("[SCANNER] 🛑 Encerrando scanner visual e áudio.");

    PararScannerVisual();

    _scannerAudioCancellation?.Cancel();
    _scannerAudioCancellation?.Dispose();
    _scannerAudioCancellation = null;

    try
    {
        if (_scannerAudioProcess != null)
        {
            if (!_scannerAudioProcess.HasExited)
            {
                _scannerAudioProcess.Kill(
                    entireProcessTree: true
                );

                Console.WriteLine(
                    "[SCANNER] 🔇 Áudio do scanner parado."
                );
            }

            _scannerAudioProcess.Dispose();
            _scannerAudioProcess = null;
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine(
            $"[SCANNER] ⚠️ Erro ao parar áudio: {ex.Message}"
        );

        _scannerAudioProcess = null;
    }
}

}
    
}
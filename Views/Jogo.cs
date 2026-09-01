using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Threading;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Sistema.Views;
using Avalonia.Media.Imaging;
using System.IO;
using Avalonia.Platform;
using System.Diagnostics;
using Avalonia.Interactivity;
using Sistema.Services;

namespace Sistema.Views
{
    public partial class Jogo : UserControl
    {
        private readonly DispatcherTimer _temporizadorJogo;
        private double _tempoTotalJogoSegundos = 0;
        private Bitmap? _imagemObjetoOriginal;
        private readonly Random _random = new();

        private string _caminhoFotoAtual = string.Empty;
        private string _palavraObjetoAtual = "OBJETO";
        private string _emojiObjetoAtual = "👾";
        private string _nomeUsuarioAtual = string.Empty;

        private bool _moverEsquerda;
        private bool _moverDireita;
        private bool _estaAtirando;

        private double _jogadorX = 180;
        private double _jogadorY = 500;
        private bool _jogoIniciado = false;
        private int _nivelAtual = 1;

        private double _tempoNivel = 0;

        private bool _pausaEntreNiveis = false;

        private double _tempoPausaNivel = 0;

        private const double DuracaoNivel = 20.0;
        private const double DuracaoPausaNivel = 3.0;

        private const double VelocidadeJogador = 12.0;
        private bool _estaSofrendoDano = false;
        private bool _jogoPausado = false;
        private string _imagemObjetoAtual = "objeto_generico.png";
        public Action? OnPartidaFinalizada { get; set; }

        private readonly List<Control> _tiros = new();
        private double _tempoUltimoTiro = 0;

        private const double VelocidadeTiro = 10.0;
        private const double IntervaloTiro = 150.0;
        private class InimigoPalavra
        {
            public Control Container { get; set; } = null!;
            public TextBlock TextBlockVisual { get; set; } = null!;
            public string PalavraAtual { get; set; } = "";
            public string Emoji { get; set; } = "";
            public bool FoiDestruido { get; set; } = false;
        }

        private readonly List<InimigoPalavra> _inimigos = new();
        private const double VelocidadeInimigo = 3.4;

        private double _tempoDesdeUltimoSpawn = 0;
        private double _intervaloSpawnAtual = 4000.0;

        private readonly StringBuilder _bancoLetrasAcumuladas = new();
        private int _letrasConstruidas = 0;
        private int _totalLetrasObjeto = 0;
        public event Action? PartidaEncerrada;
        private int _pontuacao = 0;
        private int _vidas = 10;
        private Process? _processoConquista;
        private Process? _processoPerdendo;
        private DispatcherTimer? _fadeSomTiro;

        private int _perdendoStream = 0;
        private int _tiroSample = 0;
        private int _tiroCanal = 0;
        private Action? _iniciarOndaVoz;
        private Action? _pararOndaVoz;

public Jogo()
{
    InitializeComponent();

    Focusable = true;

    KeyDown += AoPressionarTeclado;
    KeyUp += AoSoltarTeclado;

    Focus();

    GameCanvas.SizeChanged += (_, _) =>
    {
        if (_jogoPausado)
            CentralizarPainelPausa();
    };

    _temporizadorJogo = new DispatcherTimer
    {
        Interval = TimeSpan.FromMilliseconds(16)
    };

    _temporizadorJogo.Tick += ExecutarLoopJogo;

    Loaded += Jogo_Loaded;

    AtualizarRenderizacaoTexturaIA();


    CarregarSons();

}
public void ConfigurarOndaVoz(
    Action iniciarOnda,
    Action pararOnda)
{
    _iniciarOndaVoz = iniciarOnda;
    _pararOndaVoz = pararOnda;
}
private void Jogo_Loaded(object? sender, RoutedEventArgs e)
{
    Focus();

    if (_jogoIniciado &&
        GameOverOverlay != null &&
        !GameOverOverlay.IsVisible)
    {
        if (!_temporizadorJogo.IsEnabled)
        {
            _temporizadorJogo.Start();

            Console.WriteLine(
                "[JOGO] ▶️ Timer retomado ao retornar para a aba."
            );
        }
    }
}
private async Task FalarComOndaAsync(
    VozService vozService,
    string texto)
{
    try
    {
        _iniciarOndaVoz?.Invoke();

        Console.WriteLine(
            $"[JOGO] 🎙️ Inside: {texto}"
        );

        await vozService.FalarRespostaDoOllamaAsync(
            texto
        );
    }
    finally
    {
        _pararOndaVoz?.Invoke();
    }
}
private void CarregarSons()
{
    try
    {
        Console.WriteLine("==========================================");
        Console.WriteLine("[SOM] INICIANDO SISTEMA DE ÁUDIO");
        Console.WriteLine("==========================================");


        bool bassPronto;

        if (ManagedBass.Bass.CurrentDevice != -1)
        {
            bassPronto = true;

            Console.WriteLine(
                "[SOM] BASS já estava inicializado."
            );
        }
        else
        {
            bassPronto = ManagedBass.Bass.Init(
                -1,
                44100,
                ManagedBass.DeviceInitFlags.Default
            );

            Console.WriteLine(
                $"[SOM] BASS Init: {bassPronto}"
            );
        }

        if (!bassPronto)
        {
            Console.WriteLine(
                $"[SOM] ❌ BASS falhou: {ManagedBass.Bass.LastError}"
            );

            return;
        }

        Console.WriteLine(
            $"[SOM] Device: {ManagedBass.Bass.CurrentDevice}"
        );

        string pastaSom = Path.Combine(
            AppContext.BaseDirectory,
            "SomJogo"
        );

        Console.WriteLine(
            $"[SOM] Pasta de sons: {pastaSom}"
        );

        string caminhoTiro = Path.Combine(
            pastaSom,
            "tiro_nave.mp3"
        );

        string caminhoPerdendo = Path.Combine(
            pastaSom,
            "perdendo.mp3"
        );

        Console.WriteLine(
            $"[SOM] Tiro: {caminhoTiro}"
        );

        Console.WriteLine(
            $"[SOM] Tiro existe: {File.Exists(caminhoTiro)}"
        );

        Console.WriteLine(
            $"[SOM] Perda: {caminhoPerdendo}"
        );

        Console.WriteLine(
            $"[SOM] Perda existe: {File.Exists(caminhoPerdendo)}"
        );


        if (File.Exists(caminhoTiro))
        {
            _tiroSample = ManagedBass.Bass.SampleLoad(
                caminhoTiro,
                0,
                0,
                4,
                ManagedBass.BassFlags.Default
            );

            Console.WriteLine(
                $"[SOM] Tiro Sample: {_tiroSample}"
            );

            if (_tiroSample == 0)
            {
                Console.WriteLine(
                    $"[SOM] ❌ Erro tiro: " +
                    $"{ManagedBass.Bass.LastError}"
                );
            }
        }

        if (File.Exists(caminhoPerdendo))
        {
            Console.WriteLine(
                "[SOM] 🔄 Carregando perdendo.mp3..."
            );

        }
        else
        {
            Console.WriteLine(
                "[SOM] ❌ PERDENDO.MP3 NÃO EXISTE!"
            );
        }

        Console.WriteLine("==========================================");
        
        Console.WriteLine("==========================================");
    }
    catch (Exception ex)
    {
        Console.WriteLine(
            $"[SOM] ❌ ERRO CRÍTICO: {ex}"
        );
    }
}

private void AoPressionarTeclado(
    object? sender,
    KeyEventArgs e)
{
    if (e.Key == Key.P)
    {
        AlternarPausa();

        e.Handled = true;

        return;
    }

    if (!_jogoIniciado || _jogoPausado)
        return;

    if (e.Key == Key.Left || e.Key == Key.A)
    {
        _moverEsquerda = true;
    }

    if (e.Key == Key.Right || e.Key == Key.D)
    {
        _moverDireita = true;
    }

    if (e.Key == Key.Space)
    {
        if (!_estaAtirando)
        {
            _estaAtirando = true;

            IniciarSomTiro();
        }
    }
}

protected override void OnUnloaded(RoutedEventArgs e)
{
    base.OnUnloaded(e);

    _temporizadorJogo?.Stop();

    PararSomTiro();
    PararSomConquista();
    PararSomPerdendo();

    _pararOndaVoz?.Invoke();
}

        private void AoSoltarTeclado(object? sender, KeyEventArgs e)
{
    if (e.Key == Key.Left || e.Key == Key.A)
        _moverEsquerda = false;

    if (e.Key == Key.Right || e.Key == Key.D)
        _moverDireita = false;

    if (e.Key == Key.Space)
    {
        _estaAtirando = false;

        PararSomTiro();
    }
}
private void AlternarPausa()
{
    _jogoPausado = !_jogoPausado;

    if (_jogoPausado)
    {
        Console.WriteLine("[JOGO] ⏸️ Jogo pausado!");

        _moverEsquerda = false;
        _moverDireita = false;
        _estaAtirando = false;

        PararSomTiro();

        if (PainelPausa != null)
        {
            PainelPausa.IsVisible = true;
            CentralizarPainelPausa();
        }
    }
    else
    {
        Console.WriteLine("[JOGO] ▶️ Jogo retomado!");

        if (PainelPausa != null)
            PainelPausa.IsVisible = false;

        Focus();
    }
}
private void CentralizarPainelPausa()
{
    if (PainelPausa == null || GameCanvas == null)
        return;

    double largura = GameCanvas.Bounds.Width;
    double altura = GameCanvas.Bounds.Height;

    if (largura <= 0 || altura <= 0)
        return;

    double larguraPainel = PainelPausa.Width;
    double alturaPainel = PainelPausa.Height;

    Canvas.SetLeft(
        PainelPausa,
        (largura - larguraPainel) / 2
    );

    Canvas.SetTop(
        PainelPausa,
        (altura - alturaPainel) / 2
    );
}
        private void ExecutarLoopJogo(object? sender, EventArgs e)
{
    if (_jogoPausado)
        return;

    if (GameCanvas.Bounds.Width <= 0 || GameCanvas.Bounds.Height <= 0)
        return;

    _tempoTotalJogoSegundos += 0.016;
    AtualizarDificuldadeProgressiva();

    if (_moverEsquerda) _jogadorX -= VelocidadeJogador;
    if (_moverDireita) _jogadorX += VelocidadeJogador;

    _jogadorX = Math.Clamp(
        _jogadorX,
        0,
        GameCanvas.Bounds.Width - 44
    );

    Canvas.SetLeft(PlayerShip, _jogadorX);
    Canvas.SetTop(PlayerShip, _jogadorY);

    _tempoUltimoTiro += 16;

    if (_estaAtirando && _tempoUltimoTiro >= IntervaloTiro)
    {
        Atirar();
        _tempoUltimoTiro = 0;
    }

    if (!_pausaEntreNiveis)
    {
        _tempoDesdeUltimoSpawn += 16;

        if (_tempoDesdeUltimoSpawn >= _intervaloSpawnAtual)
        {
            _tempoDesdeUltimoSpawn = 0;
            CriarNovoObstaculoNaTela();
        }
    }

    AtualizarTiros();
    AtualizarInimigos();
}
private void TocarSomConquista()
{
    try
    {
        string caminhoSom = Path.Combine(AppContext.BaseDirectory, "SomJogo", "conquista.mp3");
        if (!File.Exists(caminhoSom)) return;

        PararSomConquista();

        _processoConquista = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "mpv",
                Arguments = $"--no-video --really-quiet \"{caminhoSom}\"",
                UseShellExecute = false,
                CreateNoWindow = true
            }
        };

        _processoConquista.Start();
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[SOM] Erro ao tocar conquista: {ex.Message}");
    }
}
private void PararSomConquista()
{
    try
    {
        if (_processoConquista != null && !_processoConquista.HasExited)
        {
            _processoConquista.Kill();
            _processoConquista.Dispose();
        }
    }
    catch { }
    _processoConquista = null;
}
private void MostrarConquista()
{
    try
    {
        Console.WriteLine(
            $"[JOGO] 🏆 Objeto final: {_palavraObjetoAtual}"
        );

        Console.WriteLine(
            $"[JOGO] 🖼️ Imagem final: {_imagemObjetoAtual}"
        );

        AtualizarImagemObjetoCard();
        AtualizarHUD();

        if (GameOverOverlay != null)
            GameOverOverlay.IsVisible = true;
            PartidaEncerrada?.Invoke();

        Console.WriteLine(
            "[JOGO] ✅ Carta exibida."
        );
    }
    catch (Exception ex)
    {
        Console.WriteLine(
            $"[JOGO] ❌ Erro ao mostrar carta: {ex}"
        );
    }
}
private void TocarSomPerdendo()
{
    try
    {
        string caminhoSom = Path.Combine(AppContext.BaseDirectory, "SomJogo", "perdendo.mp3");
        if (!File.Exists(caminhoSom)) return;

        PararSomPerdendo();

        _processoPerdendo = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "mpv",
                Arguments = $"--no-video --really-quiet --ao=pipewire \"{caminhoSom}\"",
                UseShellExecute = false,
                CreateNoWindow = true
            }
        };

        _processoPerdendo.Start();
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[SOM] Erro ao tocar perdendo: {ex.Message}");
    }
}
private void PararSomPerdendo()
{
    try
    {
        if (_processoPerdendo != null && !_processoPerdendo.HasExited)
        {
            _processoPerdendo.Kill();
            _processoPerdendo.Dispose();
        }
    }
    catch { }
    _processoPerdendo = null;
}
        private void AtualizarDificuldadeProgressiva()
{
    if (_pausaEntreNiveis)
    {
        _tempoPausaNivel += 0.016;

        _intervaloSpawnAtual = double.MaxValue;

        if (_tempoPausaNivel >= DuracaoPausaNivel)
        {
            _pausaEntreNiveis = false;
            _tempoPausaNivel = 0;

            _nivelAtual++;

            Console.WriteLine(
                $"[JOGO] 🚀 Iniciando Nível {_nivelAtual}"
            );

            _tempoNivel = 0;
            _tempoDesdeUltimoSpawn = 0;
        }

        return;
    }

    _tempoNivel += 0.016;

    switch (_nivelAtual)
    {
        case 1:
            _intervaloSpawnAtual = 3500;
            break;

        case 2:
            _intervaloSpawnAtual = 2700;
            break;

        case 3:
            _intervaloSpawnAtual = 2000;
            break;

        case 4:
            _intervaloSpawnAtual = 1500;
            break;

        default:
            _intervaloSpawnAtual = 1000;
            break;
    }

    if (_tempoNivel >= DuracaoNivel)
    {
        _pausaEntreNiveis = true;
        _tempoPausaNivel = 0;

        Console.WriteLine(
            $"[JOGO] ⏸️ Nível {_nivelAtual} concluído!"
        );
    }
}

public async Task IniciarPartidaAsync()
{
    Console.WriteLine(
        $"[JOGO] 🎮 Preparando partida com objeto: {_palavraObjetoAtual}"
    );

    Console.WriteLine(
        $"[JOGO] 😀 Emoji: {_emojiObjetoAtual}"
    );

    await ContagemRegressivaAsync();

    _jogoIniciado = true;

    _temporizadorJogo.Start();

    Focus();

    Console.WriteLine(
        "[JOGO] 🚀 Jogo iniciado!"
    );
}

        private void AtualizarTiros()
        {
            for (int i = _tiros.Count - 1; i >= 0; i--)
            {
                Control tiro = _tiros[i];
                double topo = Canvas.GetTop(tiro);

                topo -= VelocidadeTiro;

                if (topo < -30)
                {
                    GameCanvas.Children.Remove(tiro);
                    _tiros.RemoveAt(i);
                }
                else
                {
                    Canvas.SetTop(tiro, topo);
                }
            }
        }

        private async void AtualizarInimigos()
        {
            for (int i = _inimigos.Count - 1; i >= 0; i--)
            {
                var inimigoObj = _inimigos[i];
                Control inimigo = inimigoObj.Container;

                double topo = Canvas.GetTop(inimigo);
                topo += VelocidadeInimigo;

                if (topo > GameCanvas.Bounds.Height)
                {
                    Console.WriteLine(
                        $"[JOGO] 💥 OBJETO PERDIDO: {inimigoObj.PalavraAtual}"
                    );

                    GameCanvas.Children.Remove(inimigo);
                    _inimigos.RemoveAt(i);

                    TocarSomPerdendo();

                    await TratarPerdaDeVidaAsync();

                    continue;
                }

                Canvas.SetTop(inimigo, topo);

                Rect areaInimigo = new Rect(
                    Canvas.GetLeft(inimigo),
                    Canvas.GetTop(inimigo),
                    inimigo.Bounds.Width > 0 ? inimigo.Bounds.Width : 100,
                    inimigo.Bounds.Height > 0 ? inimigo.Bounds.Height : 55
                );

                for (int j = _tiros.Count - 1; j >= 0; j--)
                {
                    Control tiro = _tiros[j];

                    Rect areaTiro = new Rect(
                        Canvas.GetLeft(tiro),
                        Canvas.GetTop(tiro),
                        tiro.Bounds.Width > 0 ? tiro.Bounds.Width : 6,
                        tiro.Bounds.Height > 0 ? tiro.Bounds.Height : 15
                    );

                    if (areaInimigo.Intersects(areaTiro))
                    {
                        // Remove o tiro imediatamente
                        GameCanvas.Children.Remove(tiro);
                        _tiros.RemoveAt(j);

                        // Se esse inimigo já foi destruído, não processa novamente
                        if (inimigoObj.FoiDestruido)
                            break;

                        // Nenhuma letra para destruir
                        if (string.IsNullOrEmpty(inimigoObj.PalavraAtual))
                            break;

                        // Pega a primeira letra
                        char letraRemovida = inimigoObj.PalavraAtual[0];

                        AdicionarLetraNaConstrucao(letraRemovida);

                        // Remove a letra da palavra
                        inimigoObj.PalavraAtual =
                            inimigoObj.PalavraAtual.Substring(1);

                        // Ainda existem letras?
                        if (!string.IsNullOrEmpty(inimigoObj.PalavraAtual))
                        {
                            inimigoObj.TextBlockVisual.Text =
                                $"{inimigoObj.Emoji}\n{inimigoObj.PalavraAtual}";

                            inimigoObj.TextBlockVisual.Foreground =
                                Brushes.White;

                            inimigoObj.TextBlockVisual.FontSize = 18;

                            _pontuacao += 25;

                            Console.WriteLine(
                                $"[JOGO] 🔫 Letra destruída: {letraRemovida} | " +
                                $"Restante: {inimigoObj.PalavraAtual}"
                            );
                        }
                        else
                        {
                            // Marca ANTES de remover
                            inimigoObj.FoiDestruido = true;

                            _pontuacao += 150;

                            Console.WriteLine(
                                $"[JOGO] 💥 Palavra destruída completamente!"
                            );

                            // Remove visualmente imediatamente
                            GameCanvas.Children.Remove(inimigo);

                            // Remove da lista imediatamente
                            _inimigos.RemoveAt(i);
                        }

                        AtualizarHUD();

                        break;

                    }
                }
            }
        }

        private async Task TratarPerdaDeVidaAsync()
{
    if (_estaSofrendoDano) return;
    _estaSofrendoDano = true;

    _vidas--;
    AtualizarHUD();

    Console.WriteLine($"Vidas restantes: {_vidas}");

    await EfeitoDanoNaveVermelhaAsync();

    if (_vidas <= 0)
    {
        Console.WriteLine("As vidas acabaram! Chamando FimDeJogo()...");
        FimDeJogo();
    }

    _estaSofrendoDano = false;
}

        private async Task EfeitoDanoNaveVermelhaAsync()
        {
            var scaleTransform = PlayerShip.RenderTransform as ScaleTransform;

            if (PlayerShip is Panel painelNave)
            {
                var coresOriginais = new List<(Avalonia.Media.IBrush? Fill, Avalonia.Media.IBrush? Stroke)>();
                foreach (var filho in painelNave.Children)
                {
                    if (filho is Avalonia.Controls.Shapes.Polygon poligono)
                    {
                        coresOriginais.Add((poligono.Fill, poligono.Stroke));
                    }
                }

                for (int i = 0; i < 3; i++)
                {
                    if (scaleTransform != null)
                    {
                        scaleTransform.ScaleX = 1.35;
                        scaleTransform.ScaleY = 1.35;
                    }

                    foreach (var filho in painelNave.Children)
                    {
                        if (filho is Avalonia.Controls.Shapes.Polygon poligono)
                        {
                            poligono.Fill = Brushes.Red;
                            poligono.Stroke = Brushes.Yellow;
                        }
                    }
                    
                    await Task.Delay(120);

                    if (scaleTransform != null)
                    {
                        scaleTransform.ScaleX = 1.0;
                        scaleTransform.ScaleY = 1.0;
                    }

                    int index = 0;
                    foreach (var filho in painelNave.Children)
                    {
                        if (filho is Avalonia.Controls.Shapes.Polygon poligono && index < coresOriginais.Count)
                        {
                            poligono.Fill = coresOriginais[index].Fill;
                            poligono.Stroke = coresOriginais[index].Stroke;
                            index++;
                        }
                    }
                    
                    await Task.Delay(120);
                }
            }
        }
       private void Atirar()
{
    var tiro = new Avalonia.Controls.Shapes.Rectangle
    {
        Width = 6,
        Height = 15,
        Fill = Brushes.Yellow
    };

    Canvas.SetLeft(tiro, _jogadorX + 19);
    Canvas.SetTop(tiro, _jogadorY - 15);

    GameCanvas.Children.Add(tiro);
    _tiros.Add(tiro);
}
      
       private void AdicionarLetraNaConstrucao(char letra)
{
    if (!char.IsLetter(letra))
        return;

    if (_letrasConstruidas >= _totalLetrasObjeto)
        return;

    letra = char.ToUpperInvariant(letra);

    _bancoLetrasAcumuladas.Append(letra);
    _letrasConstruidas++;

    Console.WriteLine(
        $"[JOGO] Letra destruída: {letra} | " +
        $"Construção: {_letrasConstruidas}/{_totalLetrasObjeto}"
    );

    AtualizarRenderizacaoTexturaIA();
}
private void IniciarSomTiro()
{
    try
    {
        if (_tiroSample == 0)
        {
            Console.WriteLine("[SOM] Sample de tiro não carregado.");
            return;
        }

        _fadeSomTiro?.Stop();

        if (_tiroCanal != 0)
        {
            ManagedBass.Bass.ChannelStop(_tiroCanal);
            _tiroCanal = 0;
        }

        _tiroCanal = ManagedBass.Bass.SampleGetChannel(
            _tiroSample,
            false
        );

        if (_tiroCanal == 0)
        {
            Console.WriteLine(
                $"[SOM] Não foi possível criar canal. Erro: {ManagedBass.Bass.LastError}"
            );

            return;
        }

        ManagedBass.Bass.ChannelSetAttribute(
            _tiroCanal,
            ManagedBass.ChannelAttribute.Volume,
            1.0f
        );

        ManagedBass.Bass.ChannelSetPosition(
            _tiroCanal,
            0
        );

        ManagedBass.Bass.ChannelPlay(
            _tiroCanal,
            true
        );

        Console.WriteLine("[SOM] 🔊 Tiro iniciado.");
    }
    catch (Exception ex)
    {
        Console.WriteLine(
            $"[SOM] Erro ao iniciar tiro: {ex.Message}"
        );
    }
}
private void PararSomTiro()
{
    try
    {
        if (_tiroCanal == 0)
            return;

        int canal = _tiroCanal;

        _fadeSomTiro?.Stop();

        float volumeInicial = 1.0f;
        int passos = 6;
        int passoAtual = 0;

        _fadeSomTiro = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(10)
        };

        _fadeSomTiro.Tick += (sender, args) =>
        {
            passoAtual++;

            float volume = volumeInicial *
                           (1.0f - (passoAtual / (float)passos));

            volume = Math.Max(0.0f, volume);

            if (canal != 0)
            {
                ManagedBass.Bass.ChannelSetAttribute(
                    canal,
                    ManagedBass.ChannelAttribute.Volume,
                    volume
                );
            }

            if (passoAtual >= passos)
            {
                _fadeSomTiro?.Stop();

                if (canal != 0)
                {
                    ManagedBass.Bass.ChannelStop(canal);
                }

                if (_tiroCanal == canal)
                    _tiroCanal = 0;

                Console.WriteLine("[SOM] 🔇 Tiro parado suavemente.");
            }
        };

        _fadeSomTiro.Start();
    }
    catch (Exception ex)
    {
        Console.WriteLine(
            $"[SOM] Erro ao parar tiro: {ex.Message}"
        );
    }
}
private void AtualizarRenderizacaoTexturaIA()
{
    AtualizarImagemObjetoCard();
}
private void AtualizarImagemObjetoCard()
{
    try
    {
        if (ImagemObjetoConquista == null)
        {
            Console.WriteLine(
                "[JOGO] ❌ ImagemObjetoConquista não existe."
            );

            return;
        }

        string pastaObjetos = Path.Combine(
            AppContext.BaseDirectory,
            "Assets"
        );

        string caminho = Path.Combine(
            pastaObjetos,
            _imagemObjetoAtual
        );

        Console.WriteLine(
            $"[JOGO] 🖼️ Objeto: {_palavraObjetoAtual}"
        );

        Console.WriteLine(
            $"[JOGO] 🖼️ Imagem mapeada: {_imagemObjetoAtual}"
        );

        Console.WriteLine(
            $"[JOGO] 📁 Caminho: {caminho}"
        );

        Console.WriteLine(
            $"[JOGO] 📁 Existe: {File.Exists(caminho)}"
        );

        if (File.Exists(caminho))
        {
            var bitmap = new Bitmap(caminho);

            ImagemObjetoConquista.Source = bitmap;

            Console.WriteLine(
                $"[JOGO] ✅ Imagem de {_palavraObjetoAtual} carregada."
            );

            return;
        }

        string fallback = Path.Combine(
            pastaObjetos,
            "objeto_generico.png"
        );

        if (File.Exists(fallback))
        {
            ImagemObjetoConquista.Source =
                new Bitmap(fallback);

            Console.WriteLine(
                "[JOGO] ⚠️ Usando objeto_generico.png."
            );
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine(
            $"[JOGO] ❌ Erro ao carregar imagem: {ex}"
        );
    }
}

public void AdicionarObstaculo(string objeto)
{
    if (string.IsNullOrWhiteSpace(objeto))
        return;

    objeto = objeto.Trim().ToUpperInvariant();

    char[] letrasPermitidas =
        Array.FindAll(
            objeto.ToCharArray(),
            c => char.IsLetter(c));

    objeto = new string(letrasPermitidas);

    Console.WriteLine(
        $"[TESTE] 'copo' → " +
        $"{EmojiObjetoService.ObterImagemCorrespondente("copo")}"
    );

    Console.WriteLine(
        $"[TESTE] 'COPO' → " +
        $"{EmojiObjetoService.ObterImagemCorrespondente("COPO")}"
    );

    Console.WriteLine(
        $"[TESTE] 'copo de vidro' → " +
        $"{EmojiObjetoService.ObterImagemCorrespondente("copo de vidro")}"
    );

    Console.WriteLine(
        $"[TESTE] objeto da IA '{objeto}' → " +
        $"{EmojiObjetoService.ObterImagemCorrespondente(objeto)}"
    );


    _palavraObjetoAtual = objeto;
    _totalLetrasObjeto = _palavraObjetoAtual.Length;

    Console.WriteLine($"[DEBUG] Palavra limpa processada: '{_palavraObjetoAtual}' (Tamanho: {_totalLetrasObjeto})");

    _emojiObjetoAtual = EmojiObjetoService.ObterEmojiCorrespondente(objeto);
    CriarNovoObstaculoNaTela();
    AtualizarRenderizacaoTexturaIA();
}

        private void CriarNovoObstaculoNaTela()
{
    var painel = new Border
    {
        Width = 110,
        Height = 60,

        Background = new SolidColorBrush(
            Color.Parse("#24152F")),

        BorderBrush = new SolidColorBrush(
            Color.Parse("#FF0055")),

        BorderThickness = new Thickness(2),

        CornerRadius = new CornerRadius(6),

        ClipToBounds = true
    };

    var texto = new TextBlock
    {
        Text = $"{_emojiObjetoAtual}\n{_palavraObjetoAtual}",

        Foreground = Brushes.White,

        FontSize = 18,

        FontWeight = FontWeight.Bold,

        TextAlignment = TextAlignment.Center,

        HorizontalAlignment =
            Avalonia.Layout.HorizontalAlignment.Center,

        VerticalAlignment =
            Avalonia.Layout.VerticalAlignment.Center,

        TextWrapping = TextWrapping.NoWrap,

        ClipToBounds = true
    };

    painel.Child = texto;

    double larguraTela = GameCanvas.Bounds.Width;

    double posicaoX =
        larguraTela > 120
            ? _random.NextDouble() * (larguraTela - 120)
            : 10;

    Canvas.SetLeft(painel, posicaoX);
    Canvas.SetTop(painel, -60);

    GameCanvas.Children.Add(painel);

    _inimigos.Add(new InimigoPalavra
    {
        Container = painel,
        TextBlockVisual = texto,
        PalavraAtual = _palavraObjetoAtual,
        Emoji = _emojiObjetoAtual,
        FoiDestruido = false
    });
}

               private void AtualizarHUD()
        {
            TxtScore.Text = _pontuacao.ToString("D6");
            
            if (PanelBlocosVidas != null && TxtLivesCount != null)
            {
                int vidasValidas = Math.Max(0, _vidas);
                TxtLivesCount.Text = $"{vidasValidas}/10";

                PanelBlocosVidas.Children.Clear();

                IBrush corBloco;
                if (vidasValidas == 1)
                {
                    corBloco = Brushes.Red;
                }
                else if (vidasValidas == 2)
                {
                    corBloco = Brushes.Gold;
                }
                else
                {
                    corBloco = Brushes.LimeGreen;
                }

                for (int i = 0; i < 10; i++)
                {
                    var bloco = new Border
                    {
                        Width = 18,
                        Height = 7,
                        CornerRadius = new CornerRadius(2),
                        Background = i < vidasValidas ? corBloco : new SolidColorBrush(Color.Parse("#33FFFFFF"))
                    };
                    PanelBlocosVidas.Children.Add(bloco);
                }
            }

            if (TxtObjetoNome != null && TxtObjetoIcone != null)
            {
                TxtObjetoNome.Text = _palavraObjetoAtual;
                TxtObjetoIcone.Text = _emojiObjetoAtual;
            }
        }
        private void BotaoReiniciar_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            ReiniciarJogo();
        }

        private void FimDeJogo()
{
    _temporizadorJogo.Stop();

    PararSomTiro();

    _moverEsquerda = false;
    _moverDireita = false;
    _estaAtirando = false;

    Console.WriteLine(
        "[JOGO] 💀 GAME OVER!"
    );


    MostrarConquista();

    TocarSomConquista();


    try
    {
        System.Diagnostics.Debug.WriteLine(
            $"Tentando salvar pontuação para: " +
            $"{_nomeUsuarioAtual} com {_pontuacao} pontos."
        );

        var bancoService =
            new Sistema.Services.BancoService();

        bancoService.SalvarPontuacao(
            _nomeUsuarioAtual,
            _pontuacao,
            _caminhoFotoAtual,
            _palavraObjetoAtual
        );

        System.Diagnostics.Debug.WriteLine(
            "Pontuação salva com sucesso!"
        );
        OnPartidaFinalizada?.Invoke();
    }
    catch (Exception ex)
    {
        Console.WriteLine(
            $"ERRO CRÍTICO AO SALVAR NO BANCO: " +
            $"{ex.Message} | StackTrace: {ex.StackTrace}"
        );
    }

    if (GameOverOverlay != null)
    {
        GameOverOverlay.IsVisible = true;
    }
}
        public void ReiniciarJogo()
        {
            _temporizadorJogo.Stop();
            PararSomTiro();

            if (GameOverOverlay != null)
            {
                GameOverOverlay.IsVisible = false;
            }

            foreach (Control tiro in _tiros) GameCanvas.Children.Remove(tiro);
            _tiros.Clear();

            foreach (var inimigoObj in _inimigos) GameCanvas.Children.Remove(inimigoObj.Container);
            _inimigos.Clear();

            _pontuacao = 0;
            _vidas = 10;
            _jogadorX = 180;
            _jogadorY = 500;
            _tempoUltimoTiro = 0;
            _tempoTotalJogoSegundos = 0;
            _intervaloSpawnAtual = 4000.0;
            _tempoDesdeUltimoSpawn = 0;
            _bancoLetrasAcumuladas.Clear();
            _letrasConstruidas = 0;
            AtualizarHUD();
            AtualizarRenderizacaoTexturaIA();

            Canvas.SetLeft(PlayerShip, _jogadorX);
            Canvas.SetTop(PlayerShip, _jogadorY);

            _temporizadorJogo.Start();
            Focus();
        }
        public void ConfigurarDadosPartida(string nomeUsuario, string caminhoFoto)
{
    Console.WriteLine(
        $"[JOGO] 👤 Configurando usuário da partida: '{nomeUsuario}'"
    );

    if (!string.IsNullOrWhiteSpace(nomeUsuario))
    {
        _nomeUsuarioAtual = nomeUsuario.Trim();
    }

    if (!string.IsNullOrWhiteSpace(caminhoFoto))
    {
        _caminhoFotoAtual = caminhoFoto;
    }

    Console.WriteLine(
        $"[JOGO] 👤 Usuário atual: '{_nomeUsuarioAtual}'"
    );
}
        
        public void DefinirEmojiObjeto(string emoji)
{
    if (string.IsNullOrWhiteSpace(emoji))
        return;

    _emojiObjetoAtual = emoji.Trim();

    Console.WriteLine(
        $"[JOGO] 😀 Emoji definido: {_emojiObjetoAtual}"
    );

    AtualizarHUD();
    AtualizarRenderizacaoTexturaIA();
}
        public void DefinirObjetoAlvo(string objeto)
{
    if (string.IsNullOrWhiteSpace(objeto))
        return;

    _palavraObjetoAtual = objeto.Trim().ToUpperInvariant();

    _bancoLetrasAcumuladas.Clear();
    _letrasConstruidas = 0;

    _totalLetrasObjeto = 0;

    foreach (char c in _palavraObjetoAtual)
    {
        if (char.IsLetter(c))
            _totalLetrasObjeto++;
    }

    AtualizarHUD();
    AtualizarRenderizacaoTexturaIA();
}
public void DefinirObjetoVisual(string objeto)
{
    if (string.IsNullOrWhiteSpace(objeto))
        return;

    _palavraObjetoAtual =
        objeto.Trim().ToUpperInvariant();

    _emojiObjetoAtual =
        EmojiObjetoService.ObterEmojiCorrespondente(
            objeto
        );

    _imagemObjetoAtual =
        EmojiObjetoService.ObterImagemCorrespondente(
            objeto
        );

    Console.WriteLine(
        $"[JOGO] Objeto visual: {_palavraObjetoAtual}"
    );

    Console.WriteLine(
        $"[JOGO] Emoji: {_emojiObjetoAtual}"
    );

    Console.WriteLine(
        $"[JOGO] Imagem: {_imagemObjetoAtual}"
    );

    AtualizarHUD();

    AtualizarImagemObjetoCard();
}

public async Task ConfigurarObjetoParaPartidaAsync(
    string nomeObjeto,
    string caminhoFoto)
{
    if (string.IsNullOrWhiteSpace(nomeObjeto))
        return;


    _palavraObjetoAtual =
        nomeObjeto.Trim().ToUpperInvariant();


    _caminhoFotoAtual =
        caminhoFoto ?? string.Empty;

    

    _emojiObjetoAtual =
        EmojiObjetoService.ObterEmojiCorrespondente(
            _palavraObjetoAtual
        );


    _imagemObjetoAtual =
        EmojiObjetoService.ObterImagemCorrespondente(
            _palavraObjetoAtual
        );

    Console.WriteLine(
        $"[JOGO] 🎯 Objeto recebido: {_palavraObjetoAtual}"
    );

    Console.WriteLine(
        $"[JOGO] 😀 Emoji recebido: {_emojiObjetoAtual}"
    );

    Console.WriteLine(
        $"[JOGO] 🖼️ Imagem mapeada: {_imagemObjetoAtual}"
    );

    Console.WriteLine(
        $"[JOGO] 📸 Foto original: {_caminhoFotoAtual}"
    );

    
    _bancoLetrasAcumuladas.Clear();

    _letrasConstruidas = 0;

    _totalLetrasObjeto = 0;

    foreach (char c in _palavraObjetoAtual)
    {
        if (char.IsLetter(c))
            _totalLetrasObjeto++;
    }


    await CarregarImagemObjetoAsync(
        _caminhoFotoAtual
    );

    AtualizarHUD();

    AtualizarRenderizacaoTexturaIA();
}

private async Task CarregarImagemObjetoAsync(string caminhoFoto)
{
    if (string.IsNullOrWhiteSpace(caminhoFoto))
        return;

    if (!File.Exists(caminhoFoto))
    {
        Console.WriteLine(
            $"[JOGO] Foto não encontrada: {caminhoFoto}"
        );

        return;
    }

    try
    {
        await using var stream = File.OpenRead(caminhoFoto);

        _imagemObjetoOriginal = await Task.Run(
            () => new Bitmap(stream)
        );

        Console.WriteLine(
            $"[JOGO] Imagem do objeto carregada: {_palavraObjetoAtual}"
        );
    }
    catch (Exception ex)
    {
        Console.WriteLine(
            $"[JOGO] Erro ao carregar imagem: {ex.Message}"
        );
    }
}
private async Task ContagemRegressivaAsync()
{
    try
    {
        var vozService =
            new Sistema.Services.VozService();

        string objeto =
            string.IsNullOrWhiteSpace(_palavraObjetoAtual)
                ? "objeto"
                : _palavraObjetoAtual.ToLowerInvariant();

        await FalarComOndaAsync(
            vozService,
            $"Objeto identificado: {objeto}."
        );

        await Task.Delay(30);

        await FalarComOndaAsync(
            vozService,
            "O jogo iniciará em 3."
        );

        await Task.Delay(30);

        await FalarComOndaAsync(
            vozService,
            "2."
        );

        await Task.Delay(30);

        await FalarComOndaAsync(
            vozService,
            "1."
        );

        await Task.Delay(30);

        await FalarComOndaAsync(
            vozService,
            "Começar!"
        );

        Console.WriteLine(
            "[JOGO] ✅ Contagem finalizada!"
        );
    }
    catch (Exception ex)
    {
        Console.WriteLine(
            $"[JOGO] ❌ Erro na contagem: {ex}"
        );
    }
}
    
    }
}
using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Styling;
using Launcher.Services;
using System;
using System.Threading.Tasks;

namespace Launcher;

public partial class MainWindow : Window
{
    private readonly ProcessoService _processoService;

    public MainWindow()
    {
        InitializeComponent();
        this.WindowStartupLocation = WindowStartupLocation.CenterScreen;

        _processoService = new ProcessoService();

        var barraTitulo =
            this.FindControl<Grid>("TitleBar");

        if (barraTitulo != null)
        {
            barraTitulo.PointerPressed +=
                PressionarBarraTitulo;
        }

        var botaoFechar =
            this.FindControl<Button>("FecharBotao");

        if (botaoFechar != null)
        {
            botaoFechar.Click +=
                ClicarBotaoFechar;
        }

        var botaoTentarNovamente =
            this.FindControl<Button>(
                "BtnTentarNovamente"
            );

        if (botaoTentarNovamente != null)
        {
            botaoTentarNovamente.Click +=
                BtnTentarNovamente_Click;
        }

        IniciarAnimacaoBarra();

        Opened += MainWindow_Opened;
    }

private async void MainWindow_Opened(object? sender, EventArgs e)
{
    await Task.Delay(50); 

    try
    {
        var screen = Screens.Primary;
        if (screen != null)
        {
            var bounds = screen.WorkingArea;
            var scaling = screen.Scaling;

            double larguraAtual = Bounds.Width > 0 ? Bounds.Width : Width;
            double alturaAtual = Bounds.Height > 0 ? Bounds.Height : Height;

            int x = (int)((bounds.Width / scaling - larguraAtual) / 2) + (int)(bounds.X / scaling);
            int y = (int)((bounds.Height / scaling - alturaAtual) / 2) + (int)(bounds.Y / scaling);

            Position = new Avalonia.PixelPoint(x, y);
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[AVISO] Não foi possível centralizar: {ex.Message}");
    }

    try
    {
        await IniciarSistemaAsync();
    }
    catch (Exception ex)
    {
        Console.WriteLine("=================================");
        Console.WriteLine("ERRO NO LAUNCHER");
        Console.WriteLine(ex);
        Console.WriteLine("=================================");

        var status = this.FindControl<TextBlock>("TxtStatus");
        if (status != null)
        {
            status.Text = $"Erro: {ex.Message}";
        }

        var botao = this.FindControl<Button>("BtnTentarNovamente");
        if (botao != null)
        {
            botao.IsVisible = true;
        }
    }
}

    private async Task IniciarSistemaAsync()
    {
        var status =
            this.FindControl<TextBlock>(
                "TxtStatus"
            );

        var botao =
            this.FindControl<Button>(
                "BtnTentarNovamente"
            );

        if (status != null)
        {
            status.Text =
                "Preparando para iniciar o Sistema...";
        }

        if (botao != null)
        {
            botao.IsVisible = false;
        }

        var sucesso =
            await _processoService.IniciarTudoAsync(
                mensagem =>
                {
                    if (status != null)
                    {
                        status.Text = mensagem;
                    }
                });

        if (!sucesso)
        {
            if (botao != null)
            {
                botao.IsVisible = true;
            }

            return;
        }

        if (status != null)
        {
            status.Text =
                "Sistema iniciado com sucesso!";
        }

        await Task.Delay(1000);

        Close();
    }


    private void IniciarAnimacaoBarra()
    {
        var barra =
            this.FindControl<StackPanel>(
                "LoadingBar"
            );

        if (barra == null)
        {
            return;
        }

        var animacao = new Animation
        {
            Duration =
                TimeSpan.FromSeconds(0.85),

            IterationCount =
                IterationCount.Infinite,

            Children =
            {
                new KeyFrame
                {
                    Cue = new Cue(0),

                    Setters =
                    {
                        new Setter(
                            TranslateTransform.XProperty,
                            -40.0)
                    }
                },

                new KeyFrame
                {
                    Cue = new Cue(1),

                    Setters =
                    {
                        new Setter(
                            TranslateTransform.XProperty,
                            380.0)
                    }
                }
            }
        };

        _ = animacao.RunAsync(barra);
    }


    private void PressionarBarraTitulo(
        object? remetente,
        PointerPressedEventArgs e)
    {
        if (
            e.GetCurrentPoint(this)
                .Properties
                .IsLeftButtonPressed
        )
        {
            BeginMoveDrag(e);
        }
    }


    private void ClicarBotaoFechar(
        object? remetente,
        RoutedEventArgs e)
    {
        Close();
    }


    private async void BtnTentarNovamente_Click(
        object? sender,
        RoutedEventArgs e)
    {
        try
        {
            await IniciarSistemaAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine(
                $"Erro ao tentar iniciar novamente: {ex}"
            );
        }
    }
}
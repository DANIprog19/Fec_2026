using Avalonia.Interactivity;
using Avalonia.Controls;
using Sistema.Services;
using System;

namespace Sistema.Views
{
    public partial class Login : UserControl
    {
        private readonly BancoService _dbService;

        public event EventHandler<string>? UsuarioLogadoComSucesso;

        public Login()
        {
            InitializeComponent();
            _dbService = new BancoService(); 

            BotaoIrParaCriar.Click += (s, e) => 
            {
                PainelLogin.IsVisible = false;
                PainelCriarUsuario.IsVisible = true;
                TxtStatusRegistro.Text = "";
                CheckCriarAleatorio.IsChecked = false;
            };

           
            BotaoVoltarLogin.Click += (s, e) => 
            {
                PainelCriarUsuario.IsVisible = false;
                PainelLogin.IsVisible = true;
                TxtStatusLogin.Text = "";
            };

            
            CheckCriarAleatorio.IsCheckedChanged += (s, e) =>
            {
                if (CheckCriarAleatorio.IsChecked == true)
                {
                    string randomUser = "FEC_" + Guid.NewGuid().ToString().Substring(0, 5).ToUpper();
                    TxtNovoUsuario.Text = randomUser;
                    TxtNovoUsuario.IsEnabled = false;
                }
                else
                {
                    TxtNovoUsuario.Text = string.Empty;
                    TxtNovoUsuario.IsEnabled = true;
                }
            };

            BotaoEntrar.Click += OnBotaoEntrarClick;
            BotaoSalvarUsuario.Click += OnBotaoSalvarUsuarioClick;
        }

        private void OnBotaoEntrarClick(object? sender, RoutedEventArgs e)
{
    string usuario = TxtNomeUsuario.Text?.Trim() ?? string.Empty;

    if (string.IsNullOrEmpty(usuario))
    {
        TxtStatusLogin.Text = "Insira um nome de usuário válido.";
        return;
    }


    if (usuario.Equals(
        "insidetech",
        StringComparison.OrdinalIgnoreCase))
    {
        SessaoUsuario.Nome = usuario;

        Console.WriteLine(
            $"[LOGIN] Usuário logado: {SessaoUsuario.Nome}"
        );

        TxtStatusLogin.Foreground =
            Avalonia.Media.Brushes.Green;

        TxtStatusLogin.Text =
            "Acesso Master Liberado!";

        UsuarioLogadoComSucesso?.Invoke(
            this,
            usuario
        );

        return;
    }


    if (_dbService.UsuarioExiste(usuario))
    {
        SessaoUsuario.Nome = usuario;

        Console.WriteLine(
            $"[LOGIN] Usuário logado: {SessaoUsuario.Nome}"
        );

        TxtStatusLogin.Foreground =
            Avalonia.Media.Brushes.Green;

        TxtStatusLogin.Text =
            "Acesso concedido!";

        UsuarioLogadoComSucesso?.Invoke(
            this,
            usuario
        );
    }
    else
    {
        TxtStatusLogin.Foreground =
            Avalonia.Media.Brushes.Red;

        TxtStatusLogin.Text =
            "Usuário não encontrado.";
    }
}

        private void OnBotaoSalvarUsuarioClick(object? sender, RoutedEventArgs e)
        {
            string novoUsuario = TxtNovoUsuario.Text?.Trim() ?? string.Empty;

            if (string.IsNullOrEmpty(novoUsuario))
            {
                TxtStatusRegistro.Foreground = Avalonia.Media.Brushes.Red;
                TxtStatusRegistro.Text = "Preencha ou gere o nome de usuário!";
                return;
            }

            
            if (novoUsuario.Equals("insidetech", StringComparison.OrdinalIgnoreCase))
            {
                TxtStatusRegistro.Foreground = Avalonia.Media.Brushes.Red;
                TxtStatusRegistro.Text = "Este nome de usuário é reservado!";
                return;
            }

           
            bool sucesso = _dbService.CadastrarUsuario(novoUsuario);

            if (sucesso)
            {
                TxtStatusRegistro.Foreground = Avalonia.Media.Brushes.Green;
                TxtStatusRegistro.Text = $"Sucesso! '{novoUsuario}' registrado.";
                
                CheckCriarAleatorio.IsChecked = false;
                TxtNovoUsuario.Text = "";
                TxtNovoUsuario.IsEnabled = true;
            }
            else
            {
                TxtStatusRegistro.Foreground = Avalonia.Media.Brushes.Red;
                TxtStatusRegistro.Text = "Erro: Este nome já existe no sistema!";
            }
        }
        private void BotaoVoltarLogin_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            
            PainelCriarUsuario.IsVisible = false;
            TxtStatusRegistro.Text = string.Empty;
            TxtNovoUsuario.Text = string.Empty;

            
            PainelLogin.IsVisible = true;
            TxtStatusLogin.Text = string.Empty;
        }
    }
}
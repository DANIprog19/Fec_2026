using Avalonia.Controls;
using System;
using Sistema.Views;
using Sistema.Services;

namespace Sistema.Views
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            CarregarTelaLogin();
        }

        private void CarregarTelaLogin()
{
    VozService.PararVoz();

    var login = new Login();

    login.UsuarioLogadoComSucesso += (s, nomeUsuario) =>
    {
        CarregarJanelaPrincipal(nomeUsuario);
    };

    ConteudoPrincipal.Content = login;
}

        private void CarregarJanelaPrincipal(string nomeUsuario)
{
    var janelaPrincipal = new JanelaPrincipal();

    janelaPrincipal.DefinirUsuarioAtual(nomeUsuario);

    Console.WriteLine(
        $"[LOGIN] Usuário logado: {nomeUsuario}"
    );

    janelaPrincipal.VoltarParaLoginSolicitado += (s, e) =>
    {
        VozService.PararVoz();
        CarregarTelaLogin();
    };

    ConteudoPrincipal.Content = janelaPrincipal;
}
        protected override void OnClosed(
    System.EventArgs e)
{
    Console.WriteLine(
        "[MAIN WINDOW] ⛔ Aplicação encerrada."
    );

    VozService.PararVoz();

    base.OnClosed(e);
}
    }
}
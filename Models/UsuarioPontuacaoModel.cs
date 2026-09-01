using Avalonia.Media;
using Avalonia.Media.Imaging;

namespace Sistema.Models
{
    public class UsuarioPontuacaoModel
    {
        public string Nome { get; set; } = string.Empty;
        public string Setor { get; set; } = string.Empty;
        public int Pontos { get; set; }
        public string CaminhoFoto { get; set; } = string.Empty;
        public Bitmap? FotoBitmap { get; set; }
        public string DataHoraJogada { get; set; } = string.Empty;
        public IBrush CorDestaque { get; set; } = Brushes.Gray;
        public bool IsEmpatado { get; set; }
    }
}
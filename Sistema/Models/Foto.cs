namespace Sistema.Models
{
    public class Foto
    {
        public int Id { get; set; }

        public string Caminho { get; set; } = "";

        public string DataHora { get; set; } = "";

        public string NomeObjeto { get; set; } = "";

        public string Descricao { get; set; } = "";

        public bool Selecionada { get; set; }
    }
}
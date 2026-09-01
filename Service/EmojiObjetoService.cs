using System;

namespace Sistema.Services
{
    public static class EmojiObjetoService
    {


        public static string ObterEmojiCorrespondente(
            string objetoDetectado)
        {
            if (string.IsNullOrWhiteSpace(objetoDetectado))
                return "❓";

            string texto = NormalizarTexto(objetoDetectado);

            string[] familiaCopos =
            {
                "COPO",
                "XICARA",
                "XIARA",
                "CANECA",
                "TACA",
                "JARRA",
                "MUG"
            };

            if (ContemAlgum(texto, familiaCopos))
                return "☕";

            string[] familiaCorte =
            {
                "TESOURA",
                "ALICATE",
                "ESTILETE",
                "FACA",
                "CANIVETE",
                "LAMINA",
                "CORTE",
                "CORTAR",
                "UTENSILIO DE CORTE",
                "FERRAMENTA DE CORTE"
            };

            if (ContemAlgum(texto, familiaCorte))
                return "✂️";

            string[] familiaEscrita =
            {
                "CANETA",
                "PEN",
                "LAPIS",
                "BORRACHA",
                "REGUA"
            };

            if (ContemAlgum(texto, familiaEscrita))
                return "🖊️";


            string[] familiaEletronicos =
            {
                "CELULAR",
                "SMARTPHONE",
                "TELEFONE",
                "CONTROLE",
                "GAMEPAD"
            };

            if (ContemAlgum(texto, familiaEletronicos))
            {
                if (
                    texto.Contains("CONTROLE") ||
                    texto.Contains("GAMEPAD")
                )
                {
                    return "🎮";
                }

                return "📱";
            }

            string[] familiaLeitura =
            {
                "LIVRO",
                "BOOK",
                "CADERNO",
                "NOTEBOOK",
                "NOTEPAD"
            };

            if (ContemAlgum(texto, familiaLeitura))
            {
                if (
                    texto.Contains("CADERNO") ||
                    texto.Contains("NOTEBOOK") ||
                    texto.Contains("NOTEPAD")
                )
                {
                    return "📓";
                }

                return "📖";
            }

            if (
                texto.Contains("OCULOS") ||
                texto.Contains("GLASSES") ||
                texto.Contains("EYEGLASSES")
            )
            {
                return "👓";
            }

            if (texto.Contains("TECLADO"))
                return "⌨️";

            if (texto.Contains("MOUSE"))
                return "🖱️";

            if (
                texto.Contains("COMPUTADOR") ||
                texto.Contains("LAPTOP")
            )
            {
                return "💻";
            }

            if (
                texto.Contains("GARRAFA") ||
                texto.Contains("BOTTLE")
            )
            {
                return "🧴";
            }


            if (
                texto.Contains("CHAVE") ||
                texto.Contains("KEY")
            )
            {
                return "🔑";
            }


            if (
                texto.Contains("RELOGIO") ||
                texto.Contains("WATCH") ||
                texto.Contains("CLOCK")
            )
            {
                return "⌚";
            }


            if (
                texto.Contains("MOCHILA") ||
                texto.Contains("BACKPACK")
            )
            {
                return "🎒";
            }


            if (
                texto.Contains("FONE") ||
                texto.Contains("HEADPHONE") ||
                texto.Contains("HEADSET") ||
                texto.Contains("EARPHONE") ||
                texto.Contains("EARBUD")
            )
            {
                return "🎧";
            }


            if (
                texto.Contains("CARTEIRA") ||
                texto.Contains("WALLET")
            )
            {
                return "👛";
            }

            if (
                texto.Contains("CACHORRO") ||
                texto.Contains("CACHORRINHO") ||
                texto.Contains("DOG")
            )
            {
                return "🐶";
            }

            if (
                texto.Contains("GATO") ||
                texto.Contains("GATINHO") ||
                texto.Contains("CAT")
            )
            {
                return "🐱";
            }

            if (
                texto.Contains("CAIXA") ||
                texto.Contains("EMBALAGEM") ||
                texto.Contains("PACOTE")
            )
            {
                return "📦";
            }

            return "📦";
        }

        public static string ObterImagemCorrespondente(
            string objetoDetectado)
        {
            if (string.IsNullOrWhiteSpace(objetoDetectado))
                return "objeto_generico.png";

            string texto = NormalizarTexto(objetoDetectado);


            if (ContemAlgum(
                texto,
                "TESOURA",
                "ALICATE",
                "ESTILETE",
                "FACA",
                "CANIVETE",
                "LAMINA"))
            {
                return "tesoura.png";
            }

            if (ContemAlgum(
                texto,
                "CANETA",
                "PEN",
                "LAPIS",
                "BORRACHA",
                "REGUA"))
            {
                return "caneta.png";
            }


            if (ContemAlgum(
                texto,
                "COPO",
                "XICARA",
                "XIARA",
                "CANECA",
                "TACA",
                "JARRA",
                "MUG"))
            {
                return "copo.png";
            }


            if (ContemAlgum(
                texto,
                "CELULAR",
                "SMARTPHONE",
                "TELEFONE"))
            {
                return "celular.png";
            }

            if (ContemAlgum(
                texto,
                "CONTROLE",
                "GAMEPAD"))
            {
                return "controle.png";
            }


            if (ContemAlgum(
                texto,
                "LIVRO",
                "BOOK"))
            {
                return "livro.png";
            }


            if (ContemAlgum(
                texto,
                "CADERNO",
                "NOTEBOOK",
                "NOTEPAD"))
            {
                return "caderno.png";
            }

            if (ContemAlgum(
                texto,
                "OCULOS",
                "GLASSES",
                "EYEGLASSES"))
            {
                return "oculos.png";
            }


            if (texto.Contains("TECLADO"))
                return "teclado.png";

            if (texto.Contains("MOUSE"))
                return "mouse.png";


            if (ContemAlgum(
                texto,
                "GARRAFA",
                "BOTTLE"))
            {
                return "garrafa.png";
            }

            if (ContemAlgum(
                texto,
                "CHAVE",
                "KEY"))
            {
                return "chave.png";
            }

            if (ContemAlgum(
                texto,
                "RELOGIO",
                "WATCH",
                "CLOCK"))
            {
                return "relogio.png";
            }


            if (ContemAlgum(
                texto,
                "MOCHILA",
                "BACKPACK"))
            {
                return "mochila.png";
            }



            if (ContemAlgum(
                texto,
                "FONE",
                "HEADPHONE",
                "HEADSET",
                "EARPHONE",
                "EARBUD"))
            {
                return "fone.png";
            }

            if (ContemAlgum(
                texto,
                "CARTEIRA",
                "WALLET"))
            {
                return "carteira.png";
            }

            if (ContemAlgum(
                texto,
                "CACHORRO",
                "CACHORRINHO",
                "DOG"))
            {
                return "cachorro.png";
            }

            if (ContemAlgum(
                texto,
                "GATO",
                "GATINHO",
                "CAT"))
            {
                return "gato.png";
            }

            if (ContemAlgum(
                texto,
                "CAIXA",
                "EMBALAGEM",
                "PACOTE"))
            {
                return "caixa.png";
            }

            return "objeto_generico.png";
        }

        private static string NormalizarTexto(
            string texto)
        {
            return texto
                .Trim()
                .ToUpperInvariant()
                .Replace("Á", "A")
                .Replace("À", "A")
                .Replace("Ã", "A")
                .Replace("Â", "A")
                .Replace("Ä", "A")
                .Replace("É", "E")
                .Replace("Ê", "E")
                .Replace("Ë", "E")
                .Replace("Í", "I")
                .Replace("Î", "I")
                .Replace("Ï", "I")
                .Replace("Ó", "O")
                .Replace("Ò", "O")
                .Replace("Õ", "O")
                .Replace("Ô", "O")
                .Replace("Ö", "O")
                .Replace("Ú", "U")
                .Replace("Ù", "U")
                .Replace("Û", "U")
                .Replace("Ü", "U")
                .Replace("Ç", "C");
        }

        private static bool ContemAlgum(
            string texto,
            params string[] termos)
        {
            foreach (string termo in termos)
            {
                if (texto.Contains(termo))
                    return true;
            }

            return false;
        }
    }
}
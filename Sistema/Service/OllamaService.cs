using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Sistema.Services
{
    public class OllamaService
    {
        private readonly HttpClient _httpClient;

        public OllamaService()
        {
            _httpClient = new HttpClient
            {
                BaseAddress = new Uri("http://localhost:11434")
            };

            _httpClient.Timeout = TimeSpan.FromMinutes(10);
        }

        public async Task<string> GerarRespostaAsync(string prompt)
        {
            var dados = new
            {
                model = "qwen3-vl:2b",
                prompt = prompt,
                stream = false
            };

            string json = JsonSerializer.Serialize(dados);

            using var conteudo = new StringContent(
                json,
                Encoding.UTF8,
                "application/json"
            );

            Console.WriteLine("[OLLAMA] Enviando solicitação...");

            var resposta = await _httpClient.PostAsync(
                "/api/generate",
                conteudo
            );

            resposta.EnsureSuccessStatusCode();

            string resultado = await resposta.Content.ReadAsStringAsync();

            using JsonDocument documento =
                JsonDocument.Parse(resultado);

            if (documento.RootElement.TryGetProperty(
                "response",
                out JsonElement respostaOllama))
            {
                string texto = respostaOllama.GetString() ?? "";

                Console.WriteLine($"[OLLAMA] Resposta: {texto}");

                return texto.Trim();
            }

            return "";
        }
    }
}
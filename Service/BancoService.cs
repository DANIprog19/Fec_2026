using System;
using System.IO;
using System.Collections.Generic;
using Sistema.Models;
using Microsoft.Data.Sqlite;
using Avalonia.Media.Imaging;

namespace Sistema.Services
{
    public class BancoService
    {
        private readonly string _connectionString;
        private readonly string _dbFolderPath;
        private readonly string _dbFilePath;

        public BancoService()
        {
            _dbFolderPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data");
            _dbFilePath = Path.Combine(_dbFolderPath, "Sistema.db");
            _connectionString = $"Data Source={_dbFilePath}";

            Console.WriteLine(">>> CAMINHO EXATO DO BANCO DE DADOS: " + _dbFilePath);

            InicializarBanco();
        }

        private void InicializarBanco()
        {
            Directory.CreateDirectory(_dbFolderPath);

            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText = @"
                CREATE TABLE IF NOT EXISTS Usuarios (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    NomeUsuario TEXT UNIQUE NOT NULL
                );

                CREATE TABLE IF NOT EXISTS Pontuacao (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    Data TEXT NOT NULL,
                    Hora TEXT NOT NULL,
                    Pontos INTEGER NOT NULL,
                    Usuario_id INTEGER UNIQUE NOT NULL, -- UNIQUE impede duplicadas para o mesmo usuário
                    CaminhoFoto TEXT,
                    ObjetoAnalisado TEXT,
                    FOREIGN KEY (Usuario_id) REFERENCES Usuarios(id)
                );

                CREATE TABLE IF NOT EXISTS HistoricoConversas (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    Remetente TEXT NOT NULL,
                    Mensagem TEXT NOT NULL,
                    DataHora DATETIME DEFAULT CURRENT_TIMESTAMP
                );

                CREATE TABLE IF NOT EXISTS ConfigSistema (
                    Chave TEXT PRIMARY KEY,
                    Valor TEXT
                );

                INSERT OR IGNORE INTO Usuarios (NomeUsuario) VALUES ('insidetech');
                INSERT OR IGNORE INTO Usuarios (NomeUsuario) VALUES ('convidado');
            ";
            command.ExecuteNonQuery();
        }

        public bool CadastrarUsuario(string nomeUsuario)
        {
            try
            {
                using (var connection = new SqliteConnection(_connectionString))
                {
                    connection.Open();
                    string insertQuery = "INSERT INTO Usuarios (NomeUsuario) VALUES (@nome)";
                    
                    using (var command = new SqliteCommand(insertQuery, connection))
                    {
                        command.Parameters.AddWithValue("@nome", nomeUsuario.Trim());
                        command.ExecuteNonQuery();
                    }
                }
                return true;
            }
            catch (SqliteException)
            {
                return false;
            }
        }

        public bool UsuarioExiste(string nomeUsuario)
        {
            nomeUsuario = nomeUsuario.Trim();

            using (var connection = new SqliteConnection(_connectionString))
            {
                connection.Open();
                string query = "SELECT COUNT(1) FROM Usuarios WHERE NomeUsuario = @nome";

                using (var command = new SqliteCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@nome", nomeUsuario);
                    long resultado = (long)(command.ExecuteScalar() ?? 0L);
                    return resultado > 0;
                }
            }
        }

        public bool EhAdministrador(string nomeUsuario)
        {
            return nomeUsuario.Trim().Equals("insidetech", StringComparison.OrdinalIgnoreCase);
        }

        public void SalvarPontuacao(string nomeUsuario, int pontos, string caminhoFoto, string objetoAnalisado)
{
    try
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        long usuarioId = -1;
        var cmdUsuario = connection.CreateCommand();
        cmdUsuario.CommandText = "SELECT id FROM Usuarios WHERE lower(NomeUsuario) = lower($nome)";
        cmdUsuario.Parameters.AddWithValue("$nome", nomeUsuario.Trim());
        
        var resultado = cmdUsuario.ExecuteScalar();
        if (resultado != null)
        {
            usuarioId = (long)resultado;
        }
        else
        {
            return;
        }

        var cmdVerifica = connection.CreateCommand();
        cmdVerifica.CommandText = "SELECT COUNT(*) FROM Pontuacao WHERE Usuario_id = $usuarioId";
        cmdVerifica.Parameters.AddWithValue("$usuarioId", usuarioId);
        long existe = (long)cmdVerifica.ExecuteScalar()!;

        var cmdSalvar = connection.CreateCommand();

        if (existe > 0)
        {
            cmdSalvar.CommandText = @"
                UPDATE Pontuacao 
                SET Pontos = $pontos, 
                    Data = $data, 
                    Hora = $hora, 
                    CaminhoFoto = $caminhoFoto, 
                    ObjetoAnalisado = $objetoAnalisado
                WHERE Usuario_id = $usuarioId";
        }
        else
        {
            cmdSalvar.CommandText = @"
                INSERT INTO Pontuacao (Data, Hora, Pontos, Usuario_id, CaminhoFoto, ObjetoAnalisado) 
                VALUES ($data, $hora, $pontos, $usuarioId, $caminhoFoto, $objetoAnalisado)";
        }

        cmdSalvar.Parameters.AddWithValue("$data", DateTime.Now.ToString("yyyy-MM-dd"));
        cmdSalvar.Parameters.AddWithValue("$hora", DateTime.Now.ToString("HH:mm:ss"));
        cmdSalvar.Parameters.AddWithValue("$pontos", pontos);
        cmdSalvar.Parameters.AddWithValue("$usuarioId", usuarioId);
        cmdSalvar.Parameters.AddWithValue("$caminhoFoto", (object?)caminhoFoto ?? DBNull.Value);
        cmdSalvar.Parameters.AddWithValue("$objetoAnalisado", (object?)objetoAnalisado ?? DBNull.Value);

        cmdSalvar.ExecuteNonQuery();
    }
    catch (Exception ex)
    {
        System.Diagnostics.Debug.WriteLine($"Erro ao salvar pontuação: {ex.Message}");
    }
}

        public List<UsuarioPontuacaoModel> ObterRanking(int limite)
        {
            var lista = new List<UsuarioPontuacaoModel>();

            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            var command = connection.CreateCommand();
            
            command.CommandText = @"
                SELECT p.Pontos, u.NomeUsuario, p.ObjetoAnalisado, p.CaminhoFoto, p.Data, p.Hora
                FROM Pontuacao p
                JOIN Usuarios u ON p.Usuario_id = u.id
                JOIN (
                    SELECT Usuario_id, MAX(Pontos) as MaxPontos
                    FROM Pontuacao
                    GROUP BY Usuario_id
                ) m ON p.Usuario_id = m.Usuario_id AND p.Pontos = m.MaxPontos
                ORDER BY p.Pontos DESC
                LIMIT $limite;";
            
            command.Parameters.AddWithValue("$limite", limite);

            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                string caminhoFoto = reader.IsDBNull(3) ? string.Empty : reader.GetString(3);
                Bitmap? bitmap = null;

                try
                {
                    if (!string.IsNullOrEmpty(caminhoFoto) && File.Exists(caminhoFoto))
                    {
                        bitmap = new Bitmap(caminhoFoto);
                    }
                }
                catch { }

                lista.Add(new UsuarioPontuacaoModel
                {
                    Pontos = reader.GetInt32(0),
                    Nome = reader.GetString(1),
                    Setor = reader.IsDBNull(2) ? "Geral" : reader.GetString(2),
                    CaminhoFoto = caminhoFoto,
                    FotoBitmap = bitmap,
                    DataHoraJogada = $"{reader.GetString(4)} às {reader.GetString(5)}"
                });
            }

            return lista;
        }

        public void SalvarMensagem(string remetente, string mensagem)
        {
            using var conexao = new SqliteConnection(_connectionString);
            conexao.Open();

            string query = "INSERT INTO HistoricoConversas (Remetente, Mensagem) VALUES (@remetente, @mensagem)";
            using var comando = new SqliteCommand(query, conexao);
            comando.Parameters.AddWithValue("@remetente", remetente);
            comando.Parameters.AddWithValue("@mensagem", mensagem);
            comando.ExecuteNonQuery();
        }

        public List<string> ObterUltimasConversas(int limite = 5)
        {
            var historico = new List<string>();
            using var conexao = new SqliteConnection(_connectionString);
            conexao.Open();

            string query = "SELECT Remetente, Mensagem FROM HistoricoConversas ORDER BY Id DESC LIMIT @limite";
            using var comando = new SqliteCommand(query, conexao);
            comando.Parameters.AddWithValue("@limite", limite);

            using var reader = comando.ExecuteReader();
            while (reader.Read())
            {
                string remetente = reader.GetString(0);
                string mensagem = reader.GetString(1);
                historico.Insert(0, $"{remetente}: {mensagem}");
            }

            return historico;
        }

        public string? ObterConfiguracao(string chave)
        {
            using var conexao = new SqliteConnection(_connectionString);
            conexao.Open();

            string query = "SELECT Valor FROM ConfigSistema WHERE Chave = @chave";
            using var comando = new SqliteCommand(query, conexao);
            comando.Parameters.AddWithValue("@chave", chave);
            
            return comando.ExecuteScalar()?.ToString();
        }

        public void SalvarConfiguracao(string chave, string valor)
        {
            using var conexao = new SqliteConnection(_connectionString);
            conexao.Open();

            string query = "INSERT OR REPLACE INTO ConfigSistema (Chave, Valor) VALUES (@chave, @valor)";
            using var comando = new SqliteCommand(query, conexao);
            comando.Parameters.AddWithValue("@chave", chave);
            comando.Parameters.AddWithValue("@valor", valor);
            comando.ExecuteNonQuery();
        }
    }
}
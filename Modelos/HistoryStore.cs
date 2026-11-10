using System.IO;
using System.Security.Cryptography;
using Microsoft.Data.Sqlite;
using SQLitePCL;

namespace TranscriptionMedia;

internal sealed class HistoryStore : IDisposable
{
	private SqliteConnection? db;
	private SqliteConnection Connection => db ?? throw new InvalidOperationException("Historial no disponible.");
	public bool IsOpen => db != null;

	public void Open()
	{
		Batteries_V2.Init();
		string text = System.IO.Path.Combine(AppPaths.Root, "datos", Environment.UserName);
		Directory.CreateDirectory(text);
		string path = System.IO.Path.Combine(text, "clave.bin");
		byte[] array;
		if (File.Exists(path))
		{
			array = ProtectedData.Unprotect(File.ReadAllBytes(path), null, DataProtectionScope.CurrentUser);
		}
		else
		{
			if (File.Exists(System.IO.Path.Combine(text, "historial.db")))
			{
				throw new IOException();
			}
			array = RandomNumberGenerator.GetBytes(32);
			File.WriteAllBytes(path, ProtectedData.Protect(array, null, DataProtectionScope.CurrentUser));
		}
		try
		{
			var connection = new SqliteConnection(new SqliteConnectionStringBuilder
			{
				DataSource = System.IO.Path.Combine(text, "historial.db"),
				Password = Convert.ToHexString(array),
				Pooling = false
			}.ToString());
			try
			{
				connection.Open();
				using var cipher = connection.CreateCommand();
				cipher.CommandText = "PRAGMA cipher_version";
				if (string.IsNullOrWhiteSpace(cipher.ExecuteScalar()?.ToString()))
				{
					throw new InvalidOperationException("El historial requiere SQLCipher; se canceló la apertura para no guardar datos sin cifrar.");
				}
				using SqliteCommand sqliteCommand = connection.CreateCommand();
				sqliteCommand.CommandText = "PRAGMA journal_mode=DELETE; PRAGMA secure_delete=ON; CREATE TABLE IF NOT EXISTS history(id INTEGER PRIMARY KEY,date TEXT NOT NULL,source TEXT NOT NULL,text TEXT NOT NULL);";
				sqliteCommand.ExecuteNonQuery();
				db = connection;
			}
			catch
			{
				connection.Dispose();
				throw;
			}
		}
		finally
		{
			CryptographicOperations.ZeroMemory(array);
		}
	}

	public List<Entry> Read()
	{
		var entries = new List<Entry>();
		using SqliteCommand sqliteCommand = Connection.CreateCommand();
		sqliteCommand.CommandText = "SELECT id,date,source,text FROM history ORDER BY id DESC";
		using SqliteDataReader sqliteDataReader = sqliteCommand.ExecuteReader();
		while (sqliteDataReader.Read())
		{
			entries.Add(new Entry(sqliteDataReader.GetInt64(0), sqliteDataReader.GetString(1), sqliteDataReader.GetString(2), sqliteDataReader.GetString(3)));
		}
		return entries;
	}

	public long Save(string source, string text, long? id = null)
	{
		using SqliteCommand sqliteCommand = Connection.CreateCommand();
		sqliteCommand.CommandText = (id.HasValue ? "UPDATE history SET text=$text WHERE id=$id; SELECT $id;" : "INSERT INTO history(date,source,text) VALUES($date,$source,$text); SELECT last_insert_rowid();");
		sqliteCommand.Parameters.AddWithValue("$date", DateTime.Now.ToString("dd/MM/yyyy HH:mm"));
		sqliteCommand.Parameters.AddWithValue("$source", source);
		sqliteCommand.Parameters.AddWithValue("$text", text);
		sqliteCommand.Parameters.AddWithValue("$id", id.GetValueOrDefault());
		return Convert.ToInt64(sqliteCommand.ExecuteScalar());
	}

	public void Delete(bool all, long? id)
	{
		using var command = Connection.CreateCommand();
		command.CommandText = all ? "DELETE FROM history; VACUUM;" : "DELETE FROM history WHERE id=$id";
		if (!all)
		{
			command.Parameters.AddWithValue("$id", id ?? throw new ArgumentNullException(nameof(id)));
		}
		command.ExecuteNonQuery();
	}

	public string? ReadCipherVersion()
	{
		using var command = Connection.CreateCommand();
		command.CommandText = "PRAGMA cipher_version";
		return command.ExecuteScalar()?.ToString();
	}

	public void Dispose()
	{
		db?.Dispose();
		db = null;
	}
}
using System;
using System.Text;
using System.Data.SQLite;
var connectionString = "Data Source=db.sqlite;Version=3;";
using (SQLiteConnection connection = new SQLiteConnection(connectionString))
{
    string insertQuery = "INSERT INTO ZeroKelvin (`batch`, `file`, `content`) VALUES (@batch, @file, @content)";
    using (SQLiteCommand insertCommand = new SQLiteCommand(insertQuery, connection))
    {
        insertCommand.Connection.Open();
        insertCommand.Parameters.AddWithValue("@batch", args[0]);
        insertCommand.Parameters.AddWithValue("@file", args[1]);
        var fileHashes = ComputeHashes(args[1]);
        insertCommand.Parameters.AddWithValue("@content", fileHashes);
        var insertResult = insertCommand.ExecuteNonQuery();
        Console.WriteLine($"{insertResult.ToString()} rows inserted");
    }
    Console.WriteLine("Doing useful work / sleeping");
    // System.Threading.Thread.Sleep(26 * 1000);

    string selectQuery = "SELECT batch, file, content FROM ZeroKelvin";
    using (SQLiteCommand selectCommand = new SQLiteCommand(selectQuery, connection))
    {
        var selectResult = selectCommand.ExecuteReader();
        selectResult.Read();
        Console.WriteLine($"{(selectResult.HasRows ? "1" : "0")} rows selected: {selectResult[0]}, {selectResult[1]}, [{selectResult[2]}]");
    }

    var deleteQuery = "DELETE FROM ZeroKelvin WHERE batch=@batch AND file = @file";
    using (SQLiteCommand deleteCommand = new SQLiteCommand(deleteQuery, connection))
    {
        deleteCommand.Parameters.AddWithValue("@batch", args[0]);
        deleteCommand.Parameters.AddWithValue("@file", args[1]);
        var deleteResult = deleteCommand.ExecuteNonQuery();
        Console.WriteLine($"{deleteResult.ToString()} rows deleted");
    }
}

static string ComputeHashes(string fileName)
{
    StringBuilder hashes = new StringBuilder();
    FileStream file = System.IO.File.Open(fileName, FileMode.Open);
    const int BlockSize = 65536 * 4;
    var fileSize = file.Length;
    var blockData = new byte[BlockSize + 100];
    file.Position = BlockSize; // Skipping first block
    for (int i = 1 * BlockSize; i <= fileSize - BlockSize; i += BlockSize)
    {
        file.Read(blockData, 0, BlockSize);
        var hash = System.Security.Cryptography.SHA256.HashData(blockData);
        hashes.AppendLine(Convert.ToHexString(hash)); // I'll ad a salt later
    }
    return hashes.ToString();
}

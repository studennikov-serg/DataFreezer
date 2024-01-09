using System;
using System.Text;
//using System.Data.SQLite;

var rand = new Random();
var fileContent = System.IO.File.ReadAllBytes(args[0]);
var contentLength = fileContent.Length;
//contentLength = 1025;
Console.WriteLine($"content length: {contentLength}");
var log2 = Math.Log2(contentLength);
Console.WriteLine($"log2: {log2}");
var pow = Math.Pow(2, log2);
Console.WriteLine($"pow {pow}");

var truncatedLog2 = (int)(log2);
Console.WriteLine($"truncate log2: {truncatedLog2}");
var maxBlockSize = 1 << truncatedLog2;

var fileHashes = ComputeHashes(maxBlockSize, fileContent, rand);
Console.WriteLine($"[{fileHashes}]");
//System.IO.File.WriteAllText(".", "out.hashes");

static string ComputeHashes(int maxBlockSize, byte[] fileContent, Random rand)
{
    StringBuilder hashes = new StringBuilder();
    MemoryStream stream = new System.IO.MemoryStream();
    for (int i = 0; i < 10; ++i)
    {
        var blockSize = rand.Next(maxBlockSize);
        var blockData = new byte[blockSize];

        var offset = rand.Next(fileContent.Length - blockSize);
        stream.Position = offset;
        stream.Read(blockData, 0, blockSize);
        var hash = System.Security.Cryptography.SHA256.HashData(blockData);
        hashes.
        Append('"').Append(i).Append("\": {")
        .Append("\"offset\": \"").Append(offset).Append("\", ").AppendLine()
        .Append("\"blockSize\": \"").Append(blockSize).Append("\",").AppendLine()
        .Append("\"hash\": \"").Append(Convert.ToHexString(hash)).Append("\" ]").AppendLine();
        //.AppendLine(" }]");
    }
    return hashes.ToString();
}


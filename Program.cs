using System;
using System.Text;
//using System.Data.SQLite;

var rand = new Random();
var fileContent = System.IO.File.ReadAllBytes(args[0]);
var contentLength = fileContent.Length;
Console.WriteLine($"content length: {contentLength}");
var log2 = Math.Log2(contentLength);
Console.WriteLine($"log2: {log2}");

var truncatedLog2 = (int)(log2);
Console.WriteLine($"truncated log2: {truncatedLog2}");
var pow = Math.Pow(2, truncatedLog2);
Console.WriteLine($"pow {pow}");
var maxBlockSize = 1 << truncatedLog2; // less than file size and is a power of 2

var fileHashes = ComputeHashes(maxBlockSize, fileContent, rand);
Console.WriteLine($"[{fileHashes}]");

static string ComputeHashes(int maxBlockSize, byte[] fileContent, Random rand)
{
    var hashes = new StringBuilder();
    hashes.AppendLine("<hashes>");
    MemoryStream stream = new System.IO.MemoryStream();
    for (int i = 0; i < 10; ++i)
    {
        var blockSize = rand.Next(maxBlockSize);
        var blockData = new byte[blockSize];

        var offset = rand.Next(fileContent.Length - blockSize);
        stream.Position = offset;
        stream.Read(blockData, 0, blockSize);
        var hash = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(blockData));
        var dataEntity = $"<hash offset=\"{offset}\", blockSize=\"{blockSize}\">{hash}</hash>";
        hashes.AppendLine(dataEntity);
    }
    hashes.AppendLine("</hashes>");
    return hashes.ToString();
}


using System;
using System.Text;
//using System.Data.SQLite;
/*
using System.Runtime.InteropServices;
[DllImport("./libboinc_api.so.7")]
static extern int boinc_init();
[DllImport("./libboinc_api.so.7")]
static extern int boinc_finish(int status);
[DllImport("libboinc_api.so.7", CharSet = CharSet.Ansi)]
static extern int boinc_send_trickle_up(string variety, string p);
[DllImport("libboinc_api.so.7", CharSet = CharSet.Ansi)]
static extern int boinc_receive_trickle_down(string buf, int len);
*/

try
{
    //boinc_init();
    var rand = new Random();
    var path = (new System.Text.RegularExpressions.Regex("<soft_link>(.+)</soft_link>")).Match(
    System.IO.File.ReadAllText(args[0])
    ).Groups[1].Value;
    Console.Error.WriteLine($"Phisical image path: {path}");
    var fileInfo = new System.IO.FileInfo(path);
    if (fileInfo.LinkTarget != null)
    {
        fileInfo = new System.IO.FileInfo(fileInfo.LinkTarget);
        Console.Error.WriteLine($"Resolved link: {fileInfo.Name}");
    }
    else
    {
        Console.Error.WriteLine($"Not resolving symbolic link: {fileInfo.Name}");
    }
    var contentLength = fileInfo.Length;
    Console.Error.WriteLine($"content length: {contentLength}");
    var log2 = Math.Log2(contentLength);
    Console.Error.WriteLine($"log2: {log2}");

    var truncatedLog2 = (int)(log2);
    Console.Error.WriteLine($"truncated log2: {truncatedLog2}");
    //var pow = Math.Pow(2, truncatedLog2);
    //Console.WriteLine($"pow {pow}");
    var maxBlockSize = 1 << truncatedLog2; // less than file size and is a power of 2
    Console.Error.WriteLine($"Max block size: {maxBlockSize}");
    var fileHashes = ComputeHashes(truncatedLog2, fileInfo, rand);
    Console.WriteLine($"[{fileHashes}]");
}
catch (Exception e)
{
    Console.Error.WriteLine(e.ToString());
    return 1;
    //boinc_finish(1);
}
finally
{
    //boinc_finish(0);
}
return 0;

static string ComputeHashes(int maxBlockSizeLog2, System.IO.FileInfo fileInfo, Random rand)
{
    var hashes = new StringBuilder();
    hashes.AppendLine("<hashes>");
    FileStream stream = fileInfo.OpenRead();
    for (int i = 0; i < 10; ++i)
    {
        var blockSize = 1 << (rand.Next(maxBlockSizeLog2 - 10) + 10 + 1);
        Console.Error.WriteLine($"Random block size: {blockSize}");
        var blockData = new byte[blockSize];

        var offset = rand.Next((int)fileInfo.Length - blockSize);
        stream.Position = offset;
        stream.Read(blockData, 0, blockSize);
        var hash = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(blockData));
        var dataEntity = $"<hash offset=\"{offset}\", blockSize=\"{blockSize}\">{hash}</hash>";
        hashes.AppendLine(dataEntity);
        System.Threading.Thread.Sleep(1000 * 60 * 1);
    }
    hashes.AppendLine("</hashes>");
    return hashes.ToString();
}


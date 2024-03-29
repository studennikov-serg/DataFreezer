using System;
using System.IO;
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
    var path = (new System.Text.RegularExpressions.Regex("<soft_link>(.+)</soft_link>")).Match(
    File.ReadAllText(args[0])
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
    var log2 = IntegerLog2(contentLength) - 1;
    Console.Error.WriteLine($"log2: {log2}");

    //var truncatedLog2 = (int)(log2);
    //Console.Error.WriteLine($"truncated log2: {truncatedLog2}");
    //var pow = Math.Pow(2, truncatedLog2);
    //Console.WriteLine($"pow {pow}");
    var maxBlockSize = 1 << log2; // less than file size and is a power of 2
    Console.Error.WriteLine($"Max block size: {maxBlockSize}");
    var firstRandom = GetFirstRandom(fileInfo); // Bytes from in[put file, from offset 888
    var fileHashes = ComputeHashes(log2, fileInfo, firstRandom);
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

static string ComputeHashes(int maxBlockSizeLog2, FileInfo fileInfo, (Int64 offset, byte blockSize, Random xoRand) rand)
{
    var hashes = new StringBuilder();
    hashes.AppendLine("<hashes>");
    using (FileStream stream = fileInfo.OpenRead())
    {
        for (int i = 0; i < 1000; ++i)
        {
            var randBlockSizeLog2 = rand.blockSize % (maxBlockSizeLog2 - 10 + 1); // 0 <= randBlockSizeLog2 < maxBlockSizeLog2
            var blockSize = 1 << (randBlockSizeLog2 + 9 + 1); // min block size is 1024 bytes max is max block size
            var offset = rand.offset % (fileInfo.Length - blockSize + 1);
            var blockData = new byte[blockSize];

            stream.Position = offset;
            stream.Read(blockData, 0, blockSize);
            var hashBytes = System.Security.Cryptography.SHA256.HashData(blockData);
            rand = (
            offset: Math.Abs(BitConverter.ToInt64(hashBytes, 0)) ^ rand.xoRand.NextInt64(),
            blockSize: (byte)(hashBytes[8] ^ rand.xoRand.Next()),
            xoRand: rand.xoRand
            );
            // new rand from current block hash
            var hash = Convert.ToBase64String(hashBytes, Base64FormattingOptions.InsertLineBreaks);
            var dataEntity = $"<hash offset=\"{offset}\", blockSize=\"{blockSize}\">{hash}</hash>";
            hashes.AppendLine(dataEntity);
            System.Threading.Thread.Sleep(1000);
        }
    } // using fileStream
    hashes.AppendLine("</hashes>");
    return hashes.ToString();
}

static (Int64 offset, byte blockSize, Random xoRand) GetFirstRandom(FileInfo source)
{
    using (var fileStream = source.Open(FileMode.Open, FileAccess.Read))
    {
        fileStream.Position = 888;
        using (var reader = new BinaryReader(fileStream))
        {
            return (offset: Math.Abs(reader.ReadInt64()), blockSize: reader.ReadByte(), xoRand: new Random(reader.ReadInt32()));
        }
    }
}


static int IntegerLog2(long Value)
{
return
sizeof(long) * 8 // bytes count * 8 = bits count
- System.Numerics.BitOperations.LeadingZeroCount((ulong)Value // - leading zero bits = log2
);
}


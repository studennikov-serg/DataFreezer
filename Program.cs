using System;
using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
//using System.Data.SQLite;
using System.Runtime.InteropServices;
/*
[DllImport("/home/serg/DataFreezer/libboinc_api.so.8.1.0")]
static extern int boinc_init();
[DllImport("./libboinc_api.so.8.1.0")]
static extern int boinc_finish(int status);
[DllImport("libboinc_api.so.8.1.0", CharSet = CharSet.Ansi)]
static extern int boinc_send_trickle_up(string variety, string p);
[DllImport("libboinc_api.so.8.1.0", CharSet = CharSet.Ansi)]
static extern int boinc_receive_trickle_down(string buf, int len);
*/

try
{
    //boinc_init();
    var path = (new Regex("<soft_link>(.+)</soft_link>")).Match(
    File.ReadAllText(args[0])
    ).Groups[1].Value;
    var computeSettings = ExtractParameters(args);

    var fileInfo = new System.IO.FileInfo(path);
    var contentLength = fileInfo.Length;
    Console.Error.WriteLine($"content length: {contentLength}");
    var log2 = IntegerLog2(contentLength) - 1; // edge value - if file length is pow of 2, log2 is file length / 2
    Console.Error.WriteLine($"log2: {log2}");

    //var truncatedLog2 = (int)(log2);
    //Console.Error.WriteLine($"truncated log2: {truncatedLog2}");
    //var pow = Math.Pow(2, truncatedLog2);
    //    Console.Error.WriteLine($"pow {pow}");
    var maxBlockSize = 1 << log2; // less than file length and is power of 2
    Console.Error.WriteLine($"Max block size: {maxBlockSize}");
    var firstRandom = GetFirstRandom(fileInfo); // Bytes from in[put file, from offset 888
    var time = new Stopwatch();
    time.Start();
    var fileHashes = ComputeHashes(log2, fileInfo, firstRandom, computeSettings);
    time.Stop();
    Console.Error.WriteLine($"Hashes found: {fileHashes.count}, time elapsed: {time.Elapsed}");
    Console.Error.WriteLine(fileHashes.content);
    Console.Write(fileHashes.content);
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

static (int count, string content) ComputeHashes(
int MaxBlockSizeLog2, FileInfo FileInfo,
(Int64 offset, byte blockSize, Random xoRand) Rand,
(int compareHashPrefixLength, int testHashesCount, int intersectionSearchCount) Settings)
{
    HashSet<UInt64> testHashes = new(Settings.testHashesCount);
    Random testRand = new();
    Int64 mask = 0xffL << (sizeof(Int64) - 1) * 8; // high byte = 0xff
                                                   // shifts back to same amount bytes - prefix length; arithmetic shift extends sign bit
    mask >>= (sizeof(Int64) - 1 - Settings.compareHashPrefixLength) * 8;
    // now mask contains all high bits = 1,, prefix bits = 0
    mask = ~mask; // negotiate and get prefix mask bits = 1, high bits = 0
    UInt64 prefixMask = (UInt64)mask;
    Console.WriteLine(prefixMask.ToString("x"));
    for (int i = 0; i < Settings.testHashesCount;)
    {
        var key = (UInt64)testRand.NextInt64() & prefixMask;
        if (!testHashes.Contains(key))
        {
            testHashes.Add(key);
            ++i;
        }
    }
    var hashes = new StringBuilder();
    hashes.AppendLine("<hashes>");
    int hashesFount = 0;
    using (FileStream stream = FileInfo.OpenRead())
    {
        for (int i = 0; i < Settings.intersectionSearchCount; ++i)
        {
            var randBlockSizeLog2 = Rand.blockSize % (MaxBlockSizeLog2 - 10 + 1); // 0 <= randBlockSizeLog2 < maxBlockSizeLog2
            var blockSize = 1 << (randBlockSizeLog2 + 9 + 1); // min block size is 1024 bytes max is max block size
            var offset = Rand.offset % (FileInfo.Length - blockSize + 1);
            var blockData = new byte[blockSize];

            stream.Position = offset;
            stream.Read(blockData, 0, blockSize);
            var hashBytes = SHA256.HashData(blockData);
            Rand = (
            offset: Math.Abs(BitConverter.ToInt64(hashBytes, 0)) ^ Rand.xoRand.NextInt64(),
            blockSize: (byte)(hashBytes[8] ^ Rand.xoRand.Next()),
            xoRand: Rand.xoRand
            ); // new rand from current block hash
            UInt64 hashPart = BitConverter.ToUInt64(hashBytes, 0); // part is enough for tests
            UInt64 hashPrefix = hashPart & prefixMask;
            if (testHashes.Contains(hashPrefix))
            {
                var hashString = Convert.ToBase64String(hashBytes);
                var dataEntity = $"<hash offset=\"{offset}\", blockSize=\"{blockSize}\">{hashString}</hash>";
                hashes.AppendLine(dataEntity);
                ++hashesFount;
            }
        }
    } // using fileStream
    hashes.AppendLine("</hashes>");
    return (count: hashesFount, content: hashes.ToString());
}

static (Int64 offset, byte blockSize, Random xoRand) GetFirstRandom(FileInfo Source)
{
    using (var fileStream = Source.Open(FileMode.Open, FileAccess.Read))
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

static (int compareHashPrefixLength, int testHashesCount, int intersectionSearchCount) ExtractParameters(string[] Args)
{
    var extractedInfo = new Regex(@"--CompareHashPrefixLength (\d+) --TestHashesCount (\d+) --IntersectionSearchCount (\d+)").Match(
    string.Join(" ", Args, 1, Args.Length - 1)
        ).Groups;
    return (
    compareHashPrefixLength: int.Parse(extractedInfo[1].Value),
    testHashesCount: int.Parse(extractedInfo[2].Value),
    intersectionSearchCount: int.Parse(extractedInfo[3].Value)
    );
}


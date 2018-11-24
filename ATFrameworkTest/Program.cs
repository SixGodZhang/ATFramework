using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Security.Cryptography;

class Program
{
    static void Main(string[] args)
    {
        string path = @"C:\Users\Administrator\Desktop\T.txt";

        Console.WriteLine("way1: ");
        Stopwatch stopwatch = new Stopwatch();
        stopwatch.Start();
        MD5CryptoServiceProvider provider = new MD5CryptoServiceProvider();
        FileStream fs = new FileStream(path, FileMode.Open);

        
        byte[] md5Arr1 = provider.ComputeHash(fs);
        fs.Close();
        foreach (var item in md5Arr1)
        {
            Console.Write(item.ToString("x2"));
        }
        Console.WriteLine();
        stopwatch.Stop();
        Console.WriteLine(stopwatch.ElapsedMilliseconds);

        //Console.WriteLine("way2: ");
        //stopwatch.Restart();
        //MD5File mD5File = new MD5File(path);
        //foreach (var item in mD5File.Checksums)
        //{
        //    Console.Write(item.ToString("x2"));
        //}
        //Console.WriteLine();
        //stopwatch.Stop();
        //Console.WriteLine(stopwatch.ElapsedMilliseconds);
    }
}

public class MD5File : IDisposable
{
    private const int SECTION_SIZE = 8192;//8M

    public string FilePath { get;}
    public byte[] Checksums { get;}

    private long fileLength;
    private MemoryMappedFile memoryMappedFile;

    public MD5File(string filePath)
    {
        if (!File.Exists(filePath))
            throw new FileNotFoundException($"{filePath} was not found!");

        this.FilePath = filePath;
        this.fileLength = new FileInfo(filePath).Length;
        this.memoryMappedFile = MemoryMappedFile.CreateFromFile(filePath, FileMode.Open);

        this.Checksums = this.GenerateChecksums();
    }

    public void Dispose()
    {
        this.memoryMappedFile.Dispose();
    }

    private byte[] Read(long offset, int size)
    {
        using (MemoryMappedViewStream memoryMappedViewStream = this.memoryMappedFile.CreateViewStream(offset, size, MemoryMappedFileAccess.Read))
        using (BinaryReader binaryReader = new BinaryReader(memoryMappedViewStream))
            return binaryReader.ReadBytes(size);
    }

    private byte[] GenerateChecksums()
    {
        List<byte[]> checksums = new List<byte[]>();

        double sectionsCalc = this.fileLength / (double)SECTION_SIZE;

        long normalSizedSectionCount = (long)Math.Floor(sectionsCalc);
        int lastSectionSize = (int)((sectionsCalc - normalSizedSectionCount) * (double)SECTION_SIZE);

        using (MD5 hashProvider = MD5.Create())
        {
            for (long i = 0; i < normalSizedSectionCount; i++)
                checksums.Add(hashProvider.ComputeHash(this.Read(i * SECTION_SIZE, SECTION_SIZE)));

            checksums.Add(hashProvider.ComputeHash(this.Read(normalSizedSectionCount * SECTION_SIZE, lastSectionSize)));
        }

        int byteArrLength = 0;
        foreach (var item in checksums)
        {
            byteArrLength += item.Length;
        }

        byte[] md5 = new byte[byteArrLength];
        int currentIndex = 0;
        foreach (var item in checksums)
        {
            item.CopyTo(md5, currentIndex);
            currentIndex += item.Length;
        }

        return md5;
    }

}
﻿// Decompiled with JetBrains decompiler
// Type: UpuCore.KISSUnpacker
// Assembly: UpuGui, Version=1.0.2.0, Culture=neutral, PublicKeyToken=null
// MVID: DD1D21B2-102B-4937-9736-F13C7AB91F14
// Assembly location: C:\Users\veyvin\Desktop\UpuGui.exe

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using tar_cs;

namespace UpuCore
{
    public class KISSUnpacker
    {
        private string GetDefaultOutputPathName(string inputFilepath, string outputPath = null)
        {
            var fileInfo = new FileInfo(inputFilepath);
            var flag = false;
            if (outputPath == null)
            {
                outputPath = Path.Combine(fileInfo.Directory.FullName, fileInfo.Name + "_unpacked");
                flag = true;
            }
            if (Directory.Exists(outputPath) && flag)
            {
                var num = 2;
                string path;
                while (true)
                {
                    path = Path.Combine(fileInfo.Directory.FullName,
                        string.Concat(outputPath, (object) " (", (object) num, (object) ")"));
                    if (Directory.Exists(path))
                        ++num;
                    else
                        break;
                }
                Directory.CreateDirectory(path);
                outputPath = path;
            }
            return outputPath;
        }

        public string GetTempPath()
        {
            return Path.Combine(Path.Combine(Path.GetTempPath(), "Upu"), Path.GetRandomFileName());
        }

        public Dictionary<string, string> Unpack(string inputFilepath, string outputPath)
        {
            Console.WriteLine("Extracting " + inputFilepath + " to " + outputPath);
            var fileInfo = new FileInfo(inputFilepath);
            if (!File.Exists(inputFilepath))
            {
                inputFilepath = Path.Combine(Environment.CurrentDirectory, inputFilepath);
                if (!File.Exists(inputFilepath))
                    throw new FileNotFoundException(inputFilepath);
            }
            if (!inputFilepath.ToLower().EndsWith(".unitypackage"))
                throw new ArgumentException("File should have unitypackage extension");
            outputPath = GetDefaultOutputPathName(inputFilepath, outputPath);
            if (!Directory.Exists(outputPath))
                Directory.CreateDirectory(outputPath);
            var tempPath = GetTempPath();
            var str1 = Path.Combine(tempPath, "_UPU_TAR");
            var tarFileName = DecompressGZip(new FileInfo(inputFilepath), str1);
            var str2 = Path.Combine(tempPath, "content");
            ExtractTar(tarFileName, str2);
            Directory.Delete(str1, true);
            return GenerateRemapInfo(str2, outputPath);
        }

        private void RemoveTempFiles(string tempPath)
        {
            var directoryInfo1 = new DirectoryInfo(tempPath);
            foreach (FileSystemInfo fileSystemInfo in directoryInfo1.GetFiles())
                fileSystemInfo.Delete();
            foreach (var directoryInfo2 in directoryInfo1.GetDirectories())
                directoryInfo2.Delete(true);
        }

        private Dictionary<string, string> GenerateRemapInfo(string extractedContentPath, string remapPath)
        {
            var dictionary = new Dictionary<string, string>();
            foreach (var directoryInfo in new DirectoryInfo(extractedContentPath).GetDirectories())
            {
                var path2 = File.ReadAllLines(Path.Combine(directoryInfo.FullName, "pathname"))[0].Replace('/',
                    Path.DirectorySeparatorChar);
                var key = Path.Combine(directoryInfo.FullName, "asset");
                var fileName = Path.Combine(remapPath, path2);
                var fullName = new FileInfo(fileName).Directory.FullName;
                dictionary.Add(key, fileName);
            }
            return dictionary;
        }

        public void RemapFiles(Dictionary<string, string> map)
        {
            foreach (var keyValuePair in map)
            {
                var str = keyValuePair.Value;
                var key = keyValuePair.Key;
                var fileInfo = new FileInfo(keyValuePair.Value);
                if (!Directory.Exists(fileInfo.DirectoryName))
                {
                    Console.WriteLine("Creating directory " + str + "...");
                    Directory.CreateDirectory(fileInfo.DirectoryName);
                }
                if (File.Exists(key))
                {
                    Console.WriteLine("Extracting file " + str + "...");
                    if (File.Exists(str))
                        File.Delete(str);
                    File.Move(key, str);
                }
            }
        }

        private string DecompressGZip(FileInfo fileToDecompress, string outputPath)
        {
            using (var fileStream1 = fileToDecompress.OpenRead())
            {
                var path2 = fileToDecompress.Name;
                if (fileToDecompress.Extension.Length > 0)
                    path2 = path2.Remove(path2.Length - fileToDecompress.Extension.Length);
                if (!Directory.Exists(outputPath))
                    Directory.CreateDirectory(outputPath);
                var path = Path.Combine(outputPath, path2);
                using (var fileStream2 = File.Create(path))
                {
                    using (var gzipStream = new GZipStream(fileStream1, CompressionMode.Decompress))
                    {
                        CopyStreamDotNet20(gzipStream, fileStream2);
                        Console.WriteLine("Decompressed: {0}", fileToDecompress.Name);
                    }
                }
                return path;
            }
        }

        private void CopyStreamDotNet20(Stream input, Stream output)
        {
            var buffer = new byte[32768];
            int count;
            while ((count = input.Read(buffer, 0, buffer.Length)) > 0)
                output.Write(buffer, 0, count);
        }

        public bool ExtractTar(string tarFileName, string destFolder)
        {
            Console.WriteLine("Extracting " + tarFileName + " to " + destFolder + "...");
            var currentDirectory = Directory.GetCurrentDirectory();
            using (Stream tarredData = File.OpenRead(tarFileName))
            {
                Directory.CreateDirectory(destFolder);
                Directory.SetCurrentDirectory(destFolder);
                new TarReader(tarredData).ReadToEnd(".");
            }
            Directory.SetCurrentDirectory(currentDirectory);
            return true;
        }
    }
}
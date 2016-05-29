using CommandLineParser.Arguments;
using CommandLineParser.Exceptions;
using Ionic.Zip;
using Ionic.Zlib;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace FolderZipper
{
    internal class Options
    {
        private const string CompressionLevels = "Level0;None;BestSpeed;Level1;Level2;Level3;Level4;Level5;Default;Level6;Level7;Level8;BestCompression;Level9";

        [ValueArgument(typeof(string), 's', "sourceFolder", Description = "Source Folder", Optional = false)]
        public string SourceFolder;

        [ValueArgument(typeof(string), 'd', "destinationFolder", Description = "Destination Folder", Optional = false)]
        public string DestinationFolder;

        [SwitchArgument('t', "textFile", true, Description = "Create a textfile with all the files in a folder.", Optional = true)]
        public bool CreateTextFile;

        [SwitchArgument('u', "summaryTextFile", true, Description = "Create a summary textfile with all the files.", Optional = true)]
        public bool CreateSummaryTextFile;

        [SwitchArgument('p', "showPogress", true, Description = "Show progress in %.", Optional = true)]
        public bool ShowProgress;

        [SwitchArgument('o', "overwrite", false, Description = "Overwrite destination files", Optional = true)]
        public bool OverwriteFiles;

        [EnumeratedValueArgument(typeof(string), 'c', "compression", Description = "CompressionLevel (" + CompressionLevels + ")", AllowedValues = CompressionLevels, Optional = true, DefaultValue = "Default")]
        public string Compression;

        private CompressionLevel? compressionLevel;

        public CompressionLevel CompressionLevel
        {
            get
            {
                if (compressionLevel.HasValue)
                {
                    return compressionLevel.Value;
                }

                compressionLevel = (CompressionLevel)Enum.Parse(typeof(CompressionLevel), Compression);
                return compressionLevel.Value;
            }
        }
    }

    internal class Program
    {
        private const string FileFormat = "{0}\t{1,16:n0}\t{2}";
        private const string TotalFormat = "Total {0} file(s)\t{1,16:n0} bytes";

        private static readonly Options Options = new Options();
        private static long AllBytes;
        private static readonly List<string> AllFiles = new List<string>();
        private static List<string> AllFilesToCrawl;
        private static readonly ProgressBar ProgressBar = new ProgressBar();

        private static void Main(string[] args)
        {
            var parser = new CommandLineParser.CommandLineParser();
            parser.ExtractArgumentAttributes(Options);

            try
            {
                parser.ParseCommandLine(args);

                if (Options.SourceFolder == Options.DestinationFolder)
                {
                    Console.WriteLine("Source folder and Destination folder must be different");
                    return;
                }

                Console.WriteLine("Source folder:      {0}", Options.SourceFolder);
                Console.WriteLine("Destination folder: {0}", Options.DestinationFolder);
                Console.WriteLine("Compression Level:  {0}", Options.CompressionLevel);

                if (Options.OverwriteFiles)
                {
                    Console.WriteLine("Overwrite files.");
                }

                if (Options.CreateTextFile)
                {
                    Console.WriteLine("Creating a text file with file details in every folder.");
                }

                if (Options.CreateSummaryTextFile)
                {
                    Console.WriteLine("Creating a summary text file with all file details.");
                }

                var stopwatch = new Stopwatch();
                stopwatch.Start();

                AllFilesToCrawl = GetAllFiles(Options.SourceFolder).ToList();

                if (Options.ShowProgress)
                {
                    ProgressBar.Draw(0);
                }

                CrawlDir(Options.SourceFolder, Options.DestinationFolder);

                if (Options.ShowProgress)
                {
                    ProgressBar.Draw(100);
                }

                if (Options.CreateSummaryTextFile)
                {
                    string summaryFileName = Path.Combine(Options.DestinationFolder, "FolderZipper.Summary.txt");
                    bool doSummaryFile = true;
                    if (File.Exists(summaryFileName))
                    {
                        if (Options.OverwriteFiles)
                        {
                            DeleteFile(summaryFileName);
                        }
                        else
                        {
                            doSummaryFile = false;
                        }
                    }

                    if (doSummaryFile)
                    {
                        using (var sr = File.CreateText(summaryFileName))
                        {
                            foreach (var file in AllFiles.OrderBy(f => f))
                            {
                                var fi = new FileInfo(file);

                                sr.WriteLine(FileFormat, fi.CreationTime, fi.Length, fi.FullName);
                                AllBytes += fi.Length;
                            }

                            sr.WriteLine(TotalFormat, AllFiles.Count(), AllBytes);
                        }
                    }
                }

                stopwatch.Stop();
                Console.WriteLine();
                Console.WriteLine("Total time taken : {0}", stopwatch.Elapsed);
            }
            catch (CommandLineException e)
            {
                Console.WriteLine(e.Message);
                parser.ShowUsage();
            }
        }

        private static IEnumerable<string> GetAllFiles(string path, Func<FileInfo, bool> checkFile = null)
        {
            string[] files = Directory.GetFiles(path, "*.*", SearchOption.AllDirectories);

            return files.Where(file => checkFile == null || checkFile(new FileInfo(file)));
        }

        private static void CrawlDir(string sourceFolder, string destFolder)
        {
            try
            {
                string folderName = Path.GetFileName(sourceFolder);
                var files = Directory.GetFiles(sourceFolder).OrderBy(f => f);

                if (files.Any())
                {
                    if (!Directory.Exists(destFolder))
                    {
                        Directory.CreateDirectory(destFolder);
                    }

                    string zipFileName = Path.Combine(destFolder, folderName + ".zip");
                    bool doZipFile = true;
                    if (File.Exists(zipFileName))
                    {
                        if (Options.OverwriteFiles)
                        {
                            DeleteFile(zipFileName);
                        }
                        else
                        {
                            doZipFile = false;
                        }
                    }

                    if (doZipFile)
                    {
                        using (var zip = new ZipFile(zipFileName))
                        {
                            zip.CompressionLevel = Options.CompressionLevel;

                            zip.AddFiles(files, "");
                            zip.Save();
                        }
                    }

                    string txtFileName = Path.Combine(destFolder, folderName + ".txt");
                    if (Options.CreateTextFile)
                    {
                        bool doTxtFile = true;
                        if (File.Exists(txtFileName))
                        {
                            if (Options.OverwriteFiles)
                            {
                                DeleteFile(txtFileName);
                            }
                            else
                            {
                                doTxtFile = false;
                            }
                        }

                        if (doTxtFile)
                        {
                            using (var sr = File.CreateText(txtFileName))
                            {
                                long bytes = 0;
                                foreach (var file in files)
                                {
                                    var fi = new FileInfo(file);

                                    sr.WriteLine(FileFormat, fi.CreationTime, fi.Length, fi.Name);
                                    bytes += fi.Length;
                                }

                                sr.WriteLine(TotalFormat, files.Count(), bytes);
                            }
                        }
                    }

                    if (Options.CreateSummaryTextFile)
                    {
                        AllFiles.AddRange(files);
                    }

                    if (Options.ShowProgress)
                    {
                        int progress = (int)(AllFiles.Count * 100.0d / AllFilesToCrawl.Count);
                        if (progress % 2 == 0)
                        {
                            ProgressBar.Draw(progress);
                        }
                    }
                }

                foreach (string subFolder in Directory.GetDirectories(sourceFolder).OrderBy(d => d))
                {
                    CrawlDir(subFolder, GetPath(subFolder, destFolder));
                }
            }
            catch (UnauthorizedAccessException ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        private static bool DeleteFile(string path)
        {
            bool exists = File.Exists(path);

            if (exists)
            {
                File.Delete(path);
            }

            return exists;
        }

        private static string GetPath(string sourceFolder, string destFolder)
        {
            string folderName = Path.GetFileName(sourceFolder);
            return Path.Combine(destFolder, folderName);
        }
    }
}
using EverythingNet.Core;
using EverythingNet.Interfaces;
using EverythingNet.Query;
using Newtonsoft.Json;
using SevenZip;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Timers;

namespace AdvancedRobloxArchival
{
    public struct BinaryArchive
    {
        public enum BinaryTypes
        {
            Miscellaneous,
            RobloxClient,
            RobloxStudio,
            RCCService
        }
        public BinaryArchive(bool genuine)
        {
            Genuine = genuine;
            Version = "";
            BinaryType = BinaryTypes.Miscellaneous;
            Path = "";
            FromCache = false;
        }

        public void Populate(string version, BinaryTypes binarytype, string path, bool fromcache)
        {
            Version = version;
            BinaryType = binarytype;
            Path = path;
            FromCache = fromcache;
        }

        public bool Genuine { get; }
        public string Path { get; set; }
        public string Version { get; set; }
        public BinaryTypes BinaryType { get; set; }
        public bool FromCache { get; set; }
    }

    internal class Program
    {
        public static readonly int version = typeof(Program).Assembly.GetName().Version.Major;
        public static readonly string cache = Path.Combine(System.IO.Path.GetTempPath(), "ArchiveCache");
        public static string archivePath;
        public static List<string> CheckedFiles = new List<string>();
        public static BackgroundWorker worker = new BackgroundWorker();
        public static int ArchivedCount = 0;

        private enum Modes : int
        {
            Unspecified,
            ScanAllDrives,
            ScanSpecificDirectories
        }

        static void Main(string[] args)
        {
            Console.Title = "Advanced Roblox Archival | Made by Yakov :D";
            Start();
        }

        static void Start(Modes Mode = Modes.Unspecified)
        {
            ConsoleGlobal.Singleton.WriteContent($"Revision {version.ToString()}", ConsoleColor.DarkGray);
            ConsoleGlobal.Singleton.WriteContent(@"    _      _                         _   ___  ___  ___ _    _____  __              _    _          _ 
   /_\  __| |_ ____ _ _ _  __ ___ __| | | _ \/ _ \| _ ) |  / _ \ \/ /  __ _ _ _ __| |_ (_)_ ____ _| |
  / _ \/ _` \ V / _` | ' \/ _/ -_) _` | |   / (_) | _ \ |_| (_) >  <  / _` | '_/ _| ' \| \ V / _` | |
 /_/ \_\__,_|\_/\__,_|_||_\__\___\__,_| |_|_\\___/|___/____\___/_/\_\ \__,_|_| \__|_||_|_|\_/\__,_|_|
 by yakov :D
", ConsoleColor.Cyan);
            if (Directory.Exists(cache)) new DirectoryInfo(cache).Delete(true);
            Directory.CreateDirectory(cache);
            if (Mode == Modes.Unspecified)
            {
                ConsoleGlobal.Singleton.WriteContent(@" features:
  - it searches whole pc using voidtools sdk (it sorts through exe, zip, 7z, and rar)
  - it verifies the digital signature so it doesn't include patched/tampered binaries
  - it checks for duplicates and doesn't scan a file or a zip more than once (if need to be relaunched)
  - has error handling and dumps logs when if it breaks (hopefully it doesn't)
  - it organizes files by the version and categorizes it to the appropriate binary categories (Client, Studio, RCC)
", ConsoleColor.DarkGray);
                ConsoleGlobal.Singleton.WriteContent(" [*] What would you like to do?\n", ConsoleColor.Cyan);

                string[] Options = { "Scan all drives (recommended)", "Scan a specific directory" };

                int Choice = ConsoleGlobal.Singleton.WriteChoiceMenu(Options, ConsoleColor.Yellow, ConsoleColor.White);
                Console.Clear();
                Start((Modes)Choice);
                return;
            }

            string targetDir = string.Empty;
            if (Mode == Modes.ScanSpecificDirectories)
            {
                ConsoleGlobal.Singleton.WriteContentNoLine("[*] Enter the path of which you would like to search in: ", ConsoleColor.Yellow);
                targetDir = Console.ReadLine().Trim('"');
            }

            ConsoleGlobal.Singleton.WriteContentNoLine("[*] Enter the full directory path where archives will be kept: ", ConsoleColor.Yellow);
            archivePath = Console.ReadLine().Trim('"');
            if (!Directory.Exists(archivePath)) Directory.CreateDirectory(archivePath);
            foreach (string i in Enum.GetNames(typeof(BinaryArchive.BinaryTypes)))
            {
                string CategoryPath = Path.Combine(archivePath, i);
                if (!Directory.Exists(CategoryPath)) Directory.CreateDirectory(CategoryPath);
            }

            ConsoleGlobal.Singleton.WriteContent("[*] Starting VoidTools helper", ConsoleColor.Yellow);
            EverythingState.StartService(true, EverythingState.StartMode.Service);
            ConsoleGlobal.Singleton.WriteContent("[*] Waiting for index to finish", ConsoleColor.Yellow);
            while (!EverythingState.IsReady()) Thread.Sleep(1000);
            double attempt = 0;
            var everything = new Everything();
            var query = everything.Search(new Query().Files.Name.Extension("zip")
                                              .Or.Name.Extension("7z")
                                              .Or.Name.Extension("rar")
                                              .Or.Name.Extension("exe")
                                              .And.Size.GreaterThan(3, EverythingNet.Query.SizeUnit.Mb)
                                              .And.Size.LessThan(2, EverythingNet.Query.SizeUnit.Gb));
            if (Mode == Modes.ScanSpecificDirectories)
            {
                ConsoleGlobal.Singleton.WriteContent("[*] Specific directory searching is not available yet! Check back later.", ConsoleColor.Red);
                Console.ReadLine();
                return;
            }
            int totalattempts = query.Count();
            if (File.Exists("CheckedFiles.json")) CheckedFiles = JsonConvert.DeserializeObject<List<string>>(File.ReadAllText("CheckedFiles.json"));
            worker.DoWork += SaveConfigOccasionally;
            System.Timers.Timer timer = new System.Timers.Timer(60000);
            timer.Elapsed += timer_Elapsed;
            timer.Start();
            DateTime startTime = DateTime.Now;
            foreach (var item in query)
            {
                ConsoleGlobal.Singleton.ClearCurrentConsoleLine();
                string out1 = $"[*] Sorting through archives |(|{attempt}|/|{totalattempts}| attempts|)";
                string out2 = $"Archived: |(|{ArchivedCount}|)";
                string out3 = $"{attempt / totalattempts * 100:0}%| Complete!";

                ConsoleGlobal.Singleton.WriteColoredOutput(out1, ConsoleColor.Yellow, ConsoleColor.White, ConsoleColor.Cyan, ConsoleColor.DarkGray, ConsoleColor.Cyan, ConsoleColor.Yellow, ConsoleColor.White);
                ConsoleGlobal.Singleton.WriteRedSeparator();
                ConsoleGlobal.Singleton.WriteColoredOutput(out2, ConsoleColor.Yellow, ConsoleColor.White, ConsoleColor.Cyan, ConsoleColor.White);
                ConsoleGlobal.Singleton.WriteRedSeparator();
                ConsoleGlobal.Singleton.WriteColoredOutput(out3, ConsoleColor.Cyan, ConsoleColor.Yellow);

                attempt++;
                // TODO: better way to do this via Everything rules
                // Potential solution: acquire and modify EverythingNet to solution and add more versatile rules
                if (item.FullPath.Substring(1).StartsWith(":\\$Recycle.Bin") || item.FullPath.Substring(1).StartsWith(":\\Windows")) continue;
                if (CheckedFiles.Contains(item.FullPath)) continue; // intentionally skipped already checked zips.
                CheckedFiles.Add(item.FullPath);
                if (item.FullPath.EndsWith(".exe"))
                {
                    BinaryArchive binaryArchive = CheckFileAuthenticity(item.FullPath, false);
                    if (binaryArchive.Genuine) ArchiveFile(binaryArchive);
                }
                else
                    try
                    {
                        using (SevenZipExtractor archive = new SevenZipExtractor(item.FullPath))
                        {
                            if (!archive.Check()) continue;
                            List<string> filenames = new List<string>();
                            int traverseAttempts = 0;
                            bool unlocked = false;
                            foreach (var entry in archive.ArchiveFileData)
                            {
                                if (!unlocked && traverseAttempts >= 20) break;
                                if (entry.IsDirectory)
                                {
                                    traverseAttempts++;
                                    continue;
                                }
                                string filename = Path.GetFileName(entry.FileName);
                                if (PropertyMatching.ConsiderBinaryCandidate(filename))
                                {
                                    filenames.Add(entry.FileName);
                                    unlocked = true;
                                }
                            }
                            if (filenames.Any()) archive.ExtractFiles(cache, filenames.ToArray());
                            var archives = Directory.EnumerateFiles(cache, "*.*", SearchOption.AllDirectories);
                            foreach (string filename in archives)
                            {
                                BinaryArchive binaryArchive = CheckFileAuthenticity(filename, true);
                                if (binaryArchive.Genuine) ArchiveFile(binaryArchive);
                            }
                        }
                    }
                    catch (UnauthorizedAccessException) { continue; }
                    catch (IOException) { continue; }
                    catch (ArgumentException) { continue; }
                    catch (Exception ex)
                    {
                        worker.RunWorkerAsync();
                        if (Debugger.IsAttached)
                        {
                            ExceptionDispatchInfo.Capture(ex).Throw();
                            throw;
                        }
                        else Console.WriteLine("An unknown error has occurred: " + ex.ToString() + "\nAlready scanned archives will not be scanned again.");

                        Console.ReadLine();
                        return;
                    }
            }


            timer.Stop();
            TimeSpan totalTimeTaken = DateTime.Now.Subtract(startTime);
            worker.RunWorkerAsync();
            ConsoleGlobal.Singleton.ClearCurrentConsoleLine();

            string succ1 = "[*] |Archive Completed!!!";
            string succ2 = $"Archived |{ArchivedCount}| files in |{(int)totalTimeTaken.TotalMinutes}| minutes!!";

            ConsoleGlobal.Singleton.WriteColoredOutput(succ1, ConsoleColor.Yellow, ConsoleColor.Green);
            ConsoleGlobal.Singleton.WriteRedSeparator();
            ConsoleGlobal.Singleton.WriteColoredOutput(succ2, ConsoleColor.Yellow, ConsoleColor.Cyan, ConsoleColor.Yellow, ConsoleColor.Cyan, ConsoleColor.Yellow);
            TaskbarFlash.FlashWindowEx();
            System.Media.SystemSounds.Beep.Play();
            Console.ReadLine();
        }

        static BinaryArchive CheckFileAuthenticity(string path, bool FromCache = false)
        {
            if (File.Exists(path))
            {
                FileVersionInfo info = FileVersionInfo.GetVersionInfo(path) ?? null;
                if (PropertyMatching.IsROBLOX(info?.FileDescription))
                {
                    bool isTrusted = AuthenticodeTools.IsTrusted(path);
                    if (isTrusted)
                    {
                        BinaryArchive.BinaryTypes binType = PropertyMatching.GetBinaryTypeFromSignature(info.FileDescription);

                        BinaryArchive archive = new BinaryArchive(true);
                        archive.Populate(info.FileVersion.Replace(", ", "."), binType, path, FromCache);
                        return archive;
                    }
                }
                if (FromCache) File.Delete(path);
            }
            return new BinaryArchive(false);
        }

        static void ArchiveFile(BinaryArchive archive)
        {
            string destination = Path.Combine(archivePath, archive.BinaryType.ToString(), archive.Version + ".exe");
            if (File.Exists(destination))
            {
                if (archive.FromCache)
                    File.Delete(archive.Path);
            }
            else
            {
                ArchivedCount++;
                if (archive.FromCache)
                    File.Move(archive.Path, destination);
                else
                    File.Copy(archive.Path, destination);

                DateTime fileTimeStamp = new PeHeaderReader(destination).TimeStamp;
                if (fileTimeStamp < DateTime.UtcNow && fileTimeStamp > new DateTime(2005, 1, 1)) // Ensure they're not a super old date or a future date
                    File.SetLastWriteTimeUtc(destination, fileTimeStamp);
            }
        }

        static void timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            if (!worker.IsBusy)
                worker.RunWorkerAsync();
        }

        static void SaveConfigOccasionally(object sender, DoWorkEventArgs e)
        {
            File.WriteAllText("CheckedFiles.json", JsonConvert.SerializeObject(CheckedFiles));
        }
    }
}

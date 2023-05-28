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
    internal class Program
    {
        public static readonly Version version = typeof(Program).Assembly.GetName().Version;
        public static readonly string CachePath = Path.Combine(Path.GetTempPath(), "ArchiveCache");
        public static string ArchivePath;
        private static List<string> CheckedFiles = new List<string>();
        private static BackgroundWorker worker = new BackgroundWorker();
        public static Modes CurrentMode = Modes.Unspecified;
        public static int ArchivedCount = 0;
        public static int UploadArchivedCount = 0;
        public static int UploadQueue = 0;
        public static bool UseArchiveServer;


        public static string versionString
        {
            get
            {
                // Construct the string with the major and minor version (if applicable)
                string versionString = version.Major.ToString();
                if (version.Minor > 0)
                {
                    versionString += $".{version.Minor}";
                }

                return versionString;
            }
        }

        public enum Modes : int
        {
            Unspecified,
            ScanAllDrives,
            ScanSpecificDirectories
        }

        static void Main(string[] args)
        {
            Console.Title = "Advanced Roblox Archival | Made by Yakov :D";
            ArgumentInfo.ParseArguments(args);
            Start();
        }

        static void Start()
        {
            ConsoleGlobal.Singleton.WriteContent($"Revision {versionString}", ConsoleColor.DarkGray);
            ConsoleGlobal.Singleton.WriteContent(@"    _      _                         _   ___  ___  ___ _    _____  __              _    _          _ 
   /_\  __| |_ ____ _ _ _  __ ___ __| | | _ \/ _ \| _ ) |  / _ \ \/ /  __ _ _ _ __| |_ (_)_ ____ _| |
  / _ \/ _` \ V / _` | ' \/ _/ -_) _` | |   / (_) | _ \ |_| (_) >  <  / _` | '_/ _| ' \| \ V / _` | |
 /_/ \_\__,_|\_/\__,_|_||_\__\___\__,_| |_|_\\___/|___/____\___/_/\_\ \__,_|_| \__|_||_|_|\_/\__,_|_|
 by yakov :D
", ConsoleColor.Cyan);
            if (Directory.Exists(CachePath)) new DirectoryInfo(CachePath).Delete(true);
            Directory.CreateDirectory(CachePath);
            if (CurrentMode == Modes.Unspecified)
            {
                ConsoleGlobal.Singleton.WriteContent(@" features:
  - it searches whole pc using voidtools sdk (it sorts through exe, zip, 7z, and rar)
  - it verifies the digital signature so it doesn't include patched/tampered binaries
  - it checks for duplicates and doesn't scan a file or a zip more than once (if need to be relaunched)
  - has error handling and dumps logs when if it breaks (hopefully it doesn't)
  - it organizes files by the version and categorizes it to the appropriate binary categories (Client, Studio, RCC)
", ConsoleColor.DarkGray);

                if (!ConfigManager.ConfigExist())
                {
                    ConsoleGlobal.Singleton.WriteContent(" [*] Hey! it seems like it's your first time.", ConsoleColor.Yellow);
                }

                if (!ConfigManager.CheckKey("UseArchiveServer"))
                {
                    bool res = ConsoleGlobal.Singleton.WriteContentYesOrNo(" Would you like to contribute any newly found clients to the public robloxopolis archival FTP server?", ConsoleColor.Yellow, ConsoleColor.Cyan);
                    if (!res)
                    {
                        ConsoleGlobal.Singleton.WriteContent($"\n Alright D: Just remember you could change your mind at any time by clearing {ConfigManager.ConfigFilename}", ConsoleColor.Red);
                        ConsoleGlobal.Singleton.WriteContentNoLine(" Press any key to continue...", ConsoleColor.White);
                        Console.ReadLine();
                    }
                    ConfigManager.Settings["UseArchiveServer"] = res;
                    ConfigManager.FlushConfig();
                    Console.Clear();
                    Start();
                    return;
                }

                ConsoleGlobal.Singleton.WriteContent(" [*] What would you like to do?\n", ConsoleColor.Cyan);

                string[] Options = { "Scan all drives (recommended)", "Scan a specific directory" };

                int Choice = ConsoleGlobal.Singleton.WriteChoiceMenu(Options, ConsoleColor.Yellow, ConsoleColor.White);
                Console.Clear();
                CurrentMode = (Modes)Choice;
                Start();
                return;
            }

            string targetDir = string.Empty;
            if (CurrentMode == Modes.ScanSpecificDirectories)
            {
                ConsoleGlobal.Singleton.WriteContentNoLine("[*] Enter the path of which you would like to search in: ", ConsoleColor.Yellow);
                targetDir = Console.ReadLine().Trim('"');
            }

            ConsoleGlobal.Singleton.WriteContentNoLine("[*] Enter the full directory path where archives will be kept: ", ConsoleColor.Yellow);
            ArchivePath = Console.ReadLine().Trim('"');
            if (!Directory.Exists(ArchivePath)) Directory.CreateDirectory(ArchivePath);
            foreach (string i in Enum.GetNames(typeof(BinaryArchive.BinaryTypes)))
            {
                string CategoryPath = Path.Combine(ArchivePath, i);
                if (!Directory.Exists(CategoryPath)) Directory.CreateDirectory(CategoryPath);
            }

            ConsoleGlobal.Singleton.WriteContent("[*] Starting VoidTools helper", ConsoleColor.Yellow);
            EverythingApi.StartService();
            ConsoleGlobal.Singleton.WriteContent("[*] Waiting for index to finish", ConsoleColor.Yellow);
            while (!EverythingApi.IsReady()) Thread.Sleep(1000);
            double attempt = 0;
            var everything = new EverythingApi(EverythingApi.ResultKind.FilesOnly);
            var query = everything.Search(EverythingFilters.BuildGenericFilter($"!\"{ArchivePath}\""));
            if (CurrentMode == Modes.ScanSpecificDirectories)
            {
                query = everything.Search(EverythingFilters.BuildGenericFilter($"\"{targetDir}\""));
            }
            int totalattempts = query.Count();
            if (File.Exists("CheckedFiles.json")) CheckedFiles = JsonConvert.DeserializeObject<List<string>>(File.ReadAllText("CheckedFiles.json"));
            worker.DoWork += SaveConfigOccasionally;
            System.Timers.Timer timer = new System.Timers.Timer(60000);
            timer.Elapsed += timer_Elapsed;
            timer.Start();
            DateTime startTime = DateTime.Now;
            UseArchiveServer = ConfigManager.Settings["UseArchiveServer"].ToObject<bool>() && FtpManager.IsHostnameResolvable();
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

                if (UseArchiveServer)
                {
                    ConsoleGlobal.Singleton.WriteRedSeparator();
                    string out4 = $"Upload Queue: |(|{UploadQueue}|)";
                    ConsoleGlobal.Singleton.WriteColoredOutput(out4, ConsoleColor.Yellow, ConsoleColor.White, UploadQueue > 0 ? ConsoleColor.Red : ConsoleColor.Cyan, ConsoleColor.White);
                }

                attempt++;
                if (CheckedFiles.Contains(item)) continue; // intentionally skipped already checked zips.
                else CheckedFiles.Add(item);
                if (item.EndsWith(".exe"))
                {
                    BinaryArchive binaryArchive = BinaryArchive.CheckFileAuthenticity(item, false);
                    if (binaryArchive.Genuine) BinaryArchive.ArchiveFile(binaryArchive);
                }
                else
                    try
                    {
                        using (SevenZipExtractor archive = new SevenZipExtractor(item))
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
                            if (filenames.Any()) archive.ExtractFiles(CachePath, filenames.ToArray());
                            var archives = Directory.EnumerateFiles(CachePath, "*.*", SearchOption.AllDirectories);
                            foreach (string filename in archives)
                            {
                                BinaryArchive binaryArchive = BinaryArchive.CheckFileAuthenticity(filename, true);
                                if (binaryArchive.Genuine) BinaryArchive.ArchiveFile(binaryArchive);
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
            string succ2 = $"Archived |{ArchivedCount}| files in |{(int)totalTimeTaken.TotalMinutes}| minutes!!\n";

            ConsoleGlobal.Singleton.WriteColoredOutput(succ1, ConsoleColor.Yellow, ConsoleColor.Green);
            ConsoleGlobal.Singleton.WriteRedSeparator();
            ConsoleGlobal.Singleton.WriteColoredOutput(succ2, ConsoleColor.Yellow, ConsoleColor.Cyan, ConsoleColor.Yellow, ConsoleColor.Cyan, ConsoleColor.Yellow);
            
            if (UseArchiveServer && UploadQueue > 0)
            {
                while (UploadQueue > 0)
                {
                    string succ3 = "[*] |Client uploading has not yet finished!!";
                    string succ4 = $"(|{UploadQueue}|)| Clients remaining!";
                    ConsoleGlobal.Singleton.ClearCurrentConsoleLine();
                    ConsoleGlobal.Singleton.WriteColoredOutput(succ3, ConsoleColor.Yellow, ConsoleColor.Red);
                    ConsoleGlobal.Singleton.WriteContentNoLine(" | ", ConsoleColor.White);
                    ConsoleGlobal.Singleton.WriteColoredOutput(succ4, ConsoleColor.White, ConsoleColor.Cyan, ConsoleColor.White, ConsoleColor.Yellow);
                    Thread.Sleep(1000);
                }

                ConsoleGlobal.Singleton.ClearCurrentConsoleLine();
            }

            TaskbarFlash.FlashWindowEx();
            System.Media.SystemSounds.Beep.Play();

            Console.ReadLine();
        }

        static void timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            if (!worker.IsBusy)
                worker.RunWorkerAsync();
        }

        static void SaveConfigOccasionally(object sender, DoWorkEventArgs e)
        {
            File.WriteAllText("CheckedFiles.json", JsonConvert.SerializeObject(CheckedFiles.ToArray()));
        }
    }
}

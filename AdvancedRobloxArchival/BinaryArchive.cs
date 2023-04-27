using System;
using System.Diagnostics;
using System.IO;
using System.Threading;

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

        public static BinaryArchive CheckFileAuthenticity(string path, bool FromCache = false)
        {
            if (File.Exists(path))
            {
                FileVersionInfo info = FileVersionInfo.GetVersionInfo(path) ?? null;
                if (PropertyMatching.IsROBLOX(info?.FileDescription))
                {
                    bool isTrusted = AuthenticodeTools.IsTrusted(path);
                    if (isTrusted)
                    {
                        BinaryTypes binType = PropertyMatching.GetBinaryTypeFromSignature(info.FileDescription);

                        BinaryArchive binary = new BinaryArchive(true);
                        binary.Populate(info.FileVersion.Replace(", ", "."), binType, path, FromCache);
                        return binary;
                    }
                }
                if (FromCache) File.Delete(path);
            }
            return new BinaryArchive(false);
        }

        public static void ArchiveFile(BinaryArchive binary)
        {
            string destination = GetDestinationPath(binary);

            if (File.Exists(destination))
            {
                if (binary.FromCache)
                    File.Delete(binary.Path);
            }
            else
            {
                Program.ArchivedCount++;
                if (binary.FromCache)
                    File.Move(binary.Path, destination);
                else
                    File.Copy(binary.Path, destination);

                DateTime fileTimeStamp = new PeHeaderReader(destination).TimeStamp;
                if (fileTimeStamp < DateTime.UtcNow && fileTimeStamp > new DateTime(2005, 1, 1)) // Ensure they're not a super old date or a future date
                    File.SetLastWriteTimeUtc(destination, fileTimeStamp);

                if (Program.UseArchiveServer)
                {
                    if (!FtpManager.InitializeFtpConnection()) return;

                    Program.UploadQueue++;
                    Thread uploadThread = new Thread(() =>
                    {
                        bool exists = FtpManager.FileExists(binary);
                        if (!exists)
                        {
                            bool success = FtpManager.UploadFile(binary);
                            if (success) Program.UploadArchivedCount++;
                            else // Upload failed; attempt and retry 5 times.
                            {
                                for (int i = 1; i <= 5; i++)
                                {
                                    bool successRetry = FtpManager.UploadFile(binary);
                                    if (successRetry)
                                    {
                                        Program.UploadArchivedCount++;
                                        break;
                                    }
                                    else if (i == 5 && Program.UploadArchivedCount <= 0) Program.UseArchiveServer = false; // Disable this feature; doesn't seem to work.
                                }
                            }
                        }
                        Program.UploadQueue--;
                    });
                    uploadThread.IsBackground = true;
                    uploadThread.Start();
                }
            }
        }

        public static string GetDestinationPath(BinaryArchive binary) => System.IO.Path.Combine(Program.ArchivePath, binary.BinaryType.ToString(), binary.Version + ".exe");
    }
}

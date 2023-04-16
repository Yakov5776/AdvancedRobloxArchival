using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static AdvancedRobloxArchival.BinaryArchive;

namespace AdvancedRobloxArchival
{
    internal class FtpServerInformation
    {
        // Notice: The hardcoded FTP information included is not intended to be private or confidential, as it simply just has a limited scope of permissions.

        public const string HostName = "ftp.robloxopolis.com";
        public const string Username = "archiver";
        public const string Password = "@@81950d194d70ed74A95c8@0Afe67feA7ec95659541692A2fdc651ce8A42f@@fddc46AA9cd@@6630@9f10119d231";
        public static NetworkCredential Credentials = new NetworkCredential(Username, Password);
    }

    internal class FtpManager
    {
        private static readonly SemaphoreSlim _ftpSemaphore = new SemaphoreSlim(1);
        private static List<KeyValuePair<string, BinaryTypes>> ExistingUploads = new List<KeyValuePair<string, BinaryTypes>>();
        private static FtpWebRequest FtpRequest { get; set; }
        private static FtpWebRequest CreateNewFtpWebRequest(string requestUriString)
        {
            FtpWebRequest ftpWebRequest = (FtpWebRequest)WebRequest.Create(requestUriString);
            ftpWebRequest.UsePassive = true;
            ftpWebRequest.UseBinary = true;
            ftpWebRequest.KeepAlive = true;
            ftpWebRequest.Credentials = FtpServerInformation.Credentials;

            return ftpWebRequest;
        }
        public static bool IsHostnameResolvable()
        {
            try
            {
                IPAddress[] addresses = Dns.GetHostAddresses(FtpServerInformation.HostName);

                return (addresses.Length > 0);
            }
            catch
            {
                return false;
            }
        }

        public static bool UploadFile(BinaryArchive binary)
        {
            _ftpSemaphore.Wait();
            FtpRequest = CreateNewFtpWebRequest($"ftp://{FtpServerInformation.HostName}/{binary.BinaryType}/{binary.Version}.exe");
            FtpRequest.Method = WebRequestMethods.Ftp.UploadFile;

            try
            {
                using (Stream destinationStream = FtpRequest.GetRequestStream())
                using (FileStream sourceStream = new FileStream(binary.Path, FileMode.Open))
                {
                    sourceStream.CopyTo(destinationStream);
                }

                FtpWebResponse response = (FtpWebResponse)FtpRequest.GetResponse();

                return true;
            }
            catch
            {
                return false;
            }
            finally
            {
                _ftpSemaphore.Release();
            }
        }

        public static bool FileExists(BinaryArchive binary)
        {
            return (ExistingUploads.Any(x => x.Key == $"{binary.Version}.exe" && x.Value == binary.BinaryType));
        }

        public static bool InitializeFtpConnection()
        {
            if (FtpRequest != null) return true;

            foreach (string i in Enum.GetNames(typeof(BinaryArchive.BinaryTypes)))
            {
                FtpRequest = CreateNewFtpWebRequest($"ftp://{FtpServerInformation.HostName}/{i}/");
                FtpRequest.Method = WebRequestMethods.Ftp.ListDirectoryDetails;
                try
                {
                    using (FtpWebResponse response = (FtpWebResponse)FtpRequest.GetResponse())
                    using (Stream responseStream = response.GetResponseStream())
                    using (StreamReader reader = new StreamReader(responseStream))
                    {
                        string line;
                        while ((line = reader.ReadLine()) != null)
                        {
                            string[] lineParts = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                            string fileName = lineParts[lineParts.Length - 1];
                            ExistingUploads.Add(new KeyValuePair<string, BinaryTypes>(fileName, (BinaryTypes)Enum.Parse(typeof(BinaryTypes), i)));
                        }

                        response.Close();
                    }
                }
                catch
                {
                    Program.UseArchiveServer = false; // Can't connect! don't enable this feature.
                    return false;
                }
            }

            return true;
        }
    }
}

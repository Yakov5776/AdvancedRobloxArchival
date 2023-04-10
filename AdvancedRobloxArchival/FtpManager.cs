using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
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
        public static bool isConnected { get; set; }
        private static FtpWebRequest _ftpWebRequest;
        private static FtpWebRequest FtpRequest
        {
            get
            {
                if (_ftpWebRequest == null)
                {
                    _ftpWebRequest = CreateNewFtpWebRequest($"ftp://{FtpServerInformation.HostName}/");
                    _ftpWebRequest.Method = WebRequestMethods.Ftp.ListDirectoryDetails;
                    try
                    {
                        using (FtpWebResponse response = (FtpWebResponse)_ftpWebRequest.GetResponse())
                        {
                            response.GetResponseStream(); //TODO: put all existing files in a list so we dont reupload
                        }
                    }
                    catch
                    {
                        Program.UseArchiveServer = false; // Can't connect! don't enable this feature.
                    }
                }
                return _ftpWebRequest;
            }
            set { _ftpWebRequest = value; }
        }
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
            FtpRequest = CreateNewFtpWebRequest($"ftp://{FtpServerInformation.HostName}/{binary.BinaryType}/{Path.GetFileName(binary.Path)}");
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
        }
    }
}

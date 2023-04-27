using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace AdvancedRobloxArchival
{
    class EverythingFilters
    {
        private static readonly Unit DefualtMinimumSize = new Unit(3, SizeUnitEnum.Mb);
        private static readonly Unit DefualtMaximumSize = new Unit(2, SizeUnitEnum.Gb);
        private static readonly string[] MatchExtensions = { "zip", "7z", "rar", "exe" };
        private static readonly string[] DefualtExclusionPaths = { "!:\\$recycle.bin", "!:\\Windows" };

        public static string BuildGenericFilter(params string[] exclusionPath)
        {
            string result = string.Join("|", MatchExtensions.Select(ext => $"*.{ext}"));
            result += $" size:{DefualtMinimumSize}..{DefualtMaximumSize}";
            foreach (string path in DefualtExclusionPaths) result += $" {path}";
            foreach (string path in exclusionPath) result += $" {path}";

            return result;
        }

        public enum SizeUnitEnum
        {
            Kb,
            Mb,
            Gb,
        }

        public struct Unit
        {
            public Unit(int size, SizeUnitEnum sizeunit)
            {
                Size = size;
                SizeUnit = sizeunit;
            }

            public int Size { get; }
            public SizeUnitEnum SizeUnit { get; }

            public override string ToString()
            {
                return string.Concat(Size, SizeUnit.ToString().ToLower());
            }
        }
    }

    internal class EverythingApi
    {
        public ResultKind resultKind { get; set; }
        public enum ResultKind
        {
            Both,
            FilesOnly,
            FoldersOnly
        }
        private const int ReadyTimeout = 60 * 1000; // 1min
        private const int maxPathLength = 260;
        public enum ErrorCode
        {
            Ok = 0,
            Memory,
            Ipc,
            RegisterClassEX,
            CreateWindow,
            CreateThread,
            InvalidIndex,
            Invalidcall
        }

        public EverythingApi(ResultKind resultKind = ResultKind.Both)
        {
            this.resultKind = resultKind;
        }

        public static bool IsStarted()
        {
            Version version = GetVersion();

            return version.Major > 0;
        }

        public static bool StartService()
        {
            if (!IsStarted())
            {
                StartProcess("-admin -startup");

                int idleTime = 100;
                int remainingTime = ReadyTimeout;
                while (remainingTime > 0 && !IsStarted())
                {
                    Thread.Sleep(idleTime);
                    remainingTime -= idleTime;
                }

                return IsStarted();
            }

            return true;
        }

        public static bool IsReady()
        {
            return EverythingWrapper.Everything_IsDBLoaded();
        }

        public static Version GetVersion()
        {
            UInt32 major = EverythingWrapper.Everything_GetMajorVersion();
            UInt32 minor = EverythingWrapper.Everything_GetMinorVersion();
            UInt32 build = EverythingWrapper.Everything_GetBuildNumber();
            UInt32 revision = EverythingWrapper.Everything_GetRevision();

            return new Version(Convert.ToInt32(major), Convert.ToInt32(minor), Convert.ToInt32(build), Convert.ToInt32(revision));
        }

        public static ErrorCode GetLastError()
        {
            return (ErrorCode)EverythingWrapper.Everything_GetLastError();
        }

        internal static void StartProcess(string options)
        {
            string path = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string exePath = Path.GetFullPath(Path.Combine(path, Environment.Is64BitProcess ? "Everything64.exe" : "Everything32.exe"));

            System.Diagnostics.Process.Start(exePath, options);
        }

        public List<string> Search(string query)
        {
            EverythingWrapper.Everything_SetMatchWholeWord(false);
            EverythingWrapper.Everything_SetMatchPath(false);
            EverythingWrapper.Everything_SetMatchCase(false);
            var searchPattern = ApplySearchResultKind(query);
            EverythingWrapper.Everything_SetSearch(searchPattern);
            EverythingWrapper.Everything_Query(true);
            ErrorCode LastErrorCode = GetLastError();

            return GetResults();
        }

        private string ApplySearchResultKind(string searchPatten)
        {
            switch (resultKind)
            {
                case ResultKind.FilesOnly:
                    return $"files: {searchPatten}";
                case ResultKind.FoldersOnly:
                    return $"folders: {searchPatten}";
                default:
                    return searchPatten;
            }
        }

        private List<string> GetResults()
        {
            List<string> results = new List<string>();
            var numResults = EverythingWrapper.Everything_GetNumResults();
            for (UInt32 i = 0; i < numResults; i++)
            {
                StringBuilder builder = new StringBuilder(maxPathLength);
                EverythingWrapper.Everything_GetResultFullPathName(i, builder, maxPathLength);
                results.Add(builder.ToString());
            }   
            return results;
        }
    }

    internal class EverythingWrapper
    {
        private static readonly ReaderWriterLockSlim locker = new ReaderWriterLockSlim();

        private class Locker : IDisposable
        {
            private readonly ReaderWriterLockSlim locker;

            public Locker(ReaderWriterLockSlim locker)
            {
                this.locker = locker;
                this.locker.EnterWriteLock();
            }

            public void Dispose()
            {
                this.locker.ExitWriteLock();
            }
        }

#if x86
        private const string EverythingDLL = "Everything32.dll";
#elif x64
        private const string EverythingDLL = "Everything64.dll";
#endif

        private const int EVERYTHING_OK = 0;
        private const int EVERYTHING_ERROR_MEMORY = 1;
        private const int EVERYTHING_ERROR_IPC = 2;
        private const int EVERYTHING_ERROR_REGISTERCLASSEX = 3;
        private const int EVERYTHING_ERROR_CREATEWINDOW = 4;
        private const int EVERYTHING_ERROR_CREATETHREAD = 5;
        private const int EVERYTHING_ERROR_INVALIDINDEX = 6;
        private const int EVERYTHING_ERROR_INVALIDCALL = 7;

        public enum FileInfoIndex
        {
            FileSize = 1,
            FolderSize,
            DateCreated,
            DateModified,
            DateAccessed,
            Attributes
        }


        internal static IDisposable Lock()
        {
            return new Locker(locker);
        }

        [DllImport(EverythingDLL)]
        public static extern bool Everything_IsDBLoaded();

        [DllImport(EverythingDLL)]
        public static extern UInt32 Everything_GetMajorVersion();

        [DllImport(EverythingDLL)]
        public static extern UInt32 Everything_GetMinorVersion();

        [DllImport(EverythingDLL)]
        public static extern UInt32 Everything_GetRevision();

        [DllImport(EverythingDLL)]
        public static extern UInt32 Everything_GetBuildNumber();

        [DllImport(EverythingDLL)]
        public static extern int Everything_SetSearch(string lpSearchString);

        [DllImport(EverythingDLL)]
        public static extern void Everything_SetMatchPath(bool bEnable);

        [DllImport(EverythingDLL)]
        public static extern void Everything_SetMatchCase(bool bEnable);

        [DllImport(EverythingDLL)]
        public static extern void Everything_SetMatchWholeWord(bool bEnable);

        [DllImport(EverythingDLL)]
        public static extern void Everything_SetRegex(bool bEnable);

        [DllImport(EverythingDLL)]
        public static extern void Everything_SetMax(UInt32 dwMax);

        [DllImport(EverythingDLL)]
        public static extern void Everything_SetOffset(UInt32 dwOffset);

        [DllImport(EverythingDLL)]
        public static extern void Everything_SetReplyWindow(IntPtr handler);

        [DllImport(EverythingDLL)]
        public static extern void Everything_SetReplyID(UInt32 nId);

        [DllImport(EverythingDLL)]
        public static extern void Everything_Reset();

        [DllImport(EverythingDLL)]
        public static extern bool Everything_GetMatchPath();

        [DllImport(EverythingDLL)]
        public static extern bool Everything_GetMatchCase();

        [DllImport(EverythingDLL)]
        public static extern bool Everything_GetMatchWholeWord();

        [DllImport(EverythingDLL)]
        public static extern bool Everything_GetRegex();

        [DllImport(EverythingDLL)]
        public static extern UInt32 Everything_GetMax();

        [DllImport(EverythingDLL)]
        public static extern UInt32 Everything_GetOffset();

        [DllImport(EverythingDLL)]
        public static extern IntPtr Everything_GetSearch();

        [DllImport(EverythingDLL)]
        public static extern int Everything_GetLastError();

        [DllImport(EverythingDLL)]
        public static extern bool Everything_Query(bool bWait);

        [DllImport(EverythingDLL)]
        public static extern void Everything_SortResultsByPath();

        [DllImport(EverythingDLL)]
        public static extern UInt32 Everything_GetNumFileResults();

        [DllImport(EverythingDLL)]
        public static extern UInt32 Everything_GetNumFolderResults();

        [DllImport(EverythingDLL)]
        public static extern UInt32 Everything_GetNumResults();

        [DllImport(EverythingDLL)]
        public static extern UInt32 Everything_GetTotFileResults();

        [DllImport(EverythingDLL)]
        public static extern UInt32 Everything_GetTotFolderResults();

        [DllImport(EverythingDLL)]
        public static extern UInt32 Everything_GetTotResults();

        [DllImport(EverythingDLL)]
        public static extern bool Everything_IsVolumeResult(UInt32 nIndex);

        [DllImport(EverythingDLL)]
        public static extern bool Everything_IsFolderResult(UInt32 nIndex);

        [DllImport(EverythingDLL)]
        public static extern bool Everything_IsFileResult(UInt32 nIndex);

        [DllImport(EverythingDLL, CharSet = CharSet.Unicode)]
        public static extern void Everything_GetResultFullPathName(UInt32 nIndex, StringBuilder lpString, UInt32 nMaxCount);

        // Everything 1.4
        [DllImport(EverythingDLL)]
        public static extern void Everything_SetSort(UInt32 dwSortType);


        [DllImport(EverythingDLL)]
        public static extern UInt32 Everything_GetSort();

        [DllImport(EverythingDLL)]
        public static extern UInt32 Everything_GetResultListSort();

        [DllImport(EverythingDLL)]
        public static extern void Everything_SetRequestFlags(UInt32 dwRequestFlags);

        [DllImport(EverythingDLL)]
        public static extern UInt32 Everything_GetRequestFlags();

        [DllImport(EverythingDLL)]
        public static extern UInt32 Everything_GetResultListRequestFlags();

        [DllImport(EverythingDLL, CharSet = CharSet.Unicode)]
        public static extern string Everything_GetResultExtension(UInt32 nIndex);

        [DllImport(EverythingDLL)]
        public static extern bool Everything_GetResultSize(UInt32 nIndex, out long lpFileSize);

        [DllImport(EverythingDLL)]
        public static extern bool Everything_GetResultDateCreated(UInt32 nIndex, out long lpFileTime);

        [DllImport(EverythingDLL)]
        public static extern bool Everything_GetResultDateModified(UInt32 nIndex, out long lpFileTime);

        [DllImport(EverythingDLL)]
        public static extern bool Everything_GetResultDateAccessed(UInt32 nIndex, out long lpFileTime);

        [DllImport(EverythingDLL)]
        public static extern UInt32 Everything_GetResultAttributes(UInt32 nIndex);

        [DllImport(EverythingDLL, CharSet = CharSet.Unicode)]
        public static extern string Everything_GetResultFileListFileName(UInt32 nIndex);

        [DllImport(EverythingDLL, CharSet = CharSet.Unicode)]
        public static extern string Everything_GetResultPath(UInt32 nIndex);

        [DllImport(EverythingDLL, CharSet = CharSet.Unicode)]
        public static extern string Everything_GetResultFileName(UInt32 nIndex);

        [DllImport(EverythingDLL)]
        public static extern UInt32 Everything_GetResultRunCount(UInt32 nIndex);

        [DllImport(EverythingDLL)]
        public static extern bool Everything_GetResultDateRun(UInt32 nIndex, out long lpFileTime);

        [DllImport(EverythingDLL)]
        public static extern bool Everything_GetResultDateRecentlyChanged(UInt32 nIndex, out long lpFileTime);

        [DllImport(EverythingDLL, CharSet = CharSet.Unicode)]
        public static extern string Everything_GetResultHighlightedFileName(UInt32 nIndex);

        [DllImport(EverythingDLL, CharSet = CharSet.Unicode)]
        public static extern string Everything_GetResultHighlightedPath(UInt32 nIndex);

        [DllImport(EverythingDLL, CharSet = CharSet.Unicode)]
        public static extern string Everything_GetResultHighlightedFullPathAndFileName(UInt32 nIndex);

        [DllImport(EverythingDLL)]
        public static extern UInt32 Everything_GetRunCountFromFileName(string lpFileName);

        [DllImport(EverythingDLL)]
        public static extern bool Everything_SetRunCountFromFileName(string lpFileName, UInt32 dwRunCount);

        [DllImport(EverythingDLL)]
        public static extern UInt32 Everything_IncRunCountFromFileName(string lpFileName);

        [DllImport(EverythingDLL)]
        public static extern bool Everything_IsFileInfoIndexed(FileInfoIndex fileInfoType);
    }
}

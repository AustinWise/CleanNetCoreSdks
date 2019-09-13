// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Copyright (c) Austin Wise
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

//Portions taken from:
//https://github.com/dotnet/dotnet-cli-archiver/blob/a3b016f0de096054237546c932340b621fe8e3f2/src/Microsoft.DotNet.Archive/IndexedArchive.cs

using Microsoft.DotNet.Archive;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;

namespace Austin.CleanNetCoreSdks
{
    class NugetFallbackCleaner
    {
        readonly string mDotnetPath;
        readonly string mSdksFolder;
        readonly string mFallbackFolder;
        readonly List<string> mSdkVersions;
        readonly HashSet<string> mFilesToKeep;
        readonly List<string> mFilesToDelete;


        public NugetFallbackCleaner(string dotnetPath)
        {
            mDotnetPath = dotnetPath;
            mSdksFolder = Path.Combine(mDotnetPath, "sdk");
            mFallbackFolder = Path.Combine(mSdksFolder, "NuGetFallbackFolder");
            mSdkVersions = Directory.GetDirectories(mSdksFolder).Select(p => Path.GetFileName(p)).Where(p => p.StartsWith("2.")).ToList();
            mFilesToKeep = new HashSet<string>(mSdkVersions.Select(sdk => Path.Combine(mFallbackFolder, sdk + ".dotnetSentinel")), StringComparer.OrdinalIgnoreCase);
            mFilesToDelete = new List<string>();
        }

        public string FallbackFolderPath => mFallbackFolder;

        public int FilesToDeleteCount => mFilesToDelete.Count;
        public int FilesToKeepCount => mFilesToKeep.Count;

        public long SpaceSavingInBytes { get; private set; }

        public void FindFilesToDelete()
        {
            Parallel.ForEach(mSdkVersions, GetFilesToKeep);

            long spaceSaving = 0;

            foreach (var f in Directory.EnumerateFiles(mFallbackFolder, "*", SearchOption.AllDirectories))
            {
                if (!mFilesToKeep.Contains(f))
                {
                    spaceSaving += new FileInfo(f).Length;
                    mFilesToDelete.Add(f);
                }
            }

            SpaceSavingInBytes = spaceSaving;
        }

        public void DeleteFiles()
        {
            foreach (var f in mFilesToDelete)
            {
                File.Delete(f);
            }

            RemoveEmptyDirectories(mFallbackFolder);
        }

        void RemoveEmptyDirectories(string dir)
        {
            foreach (var d in Directory.GetDirectories(dir))
            {
                RemoveEmptyDirectories(d);
            }

            if (Directory.GetFileSystemEntries(dir).Length == 0)
                Directory.Delete(dir);
        }

        private void GetFilesToKeep(string sdk)
        {
            var myFiles = new List<string>();


            string compressedArchivePath = Path.Combine(mSdksFolder, sdk, "nuGetPackagesArchive.lzma");
            Console.WriteLine("Decompressing " + compressedArchivePath);

            using (var archiveStream = CreateTemporaryFileStream())
            {
                // decompress the LZMA stream
                using (var lzmaStream = File.OpenRead(compressedArchivePath))
                {
                    var progress = new MyProgress();
                    CompressionUtility.Decompress(lzmaStream, archiveStream, progress);
                }

                // reset the uncompressed stream
                archiveStream.Seek(0, SeekOrigin.Begin);

                Console.WriteLine("Rooting files in " + compressedArchivePath);

                // read as a zip archive
                using (var archive = new ZipArchive(archiveStream, ZipArchiveMode.Read))
                {
                    var indexEntry = archive.GetEntry(IndexFileName);
                    using (var indexReader = new StreamReader(indexEntry.Open()))
                    {

                        for (var line = indexReader.ReadLine(); line != null; line = indexReader.ReadLine())
                        {
                            var lineParts = line.Split(pipeSeperator);
                            if (lineParts.Length != 2)
                            {
                                throw new Exception("Unexpected index line format, too many '|'s.");
                            }

                            string target = lineParts[0];
                            string source = lineParts[1];

                            var zipSeperatorIndex = target.IndexOf("::", StringComparison.OrdinalIgnoreCase);

                            string destinationPath;
                            if (zipSeperatorIndex != -1)
                            {
                                string zipRelativePath = target.Substring(0, zipSeperatorIndex);
                                destinationPath = Path.Combine(mFallbackFolder, zipRelativePath);
                            }
                            else
                            {
                                destinationPath = Path.Combine(mFallbackFolder, target);
                            }

                            //Normalize path (forward slash to backslash)
                            myFiles.Add(Path.GetFullPath(destinationPath));
                        }
                    }
                }
            }

            lock (mFilesToKeep)
            {
                foreach (var f in myFiles)
                {
                    mFilesToKeep.Add(f);
                }
            }
        }

        static string IndexFileName = "index.txt";
        private static char[] pipeSeperator = new[] { '|' };

        private static FileStream CreateTemporaryFileStream()
        {
            string temp = Path.GetTempPath();
            string tempFile = Path.Combine(temp, Guid.NewGuid().ToString());
            return new FileStream(tempFile, FileMode.Create, FileAccess.ReadWrite, FileShare.Read | FileShare.Delete, 4096, FileOptions.DeleteOnClose);
        }

        class MyProgress : IProgress<ProgressReport>
        {
            public void Report(ProgressReport value)
            {
            }
        }
    }
}

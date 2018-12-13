using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace NCamel.Core.FileEndpoint
{
    public class FolderMonitorEndpointBuilder
    {
        private readonly Context ctx;

        private string folderName;
        private bool deleteFile;
        private bool recursive;
        private string searchPattern = "*.*";

        public FolderMonitorEndpointBuilder(Context ctx)
        {
            this.ctx = ctx;
        }

        public FolderMonitorEndpointBuilder Folder(string f)
        {
            folderName = f;
            return this;
        }

        public FolderMonitorEndpointBuilder Recursive(bool r)
        {
            recursive = r;
            return this;
        }

        public FolderMonitorEndpointBuilder DeleteFile(bool d = true)
        {
            deleteFile = d;
            return this;
        }

        public FolderMonitorEndpointBuilder WithPattern(string searchPattern)
        {
            this.searchPattern = searchPattern;
            return this;
        }

        public FolderMonitorEndpoint Build()
        {
            return new FolderMonitorEndpoint(ctx, folderName, deleteFile, recursive, searchPattern);
        }
    }

    public static class FolderMonitorEndpointBuilderExt
    {
        public static FolderMonitorEndpointBuilder FolderMonitorEndpointBuilder(this Context ctx)
        {
            return new FolderMonitorEndpointBuilder(ctx);
        }
        public static FolderMonitorEndpoint FolderMonitorEndpoint(this Context ctx, string folder, bool deleteFile=false, bool recursive=false, string searchPattern="*")
        {
            return new FolderMonitorEndpoint(ctx, folder, deleteFile, recursive, searchPattern);
        }
    }

    /// <summary>
    ///     example of a batching enpoint
    /// </summary>
    public class FolderMonitorEndpoint : Producer<string>
    {
        private const string ProcessedFolderName = ".ncamel/";
        private readonly string folderName;
        private readonly bool deleteFile;
        private readonly bool recursive;
        private readonly string searchPattern;

        public FolderMonitorEndpoint(string folder, bool deleteFile, bool recursive, string searchPattern)
        {
            folderName = folder;
            this.deleteFile = deleteFile;
            this.recursive = recursive;
            this.searchPattern = searchPattern;
        }

        public FolderMonitorEndpoint(Context ctx, string folder, bool deleteFile, bool recursive, string searchPattern)
        {
            Ctx = ctx;
            folderName = folder;
            this.deleteFile = deleteFile;
            this.recursive = recursive;
            this.searchPattern = searchPattern;
        }

        public Route Route { get; set; }

        public override void Execute()
        {
            Logger.Info($"{nameof(FolderMonitorEndpoint)} Checking {folderName}");

            var di = new DirectoryInfo(folderName);
            if (!di.Exists)
                throw new ArgumentException($"Folder not found \'{folderName}\'");

            var exchanges = GetDirs(di, recursive)
                .SelectMany(x => x.EnumerateFiles(searchPattern, SearchOption.TopDirectoryOnly))
                .Select(x => CreateMessage(x))
                .Select(msg => Ctx.CreateExchange(Route, msg, OnComplete))
                .ToList();

            exchanges.ForEach(x=>
            {
                Ctx.Start(x);
            });
        }

        private Message<string> CreateMessage(FileInfo x)
        {
            var file = File.ReadAllText(x.FullName);

            var fileInformation = new FileInformation(Encoding.UTF8.GetBytes(file))
            {
                Content = file,
                Info = x
            };

            var msg = new Message<string>();
            msg.MetaData.Add(fileInformation);

            return msg;
        }

        IEnumerable<DirectoryInfo> GetDirs(DirectoryInfo start, bool isRecursive)
        {
            if (!isRecursive)
                return new[]{start};

            var subDirs = start
                .EnumerateDirectories("*", SearchOption.TopDirectoryOnly)
                .Where(x => x.Name != ProcessedFolderName)
                .SelectMany(x => GetDirs(x, true));

            return new[] {start}.Union(subDirs);
        }

        public void OnComplete(Exchange e)
        {
            var info = e.Message.Get<FileInformation>().Single().Info;

            if (e.IsFaulted)
            {
                Logger.Warn($"Failed to process file '{info.FullName}' Will retry");
            }
            else
            {
                if (deleteFile)
                {
                    //File.Delete(info.FullName);
                    Logger.Warn($"FAKE DELETED '{info.FullName}'");
                }
                else
                    File.Move(info.FullName, Path.Combine(info.DirectoryName, ProcessedFolderName, info.Name));
            }
        }

        public class FileInformation
        {
            public string Content;
            public FileInfo Info;
            public string MD5String;

            public FileInformation(byte[] b)
            {
                MD5String = Convert.ToBase64String(MD5.Create().ComputeHash(new MemoryStream(b)));
            }
        }
    }
}
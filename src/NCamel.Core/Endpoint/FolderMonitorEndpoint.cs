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
        private Route route;

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

        public FolderMonitorEndpointBuilder To(Route r)
        {
            this.route = r;
            return this;
        }

        public FolderMonitorEndpoint Build()
        {
            return new FolderMonitorEndpoint(ctx, folderName, deleteFile, recursive, searchPattern, route);
        }
    }

    public static class FolderMonitorEndpointBuilderExt
    {
        public static FolderMonitorEndpointBuilder FolderMonitorEndpointBuilder(this Context ctx)
        {
            return new FolderMonitorEndpointBuilder(ctx);
        }
    }

    /// <summary>
    ///     example of a batching enpoint
    /// </summary>
    public class FolderMonitorEndpoint : IProducer
    {
        private const string ProcessedFolderName = ".ncamel/";
        private readonly Context ctx;
        private readonly Route route;
        private readonly string folderName;
        private readonly bool deleteFile;
        private readonly bool recursive;
        private readonly string searchPattern;

        public FolderMonitorEndpoint(Context ctx, string folder, bool deleteFile, bool recursive, string searchPattern, Route r)
        {
            this.ctx = ctx;
            folderName = folder;
            this.deleteFile = deleteFile;
            this.recursive = recursive;
            this.searchPattern = searchPattern;
            this.route = r;
        }

        public void Execute()
        {
            Logger.Info($"{nameof(FolderMonitorEndpoint)} Checking {folderName}");

            var di = new DirectoryInfo(folderName);
            if (!di.Exists)
                throw new ArgumentException($"Folder not found \'{folderName}\'");

            var searchOption = recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
            var messages = new[]{di}.Union( di.EnumerateDirectories("*", searchOption))
                .Where(x => x.Name != ProcessedFolderName)
                .SelectMany(x => x.EnumerateFiles(searchPattern, SearchOption.TopDirectoryOnly))
                .Select(x =>
                {
                    var file = ReadFile(x);
                    return FillMetaData(new Message<string>(), file, x);
                })
                .Select(x => new Exchange(ctx, route) {Message = x})
                .ToList();

            messages.ForEach(x=>
            {
                x.OnCompleteActions.Push(OnComplete);
                ctx.Start(x);
            });
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

        private Message<string> FillMetaData(Message<string> msg, string file, FileInfo fileInfo)
        {
            msg.MetaData.Add(new FileInformation(Encoding.UTF8.GetBytes(file))
                {
                    Content = file,
                    Info = fileInfo
                }
            );

            return msg;
        }

        private string ReadFile(FileInfo fileInfo)
        {
            return File.ReadAllText(fileInfo.FullName);
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
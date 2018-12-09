using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;

namespace NCamel.Core.FileEndpoint
{
    public class FolderMonitorEndpoint : IProducer<string>
    {
        const string ProcessedFolderName = ".ncamel/";
        private readonly Context ctx;
        private string folderName;
        private bool recursive;
        private bool deleteFile;
        string searchPattern;

        public static class MetaDataNames
        {
            public static string FilemonitorFileinfo = "FileMonitor:FileInfo";
            public static string FilemonitorMd5 = "FileMonitor:MD5";
        }

        public FolderMonitorEndpoint Recursive(bool r)
        {
            recursive = r;
            return this;
        }

        public FolderMonitorEndpoint DeleteFile(bool d)
        {
            deleteFile = d;
            return this;
        }

        public FolderMonitorEndpoint WithPattern(string searchPattern)
        {
            this.searchPattern = searchPattern;
            return this;
        }

        public FolderMonitorEndpoint Folder(string folder)
        {
            folderName = folder;
            return this;
        }

        public FolderMonitorEndpoint(Context ctx)
        {
            this.ctx = ctx;
        }

        public IEnumerable<Exchange> Execute()
        {
            Logger.Info($"{nameof(FolderMonitorEndpoint)} Checking {folderName}");

            DirectoryInfo di = new DirectoryInfo(folderName);
            if(!di.Exists)
                throw new ArgumentException($"Folder not found \'{folderName}\'");

            var messages = di.EnumerateDirectories("*",recursive?SearchOption.AllDirectories : SearchOption.TopDirectoryOnly)
                .Where(x => x.Name != ProcessedFolderName)
                .SelectMany(x => x.EnumerateFiles(searchPattern ?? "*", SearchOption.TopDirectoryOnly))
                .Select(x =>
                {
                    var file = ReadFile(x);
                    return FillMetaData(new Message<string>(), file, x);
                })
                .Select(x => new Exchange(ctx, OnComplete) {Message = x});
            
            return messages;
        }

        public void OnComplete(Exchange e)
        {
            var info = e.Message.GetFileInfo();
            if (e.IsFaulted)
            {
                Logger.Warn($"Failed to process file '{info.FullName}' Will retry");
            }
            else
            {

                if (deleteFile)
                    File.Delete(info.FullName);
                else
                    File.Move(info.FullName, Path.Combine(info.DirectoryName, ProcessedFolderName, info.Name));
            }
        }

        private Message<string> FillMetaData(Message<string> msg, string file, FileInfo fileInfo)
        {
            msg.MetaData[MetaDataNames.FilemonitorFileinfo] = fileInfo;
            msg.MetaData[MetaDataNames.FilemonitorMd5] = Convert.ToBase64String(MD5.Create().ComputeHash(new MemoryStream(System.Text.Encoding.UTF8.GetBytes(file))));
           
            return msg;
        }

        private string ReadFile(FileInfo fileInfo)
        {
            return File.ReadAllText(fileInfo.FullName);
        }
    }

    public static class FileMonitorExtensions
    {
        public static FileInfo GetFileInfo(this Message message)
        {
            return (FileInfo)message.MetaData[FolderMonitorEndpoint.MetaDataNames.FilemonitorFileinfo];
        }

        public static string GetMD5CheckSum(this Message message)
        {
            return (string)message.MetaData[FolderMonitorEndpoint.MetaDataNames.FilemonitorMd5];
        }
    }
}

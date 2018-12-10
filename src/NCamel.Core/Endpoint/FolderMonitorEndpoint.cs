using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;

namespace NCamel.Core.FileEndpoint
{
    /// <summary>
    /// example of a batching enpoint
    /// </summary>
    public class FolderMonitorEndpoint : IProducer<string>
    {
        const string ProcessedFolderName = ".ncamel/";
        private readonly Context ctx;

        private string folderName;
        private bool recursive;
        private bool deleteFile;
        string searchPattern;

        public FolderMonitorEndpoint(Context ctx)
        {
            this.ctx = ctx;
        }

	    public class FileInformation
	    {
		    public string Content;
		    public string MD5String;
		    public FileInfo Info;

		    public FileInformation(byte[] b)
		    {
			    MD5String = Convert.ToBase64String(MD5.Create().ComputeHash(new MemoryStream(b)));
		    }
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
            var info = e.Message.Get<FileInformation>().Single().Info;

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
            msg.MetaData.Add(new FileInformation(System.Text.Encoding.UTF8.GetBytes(file))
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
    }
}

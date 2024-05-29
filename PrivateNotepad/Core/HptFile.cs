using CommunityToolkit.Mvvm.ComponentModel;
using PrivateNotepad.Extensions;
using System;
using System.IO;
using System.Linq;
using System.Text;

namespace PrivateNotepad.Core
{
    public class HptFile : ObservableObject
    {
        /// <summary>
        /// 文件标识
        /// </summary>
        public static readonly byte[] FlagBinary = [77, 77, 77, 77];

        private string? path;
        private string? fileName;
        private string? rawContent;


        private bool newFile = true;
        private bool notSaved = false;

        /// <summary>
        /// 路径
        /// </summary>
        public string? Path
        {
            get => path;
            set => SetProperty(ref path, value);
        }

        /// <summary>
        /// 文件名
        /// </summary>
        public string? FileName
        {
            get => fileName;
            set => SetProperty(ref fileName, value);
        }

        /// <summary>
        /// 原文件内容
        /// </summary>
        public string? RawContent
        {
            get => rawContent;
            set
            {
                SetProperty(ref rawContent, value);
                NotSaved = true;
            }
        }

        /// <summary>
        /// Tab标题
        /// </summary>
        public string? TabTitle => NotSaved ? $"{FileName}*" : FileName;

        /// <summary>
        /// 新文件
        /// </summary>
        public bool NewFile
        {
            get => newFile;
            set => SetProperty(ref newFile, value);
        }

        /// <summary>
        /// 未保存
        /// </summary>
        public bool NotSaved
        {
            get => notSaved;
            private set
            {
                SetProperty(ref notSaved, value);
                OnPropertyChanged("TabTitle");
            }
        }

        /// <summary>
        /// 打开文件
        /// </summary>
        /// <param name="fileFullPath">完整路径</param>
        /// <returns></returns>
        public static HptFile? OpenFile(string fileFullPath)
        {
            if (!File.Exists(fileFullPath))
            {
                return null;
            }
            var bytes = File.ReadAllBytes(fileFullPath).ToList();
            if (bytes.Count < 8)
            {
                return null;
            }

            var leftFlag = bytes.Take(4).ToArray();
            if (!leftFlag.SequenceEqual(FlagBinary)) { return null; }

            var rightFlag = bytes.Skip(bytes.Count - 4).Take(4).ToArray();
            if (!rightFlag.SequenceEqual(FlagBinary)) { return null; }

            bytes.RemoveRange(0, 4);
            bytes.RemoveRange(bytes.Count - 4, 4);
            var file = new HptFile();
            file.Path = System.IO.Path.GetDirectoryName(fileFullPath) + "\\";
            file.FileName = System.IO.Path.GetFileName(fileFullPath);
            file.RawContent = Encoding.UTF8.GetString(bytes.ToArray()).DesDecrypt();
            file.NewFile = false;
            file.NotSaved = false;
            return file;
        }

        /// <summary>
        /// 保存文件内容
        /// </summary>
        /// <returns>成功返回true，失败返回false</returns>
        public bool SaveContent()
        {
            if (string.IsNullOrWhiteSpace(Path) || string.IsNullOrWhiteSpace(FileName))
            {
                throw new Exception("保存文件路径或文件名为空！");
            }
            string path = System.IO.Path.Combine(Path, FileName);

            var encryptedContent = RawContent?.DesEncrypt();
            if (string.IsNullOrWhiteSpace(encryptedContent))
            {
                throw new Exception("文件内容为空！");
            }
            var bytes = Encoding.UTF8.GetBytes(encryptedContent).ToList();
            bytes.InsertRange(0, FlagBinary);
            bytes.AddRange(FlagBinary);
            File.WriteAllBytes(path, bytes.ToArray());
            NewFile = false;
            NotSaved = false;
            return true;
        }
    }
}

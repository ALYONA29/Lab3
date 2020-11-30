using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Threading;
using ClassLibrary2;


namespace Lab2
{
    public partial class Service1 : ServiceBase
    {
        Logger logger;
        OptionManager optionManager;
        Options options;
        public Service1()
        {
            InitializeComponent();
            this.CanStop = true;
            this.CanPauseAndContinue = true;
            this.AutoLog = true;
        }

        protected override void OnStart(string[] args)
        {
            logger = new Logger();
            Thread serverThread = new Thread(new ThreadStart(logger.OnStart));
            serverThread.Start();
        }
        protected override void OnStop()
        {
            logger.OnStop();
            Thread.Sleep(1000);
        }
        class Logger
        {
            FileSystemWatcher watcher;
            object obj = new object();
            OptionManager optionManager;
            bool enabled = true;

            public Logger()
            {
                Configuration();
            }

            public void OnStart()
            {
                watcher.EnableRaisingEvents = true;
                while (enabled)
                {
                    Thread.Sleep(1000);
                }
            }

            public void OnStop()
            {
                watcher.EnableRaisingEvents = false;
                enabled = false;
            }

            void Configuration()
            {
                string directory = AppDomain.CurrentDomain.BaseDirectory;
                optionManager = new OptionManager();
                Options options = new Options();
                options = OptionManager.GetOptions();
                watcher = new FileSystemWatcher(options.SourceDirectory);
                watcher.Filter = "*.txt";
                watcher.Created += Watcher_Created;
                watcher.Changed += Watcher_Changed;
                watcher.Deleted += Watcher_Deleted;
                watcher.Renamed += Watcher_Renamed;
                watcher.EnableRaisingEvents = true;
            }

            private void Watcher_Created(object sender, FileSystemEventArgs e)
            {
                string fileEvent = "создан";
                string newPath = "Sales_" + DateTime.Now.ToString("yyyy/MM/dd") + "_" +
                    DateTime.Now.ToString("HH/mm/ss");
                if (File.Exists(e.Name))
                {
                    RecordEntry(fileEvent, newPath);
                    if (new FileInfo(e.FullPath).Length != 0)
                    {
                        ClassLibrary.Functions.Encrypt(e.Name);
                        string compressedFile = newPath + ".gz";
                        ClassLibrary.Functions.Compress(e.Name, compressedFile);
                        string fileDeName = newPath + ".txt";
                        Options options = new Options();
                        options = OptionManager.GetOptions();
                        string targetFolder = options.TargetDirectory;
                        string archive = Path.Combine(targetFolder, "archive(" +
                            DateTime.Now.ToString("yyyy/MM/dd") + ")");
                        if (!Directory.Exists(archive))
                        {
                            Directory.CreateDirectory(archive);
                        }
                        File.Move(Path.Combine(Directory.GetCurrentDirectory(), compressedFile),
                            Path.Combine(archive, compressedFile));

                        string decompressFolder = options.DecompressDirectory;
                        string files = Path.Combine(decompressFolder, "files(" +
                            DateTime.Now.ToString("yyyy/MM/dd") + ")");
                        if (!Directory.Exists(files))
                        {
                            Directory.CreateDirectory(files);
                        }
                        string targetFile = Path.Combine(archive, fileDeName);
                        ClassLibrary.Functions.Decompress(Path.Combine(archive, compressedFile), targetFile);
                        ClassLibrary.Functions.Decrypt(Path.Combine(archive, newPath) + ".txt");
                        File.Move(Path.Combine(archive, newPath + ".txt"), Path.Combine(files, newPath + ".txt"));
                        File.Delete(Path.Combine(targetFolder, compressedFile));
                        File.Delete(e.FullPath);
                    }
                }
            }
            private void Watcher_Renamed(object sender, RenamedEventArgs e)
            {
                string fileEvent = "переименован в " + e.FullPath;
                string filePath = e.OldFullPath;
                RecordEntry(fileEvent, filePath);
            }

            private void Watcher_Changed(object sender, FileSystemEventArgs e)
            {
                string fileEvent = "изменён";
                string filePath = e.FullPath;
                RecordEntry(fileEvent, filePath);
            }
            private void Watcher_Deleted(object sender, FileSystemEventArgs e)
            {
                string fileEvent = "удалён";
                string filePath = e.FullPath;
                RecordEntry(fileEvent, filePath);
            }

            private void RecordEntry(string fileEvent, string filePath)
            {
                lock (obj)
                {
                    Options options = new Options();
                    options = OptionManager.GetOptions();
                    string logFile = options.LofDirectory;
                    string path = logFile;
                    if (!File.Exists(path))
                    {
                        File.Create(path);
                    }
                    using (StreamWriter writer = new StreamWriter(path, true))
                    {
                        writer.WriteLine(String.Format("{0} файл {1} был {2}",
                            DateTime.Now.ToString("dd/MM/yyyy hh:mm:ss"), filePath, fileEvent));
                        writer.Flush();
                    }
                }
            }
        }

        public class Options
        {
            public string SourceDirectory { get; set; } = "D:\\SourceDirectory";
            public string LofDirectory { get; set; } = "D:\\templog.txt";
            public string TargetDirectory { get; set; } = "D:\\TargetDirectory\\archive";
            public string DecompressDirectory { get; set; } = "D:\\TargetDirectory\\files";
        }
        public class OptionManager
        {
            public static Options GetOptions()
            {
                Options options;
                bool isJson;
                bool isXml;
                try
                {
                    using (StreamReader sr = new StreamReader("appsettings.json"))
                    {
                        string json = sr.ReadToEnd();
                        options = ParserJson.Parser.DeserializeJson<Options>(json);
                        return options;
                    }
                }
                catch
                {
                    isJson = false;
                }
                try
                {
                    using (StreamReader sr = new StreamReader("config.xml"))
                    {
                        string xml = sr.ReadToEnd();
                        options = ParserXml.Parser.DeserializeXml<Options>(xml);
                        return options;
                    }
                }
                catch
                {
                    isXml = false;
                }
                options = new Options();
                return options;
            }
        }
        static class Checker
        {
            public static void Check(Options options)
            {
                string sourceFolder = options.SourceDirectory;
                if (!Directory.Exists(sourceFolder))
                {
                    sourceFolder = "D:\\SourceFolder";
                    Directory.CreateDirectory(sourceFolder);
                }
                string targetFolder = options.TargetDirectory;
                if (!Directory.Exists(targetFolder))
                {
                    targetFolder = "D:\\TargetFolder\\archive";
                    Directory.CreateDirectory(targetFolder);
                }
                string decompressFolder = options.DecompressDirectory;
                if (!Directory.Exists(decompressFolder))
                {
                    decompressFolder = "D:\\TargetFolder\\files";
                    Directory.CreateDirectory(decompressFolder);
                }
                string logFile = options.LofDirectory;
                if (!Directory.Exists(logFile))
                {
                    logFile = "D:\\templog.txt";
                    Directory.CreateDirectory(logFile);
                }
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using static Metaler.DVR_Info_Bot.Program;
using System.Threading.Tasks;
using Telegram.Bot;

namespace Metaler.DVR_Info_Bot
{
    internal class CamSnapshotFileWatcher
    {
        private static BotSettings BotOption = null;
        private static ITelegramBotClient TelegramBot = null;
        public CamSnapshotFileWatcher(ITelegramBotClient bot, BotSettings botSettings)
        {
            BotOption = botSettings;
            TelegramBot = bot;
        }

        public void Start()
        {
            // FileSystemWatcher ---------------------------------

            foreach (SnapshotFolder sfolder in BotOption.SnapshotFolders)
            {
                var watcher = new FileSystemWatcher(sfolder.pathfullname);

                watcher.NotifyFilter = NotifyFilters.LastWrite;

                watcher.Changed += OnChanged;
                //watcher.Created += OnCreated;
                watcher.Error += OnError;

                watcher.Filter = "*.jpg";
                watcher.EnableRaisingEvents = true;
            }
        }

        public static async Task HandleFileWatcherAsync(BotSettings botSettings, string FilePath)
        {
            if (isFileOpen(new FileInfo(FilePath)))
            {
                Console.WriteLine("File opened: " + FilePath);
            }
            else
            {
                foreach (SnapshotFolder path in BotOption.SnapshotFolders)
                {
                    if (FilePath.StartsWith(path.pathfullname))
                    {
                        foreach (LocalUser user in BotOption.Users)
                        {
                            if (user.Notifi.notificationStatus == LocalUser.Notification.NotificationStatus.On)
                            {
                                if (user.SnapshotFoldersID.Contains(path.id))
                                {
                                    using (var fileStream = new FileStream(FilePath, FileMode.Open, FileAccess.Read, FileShare.Read))
                                    {
                                        try
                                        {
#warning "почему-то чат не найден"
                                            await TelegramBot.SendPhotoAsync(user.Id64, new Telegram.Bot.Types.InputFiles.InputOnlineFile(fileStream),
                                              DateTime.Now.ToLongDateString() + " " + DateTime.Now.ToLongTimeString());
                                        }
                                        catch(Exception ex)
                                        {
                                            Console.WriteLine(ex.Message);
                                        }
                                    }

                                }
                            }
                        }
                    }
                }
            }
            await Task.CompletedTask;
        }

        // Returns true if the file is opened
        public static bool isFileOpen(FileInfo file)
        {
            FileStream str = null;
            try
            {
                str = file.Open(FileMode.Open, FileAccess.Read, FileShare.None);
            }
            catch (IOException)
            {
                return true;
            }
            finally
            {
                if (str != null)
                    str.Close();
            }
            return false;
        }

        ////////////////////////////////////////////////////////////////////////////////////
        private static async void OnChanged(object sender, FileSystemEventArgs e)
        {
            if (e.ChangeType != WatcherChangeTypes.Changed)
            {
                return;
            }

            await HandleFileWatcherAsync(BotOption, e.FullPath);

            Console.WriteLine($"Changed: {e.FullPath}");
        }

        private static void OnError(object sender, ErrorEventArgs e) =>
            PrintException(e.GetException());

        private static void PrintException(Exception ex)
        {
            if (ex != null)
            {
                Console.WriteLine($"Message: {ex.Message}");
                Console.WriteLine("Stacktrace:");
                Console.WriteLine(ex.StackTrace);
                Console.WriteLine();
                PrintException(ex.InnerException);
            }
        }
    }
}

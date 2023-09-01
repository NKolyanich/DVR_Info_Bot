using System;
using System.Threading;
using System.Threading.Tasks;
using System.IO;
using System.Linq;
using System.ComponentModel;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Encodings.Web;
using System.Collections.ObjectModel;
using static System.Net.Mime.MediaTypeNames;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.VisualBasic;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using Telegram.Bot.Requests;
using Telegram.Bot.Exceptions;
using static Metaler.DVR_Info_Bot.Program;


namespace Metaler.DVR_Info_Bot
{
    class Program
    {
        static string AssemblyPathLocation = @"D:\publish";

        const string BotConfigFileName = @"\config\bot_config.json";

        private static BotSettings BotSetting = new BotSettings();

        //private static CommandMemoryCache<BotCommands> BotCommandsCache = new CommandMemoryCache<BotCommands>();

        static ITelegramBotClient bot;
        public static async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
        {
            // Некоторые действия
            /// изменить на System.text.json
            //Console.WriteLine(Newtonsoft.Json.JsonConvert.SerializeObject(update));

           // Console.WriteLine(JsonSerializer.Serialize(update));

            switch (update.Type)
            {
                // A message was received
                case UpdateType.Message:
                    await HandleMessage(update.Message);
                    break;

                case UpdateType.MyChatMember:
                    { // Добавление/удаление из чата, смена прав
                        if (update.MyChatMember.OldChatMember.Status == ChatMemberStatus.Left)
                        {
                            if (update.MyChatMember.NewChatMember.Status == ChatMemberStatus.Member)
                            {
                                await bot.SendTextMessageAsync(update.MyChatMember.Chat,
                                    "Удалите этого бота - он не будет работать с вами.\r\n"
                                    + "Выберите в меню контакта пункт \"Остановить бота\".\r\n"
                                    + "Пока.\r\n\r\n"
                                    + "Delete this bot - it won't work with you.\r\n"
                                    + "Select \"Stop Bot\" from the contact's menu.\r\n"
                                    + "Bye.");
                            }
                        }
                    }
                    break;
                case UpdateType.CallbackQuery:
                    {
                        if (update.CallbackQuery.Data != null)
                        {
                            int userIndex = -1;
                            for (int i = 0; i < BotSetting.Users.Count; i++)
                            {
                                if (BotSetting.Users[i].Id64 == update.CallbackQuery.From.Id)
                                {
                                    userIndex = i;
                                    break;
                                }
                            }
                            if (userIndex >= 0)
                            {
                                await HendleCallbackQuery(update, userIndex);

                            }
                            else
                            {
                                await bot.AnswerCallbackQueryAsync(update.CallbackQuery.Id,
                                   "Пользователей нет 🤷‍♂️", true);
                            }
                        }
                    }
                    break;
                    /*
                    case UpdateType.ChatMember:
                    break;
                    */
            }
        }

        private static async Task HendleCallbackQuery(Update update, int localUserIndex)
        {
            if (BotSetting.Users[localUserIndex].UserRights == LocalUser.UsersRights.User)
            {
                await HendleCallbackQueryForUser(update, localUserIndex);
            }
            else if(BotSetting.Users[localUserIndex].UserRights == LocalUser.UsersRights.Admin)
            {
                await HendleCallbackQueryForAdmin(update);

            }
        }

        private static async Task HendleCallbackQueryForAdmin(Update update)
        {
            try
            {
                BotCommands.AdminMenuCallbackData menuCallbackData = new BotCommands.AdminMenuCallbackData();
                if (!menuCallbackData.CreateObject(update.CallbackQuery.Data))
                {
                    return;
                }

                if ((BotCommands.AdminMenu.Commands)menuCallbackData.OneLevelSubmenuItemIndex != BotCommands.AdminMenu.Commands.Null)
                {
                    switch ((BotCommands.AdminMenu.Commands)menuCallbackData.OneLevelSubmenuItemIndex)
                    {
                        case BotCommands.AdminMenu.Commands.UserManagment:
                            {
                                await UserManagmentCreateMenu(update);
                            }
                            break;
                        case BotCommands.AdminMenu.Commands.CamManagment:
                            {
                                await CamManagmentCreateMenu(update);
                            }
                            break;
                        case BotCommands.AdminMenu.Commands.Back:
                            {
                                await bot.AnswerCallbackQueryAsync(update.CallbackQuery.Id, " ");
                                await RootMenuCreateHelp(update.CallbackQuery.From.Id);
                            }
                            break;
                    }
                }
                else
                { 
                    if ((BotCommands.AdminMenu.UserManagement.Commands)menuCallbackData.TwoLevelSubmenuItemIndex 
                        != BotCommands.AdminMenu.UserManagement.Commands.Null)
                    {
                        switch ((BotCommands.AdminMenu.UserManagement.Commands)menuCallbackData.TwoLevelSubmenuItemIndex)
                        {
                            case BotCommands.AdminMenu.UserManagement.Commands.GetAll:
                                {
                                    string text = BotCommands.AdminMenu.UserManagement.GetAll.TitleString;
                                    foreach (LocalUser localUser in BotSetting.Users)
                                    {
                                        text += "\r\n" + localUser.Name + " (" + localUser.UserRights.ToString() + ")";

                                    }
                                    await bot.AnswerCallbackQueryAsync(
                                        update.CallbackQuery.Id,
                                        " ");
                                    await bot.SendTextMessageAsync(update.CallbackQuery.From.Id, text);
                                }
                                break;
                            case BotCommands.AdminMenu.UserManagement.Commands.AddNew:
                                {
                                    if ((BotCommands.AdminMenu.UserManagement.AddNew.Commands)menuCallbackData.ThreeLevelSubmenuItemIndex
                                            != BotCommands.AdminMenu.UserManagement.AddNew.Commands.Null)
                                    {
                                        string[] datauser = menuCallbackData.Data.Split('|');

                                        if (BotSetting.IsValidUser(Convert.ToInt64(datauser[1])) != true)
                                        {
                                            LocalUser user = new LocalUser();
                                            user.Name = datauser[0];
                                            user.Id64 = Convert.ToInt64(datauser[1]);

                                            user.SnapshotFoldersID = new List<int>();
                                            user.UserRights = LocalUser.UsersRights.User;

                                            BotSetting.Users.Add(user);
                                            SaveConfig();
                                            await bot.AnswerCallbackQueryAsync(update.CallbackQuery.Id, " ");
                                            await bot.SendTextMessageAsync(update.CallbackQuery.From.Id, " Юзер " + datauser[0] + " добавлен 🫡");
                                        }
                                        else
                                        {
                                            await bot.AnswerCallbackQueryAsync(update.CallbackQuery.Id, " ");
                                            await bot.SendTextMessageAsync(update.CallbackQuery.From.Id, "Этот юзер (" + datauser[0] + ") уже есть 🤷‍♂️");
                                        }
                                    }
                                    else
                                    {
                                        string text = "Для добавления юзера перешлите любое сообщение от него боту и следуйте инструкциям.";
                                        await bot.AnswerCallbackQueryAsync(update.CallbackQuery.Id, " ");
                                        await bot.SendTextMessageAsync(update.CallbackQuery.From.Id, text);
                                    }
                                }
                                break;
                            case BotCommands.AdminMenu.UserManagement.Commands.Change:
                                {

                                }
                                break;
                            case BotCommands.AdminMenu.UserManagement.Commands.Delete:
                                {
                                    if ((BotCommands.AdminMenu.UserManagement.Delete.Commands)menuCallbackData.ThreeLevelSubmenuItemIndex
                                            != BotCommands.AdminMenu.UserManagement.Delete.Commands.Null)
                                    {
                                        switch ((BotCommands.AdminMenu.UserManagement.Delete.Commands)menuCallbackData.ThreeLevelSubmenuItemIndex)
                                        {
                                            case BotCommands.AdminMenu.UserManagement.Delete.Commands.Delete:
                                                {
                                                    if ((string)menuCallbackData.Data
                                                        != String.Empty)
                                                    {
                                                        string name = BotCommands.AdminMenu.UserManagement.Delete.UserDeleteNotFoundString;
                                                        if (Convert.ToInt32(menuCallbackData.Data) != update.CallbackQuery.From.Id)
                                                        {
                                                            foreach (LocalUser localUser in BotSetting.Users)
                                                            {
                                                                if (localUser.Id64 == Convert.ToInt32(menuCallbackData.Data))
                                                                {
                                                                    name = BotCommands.AdminMenu.UserManagement.Delete.UserDeleteString
                                                                        + "\r\n" + localUser.Name;
                                                                    BotSetting.Users.Remove(localUser);
                                                                    SaveConfig();
                                                                    break;
                                                                }
                                                            }
                                                            await bot.AnswerCallbackQueryAsync(
                                                                update.CallbackQuery.Id, " ");
                                                            await bot.SendTextMessageAsync(update.CallbackQuery.From.Id, name);
                                                        }
                                                        else
                                                        {
                                                            await bot.AnswerCallbackQueryAsync(update.CallbackQuery.Id,
                                                                BotCommands.AdminMenu.UserManagement.Delete.UserDeleteSelfErrorString, true);
                                                        }
                                                    }
                                                }
                                                break;
                                        }
                                    }
                                    else
                                    {
                                        var list = new List<List<InlineKeyboardButton>>();
                                        for (int i=0; i<BotSetting.Users.Count; i++)
                                        {
                                            var list1 = new List<InlineKeyboardButton>();
                                            BotCommands botCommands = new BotCommands();
                                            string data = botCommands.MenuCallbackData.CreateCallbackData(
                                                    (int)BotCommands.AdminMenu.Commands.Null,
                                                    (int)BotCommands.AdminMenu.UserManagement.Commands.Delete,
                                                    (int)(BotCommands.AdminMenu.UserManagement.Delete.Commands.Delete),
                                                    BotSetting.Users[i].Id64.ToString());
                                            list1.Add(InlineKeyboardButton.WithCallbackData(
                                               BotSetting.Users[i].Name + BotCommands.AdminMenu.UserManagement.Delete.UserDeleteEmodzi, data));
                                            list.Add(list1);
                                        }

                                        var ikm = new InlineKeyboardMarkup(list);
                                        await bot.AnswerCallbackQueryAsync(
                                        update.CallbackQuery.Id, " ");
                                        await bot.SendTextMessageAsync(update.CallbackQuery.From.Id,
                                            BotCommands.AdminMenu.UserManagement.Delete.TitleString, replyMarkup: ikm);
                                    }
                                }
                                break;
                            case BotCommands.AdminMenu.UserManagement.Commands.Notifi:
                                {
                                    if ((BotCommands.AdminMenu.UserManagement.NotifiManagement.Commands)menuCallbackData.ThreeLevelSubmenuItemIndex
                                            != BotCommands.AdminMenu.UserManagement.NotifiManagement.Commands.Null)
                                    {
                                        switch((BotCommands.AdminMenu.UserManagement.NotifiManagement.Commands)menuCallbackData.ThreeLevelSubmenuItemIndex)
                                        {
                                            case BotCommands.AdminMenu.UserManagement.NotifiManagement.Commands.Disable:
                                                {
                                                    foreach (LocalUser localUser in BotSetting.Users)
                                                    {
                                                        localUser.Notifi.notifycationDelay = LocalUser.Notification.NotificationDelay.Disable;
                                                    }
                                                    await bot.AnswerCallbackQueryAsync(update.CallbackQuery.Id, "");
                                                    await bot.SendTextMessageAsync(update.CallbackQuery.From.Id, 
                                                        "Уведомления всех пользователей отключены");
                                                }
                                                break;
                                            case BotCommands.AdminMenu.UserManagement.NotifiManagement.Commands.Enable:
                                                {
                                                    foreach (LocalUser localUser in BotSetting.Users)
                                                    {
                                                        localUser.Notifi.notifycationDelay = LocalUser.Notification.NotificationDelay.Enable;
                                                    }
                                                    await bot.AnswerCallbackQueryAsync(update.CallbackQuery.Id, " ");
                                                    await bot.SendTextMessageAsync(update.CallbackQuery.From.Id,
                                                        "Уведомления всех пользователей включены");
                                                }
                                                break;
                                            case BotCommands.AdminMenu.UserManagement.NotifiManagement.Commands.Back:
                                                {
                                                    await bot.AnswerCallbackQueryAsync(update.CallbackQuery.Id, " ");
                                                    await bot.SendTextMessageAsync(update.CallbackQuery.From.Id,
                                                        "Типа возврат назад");
                                                }
                                                break;
                                        }
                                    }
                                    else
                                    {
                                        var list = new List<List<InlineKeyboardButton>>();
                                        for (int i = 0; i < BotCommands.AdminMenu.UserManagement.NotifiManagement.CommandsTitle.Length; i++)
                                        {
                                            var list1 = new List<InlineKeyboardButton>();
                                            BotCommands botCommands = new BotCommands();
                                            string data = botCommands.MenuCallbackData.CreateCallbackData(
                                                    (int)BotCommands.AdminMenu.Commands.Null,
                                                    (int)BotCommands.AdminMenu.UserManagement.Commands.Notifi,
                                                    (int)(BotCommands.AdminMenu.UserManagement.NotifiManagement.Commands)i,
                                                    String.Empty);
                                            list1.Add(InlineKeyboardButton.WithCallbackData(
                                                BotCommands.AdminMenu.UserManagement.NotifiManagement.CommandsTitle[i], data));
                                            list.Add(list1);
                                        }

                                        var ikm = new InlineKeyboardMarkup(list);
                                        await bot.AnswerCallbackQueryAsync(update.CallbackQuery.Id, " ");
                                        await bot.SendTextMessageAsync(update.CallbackQuery.From.Id,
                                            BotCommands.AdminMenu.UserManagement.NotifiManagement.TitleString, replyMarkup: ikm);
                                    }
                                }
                                break;
                            case BotCommands.AdminMenu.UserManagement.Commands.Back:
                                {
                                    await AdminMenuCreateMenu(update.CallbackQuery.Id, update.CallbackQuery.From.Id);
                                }
                                break;
                        }
                    }
                    else if ((BotCommands.AdminMenu.CamManagement.Commands)menuCallbackData.TwoLevelSubmenuItemIndex
                        != BotCommands.AdminMenu.CamManagement.Commands.Null)
                    {

                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("\r\nException message>> :" + ex.Message);
            }
            await Task.CompletedTask;
        }

        private static async Task CamManagmentCreateMenu(Update update)
        {
            var list = new List<List<InlineKeyboardButton>>();
            for (int i = 0; i < BotCommands.AdminMenu.CamManagement.CommandsTitle.Length; i++)
            {
                var list1 = new List<InlineKeyboardButton>();
                BotCommands botCommands = new BotCommands();
                string data = botCommands.MenuCallbackData.CreateCallbackData(
                        (int)BotCommands.AdminMenu.Commands.Null,
                        (int)(BotCommands.AdminMenu.CamManagement.Commands)i,
                        (int)BotCommands.AdminMenu.Commands.Null,
                        String.Empty);
                list1.Add(InlineKeyboardButton.WithCallbackData(
                    BotCommands.AdminMenu.CamManagement.CommandsTitle[i], data));
                list.Add(list1);
            }

            var ikm = new InlineKeyboardMarkup(list);
            await bot.AnswerCallbackQueryAsync(update.CallbackQuery.Id, " ");
            await bot.SendTextMessageAsync(update.CallbackQuery.From.Id,
                BotCommands.AdminMenu.CamManagement.TitleString, replyMarkup: ikm);
        }

        private static async Task UserManagmentCreateMenu(Update update)
        {
            var list = new List<List<InlineKeyboardButton>>();
            for (int i = 0; i < BotCommands.AdminMenu.UserManagement.CommandsTitle.Length; i++)
            {
                var list1 = new List<InlineKeyboardButton>();
                BotCommands botCommands = new BotCommands();
                string data = botCommands.MenuCallbackData.CreateCallbackData(
                        (int)BotCommands.AdminMenu.Commands.Null,
                        (int)(BotCommands.AdminMenu.UserManagement.Commands)i,
                        (int)BotCommands.AdminMenu.Commands.Null,
                        String.Empty);
                list1.Add(InlineKeyboardButton.WithCallbackData(
                    BotCommands.AdminMenu.UserManagement.CommandsTitle[i], data));
                list.Add(list1);
            }

            var ikm = new InlineKeyboardMarkup(list);
            await bot.AnswerCallbackQueryAsync(update.CallbackQuery.Id, " ");
            await bot.SendTextMessageAsync(update.CallbackQuery.From.Id,
                BotCommands.AdminMenu.UserManagement.TitleString, replyMarkup: ikm);
        }

        private static async Task HendleCallbackQueryForUser(Update update, int localUserIndex)
        {
            LocalUser.Notification.NotificationDelay notification =
                                (LocalUser.Notification.NotificationDelay)Convert.ToInt32(update.CallbackQuery.Data);
            switch (notification)
            {
                case LocalUser.Notification.NotificationDelay.Timeout5min:
                    {
                        if (BotSetting.Users[localUserIndex].Notifi.notificationStatus
                            == LocalUser.Notification.NotificationStatus.On)
                        {
                            BotSetting.Users[localUserIndex].Notifi.notifycationDelay = notification;
                            await bot.AnswerCallbackQueryAsync(
                            update.CallbackQuery.Id,
                            "Уведомления отключены на 5 минут", true);
                        }
                        else
                        {
                            await bot.AnswerCallbackQueryAsync(
                                update.CallbackQuery.Id, "Уведомления уже выключены", true);
                        }

                    }
                    break;
                case LocalUser.Notification.NotificationDelay.Timeout15min:
                    {
                        if (BotSetting.Users[localUserIndex].Notifi.notificationStatus
                            == LocalUser.Notification.NotificationStatus.On)
                        {
                            BotSetting.Users[localUserIndex].Notifi.notifycationDelay = notification;
                            await bot.AnswerCallbackQueryAsync(
                                update.CallbackQuery.Id,
                                "Уведомления отключены на 15 минут", true);
                        }
                        else
                        {
                            await bot.AnswerCallbackQueryAsync(
                                update.CallbackQuery.Id, "Уведомления уже выключены", true);
                        }
                    }

                    break;
                case LocalUser.Notification.NotificationDelay.Timeout30min:
                    {
                        if (BotSetting.Users[localUserIndex].Notifi.notificationStatus
                            == LocalUser.Notification.NotificationStatus.On)
                        {
                            BotSetting.Users[localUserIndex].Notifi.notifycationDelay = notification;
                            await bot.AnswerCallbackQueryAsync(
                                update.CallbackQuery.Id,
                                "Уведомления отключены на 30 минут", true);
                        }
                        else
                        {
                            await bot.AnswerCallbackQueryAsync(
                                update.CallbackQuery.Id, "Уведомления уже выключены", true);
                        }
                    }

                    break;
                case LocalUser.Notification.NotificationDelay.Timeout60min:
                    {
                        if (BotSetting.Users[localUserIndex].Notifi.notificationStatus
                            == LocalUser.Notification.NotificationStatus.On)
                        {
                            BotSetting.Users[localUserIndex].Notifi.notifycationDelay = notification;
                            await bot.AnswerCallbackQueryAsync(
                                update.CallbackQuery.Id,
                                "Уведомления отключены на 1 час", true);
                        }
                        else
                        {
                            await bot.AnswerCallbackQueryAsync(
                                update.CallbackQuery.Id, "Уведомления уже выключены", true);
                        }
                    }

                    break;
                case LocalUser.Notification.NotificationDelay.Timeout90min:
                    {
                        if (BotSetting.Users[localUserIndex].Notifi.notificationStatus
                            == LocalUser.Notification.NotificationStatus.On)
                        {
                            BotSetting.Users[localUserIndex].Notifi.notifycationDelay = notification;
                            await bot.AnswerCallbackQueryAsync(
                                update.CallbackQuery.Id,
                                "Уведомления отключены на 1,5 часа", true);
                        }
                        else
                        {
                            await bot.AnswerCallbackQueryAsync(
                                update.CallbackQuery.Id, "Уведомления уже выключены", true);
                        }
                    }

                    break;
                case LocalUser.Notification.NotificationDelay.Timeout180min:
                    {
                        if (BotSetting.Users[localUserIndex].Notifi.notificationStatus
                            == LocalUser.Notification.NotificationStatus.On)
                        {
                            BotSetting.Users[localUserIndex].Notifi.notifycationDelay = notification;
                            await bot.AnswerCallbackQueryAsync(
                                update.CallbackQuery.Id,
                                "Уведомления отключены на 3 часа", true);
                        }
                        else
                        {
                            await bot.AnswerCallbackQueryAsync(
                                update.CallbackQuery.Id, "Уведомления уже выключены", true);
                        }
                    }

                    break;
                case LocalUser.Notification.NotificationDelay.Timeout300min:
                    {
                        if (BotSetting.Users[localUserIndex].Notifi.notificationStatus
                            == LocalUser.Notification.NotificationStatus.On)
                        {
                            BotSetting.Users[localUserIndex].Notifi.notifycationDelay = notification;
                            await bot.AnswerCallbackQueryAsync(
                                update.CallbackQuery.Id,
                                "Уведомления отключены на 5 часов", true);
                        }
                        else
                        {
                            await bot.AnswerCallbackQueryAsync(
                                update.CallbackQuery.Id, "Уведомления уже выключены", true);
                        }
                    }

                    break;
                case LocalUser.Notification.NotificationDelay.Timeout600min:
                    {
                        if (BotSetting.Users[localUserIndex].Notifi.notificationStatus
                            == LocalUser.Notification.NotificationStatus.On)
                        {
                            BotSetting.Users[localUserIndex].Notifi.notifycationDelay = notification;
                            await bot.AnswerCallbackQueryAsync(
                               update.CallbackQuery.Id,
                               "Уведомления отключены на 10 часов", true);
                        }
                        else
                        {
                            await bot.AnswerCallbackQueryAsync(
                                update.CallbackQuery.Id, "Уведомления уже выключены", true);
                        }
                    }

                    break;
                case LocalUser.Notification.NotificationDelay.Disable:
                    {
                        if (BotSetting.Users[localUserIndex].Notifi.notificationStatus
                            == LocalUser.Notification.NotificationStatus.On)
                        {
                            BotSetting.Users[localUserIndex].Notifi.notifycationDelay = notification;
                            await bot.AnswerCallbackQueryAsync(
                               update.CallbackQuery.Id,
                               "Уведомления отключены навсегда", true);
                        }
                        else
                        {
                            await bot.AnswerCallbackQueryAsync(
                                update.CallbackQuery.Id, "Уведомления уже выключены", true);
                        }
                    }

                    break;
                case LocalUser.Notification.NotificationDelay.Enable:
                    {
                        if (BotSetting.Users[localUserIndex].Notifi.notificationStatus
                            == LocalUser.Notification.NotificationStatus.Off)
                        {
                            BotSetting.Users[localUserIndex].Notifi.notifycationDelay = notification;
                            await bot.AnswerCallbackQueryAsync(
                                update.CallbackQuery.Id,
                                "Уведомления снова включены", true);
                        }
                        else
                        {
                            await bot.AnswerCallbackQueryAsync(
                                update.CallbackQuery.Id, "Уведомления уже включены", true);
                        }
                    }
                    break;
            }
            await Task.CompletedTask;
        }

        public static async Task HandleMessage(Message msg)
        {
            var user = msg.From;
            var text = msg.Text ?? string.Empty;

            if (user is null)
                return;

            // Print to console
            Console.WriteLine($"{user.FirstName} wrote {text}");

            foreach (LocalUser validuser in BotSetting.Users)
            {

                if (validuser.Id64.Equals(user.Id))
                { // проверка разрешения работы с ботом
                  // When we get a command, we react accordingly
                    if (msg.ForwardFrom != null)
                    {
                        if (validuser.UserRights == LocalUser.UsersRights.Admin)
                        {
                            string answer = "Информация о пользователе:"
                                + "\r\nИмя: " + msg.ForwardFrom.FirstName
                                + "\r\nФамилия: " + msg.ForwardFrom.LastName
                                + "\r\nID: " + msg.ForwardFrom.Id
                                + "\r\nНик: @" + msg.ForwardFrom.Username;
                            AddNewUserData addNewUserData = new AddNewUserData(
                                msg.ForwardFrom.FirstName, msg.ForwardFrom.Id);

                            var list = new List<List<InlineKeyboardButton>>();
                            var list1 = new List<InlineKeyboardButton>();
                            BotCommands botCommands = new BotCommands();
                            string data = botCommands.MenuCallbackData.CreateCallbackData(
                                    (int)BotCommands.AdminMenu.Commands.Null,
                                    (int)BotCommands.AdminMenu.UserManagement.Commands.AddNew,
                                    (int)BotCommands.AdminMenu.UserManagement.AddNew.Commands.Add,
                                    msg.ForwardFrom.FirstName +"|"+ msg.ForwardFrom.Id.ToString());

                            list1.Add(InlineKeyboardButton.WithCallbackData("Добавить пользователя?", data));
                            list.Add(list1);

                            var ikm = new InlineKeyboardMarkup(list);
                            await bot.SendTextMessageAsync(user.Id, answer, replyMarkup: ikm);
                        }
                    }
                    if (text.StartsWith("/"))
                    {
                        if (msg.Chat.Type == ChatType.Private)
                        { // обработка командных сообщений из приватного чата
                            await HandleCommand(msg, text);
                        }
                    }
                    else if (text.Length > 0)
                    {
                        // To preserve the markdown, we attach entities (bold, italic..)
                        if (msg.Chat.Type == ChatType.Private)
                        { // обработка обычных сообщений из приватного чата
                          //await bot.SendTextMessageAsync(user.Id, text.ToUpper(), entities: msg.Entities);
                          //await bot.SendTextMessageAsync(msg.Chat, "Отправьте /help для справки.");
                        }
                    }
                    else
                    {   // This is equivalent to forwarding, without the sender's name
                        //await bot.CopyMessageAsync(user.Id, user.Id, msg.MessageId);
                        //await bot.SendTextMessageAsync(msg.Chat, "Отправьте /help для справки.");
                    }
                }
            }
            await Task.CompletedTask;
        }

        public static async Task HandleCommand(Message msg, string command)
        {
           // BotCommands BotCommands = new BotCommands();
            //BotCommands.AdminMenu.UserManagement.AddNew;
            switch (BotCommands.GetRootCommand(command.ToLower()))
            {
                case BotCommands.RootCommands.Start:
                    {
                        await RootMenuCreateStart(msg.From.Id);
                    }
                    break;

                case BotCommands.RootCommands.Help:
                    {
                        await RootMenuCreateHelp(msg.From.Id);
                    }
                    break;

                case BotCommands.RootCommands.ListMyCam:
                    {
                        await RootMenuCreateShowMyCams(msg.From.Id);
                    }
                    break;
                case BotCommands.RootCommands.Notification:
                    {
                        await RootMenuCreateNotification(msg.From.Id);
                    }
                    break;

                case BotCommands.RootCommands.Cancel:
                    {
                        break;
                    }

                case BotCommands.RootCommands.AdminMenu:
                    {
                        await RootMenuCreateAdminMenu(msg);
                    }
                    break;
                    //case "/config_reload":
                    //    {
                    //        LoadConfig();
                    //        await bot.SendTextMessageAsync(msg.Chat, "Конфигурация перезагружена.");
                    //        break;
                    //    }

                    //case "/upload_file_test":
                    //    {
                    //        const string path = @"\picture_test.jpg";

                    //        using (var fileStream = new FileStream(AssemblyPathLocation + path, FileMode.Open, FileAccess.Read, FileShare.Read))
                    //        {
                    //            await bot.SendPhotoAsync(
                    //                chatId: msg.Chat.Id,
                    //                photo: new Telegram.Bot.Types.InputFiles.InputOnlineFile(fileStream),
                    //                caption: "подпись"
                    //            );

                    //            //await bot.SendPhotoAsync(msg.Chat.Id, new Telegram.Bot.Types.InputFiles.InputOnlineFile(fileStream), "под писька");

                    //            // по другому подписи нет(
                    //            Telegram.Bot.Types.InputFiles.InputOnlineFile iof = new Telegram.Bot.Types.InputFiles.InputOnlineFile(fileStream); //оставляем также 
                    //            iof.FileName = path;
                    //            await bot.SendDocumentAsync(
                    //                chatId: msg.Chat.Id,
                    //                document: iof,
                    //                caption: "подпись!"
                    //            );
                    //        }
                    //        break;
                    //    }
            }

            await Task.CompletedTask;
        }

        private static async Task RootMenuCreateAdminMenu(Message msg)
        {
            foreach (LocalUser user in BotSetting.Users)
            {
                if (user.Id64 == msg.From.Id)
                {
                    if (user.UserRights == LocalUser.UsersRights.Admin)
                    {
                        await AdminMenuCreateMenu(null, msg.Chat.Id);
                    }
                }
            }
        }

        private static async Task RootMenuCreateShowMyCams(long Id)
        {
            string camlist = "Список камер, с которых вам доступны уведомления:\r\n";
            foreach (LocalUser user in BotSetting.Users)
            {
                if (user.Id64 == Id)
                {
                    foreach (int fid in user.SnapshotFoldersID)
                    {
                        camlist += BotSetting.SnapshotFolders[fid].Name + "\r\n";
                    }
                }
            }

            await bot.SendTextMessageAsync(Id, camlist, Telegram.Bot.Types.Enums.ParseMode.Html);
        }

        private static async Task RootMenuCreateNotification(long Id)
        {
            string text = "Настройки уведомлений:\r\n";
            foreach (LocalUser user in BotSetting.Users)
            {
                if (user.Id64 == Id)
                {
                    if (user.Notifi.notificationStatus == LocalUser.Notification.NotificationStatus.Off)
                    {
                        var ikm = new InlineKeyboardMarkup(new[]
                        {
                            new[]
                            {
                                InlineKeyboardButton.WithCallbackData("Включить уведомления",
                                ((Int32)LocalUser.Notification.NotificationDelay.Enable).ToString()),
                            },
                        });

                        await bot.SendTextMessageAsync(Id, text, replyMarkup: ikm);
                    }
                    else if (user.Notifi.notificationStatus == LocalUser.Notification.NotificationStatus.On)
                    {
                        var ikm = new InlineKeyboardMarkup(new[]
                        {
                                        new[]
                                        {
                                            InlineKeyboardButton.WithCallbackData("Отключить на 5 минут",
                                                ((Int32)LocalUser.Notification.NotificationDelay.Timeout5min).ToString()),
                                        },
                                        new[]
                                        {
                                            InlineKeyboardButton.WithCallbackData("Отключить на 15 минут",
                                                ((Int32)LocalUser.Notification.NotificationDelay.Timeout15min).ToString()),
                                        },
                                        new[]
                                        {
                                            InlineKeyboardButton.WithCallbackData("Отключить на 30 минут",
                                                ((Int32)LocalUser.Notification.NotificationDelay.Timeout30min).ToString()),
                                        },
                                        new[]
                                        {
                                            InlineKeyboardButton.WithCallbackData("Отключить на 1 час",
                                                ((Int32)LocalUser.Notification.NotificationDelay.Timeout60min).ToString()),
                                        },
                                        new[]
                                        {
                                            InlineKeyboardButton.WithCallbackData("Отключить на 1,5 часа",
                                                ((Int32)LocalUser.Notification.NotificationDelay.Timeout90min).ToString()),
                                        },
                                        new[]
                                        {
                                            InlineKeyboardButton.WithCallbackData("Отключить на 3 часа",
                                                ((Int32)LocalUser.Notification.NotificationDelay.Timeout180min).ToString()),
                                        },
                                        new[]
                                        {
                                            InlineKeyboardButton.WithCallbackData("Отключить на 5 часов",
                                                ((Int32)LocalUser.Notification.NotificationDelay.Timeout300min).ToString()),
                                        },
                                        new[]
                                        {
                                            InlineKeyboardButton.WithCallbackData("Отключить на 10 часов",
                                                ((Int32)LocalUser.Notification.NotificationDelay.Timeout600min).ToString()),
                                        },
                                        new[]
                                        {
                                            InlineKeyboardButton.WithCallbackData("Отключить навсегда",
                                                ((Int32)LocalUser.Notification.NotificationDelay.Disable).ToString()),
                                        },
                                    });

                        await bot.SendTextMessageAsync(Id, text, replyMarkup: ikm);
                    }
                }
            }
        }

        private static async Task RootMenuCreateHelp(long Id)
        {
            string strBotInfo = BotCommands.BotInfo;
            for (int i = 0; i < BotCommands.sRootCommandInfo.Length - 1; i++)
            {
                strBotInfo += BotCommands.sRootCommand[i] + BotCommands.sRootCommandInfo[i];
            }

            foreach (LocalUser validuser in BotSetting.Users)
            {
                if (validuser.Id64 == Id)
                {
                    if (validuser.UserRights == LocalUser.UsersRights.Admin)
                    {
                        strBotInfo +=
                            BotCommands.sRootCommand[BotCommands.sRootCommand.Length - 1]
                            + BotCommands.sRootCommandInfo[BotCommands.sRootCommand.Length - 1];
                    }
                }
            }
            await bot.SendTextMessageAsync(Id, strBotInfo);
        }

        private static async Task RootMenuCreateStart(long Id)
        {
            await bot.SendTextMessageAsync(Id, "🙃");
            await bot.SendTextMessageAsync(Id, "Отправь /help для справки.");
        }

        private static async Task AdminMenuCreateMenu(string callbackQueryId, long Id)
        {
            var list = new List<List<InlineKeyboardButton>>();

            for (int i = 0; i < BotCommands.AdminMenu.CommandsTitle.Length; i++)
            {
                var list1 = new List<InlineKeyboardButton>();
                BotCommands botCommands = new BotCommands();
                //botCommands.adminMenu.Command = (BotCommands.AdminMenu.Commands)i;
                //JsonSerializerOptions options = new JsonSerializerOptions
                //{
                //    Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
                //    WriteIndented = true,
                //    IncludeFields = true,
                //};
                //Int64 hashCode = botCommands.GetHashCode();
                //BotCommandsCache.GetOrCreate(hashCode, () => botCommands);
                string data = botCommands.MenuCallbackData.CreateCallbackData(
                    (int)(BotCommands.AdminMenu.Commands)i,
                    (int)BotCommands.AdminMenu.Commands.Null,
                    (int)BotCommands.AdminMenu.Commands.Null,
                    String.Empty);
                list1.Add(InlineKeyboardButton.WithCallbackData(
                    BotCommands.AdminMenu.CommandsTitle[i], data));
                list.Add(list1);
            }

            var ikm = new InlineKeyboardMarkup(list);
            if(callbackQueryId != null)
                await bot.AnswerCallbackQueryAsync(callbackQueryId, " ");
            await bot.SendTextMessageAsync(Id, BotCommands.AdminMenu.TitleString, replyMarkup: ikm);
        }

        public static async Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
        {
            // Некоторые действия
            Console.WriteLine(Newtonsoft.Json.JsonConvert.SerializeObject(exception));
            Console.WriteLine(exception.Message);
            //Console.WriteLine(exception.InnerException.Message);

            int ExCode = exception.HResult;
            switch (ExCode)
            {
                case -2146233088:
                    Console.WriteLine("Нет интернета скорее всего...");
                    break;
            }
        }

        class StatusChecker
        {
            public StatusChecker()
            {
            }

            // This method is called by the timer delegate.
            public async void CheckStatus(Object stateInfo)
            {
                AutoResetEvent autoEvent = (AutoResetEvent)stateInfo;


                Console.WriteLine("{0} Checking status.",
                    DateTime.Now.ToString("h:mm:ss.fff"));

                // Reset the counter and signal the waiting thread.
                autoEvent.Set();
            }
        }



        static void Main(string[] args)
        {
            string location = System.Reflection.Assembly.GetEntryAssembly().Location;
            AssemblyPathLocation = System.IO.Path.GetDirectoryName(location);
            Console.WriteLine(AssemblyPathLocation.ToString());

            LoadConfig();

            //BotSetting.BotName = "botname";
            //SaveConfig2();
            bot = new TelegramBotClient(BotSetting.BotToken);

            Console.WriteLine("Запущен бот " + bot.GetMeAsync().Result.FirstName);
            // Create an AutoResetEvent to signal the timeout threshold in the
            // timer callback has been reached.
            var autoEvent = new AutoResetEvent(false);

            var statusChecker = new StatusChecker();

            // Create a timer that invokes CheckStatus after one second, 
            // and every 1/4 second thereafter.
            Console.WriteLine("{0:h:mm:ss.fff} Creating timer.\n",
                              DateTime.Now);
            var stateTimer = new Timer(statusChecker.CheckStatus,
                                       autoEvent, 1000, 60000);
            Timer i34 = new Timer(statusChecker.CheckStatus);
            //i34.

            var cts = new CancellationTokenSource();
            var cancellationToken = cts.Token;
            var receiverOptions = new ReceiverOptions
            {
                AllowedUpdates = { }, // receive all update types
            };


            bot.StartReceiving(
                HandleUpdateAsync,
                HandleErrorAsync//,
                                //  receiverOptions,
                                // cancellationToken
            );

            /////////////////////////////////////////////////////
            CamSnapshotFileWatcher camSnapshotFileWatcher = new CamSnapshotFileWatcher(bot, BotSetting);
            camSnapshotFileWatcher.Start();

            Console.ReadLine();
        }

        private static void LoadConfig()
        {
            using (FileStream openStream = System.IO.File.OpenRead(AssemblyPathLocation + BotConfigFileName))
            {
                BotSetting = JsonSerializer.Deserialize<BotSettings>(openStream);
            }
            //////////////////
            //SnapshotFolder snapshotFolder = new SnapshotFolder();
            //snapshotFolder.id = 0;
            //snapshotFolder.Name = "Cam 1";
            //snapshotFolder.pathfullname = @"D:\Krasnova123\video\Cam1\grabs";
            //BotSettings.SnapshotFolders = new List<SnapshotFolder>();
            //BotSettings.SnapshotFolders.Add(snapshotFolder);
            //SnapshotFolder snapshotFolder1 = new SnapshotFolder();
            //snapshotFolder1.id = 1;
            //snapshotFolder1.Name = "Cam 2";
            //snapshotFolder1.pathfullname = @"D:\Krasnova123\video\Cam2\grabs";
            //BotSettings.SnapshotFolders.Add(snapshotFolder1);

            //LocalUser user = new LocalUser();
            //user.Id64 = 1248170071;
            //user.SnapshotFoldersID = new List<int>();
            //user.SnapshotFoldersID.Add(0);
            //user.SnapshotFoldersID.Add(1);
            //user.UserRights = LocalUser.UsersRights.Superadmin;
            ////User.Notification notification = new User.Notification();
            ////notification.notifycationDelay = User.Notification.NotificationDelay.Enable;
            ////user.Notifi = notification;

            //BotSettings.Users = new List<LocalUser>();
            //BotSettings.Users.Add(user);

            //string jsonString = JsonSerializer.Serialize(BotSettings);

            //System.IO.File.WriteAllText(AssemblyPathLocation + @"\config\example_bot_config.json", jsonString);
        }

        private static void SaveConfig()
        {
            string jsonString = JsonSerializer.Serialize(BotSetting);

            System.IO.File.WriteAllText(AssemblyPathLocation + BotConfigFileName,
                jsonString);
        }

        private static void SaveConfig2()
        {
            string jsonString = JsonSerializer.Serialize(BotSetting);

            System.IO.File.WriteAllText(AssemblyPathLocation + @"\config\"
                + DateTime.Now.ToShortDateString()
                + @"_" + DateTime.Now.ToShortTimeString().Replace(':', '-') + @" example_bot_config.json",
                jsonString);
        }



        public class AddNewUserData
        {
            public string FirstName { get; set; }
            //public string LastName { get; set; }
            public long Id { get; set; }
            // public string Username { get; set; }

            public AddNewUserData(string firstName,/* string lastName,*/ long id/*, string username*/)
            {
                this.FirstName = firstName;
                //this.LastName = lastName;
                this.Id = id;
                //this.Username = username;
            }
        }
        public class Callbackdata
        {
            public enum Commands
            {
                ShowAllUsers = 0,
                AddNewUser,
                DeleteUsers,
                OffNotifiAllUsers,
                OnNotifiAllUsers,
                DeleteUser
            }
            public Commands Command { get; set; }
            public string Data { get; set; }
            public Callbackdata(Commands command, string data)
            {
                this.Command = command;
                this.Data = data;
            }

            //public Callbackdata(int cmd, string data)
            //{
            //    this.Command = (Commands)cmd;
            //    this.Data = data;
            //}
        }

        public class BotSettings
        {

            public string BotName { get; set; }
            public string BotToken { get; set; }

            public List<SnapshotFolder> SnapshotFolders { get; set; }
            public List<LocalUser> Users { get; set; }

            public bool IsValidUser(long id)
            {
                foreach (LocalUser user in Users)
                {
                    if (user.Id64 == id)
                    {
                        return true;
                    }
                }
                return false;
            }
        }

        public class SnapshotFolder
        {
            public int id { get; set; }
            public string Name { get; set; }
            public string pathfullname { get; set; }
        }

        public class LocalUsers
        {
            ObservableCollection<LocalUser> UserList = new ObservableCollection<LocalUser>();
            public bool IsValidUser(long id)
            {
                foreach (LocalUser user in UserList)
                {
                    if (user.Id64 == id)
                    {
                        return true;
                    }
                }
                return false;
            }

            public bool IsAdmin(long id)
            {
                foreach (LocalUser user in UserList)
                {
                    if (user.Id64 == id)
                    {
                        if (user.UserRights == LocalUser.UsersRights.Admin)
                        {
                            return true;
                        }
                    }
                }
                return false;
            }
        }

        public class LocalUser
        {

            public Int64 Id64 { get; set; }
            public string Name { get; set; }
            public UsersRights UserRights { get; set; }
            public List<int> SnapshotFoldersID { get; set; }
            public Notification Notifi = new Notification();

            public enum UsersRights
            {
                User = 0,
                Admin
            }


            public class Notification
            {
                private NotificationDelay _notificationDelay = NotificationDelay.Enable;
                private NotificationStatus _notificationStatus = NotificationStatus.On;

                //private NotificationDelay _oldnotificationDelay = NotificationDelay.Enable;
                //private NotificationStatus _oldnotificationStatus = NotificationStatus.On;

                private Timer _timer = null;
                public enum NotificationDelay
                {
                    [Description("Sunday")]
                    Timeout5min = 0,
                    Timeout15min,
                    Timeout30min,
                    Timeout60min,
                    Timeout90min,  // 1.5 hour
                    Timeout180min, // 3 hour
                    Timeout300min, // 5 hour
                    Timeout600min, // 10 hour
                    Disable,
                    Enable
                }


                public enum NotificationStatus
                {
                    On = 0,
                    Off
                }
                //[JsonIgnore]
                public NotificationDelay notifycationDelay
                {
                    get
                    {
                        return _notificationDelay;
                    }
                    set
                    {
                        _notificationDelay = value;
                        if (_notificationDelay == NotificationDelay.Enable)
                        {
                            if (_timer != null)
                            {
                                _timer.Dispose();
                                _notificationStatus = NotificationStatus.On;
                                //bot.SendTextMessageAsync(Id64, "Конфигурация перезагружена.");
                            }
                        }
                        else if (_notificationDelay == NotificationDelay.Disable)
                        {
                            _notificationStatus = NotificationStatus.Off;
                        }
                        else
                        {
                            const long oneminut = 1000 * 60;
                            long delay = 0;

                            switch (_notificationDelay)
                            {
                                case NotificationDelay.Timeout5min:
                                    {
                                        _notificationStatus = NotificationStatus.Off;
                                        delay = oneminut * 5;
                                    }
                                    break;
                                case NotificationDelay.Timeout15min:
                                    {
                                        _notificationStatus = NotificationStatus.Off;
                                        delay = oneminut * 15;
                                    }
                                    break;
                                case NotificationDelay.Timeout30min:
                                    {
                                        _notificationStatus = NotificationStatus.Off;
                                        delay = oneminut * 30;
                                    }
                                    break;
                                case NotificationDelay.Timeout60min:
                                    {
                                        _notificationStatus = NotificationStatus.Off;
                                        delay = oneminut * 60;
                                    }
                                    break;
                                case NotificationDelay.Timeout90min:
                                    {
                                        _notificationStatus = NotificationStatus.Off;
                                        delay = oneminut * 90;
                                    }
                                    break;
                                case NotificationDelay.Timeout180min:
                                    {
                                        _notificationStatus = NotificationStatus.Off;
                                        delay = oneminut * 180;
                                    }
                                    break;
                                case NotificationDelay.Timeout300min:
                                    {
                                        _notificationStatus = NotificationStatus.Off;
                                        delay = oneminut * 300;
                                    }
                                    break;
                                case NotificationDelay.Timeout600min:
                                    {
                                        _notificationStatus = NotificationStatus.Off;
                                        delay = oneminut * 600;
                                    }
                                    break;
                                default:
                                    {
                                        _notificationStatus = NotificationStatus.Off;
                                        delay = oneminut * 5;
                                    }
                                    break;
                            }
                            NotifyCreateTimer(delay);
                        }
                    }
                }


                //[JsonIgnore]
                public NotificationStatus notificationStatus
                {
                    get
                    {
                        return _notificationStatus;
                    }
                }

                private void NotifyCreateTimer(long delay)
                {
                    _timer = new Timer(TimerOnEvent, null, delay, 0);
                }
                private void TimerOnEvent(Object o)
                {
                    _timer.Dispose();
                    _notificationStatus = NotificationStatus.On;
                    _notificationDelay = NotificationDelay.Enable;
                }

            }
            //    public static string GetAttributeDescription(this Enum enumValue)
            //    {
            //        var attribute = enumValue.GetAttributeOfType<DescriptionAttribute>();
            //        return attribute == null ? String.Empty : attribute.Description;
            //    }
        }
    }  
}

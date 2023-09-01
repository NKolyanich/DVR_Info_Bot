using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;
using static Metaler.DVR_Info_Bot.Program;
using Telegram.Bot.Types.ReplyMarkups;

namespace Metaler.DVR_Info_Bot
{
    public class BotCommands
    {
        public class AdminMenuCallbackData
        {
            private const int CallbackDataSubstringCount = 4;
            public int OneLevelSubmenuItemIndex { get; set; }
            public int TwoLevelSubmenuItemIndex { get; set; }
            public int ThreeLevelSubmenuItemIndex { get; set; }
            public string Data { get; set; }

            public string CreateCallbackData(int _rootItemIndex, int _1levelSubmenuItemIndex, int _2levelSubmenuItemIndex, string data)
            {
                string result = string.Empty;
                result = String.Format("{0}:{1}:{2}:{3}",
                    _rootItemIndex, _1levelSubmenuItemIndex, _2levelSubmenuItemIndex, data);
                return result;
            }
            public bool CreateObject(string callbackData)
            {
                string[] data = callbackData.Split(':');
                if(data.Length == CallbackDataSubstringCount)
                {
                    OneLevelSubmenuItemIndex = Convert.ToInt32(data[0]);
                    TwoLevelSubmenuItemIndex = Convert.ToInt32(data[1]);
                    ThreeLevelSubmenuItemIndex = Convert.ToInt32(data[2]);
                    Data = data[3];
                    return true;
                }
                return false;
            }
        }

        public AdminMenuCallbackData MenuCallbackData = new AdminMenuCallbackData();

        public BotCommands ()
        {
            RootCommand = RootCommands.Null;
        }
        public AdminMenu adminMenu = new AdminMenu();

        public RootCommands RootCommand { get; set; }
        public enum RootCommands
        {
            Null = 255,
            Start = 0,
            Help,
            ListMyCam,
            Notification,
            Cancel,
            AdminMenu
        }
        
        public static RootCommands GetRootCommand(string command)
        {
            for(int i=0; i<sRootCommand.Length; i++)
            {
                if(sRootCommand[i].Equals(command))
                    return (RootCommands)i;
            }
            return RootCommands.Null;
        }
        [JsonIgnore]
        public static readonly string[] sRootCommand =
        {
            "/start",
            "/help",
            "/list_my_cam",
            "/notification",
            "/cancel",
            "/admin_menu"
        };
        [JsonIgnore]
        public static readonly string[] sRootCommandInfo =
        {
            " - начало работы\r\n",
            " - справка\r\n",
            " - вывод списка доступных камер\r\n",
            " - настройка уведомлений\r\n",
            " - прервать текущую операцию\r\n",
            " - меню команд админа\r\n"
        };
        [JsonIgnore]
        public static readonly string BotInfo = "Я - бот-информер видеонаблюдения.\r\n"
                            + "Присылаю фото при срабатывании детектора движения камер видеонаблюдения.\r\n"
                            + "Мои команды:\r\n";
        

        public class AdminMenu
        {
            public AdminMenu()
            {
                Command = Commands.Null;
            }
            public Commands Command { get; set; }

            [JsonIgnore]
            public static readonly string TitleString = "Меню администратора:";

            public enum Commands
            {
                Null = 255,
                UserManagment = 0,
                CamManagment,
                Back
            }

            [JsonIgnore]
            public static readonly string[] CommandsTitle =
            {
                "Управление пользователями",
                "Управление камерами",
                "<<Назад"
            };

            public UserManagement userManagement = null;// = new UserManagement();
            public CamManagement camManagement = null;// = new CamManagement();

            public class UserManagement
            {
                public UserManagement()
                {
                    Command = Commands.Null;
                }
                public Commands Command { get; set; }

                [JsonIgnore]
                public static readonly string TitleString = "Управление пользователями:";
                public enum Commands
                {
                    Null = 255,
                    GetAll = 0,
                    AddNew,
                    Change,
                    Delete,
                    Notifi,
                    Back
                }

                [JsonIgnore]
                public static readonly string[] CommandsTitle = 
                {
                    "Список юзеров",
                    "Добавить юзера",
                    "Редактировать юзера",
                    "Удалить юзера",
                    "Управление уведомлениями",
                    "<<Назад"
                };

                public class GetAll
                {
                    public static readonly string TitleString = "Список юзеров:";
                }


                public class AddNew
                {
                    [JsonIgnore]
                    public static readonly string TitleString = "Для добавления юзера перешлите любое сообщение от него боту и следуйте инструкциям.";
                    public enum Commands
                    {
                        Null = 255,
                        Add = 0,
                        Back
                    }

                    [JsonIgnore]
                    public static readonly string[] CommandsTitle =
                    {
                        "Добавить юзера",
                        "<<Назад"
                    };

                    [JsonIgnore]
                    public static readonly string UserInfoString = "Информация о пользователе:";
                    [JsonIgnore]
                    public static readonly string UserAddedString = "Юзер добавлен: ";
                    [JsonIgnore]
                    public static readonly string UserAddedEmodzi = " 🫡"; 
                }

                public class Delete
                {
                    [JsonIgnore]
                    public static readonly string TitleString = "Выберите юзера для удаления:";
                    public enum Commands
                    {
                        Null = 255,
                        Delete = 0,
                        Back
                    }

                    [JsonIgnore]
                    public static readonly string UserDeleteString = "Пользователь удалён:";
                    [JsonIgnore]
                    public static readonly string UserDeleteNotFoundString = "Такой пользователь отсутствует.";
                    [JsonIgnore]
                    public static readonly string UserDeleteSelfErrorString = "Самого себя удалить нельзя 🤷‍♂️";
                    [JsonIgnore]
                    public static readonly string UserDeleteEmodzi = "   ❌";
                }

                public class NotifiManagement
                {
                    public NotifiManagement()
                    {
                        Command = Commands.Null;
                    }
                    public Commands Command { get; set; }

                    [JsonIgnore]
                    public static readonly string TitleString = "Управление уведомлениями:";
                    public enum Commands
                    {
                        Null = 255,
                        Enable = 0,
                        //Timeout5min,
                        //Timeout15min,
                        //Timeout30min,
                        //Timeout60min,
                        //Timeout90min,
                        //Timeout180min,
                        //Timeout300min,
                        //Timeout600min,
                        Disable,
                        Back
                    }

                    [JsonIgnore]
                    public static readonly string[] CommandsTitle =
                    {
                    "Включить уведомления",
                    //"Отключить на 5 минут",
                    //"Отключить на 15 минут",
                    //"Отключить на 30 минут",
                    //"Отключить на 1 час",
                    //"Отключить на 1,5 часа",
                    //"Отключить на 3 часа",
                    //"Отключить на 5 часов",
                    //"Отключить на 10 часов",
                    "Отключить навсегда",
                    "<<Назад"
                };
                }
            }

            public class CamManagement
            {
                public CamManagement()
                {
                    Command = Commands.Null;
                }
                public Commands Command { get; set; }

                [JsonIgnore]
                public static readonly string TitleString = "Управление камерами:";
                public enum Commands
                {
                    Null = 255,
                    GetAll = 0,
                    AddNew,
                    Change,
                    Delete,
                    Notifi,
                    Back
                }

                [JsonIgnore]
                public static readonly string[] CommandsTitle =
                {
                    "Список камер",
                    "Добавление камеры",
                    "Выберите камеру для изменения",
                    "Выберите камеру для удаления",
                    "Управление уведомлениями",
                    "<<Назад"
                };
            }

            
        }

    }
}

using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using Telegram.Bot;
using Telegram.Bot.Types;
using TekegramBotRent.Models;
using TekegramBotRent;
using System.Linq;
using dotenv.net;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;
using Update = Telegram.Bot.Types.Update;
using System.Collections;


namespace TelegramBotRent
{
    internal class Program
    {
        static void Main(string[] args)
        {
            DotEnv.Load(options: new DotEnvOptions(envFilePaths: new[] { "C:\\Users\\Эмиль\\source\\repos\\entity\\.env" }));
            string token = Environment.GetEnvironmentVariable("TELEGRAM_BOT_TOKEN");
            var host = new Host(token);
            host.Start();
            host.OnMessage += Start;
            Console.ReadLine();
        }
        private static async void Start(ITelegramBotClient client, Update update)
        {
            switch (update.Type)
            {
                case UpdateType.Message:

                    switch (update.Message.Text)
                    {
                        case "/start":
                            using (var db = new RentbotContext())
                            {
                                var user = db.Users.FirstOrDefault(x => x.Id == update.Message.Chat.Id.ToString());
                                if (user == null)
                                {
                                    ReplyKeyboardMarkup replyKeyboard = new ReplyKeyboardMarkup(new KeyboardButton("Согласен") { RequestContact = true })
                                    {
                                        ResizeKeyboard = true,
                                        OneTimeKeyboard = true
                                    };
                                    await client.SendTextMessageAsync(update.Message.Chat.Id, "Перед началом нужно принять соглашение на обработку персональных данных", replyMarkup: replyKeyboard);
                                }
                                else 
                                {
                                    if (user.Username.Trim() != update.Message.Chat.Username)
                                    {
                                        user.Username = update.Message.Chat.Username;
                                        db.SaveChanges();
                                    }
                                    await client.SendTextMessageAsync(update.Message.Chat.Id, "С возвращением, я рад продолжить свою работу", replyMarkup: InlineKeyboardMarkupGenerator("Продолжить", "main_menu_callback"));
                                }
                                return;
                            }
                        default: //регистрация пользователя
                            if (update.Message?.Contact != null)
                            {
                                using (RentbotContext db = new RentbotContext())
                                {
                                    var user = new TekegramBotRent.Models.User()
                                    {
                                        Id = update.Message.Chat.Id.ToString(),
                                        Username = update.Message.Chat.Username,
                                        Contact = update.Message?.Contact.PhoneNumber.ToString()
                                    };
                                    db.Users.Add(user);
                                    db.SaveChanges();
                                }
                                await client.SendTextMessageAsync(update.Message.Chat.Id, $"Отлично, давай начнём работу", replyMarkup: InlineKeyboardMarkupGenerator("Начать", "main_menu_callback"));
                                return;
                            }
                            await client.SendTextMessageAsync(update.Message.Chat.Id, "Я ещё не знаю как ответить на эту команду");
                            return;
                    }
                case UpdateType.CallbackQuery:
                    if (update.CallbackQuery.Data.StartsWith("disable_")) //отключение квартиры 
                    {
                        string flatId = update.CallbackQuery.Data.Replace("disable_", "").Trim();
                        FlatStatus(false, flatId, update, client);
                    }
                    else if (update.CallbackQuery.Data.StartsWith("active_")) //включение квартиры
                    {
                        string flatId = update.CallbackQuery.Data.Replace("active_", "").Trim();
                        FlatStatus(true, flatId, update, client);
                    }
                    else if (update.CallbackQuery.Data.StartsWith("confirm_")) //подтверждение брони квартиры
                    {
                        string rentId = update.CallbackQuery.Data.Replace("confirm_", "").Trim();
                        RentConfirm(client, update, true, rentId);
                    }
                    else if (update.CallbackQuery.Data.StartsWith("reason_cancel_")) //выбор причины отмены бронирования квартиры со стороны арендодателя
                    {

                        string rentId = update.CallbackQuery.Data.Replace("reason_cancel_", "").Trim();
                        InlineKeyboardMarkup inline = new(new[]
{
                                new []
                                {
                                    InlineKeyboardButton.WithCallbackData(text: "Поменялись планы", callbackData: $"cancel_a_{rentId}"),
                                },
                                new []
                                {
                                    InlineKeyboardButton.WithCallbackData(text: "Квартира сдана другому", callbackData: $"cancel_b_{rentId}"),
                                },
                                new []
                                {
                                    InlineKeyboardButton.WithCallbackData(text: "Проблемы в квартире", callbackData: $"cancel_c_{rentId}"),
                                },
                                new []
                                {
                                    InlineKeyboardButton.WithCallbackData(text: "Назад", callbackData: "reservations_"),
                                },
                            });
                        await client.EditMessageTextAsync(update.CallbackQuery.Message.Chat.Id, update.CallbackQuery.Message.MessageId, "Выберите причину отмены", replyMarkup: inline);
                    }
                    else if (update.CallbackQuery.Data.StartsWith("cancel_")) //отмена квартиры со сторны арендодателя
                    {
                        string reasonWithId = update.CallbackQuery.Data.Replace("cancel_", "").Trim();
                        if (reasonWithId.StartsWith("a"))
                        {
                            var rentId = reasonWithId.Replace("a_", "");
                            RentConfirm(client, update, false, rentId, "Поменялись планы");
                        }
                        else if (reasonWithId.StartsWith("b"))
                        {
                            var rentId = reasonWithId.Replace("b_", "");
                            RentConfirm(client, update, false, rentId, "Квартира сдана другому");
                        }
                        else if (reasonWithId.StartsWith("c"))
                        {
                            var rentId = reasonWithId.Replace("c_", "");
                            RentConfirm(client, update, false, rentId, "Проблемы в квартире");
                        }
                    }
                    else if (update.CallbackQuery.Data.StartsWith("tenantReason_cancel_")) //выбор причины отмены бронирования квартиры со стороны арендатора
                    {
                        string rentId = update.CallbackQuery.Data.Replace("tenantReason_cancel_", "").Trim();
                        InlineKeyboardMarkup inline = new(new[]
{
                                new []
                                {
                                    InlineKeyboardButton.WithCallbackData(text: "Поменялись планы", callbackData: $"tenantCancel_a_{rentId}"),
                                },
                                new []
                                {
                                    InlineKeyboardButton.WithCallbackData(text: "Заболел", callbackData: $"tenantCancel_b_{rentId}"),
                                },
                                new []
                                {
                                    InlineKeyboardButton.WithCallbackData(text: "Нашли другую квартиру", callbackData: $"tenantCancel_c_{rentId}"),
                                },
                                new []
                                {
                                    InlineKeyboardButton.WithCallbackData(text: "Назад", callbackData: $"back_reservation_{rentId}"),
                                },
                            });
                        await client.EditMessageTextAsync(update.CallbackQuery.Message.Chat.Id, update.CallbackQuery.Message.MessageId, "Выберите причину отмены", replyMarkup: inline);
                    }
                    else if (update.CallbackQuery.Data.StartsWith("tenantCancel_"))//отмена квартиры арендатором
                    {
                        string reasonWithId = update.CallbackQuery.Data.Replace("tenantCancel_", "").Trim();
                        if (reasonWithId.StartsWith("a"))
                        {
                            var rentId = reasonWithId.Replace("a_", "");
                            TenantCancelBook(client, update, rentId, "Поменялись планы");
                        }
                        else if (reasonWithId.StartsWith("b"))
                        {
                            var rentId = reasonWithId.Replace("b_", "");
                            TenantCancelBook(client, update, rentId, "Заболел");
                        }
                        else if (reasonWithId.StartsWith("c"))
                        {
                            var rentId = reasonWithId.Replace("c_", "");
                            TenantCancelBook(client, update, rentId, "Нашли другую квартиру");
                        }
                    }
                    else if (update.CallbackQuery.Data.StartsWith("back_reservation_")) //вернуться назад при выборе причины отмены бронирования со стороны арендатора 
                    {
                        string resId = update.CallbackQuery.Data.Replace("back_reservation_", "").Trim();
                        Rent res;
                        using (var db = new RentbotContext())
                        {
                            res = db.Rents.FirstOrDefault(rent => rent.Id == resId);
                            var status = res.IsCanceledOwner.Value ? "Заявка отменена арендодателем" : res.IsCanceledTenant.Value ? "Вы отменили заявку" : res.IsConfirmed.Value ? $"Заявка подтверждена\nКонтактная информация:\n@{db.Users.FirstOrDefault(user => user.Id == db.Flats.FirstOrDefault(flat => flat.Id == res.FlatId).OwnerId).Username}\n{db.Users.FirstOrDefault(user => user.Id == db.Flats.FirstOrDefault(flat => flat.Id == res.FlatId).OwnerId).Contact}" : "Заявка не подтверждена";
                            await client.EditMessageTextAsync(update.CallbackQuery.Message.Chat.Id, update.CallbackQuery.Message.MessageId, $"{ReturnRentText(res, db.Flats.FirstOrDefault(x => x.Id == res.FlatId))}\nСтатус: {status}", replyMarkup: res.IsCanceledOwner.Value ? null : InlineKeyboardMarkupGenerator("Отменить", $"tenantReason_cancel_{res.Id}"));
                        }
                    }
                    else if (update.CallbackQuery.Data.StartsWith("back_rent_")) //вернуться назад при выборе причины отмены бронирования со стороны арендодателя
                    {
                        string rentId = update.CallbackQuery.Data.Replace("back_rent_", "").Trim();
                        Rent rent;
                        using (var db = new RentbotContext())
                        {
                            rent = db.Rents.FirstOrDefault(rent => rent.Id == rentId);
                            var status = rent.IsCanceledTenant == true ? "Заявка отменена арендатором" : rent.IsCanceledOwner.Value ? "Вы отменили заявку" : rent.IsConfirmed.Value ? $"Заявка подтверждена\nКонтактная информация:\n@{db.Users.FirstOrDefault(user => user.Id == db.Flats.FirstOrDefault(flat => flat.Id == rent.FlatId).OwnerId).Username}\n{db.Users.FirstOrDefault(user => user.Id == db.Flats.FirstOrDefault(flat => flat.Id == rent.FlatId).OwnerId).Contact}" : "Заявка не подтверждена";
                            await client.EditMessageTextAsync(update.CallbackQuery.Message.Chat.Id, update.CallbackQuery.Message.MessageId, $"{ReturnRentText(rent, db.Flats.FirstOrDefault(x => x.Id == rent.FlatId))}\nСтатус: {status}", replyMarkup: rent.IsCanceledOwner.Value ? null : InlineKeyboardMarkupGenerator("Отменить", $"tenantReason_cancel_{rent.Id}"));
                        }

                    }
                    else if (update.CallbackQuery.Data.StartsWith("reservations_"))//просмотр броней квартиры, как арендодатель
                    {
                        string flatId = update.CallbackQuery.Data.Replace("reservations_", "").Trim();
                        using (RentbotContext db = new RentbotContext())
                        {
                            List<Rent> rents = db.Rents.Where(rent => rent.FlatId == flatId && !rent.IsCanceledOwner.Value).ToList();
                            if (rents.Count == 0)
                            {
                                await client.EditMessageTextAsync(update.CallbackQuery.Message.Chat.Id, update.CallbackQuery.Message.MessageId, "Броней нет", replyMarkup: InlineKeyboardMarkupGenerator("Назад", $"back_callback_{flatId}"));
                            }
                            else
                            {
                                foreach (var rent in rents)
                                {
                                    if (DateTime.Parse(rent.DepartureDate.ToString()) < DateTime.Now.Date)
                                        continue;
                                    InlineKeyboardMarkup rentInlineWithConfirm = new(new[]
                                    {
                                                new []
                                                {
                                                    InlineKeyboardButton.WithCallbackData(text: "Подтвердить", callbackData : $"confirm_{rent.Id}"),
                                                    InlineKeyboardButton.WithCallbackData(text: "Отменить", callbackData : $"reason_cancel_{rent.Id}"),
                                                }
                                            }); ;

                                    var status = rent.IsCanceledTenant == true ? "Заявка отменена арендатором" : rent.IsCanceledOwner.Value ? "Вы отменили заявку" : rent.IsConfirmed.Value ? $"Заявка подтверждена\nКонтактная информация:\n@{db.Users.FirstOrDefault(user => user.Id == db.Flats.FirstOrDefault(flat => flat.Id == rent.FlatId).OwnerId).Username}\n{db.Users.FirstOrDefault(user => user.Id == db.Flats.FirstOrDefault(flat => flat.Id == rent.FlatId).OwnerId).Contact}" : "Заявка не подтверждена";
                                    await client.SendTextMessageAsync(update.CallbackQuery.Message.Chat.Id, $"{ReturnRentText(rent, db.Flats.FirstOrDefault(x => x.Id == rent.FlatId))}\nСтатус: {status}", replyMarkup: rent.IsCanceledTenant.Value || rent.IsCanceledOwner.Value ? null : rent.IsConfirmed.Value ? InlineKeyboardMarkupGenerator("Отменить", $"reason_cancel_{rent.Id}") : rentInlineWithConfirm);
                                }
                                await client.SendTextMessageAsync(update.CallbackQuery.Message.Chat.Id, "Назад к квартирам?", replyMarkup: InlineKeyboardMarkupGenerator("Назад", $"my_flats"));
                            }
                        }
                    }

                    else if (update.CallbackQuery.Data.StartsWith("back_callback_"))//вернуться назад к квартире при просмотре её бронирований 
                    {
                        string flatId = update.CallbackQuery.Data.Replace("back_callback_", "").Trim();
                        using (var db = new RentbotContext())
                        {
                            var flat = db.Flats.FirstOrDefault(x => x.Id == flatId);
                            FlatStatus(flat.IsActive.Value, flatId, update, client);
                        }

                    }
                    else if (update.CallbackQuery.Data.StartsWith("book_callback_"))//отправка запроса на бронирование арендодателю 
                    {
                        string flatId = update.CallbackQuery.Data.Replace("book_callback_", "").Trim();
                        RentTableFill(client, update.CallbackQuery.Message.Chat.Id.ToString(), flatId.Trim());
                        await client.EditMessageTextAsync(update.CallbackQuery.Message.Chat.Id, update.CallbackQuery.Message.MessageId, "Запрос о бронировании отправлен, вы получите ответ после его рассмотрения арендодателем.", replyMarkup: InlineKeyboardMarkupGenerator("Мои брони", "my_reservations"));
                    }

                    switch (update.CallbackQuery?.Data)
                    {
                        case "main_menu_callback": //главное меню
                            HandleMainMenu(client, update);
                            return;
                        case "post_callback": //разместить объявление
                            await client.EditMessageTextAsync(update.CallbackQuery.Message.Chat.Id, update.CallbackQuery.Message.MessageId, $"Для того, чтобы разместить объявление требуется заполнить Яндекс форму:\nhttps://forms.yandex.ru/u/6647381a84227c3b5b63f261/?person_id={update.CallbackQuery.Message.Chat.Id}", replyMarkup: InlineKeyboardMarkupGenerator("Назад", "main_menu_callback"));
                            return;
                        case "search_callback": //поиск объявлений
                            await client.EditMessageTextAsync(update.CallbackQuery.Message.Chat.Id, update.CallbackQuery.Message.MessageId, $"Для того, чтобы выполнить поиск требуется заполнить фильтры  с помощью Яндекс формы:\nhttps://forms.yandex.ru/u/6653849ad04688b8f02bd2f7/?person_id={update.CallbackQuery.Message.Chat.Id}", replyMarkup: InlineKeyboardMarkupGenerator("Назад", "main_menu_callback"));
                            return;
                        case "save": //сохранение квартиры в базу данных
                            Dictionary<string, string> flatInfo = new Dictionary<string, string>();
                            string[] lines = update.CallbackQuery.Message.Text.Split('\n');
                            lines = lines.Where(x => !string.IsNullOrEmpty(x)).ToArray();
                            for (int i = 0; i < lines.Length; i += 2)
                            {
                                if (lines[i].TrimEnd(':') == "Подробное описание")
                                {
                                    int endIndex = FindIndexInArray(lines, lines.Skip(i + 1).FirstOrDefault(x => x.EndsWith(':'), " "));
                                    flatInfo[lines[i].TrimEnd(':')] = string.Join("\n", lines, i + 1, endIndex - i - 1);
                                    i = endIndex - 2;
                                }
                                else
                                    flatInfo[lines[i].TrimEnd(':')] = lines[i + 1].Trim();
                            }
                            FlatsTableFill(flatInfo, update.CallbackQuery.Message.Chat.Id.ToString());
                            HandleMainMenu(client, update);
                            return;
                        case "my_flats": //мои квартиры
                            var myFlats = FlatsReturn(update.CallbackQuery.Message.Chat.Id.ToString());
                            if (myFlats.Count == 0) 
                            {
                                await client.EditMessageTextAsync(update.CallbackQuery.Message.Chat.Id, update.CallbackQuery.Message.MessageId, "Квартиры не найдены", replyMarkup: InlineKeyboardMarkupGenerator("Назад", "main_menu_callback"));
                                return;
                            }
                            else
                            {
                                foreach (var flat in myFlats)
                                {
                                    InlineKeyboardMarkup disableInline = new(new[]
                                    {
                                                new []
                                                {
                                                    InlineKeyboardButton.WithCallbackData(text: "Отключить", callbackData : $"disable_{flat.Id}"),
                                                    InlineKeyboardButton.WithCallbackData(text: "Посмотреть бронирования", callbackData : $"reservations_{flat.Id}"),
                                                }
                                            }); ;
                                    InlineKeyboardMarkup activeInline = new(new[]
                                    {
                                                new []
                                                {
                                                    InlineKeyboardButton.WithCallbackData(text: "Включить", callbackData : $"active_{flat.Id}"),
                                                    InlineKeyboardButton.WithCallbackData(text: "Посмотреть бронирования", callbackData : $"reservations_{flat.Id}"),
                                                }
                                            });
                                    await client.SendTextMessageAsync(update.CallbackQuery.Message.Chat.Id, FlatTextReturn(flat), parseMode: ParseMode.Markdown, replyMarkup: flat.IsActive == true ? disableInline : activeInline);
                                }
                                await client.SendTextMessageAsync(update.CallbackQuery.Message.Chat.Id, "Вернуться в главное меню?", parseMode: ParseMode.Markdown, replyMarkup: InlineKeyboardMarkupGenerator("Да", "main_menu_callback"));

                            }
                            return;
                        case "apply": //подвердить фильтры для поиска квартир 
                            Dictionary<string, string> searchInfo = new Dictionary<string, string>();
                            string[] searchLines = update.CallbackQuery.Message.Text.Split('\n');
                            searchLines = searchLines.Where(x => !string.IsNullOrEmpty(x)).ToArray();
                            for (int i = 0; i < searchLines.Length; i += 2)
                            {
                                searchInfo[searchLines[i].TrimEnd(':')] = searchLines[i + 1].Trim();
                            }
                            var searchFlats = SearchFlatWithFilters(searchInfo);
                            if (searchFlats.Count == 0)
                            {
                                await client.EditMessageTextAsync(update.CallbackQuery.Message.Chat.Id, update.CallbackQuery.Message.MessageId, "Квартиры с заданными фильтрами не найдены", parseMode: ParseMode.Markdown, replyMarkup: InlineKeyboardMarkupGenerator("Применить другие фильтры", "search_callback"));
                                return;
                            }
                            SearchSession searchSession = new SearchSession()
                            {
                                Id = GenerateShortGuid(),
                                TenantId = update.CallbackQuery.Message.Chat.Id.ToString(),
                                Dates = searchInfo["Даты"]
                            };
                            await client.DeleteMessageAsync(update.CallbackQuery.Message.Chat.Id, update.CallbackQuery.Message.MessageId);
                            using (RentbotContext db = new RentbotContext())
                            {
                                foreach (var flat in searchFlats)
                                {
                                    var tenant = db.Tenants.FirstOrDefault(x => x.Id == update.CallbackQuery.Message.Chat.Id.ToString());
                                    if (tenant == null)
                                    {
                                        tenant = new Tenant()
                                        {
                                            Id = update.CallbackQuery.Message.Chat.Id.ToString()
                                        };
                                        db.Tenants.Add(tenant);
                                    }
                                    if (db.SearchSessions.FirstOrDefault(x => x.Id == searchSession.Id) == null) 
                                        db.SearchSessions.Add(searchSession);
                                    db.SaveChanges();

                                    await client.SendTextMessageAsync(update.CallbackQuery.Message.Chat.Id, FlatTextReturn(flat), parseMode: ParseMode.Markdown, replyMarkup: InlineKeyboardMarkupGenerator("Забронировать", $"book_callback_{flat.Id}"));

                                }
                            }
                            return;
                        case "my_reservations": //мои бронирования, как арендатора
                            using (RentbotContext db = new RentbotContext())
                            {
                                var reservations = db.Rents.Where(rent => rent.TenantId.Trim() == update.CallbackQuery.Message.Chat.Id.ToString()).ToList();
                                if (reservations.Count == 0) 
                                {
                                    await client.EditMessageTextAsync(update.CallbackQuery.Message.Chat.Id, update.CallbackQuery.Message.MessageId,"Вы не бронировали квартиры", replyMarkup: InlineKeyboardMarkupGenerator("Назад", "main_menu_callback"));

                                }
                                foreach (var res in reservations)
                                {
                                    var status = res.IsCanceledOwner.Value ? "Заявка отменена арендодателем" : res.IsCanceledTenant.Value ? "Вы отменили заявку": res.IsConfirmed.Value ? $"Заявка подтверждена\nКонтактная информация:\n@{db.Users.FirstOrDefault(user => user.Id == db.Flats.FirstOrDefault(flat => flat.Id == res.FlatId).OwnerId).Username}\n{db.Users.FirstOrDefault(user => user.Id == db.Flats.FirstOrDefault(flat => flat.Id == res.FlatId).OwnerId).Contact}" : "Заявка не подтверждена";

                                    await client.SendTextMessageAsync(update.CallbackQuery.Message.Chat.Id, $"{ReturnRentText(res, db.Flats.FirstOrDefault(x => x.Id == res.FlatId))}\nСтатус: {status}", replyMarkup: res.IsCanceledOwner.Value || res.IsCanceledTenant.Value? null : InlineKeyboardMarkupGenerator("Отменить", $"tenantReason_cancel_{res.Id}"));

                                }

                            }
                            return;

                    }
                    return;
            };
        }

        private static async void RentConfirm(ITelegramBotClient client, Update update, bool isConfirm, string rentId, string reasonCancel = "Не указана")//подтверждение бронирования
        {
            using (var db = new RentbotContext())
            {
                var rent = db.Rents.FirstOrDefault(x => x.Id == rentId);
                var currentRents = db.Rents.Where(x => x.FlatId == rent.FlatId && x.IsConfirmed.Value);
                foreach (var currentRent in currentRents) 
                {
                    if (!(rent.DepartureDate < currentRent.ArrivalDate || rent.ArrivalDate > currentRent.DepartureDate)) 
                    {
                        await client.EditMessageTextAsync(update.CallbackQuery.Message.Chat.Id, update.CallbackQuery.Message.MessageId, "Подтвердить бронь невозможно, уже имеется бронирование в этот диапазон дат", replyMarkup: InlineKeyboardMarkupGenerator("Назад", $"back_rent_{rentId}"));
                        return;
                    }
                }
                rent.IsConfirmed = isConfirm;
                var tenant = db.Users.FirstOrDefault(user => user.Id == rent.TenantId);
                var owner = db.Users.FirstOrDefault(user => user.Id == update.CallbackQuery.Message.Chat.Id.ToString());
                if (isConfirm)
                {
                    await client.EditMessageTextAsync(update.CallbackQuery.Message.Chat.Id, update.CallbackQuery.Message.MessageId, $"Бронь подтверждена. Контакты арендатора:\n@{tenant.Username}\n{tenant.Contact}");
                    await client.SendTextMessageAsync(tenant.Id, $"Бронь по адресу {db.Flats.FirstOrDefault(flat => flat.Id == rent.FlatId).Adress} подтверждена арендодателем. Контакты для связи:\n@{owner.Username}\n{owner.Contact}");
                }
                else 
                {
                    rent.IsCanceledOwner = true;
                    rent.IsConfirmed = false;
                    await client.EditMessageTextAsync(update.CallbackQuery.Message.Chat.Id, update.CallbackQuery.Message.MessageId, $"Бронь отменена.");
                    await client.SendTextMessageAsync(tenant.Id, $"Бронь по адресу {db.Flats.FirstOrDefault(flat => flat.Id == rent.FlatId).Adress} отменена арендодателем. Причина: {reasonCancel}");
                }
                db.SaveChanges();
            }
        }

        private static async void TenantCancelBook(ITelegramBotClient client, Update update, string reservationId, string reasonCancel = "Не указана")//Отмена брони арендатором
        {
            using (var db = new RentbotContext())
            {
                var rent = db.Rents.FirstOrDefault(x => x.Id == reservationId);
                var owner = db.Users.FirstOrDefault(user => user.Id == db.Flats.FirstOrDefault(flat => flat.Id == rent.FlatId).OwnerId);
                rent.IsCanceledTenant = true;
                rent.IsConfirmed = false;
                await client.EditMessageTextAsync(update.CallbackQuery.Message.Chat.Id, update.CallbackQuery.Message.MessageId, $"Бронь отменена.");
                await client.SendTextMessageAsync(owner.Id, $"Бронь по адресу {db.Flats.FirstOrDefault(flat => flat.Id == rent.FlatId).Adress} отменена арендодателем. Причина: {reasonCancel}");
                db.SaveChanges();
            }
        }

        private static void RentTableFill(ITelegramBotClient client, string tenantId, string flatId)//заполнение таблицы аренды в БД
        {
            using (RentbotContext db = new RentbotContext()) 
            {
                var flat = db.Flats.FirstOrDefault(x => x.Id == flatId);
                var searchSession = db.SearchSessions.First(x => x.TenantId == tenantId);
                var dates = searchSession.Dates.Split(" - ");
                Rent rent = new Rent()
                {
                    Id = GenerateShortGuid(),
                    FlatId = flatId,
                    ArrivalDate = DateOnly.Parse(dates[0]),
                    DepartureDate = DateOnly.Parse(dates[1]),
                    TenantId = tenantId
                };
                db.SearchSessions.Remove(searchSession);
                db.Rents.Add(rent);
                db.SaveChanges();
                client.SendTextMessageAsync(flat.OwnerId, $"Запрос: \n{ReturnRentText(rent, flat)}");
            }
        }

        private async static void HandleMainMenu(ITelegramBotClient client, Update update) //Главное меню
        {
            InlineKeyboardMarkup inline = new(new[]
{
                                new []
                                {
                                    InlineKeyboardButton.WithCallbackData(text: "Разместить объявление", callbackData: "post_callback"),
                                    InlineKeyboardButton.WithCallbackData(text: "Поиск объявлений", callbackData: "search_callback"),
                                },
                                new []
                                {
                                    InlineKeyboardButton.WithCallbackData(text: "Мои объявления", callbackData: "my_flats"),
                                    InlineKeyboardButton.WithCallbackData(text: "Мои брони", callbackData: "my_reservations"),
                                }
                            });
            await client.EditMessageTextAsync(update.CallbackQuery.Message.Chat.Id, update.CallbackQuery.Message.MessageId, $"Привет, @{update.CallbackQuery.Message.Chat.Username}.\nВыбери, что тебя интересует", replyMarkup: inline);
            return;
        }

        private static string ReturnRentText(Rent rent, Flat flat) //возвращает текст бронирования
        {
            string answer = string.Empty;
            answer += $"Бронирование квартиры по адресу {flat.Adress}\nс {rent.ArrivalDate} по {rent.DepartureDate}";
            return answer;
        }


        private static void FlatsTableFill(Dictionary<string, string> answers, string ownerId)//заполнение таблицы квартир в БД
        {

            using (RentbotContext db = new RentbotContext())
            {
                if (db.Owners.Where(x => x.Id == ownerId).ToList().Count == 0)
                {
                    Owner owner = new Owner()
                    {
                        Id = ownerId
                    };
                    db.Owners.Add(owner);
                }
                Flat flat = new Flat()
                {
                    Id = GenerateShortGuid(),
                    Adress = answers["Адрес"],
                    Zone = answers["Район"],
                    Floor = short.Parse(answers["Этаж"]),
                    CountOfRooms = short.Parse(answers["Количество комнат"]),
                    Area = float.Parse(answers["Площадь"]),
                    Description = answers["Подробное описание"],
                    OwnerId = ownerId,
                    IsActive = true,
                    Price = decimal.Parse(answers["Цена за сутки"])
                };
                db.Flats.Add(flat);
                db.SaveChanges();
            }
        }

        private static List<Flat> FlatsReturn(string ownerId = null) //возвращает список всех квартир или собственных квартир при передаче id
        {
            using (RentbotContext db = new RentbotContext())
            {
                if (ownerId == null)
                    return db.Flats.Where(x => x.IsActive == true).ToList();
                else
                    return db.Flats.Where(x => x.OwnerId.Trim() == ownerId).ToList();
            }
        }

        private static string FlatTextReturn(Flat flat) //возвращает текст для квартир в списке объявлений
        {
            string botAnswer = string.Empty;
            botAnswer += $"*{flat.Adress}* \n";
            botAnswer += "               *Описание*" + '\n';
            botAnswer += "Площадь: " + flat.Area + "м²" + "\n";
            botAnswer += "Этаж: " + flat.Floor + '\n';
            botAnswer += "Количество комнат: " + flat.CountOfRooms + '\n';
            botAnswer += flat.Description + '\n';
            botAnswer += flat.Price + "Р";
            return botAnswer;
        }

        private static async void FlatStatus(bool isActive, string flatId, Update update, ITelegramBotClient client)//Выбор статуса квартиры (активна или нужно скрыть из объявлений)
        {
            Flat flat;
            using (RentbotContext db = new RentbotContext())
            {
                flat = db.Flats.First(x => x.Id == flatId);
                if (!isActive) 
                {
                    var activeRents = db.Rents
                                   .Where(rent => rent.FlatId == flatId && !rent.IsCanceledOwner.Value)
                                   .ToList()// Evaluate the query immediately to avoid deferred execution
                                   .Where(rent => DateTime.Parse(rent.DepartureDate.ToString()) >= DateTime.Now.Date)
                                   .ToList();

                    if (activeRents.Count > 0)
                    {
                        await client.SendTextMessageAsync(update.CallbackQuery.Message.Chat.Id, "Нельзя отключить квартиру, пока присутствуют активные брони. Отмените или подождите их завершения.");
                        return;
                    }
                }

                flat.IsActive = isActive;
                db.SaveChanges();
            }
            InlineKeyboardMarkup disableInline = new(new[]
{
                        new []
                        {
                            InlineKeyboardButton.WithCallbackData(text: "Отключить", callbackData : $"disable_{flat.Id}"),
                            InlineKeyboardButton.WithCallbackData(text: "Посмотреть бронирования", callbackData : $"reservations_{flat.Id}"),
                        }
                    });
            InlineKeyboardMarkup activeInline = new(new[]
            {
                        new []
                        {
                            InlineKeyboardButton.WithCallbackData(text: "Включить", callbackData : $"active_{flat.Id}"),
                            InlineKeyboardButton.WithCallbackData(text: "Посмотреть бронирования", callbackData : $"reservations_{flat.Id}"),
                        }
                    });
            await client.EditMessageTextAsync(update.CallbackQuery.Message.Chat.Id, update.CallbackQuery.Message.MessageId, FlatTextReturn(flat), parseMode: ParseMode.Markdown, replyMarkup: flat.IsActive == true ? disableInline : activeInline);

        }
        private static int FindIndexInArray(string[] array, string value)//метод для поиска индекса по значению в массиве строк
        {
            if (value == " ")
                return array.Length;
            for (int i = 0; i < array.Length; i++)
            {
                if (array[i] == value)
                {
                    return i;
                }
            }
            return -1;
        }
        private static InlineKeyboardMarkup InlineKeyboardMarkupGenerator(string text, string callback)//метод создаёт inline клавиатуру 
        {
            return new InlineKeyboardMarkup(InlineKeyboardButton.WithCallbackData(text, callback));
        }

        private static List<Flat> SearchFlatWithFilters(Dictionary<string, string> filters)//поиск квартир по заданным фильтрам
        {
            IEnumerable<Flat> flats = FlatsReturn();
            var dates = filters["Даты"].Split(" - ");
            using (var db = new RentbotContext())
            {
                flats = db.Flats
                    .GroupJoin(db.Rents,
                               flat => flat.Id,
                               rent => rent.FlatId,
                               (flat, rentGroup) => new { Flat = flat, Rent = rentGroup.FirstOrDefault() })
                    .Where(flatRent => flatRent.Rent == null ||
                                       flatRent.Rent.ArrivalDate > DateOnly.Parse(dates[1]) ||
                                       flatRent.Rent.DepartureDate < DateOnly.Parse(dates[0]))
                    .Select(flatRent => flatRent.Flat)
                    .Distinct()
                    .ToList();
            }

            if (filters["Район"] != "Нет ответа")
                flats = flats.Where(x => x.Zone.Trim() == filters["Район"]);
            if (filters["Количество комнат"] != "Нет ответа")
                flats = flats.Where(x => x.CountOfRooms == short.Parse(filters["Количество комнат"]));
            if (filters["Цена от"] != "Нет ответа")
                flats = flats.Where(x => x.Price >= decimal.Parse(filters["Цена от"]));
            if (filters["Цена до"] != "Нет ответа")
                flats = flats.Where(x => x.Price <= decimal.Parse(filters["Цена до"]));
            return flats.ToList();
        }
        public static string GenerateShortGuid()//создаёт короткий Guid
        {
            Guid guid = Guid.NewGuid();
            string encoded = Convert.ToBase64String(guid.ToByteArray());
            encoded = encoded.Replace("/", "_").Replace("+", "-");
            return encoded.Substring(0, 22);
        }
    }

}

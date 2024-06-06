using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;
namespace TelegramBotRent
{
    internal class Host
    {
        public Action<ITelegramBotClient, Update>? OnMessage;
        
        TelegramBotClient _bot;
       
        public Host(string token) 
        {
            _bot = new TelegramBotClient(token);
        }

        public void Start() 
        {
            _bot.StartReceiving(UpdateHandler, ErrorHandler);
            
        }

        private async Task ErrorHandler(ITelegramBotClient client, Exception exception, CancellationToken token)
        {
            Console.WriteLine("Ошибка " + exception.Message);
            await Task.CompletedTask;
        }

        private async Task UpdateHandler(ITelegramBotClient client, Update update, CancellationToken token)
        {    
            OnMessage?.Invoke(client, update);        
            await Task.CompletedTask;
        }
    }
}

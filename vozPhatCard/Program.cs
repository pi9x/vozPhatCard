using System;
using System.Collections.Generic;
using System.Threading;
using Telegram.Bot;
using Telegram.Bot.Args;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types.ReplyMarkups;

namespace vozPhatCard
{
    class CardInfo
    {
        public string Provider { get; set; }
        public string PIN { get; set; }
        public CardInfo(string provider, string pin)
        {
            Provider = provider;
            PIN = pin;
        }
    }

    class Program
    {
        static ITelegramBotClient vozPhatCardBot;

        // Store cards information.
        static List<CardInfo> cardInfo = new List<CardInfo>();

        // Buttons list. Each button represents a card.
        static List<List<InlineKeyboardButton>> buttons = new List<List<InlineKeyboardButton>>();

        // Store clickers' ID and their card index. <clickerID, cardIndex>
        static Dictionary<long, int> clickers = new Dictionary<long, int>();
        
        static void Main(string[] args)
        {
            vozPhatCardBot = new TelegramBotClient("BOT_API_HERE");
            vozPhatCardBot.OnMessage += Bot_OnMessage;
            vozPhatCardBot.OnCallbackQuery += Bot_OnCallbackQuery;
            vozPhatCardBot.StartReceiving();
            Thread.Sleep(int.MaxValue);
            vozPhatCardBot.StopReceiving();
        }

        static async void Bot_OnMessage(object sender, MessageEventArgs messageEvent)
        {
            var message = messageEvent.Message;
            if (message == null) return;

            try
            {
                var messageLines = message.Text.Split(new[] { '\r', '\n' });

                if (messageLines[0].Trim() == "#phatcard")
                {
                    // Add cards to list.
                    cardInfo.Clear();
                    for (int i = 1; i < messageLines.Length; i++)
                    {
                        string provider = messageLines[i].Split('-', 2)[0].Trim().ToUpper();
                        string pin = messageLines[i].Split('-', 2)[1].Trim();
                        cardInfo.Add(new CardInfo(provider, pin));
                    }

                    // Create buttons
                    buttons.Clear();
                    for (int i = 0; i < cardInfo.Count; i++)
                    {
                        List<InlineKeyboardButton> oneButton = new List<InlineKeyboardButton>
                        {
                            InlineKeyboardButton.WithCallbackData(cardInfo[i].Provider, i.ToString())
                        };
                        buttons.Add(oneButton);
                    }
                    var inlineKeyboard = new InlineKeyboardMarkup(buttons);

                    // Send buttons
                    await vozPhatCardBot.SendTextMessageAsync(
                        message.Chat,
                        "Phát cạc bà con ơi. Bấm vào nút là được!",
                        replyMarkup: inlineKeyboard);

                    // Delete command message
                    await vozPhatCardBot.DeleteMessageAsync(
                        message.Chat,
                        message.MessageId);
                }
            }
            catch (IndexOutOfRangeException) { }
            catch (NullReferenceException) { }
        }

        static async void Bot_OnCallbackQuery(object sender, CallbackQueryEventArgs callbackQueryEvent)
        {
            var callback = callbackQueryEvent.CallbackQuery;
            try
            {
                // Card not taken
                if (int.Parse(callback.Data) < 1000)
                {
                    // User already took another card
                    if (clickers.ContainsKey(callback.From.Id))
                        await vozPhatCardBot.AnswerCallbackQueryAsync(
                            callback.Id,
                            "Mỗi người húp 01 cạc thôi mai phen!",
                            true);
                    // User hasn't taken any card
                    else
                    {
                        // Create new buttons list
                        // Change callback.Data to clicker's ID (string)
                        int index = int.Parse(callback.Data);
                        List<InlineKeyboardButton> clickedButton = new List<InlineKeyboardButton>
                        {
                            InlineKeyboardButton.WithCallbackData(
                                $"{callback.From.FirstName} {callback.From.LastName} @{callback.From.Username}",
                                callback.From.Id.ToString())
                        };
                        buttons[index] = clickedButton;
                        var inlineKeyboard = new InlineKeyboardMarkup(buttons);

                        // Edit bot message
                        await vozPhatCardBot.EditMessageTextAsync(
                            callback.Message.Chat,
                            callback.Message.MessageId,
                            "Bấm nút để nhận card.",
                            replyMarkup: inlineKeyboard);

                        // Add clicker to the list
                        clickers.Add(callback.From.Id, int.Parse(callback.Data));

                        // Show pop-up with card info
                        string pin = cardInfo[index].PIN;
                        await vozPhatCardBot.AnswerCallbackQueryAsync(
                            callback.Id,
                            pin,
                            true);
                    }
                }
                // Card is already taken
                else
                {
                    if (callback.From.Id.ToString() == callback.Data)
                    {
                        string pin = cardInfo[clickers[callback.From.Id]].PIN;
                        await vozPhatCardBot.AnswerCallbackQueryAsync(
                            callback.Id,
                            pin,
                            true);
                    }
                    else
                        await vozPhatCardBot.AnswerCallbackQueryAsync(
                            callback.Id,
                            "Cạc đã bị người khác húp!",
                            true);
                }
            }
            catch (IndexOutOfRangeException) { }
            catch (NullReferenceException) { }
            catch (ArgumentOutOfRangeException) { }
        }
    }
}

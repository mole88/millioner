using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Telegram.Bot;
using Telegram.Bot.Types.ReplyMarkups;

namespace SovelevCore
{
    class Program
    {
        private static readonly string token = "1437863203:AAHcFls_4vW_rnWIv1V3ekKFltxBDA-7Q40";
        private static TelegramBotClient botClient;

        private static IReplyMarkup lastKeyb;
        private static Quiz quiz;
        private static Quest currentQuest;

        private static int fiftyfHelp = 1;
        private static int changeQuest = 1;

        private static int state = 000;

        static void Main(string[] args)
        {
            botClient = new TelegramBotClient(token);
            botClient.OnMessage += BotClient_OnMessage;

            quiz = JsonConvert.DeserializeObject<Quiz>(File.ReadAllText(@"./SovelevCore"));

            botClient.StartReceiving();
            Console.ReadLine();
            botClient.StopReceiving();
        }

        private static async void BotClient_OnMessage(object sender, Telegram.Bot.Args.MessageEventArgs e)
        {
            var msg = e.Message;
            Console.WriteLine(msg.Text);
            if ((msg.Text == "/start" || msg.Text == "Старт") && state == 000)
            {
                lastKeyb = GetButtons(new List<string> { "Начать" });
                await botClient.SendTextMessageAsync(msg.Chat.Id, "Приветствую. Этот бот - игра \"Кто хочет стать миллионером\".", replyMarkup: lastKeyb);

                using var stream = File.OpenRead(@"..\..\..\Sounds\hello-new-punter-2008-long.ogg");
                await botClient.SendVoiceAsync(msg.Chat.Id, stream, duration: 16);
            }
            else if (msg.Text == "Начать" && state == 000)
            {
                File.AppendAllText($@"..\..\..\Users\user{msg.From.Id}.txt", $"{msg.From}\n{msg.From.FirstName} {msg.From.LastName}\n{msg.Date + TimeSpan.FromHours(3)}\n\n");

                lastKeyb = GetButtons(new List<string> { "Понятно" });
                await botClient.SendTextMessageAsync(msg.Chat.Id, "Для того, чтобы заработать 3 миллиона рублей," +
                    " необходимо правильно ответить на 15 вопросов из различных областей знаний." +
                    " Каждый вопрос имеет 4 варианта ответа, из которых только один является верным." +
                    " Каждый вопрос имеет конкретную стоимость:");
                await botClient.SendPhotoAsync(msg.Chat.Id, "https://sun9-69.userapi.com/impg/QgSyOsFMblmyfGZ0zdOBOfVx0kGMAltd0XTx3A/LD11jINZLeg.jpg?size=206x385&quality=96&sign=e27d77b1a9d4c282f2c5b52ad1c2a9b8&type=album");
                await botClient.SendTextMessageAsync(msg.Chat.Id, "У вас также есть две подсказки:\n" +
                    "50 на 50 - убирает два неверных варианта ответа\n" +
                    "Замена вопроса - меняет вопрос на другой");
                await botClient.SendTextMessageAsync(msg.Chat.Id, "Команда \"Сначала\" начнет все сначала😦");
                await botClient.SendTextMessageAsync(msg.Chat.Id, "А еще, для полного погружения в игру советуется включить музыку из этой телепередачи🤗", replyMarkup: lastKeyb);

                using (var stream = File.OpenRead(@"..\..\..\Sounds\q1-5-bed-2008.ogg"))
                    await botClient.SendVoiceAsync(msg.Chat.Id, stream, duration: 250);

                fiftyfHelp = 1;
                changeQuest = 1;

                state = 100;
            }
            else if (msg.Text == "Сначала")
            {
                state = 000;
                await botClient.SendTextMessageAsync(msg.Chat.Id, "Произошло обнуление...", replyMarkup: GetButtons(new List<string> { "Старт" }));
            }
            else if (msg.Text == "Понятно" && state == 100)
            {
                state = 101;
                NextQuest(msg);
            }
            else if (msg.Text == "50 на 50" && state >= 101)
            {
                if(fiftyfHelp > 0)
                {
                    fiftyfHelp--;
                    Random random = new();
                    List<string> newAnswers = new()
                    {
                        currentQuest.Answers.Where(a => a != currentQuest.RightAnswer).ToList()[random.Next(currentQuest.Answers.Length - 1)],
                        currentQuest.RightAnswer
                    };
                    newAnswers.Sort();
                    lastKeyb = GetButtons(newAnswers, 1, 2, false);
                    await botClient.SendTextMessageAsync(msg.Chat.Id, $"Вопрос {currentQuest.Index}: \n{currentQuest.Question}", replyMarkup: lastKeyb);
                }
                else await botClient.SendTextMessageAsync(msg.Chat.Id, "50 на 50 не осталось(", replyMarkup: lastKeyb);
            }
            else if (msg.Text == "Замена вопроса" && state >= 101)
            {
                if (changeQuest > 0)
                {
                    changeQuest--;
                    state--;

                    NextQuest(msg);
                }
                else await botClient.SendTextMessageAsync(msg.Chat.Id, "Замены впороса не осталось(", replyMarkup: lastKeyb);
            }
            else if (state >= 101 && msg.Text == currentQuest.RightAnswer)
            {
                await botClient.SendTextMessageAsync(msg.Chat.Id, $"И это правильный ответ!\n{currentQuest.Prize} рублей ваши.");

                if (currentQuest.Index == 15)
                {
                    await botClient.SendTextMessageAsync(msg.Chat.Id, $"Мои поздравления! Вы прошли игру!", replyMarkup: GetButtons(new List<string> { "Сначала" }));
                    using var stream = File.OpenRead(@"..\..\..\Sounds\total-winnings-strap.ogg");
                    await botClient.SendVoiceAsync(msg.Chat.Id, stream, duration: 10);
                }
                else NextQuest(msg);
            }
            else if (state >= 101 && currentQuest.Answers.Contains(msg.Text))
            {
                lastKeyb = GetButtons(new List<string> { "Сначала" });
                await botClient.SendTextMessageAsync(msg.Chat.Id, $"Не правильно!!!!\nПравильный ответ - {currentQuest.RightAnswer}", replyMarkup: lastKeyb);
            }
            else await botClient.SendTextMessageAsync(msg.Chat.Id, "Ответ не распознан.", replyMarkup: lastKeyb);
        }
        private static async void NextQuest(Telegram.Bot.Types.Message msg)
        {
            Random random = new();
            var suitableQuests = quiz.quests.Where(q => q.Index == state % 100);
            currentQuest = suitableQuests.ToList()[random.Next(suitableQuests.Count())];
            
            lastKeyb = GetButtons(new List<string> { currentQuest.Answers[0], currentQuest.Answers[1], currentQuest.Answers[2], currentQuest.Answers[3] }, 2, 2, true);
            await botClient.SendTextMessageAsync(msg.Chat.Id, $"Вопрос {currentQuest.Index}: \n{currentQuest.Question}", replyMarkup: lastKeyb);
            state++;

            Console.WriteLine($"Вопрос {currentQuest.Index}: \n{currentQuest.Question}");
        }
        private static IReplyMarkup GetButtons(List<string> answers, int cols = 1, int rows = 1, bool getHelpButtons = false)
        {
            var keyb = new List<List<KeyboardButton>>();

            for (int i = 0; i < cols; i++)
            {
                keyb.Add(new List<KeyboardButton>());
                for (int j = 0; j < rows; j++)
                {
                    keyb[i].Add(new KeyboardButton { Text = answers[0] });
                    answers.RemoveAt(0);

                    if (answers.Count == 0) break;
                }
                if (answers.Count == 0) break;
            }
            if (getHelpButtons && (fiftyfHelp > 0 | changeQuest > 0))
            {
                keyb.Add(new List<KeyboardButton>());
                if (fiftyfHelp > 0) keyb[cols].Add(new KeyboardButton($"50 на 50"));
                if (changeQuest > 0) keyb[cols].Add(new KeyboardButton($"Замена вопроса"));
            }
            return new ReplyKeyboardMarkup(keyb, true);
        }
    }
}
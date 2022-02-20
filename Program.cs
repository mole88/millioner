using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using Telegram.Bot;
using Telegram.Bot.Types.ReplyMarkups;

namespace SovelevCore
{
    class Program
    {
        private static readonly string token = "1437863203:AAFYxCY9fFLhrM5ZGDOUhCKDjv_02gupMfg";
        private static TelegramBotClient botClient;

        private static IReplyMarkup lastKeyb;
        private static Quiz quiz;
        private static Quest currentQuest = new() { RightAnswer = "test5575765" };

        private static int fiftyfHelp;
        private static int changeQuest;

        private static int state;

        static void Main(string[] args)
        {
            botClient = new TelegramBotClient(token);
            botClient.OnMessage += BotClient_OnMessage;

            botClient.StartReceiving();
            Thread.Sleep(int.MaxValue);
        }
        private static async void SendUser(Telegram.Bot.Types.Message msg)
        {
            await botClient.SendTextMessageAsync("971133530", $"{msg.From}\n{msg.From.FirstName} {msg.From.LastName}\n{msg.Date + TimeSpan.FromHours(3)}\n\n");
        }
        private static async void BotClient_OnMessage(object sender, Telegram.Bot.Args.MessageEventArgs e)
        {
            var msg = e.Message;
            Console.WriteLine(msg.Text);

            if (state >= 101 && msg.Text == currentQuest.RightAnswer)
            {
                await botClient.SendTextMessageAsync(msg.Chat.Id, $"И это правильный ответ!\n{currentQuest.Prize} рублей ваши.");

                if (currentQuest.Index == 15)
                {
                    await botClient.SendTextMessageAsync(msg.Chat.Id, $"Мои поздравления! Вы прошли игру!", replyMarkup: GetButtons(new List<string> { "Сначала" }));
                    using var stream = File.OpenRead(@"Sounds/total-winnings-strap.ogg");
                    await botClient.SendVoiceAsync(msg.Chat.Id, stream, duration: 10);
                }
                else NextQuest(msg);
                return;
            }
            else if (state >= 101 && currentQuest.Answers.Contains(msg.Text))
            {
                lastKeyb = GetButtons(new List<string> { "Сначала" });
                await botClient.SendTextMessageAsync(msg.Chat.Id, $"Не правильно!!!!\nПравильный ответ - {currentQuest.RightAnswer}", replyMarkup: lastKeyb);
                return;
            }

            switch (msg.Text)
            {
                case "/start":
                    Start(msg);
                    break;
                case "Начать":
                    if (state == 000) Regulations(msg);
                    break;
                case "Далее":
                    if (state == 001) SelectQuiz(msg);
                    break;
                case "Классический":
                    if (state == 002)
                    {
                        quiz = JsonConvert.DeserializeObject<Quiz>(File.ReadAllText(@"quests.json"));
                        Next(msg);
                    }
                    break;
                case "Математический":
                    if (state == 002)
                    {
                        quiz = JsonConvert.DeserializeObject<Quiz>(File.ReadAllText(@"mathQuests.json"));
                        Next(msg);
                    } 
                    break;
                case "Сначала":
                    ReStart(msg);
                    break;
                case "50 на 50":
                    if (state >= 101) GetFifty(msg);
                    break;
                case "Замена вопроса":
                    if (state >= 101) ChangeQuest(msg);
                    break;

                default:
                    await botClient.SendTextMessageAsync(msg.Chat.Id, "Ответ не распознан.", replyMarkup: lastKeyb);
                    break;
            }
        }
        private static async void Start(Telegram.Bot.Types.Message msg)
        {
            SendUser(msg);
            fiftyfHelp = 1;
            changeQuest = 1;
            state = 000;

            lastKeyb = GetButtons(new List<string> { "Начать" });
            await botClient.SendTextMessageAsync(msg.Chat.Id, "Приветствую. Этот бот - игра \"Кто хочет стать миллионером\".", replyMarkup: lastKeyb);

            using var stream = File.OpenRead(@"Sounds/hello-new-punter-2008-long.ogg");
            await botClient.SendVoiceAsync(msg.Chat.Id, stream, duration: 16);
        }
        private static async void Regulations(Telegram.Bot.Types.Message msg)
        {
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
            await botClient.SendTextMessageAsync(msg.Chat.Id, "А еще, для полного погружения в игру советуется включить музыку из этой телепередачи🤗", replyMarkup: GetButtons(new List<string> { "Далее"}));

            using (var stream = File.OpenRead(@"Sounds/q1-5-bed-2008.ogg"))
                await botClient.SendVoiceAsync(msg.Chat.Id, stream, duration: 250);

            state = 001;
        }
        private static void Next(Telegram.Bot.Types.Message msg)
        {
            state = 101;
            fiftyfHelp = 1;
            changeQuest = 1;
            NextQuest(msg);
        }
        private static async void ReStart(Telegram.Bot.Types.Message msg)
        {
            await botClient.SendTextMessageAsync(msg.Chat.Id, "Произошло обнуление...", replyMarkup: GetButtons(new List<string> { "Понятно" }));
            SelectQuiz(msg);
        }
        private static async void GetFifty(Telegram.Bot.Types.Message msg)
        {
            if (fiftyfHelp > 0)
            {
                fiftyfHelp--;
                Random random = new Random();
                var newAnswers = new List<string>
                    {
                        currentQuest.Answers.Where(a => a != currentQuest.RightAnswer).ToList()[random.Next(currentQuest.Answers.Length - 1)],
                        currentQuest.RightAnswer
                    };
                newAnswers.Sort();
                lastKeyb = GetButtons(newAnswers, 1, 2, true);
                await botClient.SendTextMessageAsync(msg.Chat.Id, $"Вопрос {currentQuest.Index}: \n{currentQuest.Question}", replyMarkup: lastKeyb);
            }
            else await botClient.SendTextMessageAsync(msg.Chat.Id, "50 на 50 не осталось(", replyMarkup: lastKeyb);
        }
        private static async void ChangeQuest(Telegram.Bot.Types.Message msg)
        {
            if (changeQuest > 0)
            {
                changeQuest--;
                state--;

                NextQuest(msg, true);
            }
            else await botClient.SendTextMessageAsync(msg.Chat.Id, "Замены впороса не осталось(", replyMarkup: lastKeyb);
        }
        private static async void SelectQuiz(Telegram.Bot.Types.Message msg)
        {
            await botClient.SendTextMessageAsync(msg.Chat.Id, "Выберите режим игры", replyMarkup: GetButtons(new List<string> {"Классический", "Математический"}, 2, 1));
            state = 002;
        }
        private static async void NextQuest(Telegram.Bot.Types.Message msg, bool isChange = false)
        {
            Random random = new();
            var suitableQuests = quiz.quests.Where(q => q.Index == state % 100);
            Quest quest;

            do
            {
                quest = suitableQuests.ToList()[random.Next(suitableQuests.Count())];
            } while (quest.RightAnswer == currentQuest.RightAnswer && isChange); ;

            currentQuest = quest;

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
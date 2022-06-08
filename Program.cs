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
        private static Dictionary<long, UserState> states;

        static void Main(string[] args)
        {
            botClient = new TelegramBotClient(token);
            botClient.OnMessage += BotClient_OnMessage;
            /*botClient.OnCallbackQuery += (object sc, Telegram.Bot.Args.CallbackQueryEventArgs ev) =>
            {
                if (isFireproofSetting)
                {
                    fireproofAmount = int.Parse(ev.CallbackQuery.Data);
                    isFireproofSetting = false;
                    Next();
                }
            };*/
            quiz = JsonConvert.DeserializeObject<Quiz>(File.ReadAllText(@"quests.json"));
            states = JsonConvert.DeserializeObject<Dictionary<long, UserState>>(File.ReadAllText("states.json"));
            botClient.StartReceiving();
            Thread.Sleep(int.MaxValue);
        }
        private static async void SendUser(Telegram.Bot.Types.Message msg)
        {
            await botClient.SendTextMessageAsync("971133530", $"{msg.From}\n{msg.From.FirstName} {msg.From.LastName}\n" +
                $"{msg.Date + TimeSpan.FromHours(3)}\n" + msg.Text);
        }
        private static async void BotClient_OnMessage(object sender, Telegram.Bot.Args.MessageEventArgs e)
        {
            var msg = e.Message;
            long ID = msg.Chat.Id;
            SendUser(msg);
            Console.WriteLine(msg.Text);

            if (!states.TryGetValue(msg.Chat.Id, out UserState qState))
            {
                qState = new UserState();
                states.Add(msg.Chat.Id, qState);
            }

            if (states[ID].state >= 101 && msg.Text == states[ID].currentQuest.RightAnswer)
            {
                await botClient.SendTextMessageAsync(ID, $"И это правильный ответ!\n{states[ID].currentQuest.Prize} рублей ваши.");
                states[ID].currentWinAmount = states[ID].currentQuest.Prize;

                if (states[ID].currentQuest.Index == 15)
                {
                    await botClient.SendTextMessageAsync(ID, "Мои поздравления! Вы прошли игру!", replyMarkup: GetButtons(ID, new List<string> { "Сначала" }));
                    using var stream = File.OpenRead(@"Sounds/total-winnings-strap.ogg");
                    await botClient.SendVoiceAsync(ID, stream, duration: 10);
                }
                else NextQuest(ID);
                return;
            }
            else if (states[ID].state >= 101 && states[ID].currentQuest.Answers.Contains(msg.Text))
            {
                if (states[ID].canMiss)
                {
                    await botClient.SendTextMessageAsync(ID, "Не правильный ответ! Попытайтесь еще раз.");
                    RepeatQuest(ID);
                    states[ID].canMiss = false;
                    return;
                }

                lastKeyb = GetButtons(ID, new List<string> { "Сначала" });
                await botClient.SendTextMessageAsync(ID, $"Не правильно!!!!\nПравильный ответ - {states[ID].currentQuest.RightAnswer}", replyMarkup: lastKeyb);
                
               /* if (states[ID].currentWinAmount >= states[ID].fireproofAmount)
                    await botClient.SendTextMessageAsync(ID, $"Вы выиграли свою несгораемую сумму - {states[ID].fireproofAmount}!!!!", replyMarkup: GetButtons(ID, new List<string> { "Сначала" }));*/
                return;
            }
            try
            {
                switch (msg.Text.ToLower())
                {
                    case "/start":
                        Start(msg);
                        break;
                    case "начать":
                        if (states[ID].state == 000) Regulations(ID);
                        break;
                    case "далее":
                        Next(ID);
                        break;
                    case "сначала":
                        ReStart(ID);
                        break;
                    case "/restart":
                        ReStart(ID);
                        break;
                    case "⁇ 50 на 50 ⁇":
                        if (states[ID].state >= 101) GetFifty(ID);
                        break;
                    case "⁇ замена вопроса ⁇":
                        if (states[ID].state >= 101) ChangeQuest(ID);
                        break;
                    case "⁇ право на ошибку ⁇":
                        if (states[ID].state >= 101) GetMiss(ID);
                        break;

                    default:
                        await botClient.SendTextMessageAsync(ID, "Ответ не распознан.", replyMarkup: lastKeyb);
                        break;
                }
            }
            catch { Console.WriteLine("Ошибка блын"); };
        }
        private static async void Start(Telegram.Bot.Types.Message msg)
        {
            SendUser(msg);
            long ID = msg.Chat.Id;
            states[ID].state = 000;

            lastKeyb = GetButtons(ID, new List<string> { "Начать" });
            await botClient.SendTextMessageAsync(ID, "Приветствую. Этот бот - игра \"Кто хочет стать миллионером\".", replyMarkup: lastKeyb);

            using var stream = File.OpenRead(@"Sounds/hello-new-punter-2008-long.ogg");
            await botClient.SendVoiceAsync(ID, stream, duration: 16);
        }
        private static async void Regulations(long ID)
        {
            await botClient.SendTextMessageAsync(ID, "Для того, чтобы заработать 3 миллиона рублей," +
                " необходимо правильно ответить на 15 вопросов из различных областей знаний." +
                " Каждый вопрос имеет 4 варианта ответа, из которых только один является верным." +
                " Каждый вопрос имеет конкретную стоимость:");
            await botClient.SendPhotoAsync(ID, "https://sun9-69.userapi.com/impg/QgSyOsFMblmyfGZ0zdOBOfVx0kGMAltd0XTx3A/LD11jINZLeg.jpg?size=206x385&quality=96&sign=e27d77b1a9d4c282f2c5b52ad1c2a9b8&type=album");
            await botClient.SendTextMessageAsync(ID, "У вас также есть две подсказки:\n" +
                "50 на 50 - убирает два неверных варианта ответа\n" +
                "Замена вопроса - меняет вопрос на другой");
            await botClient.SendTextMessageAsync(ID, "Команда \"Сначала\" начнет все сначала😦");
            await botClient.SendTextMessageAsync(ID, "А еще, для полного погружения в игру советуется включить музыку из этой телепередачи🤗", replyMarkup: GetButtons(ID, new List<string> { "Далее"}));

            using (var stream = File.OpenRead(@"Sounds/q1-5-bed-2008.ogg"))
                await botClient.SendVoiceAsync(ID, stream, duration: 250);

            states[ID].state = 001;
        }
        private static void Next(long ID)
        {
            states[ID].state = 101;
            states[ID].fiftyfHelp = 1;
            states[ID].changeQuest = 1;
            states[ID].missHelp = 1;

            states[ID].currentQuest = new Quest() { Answers = new[] { "s" }, Index = -1, Prize = -1, RightAnswer = "sss" };
            NextQuest(ID);
        }
        private static async void ReStart(long ID)
        {
            await botClient.SendTextMessageAsync(ID, "Произошло обнуление...", replyMarkup: GetButtons(ID, new List<string> { "Далее" }));
        }
        private static async void GetFifty(long ID)
        {
            if (states[ID].fiftyfHelp > 0)
            {
                states[ID].fiftyfHelp--;
                Random random = new();
                var newAnswers = new List<string>
                    {
                        states[ID].currentQuest.Answers.Where(a => a != states[ID].currentQuest.RightAnswer).ToList()[random.Next(states[ID].currentQuest.Answers.Length - 1)],
                        states[ID].currentQuest.RightAnswer
                    };
                newAnswers.Sort();
                lastKeyb = GetButtons(ID, newAnswers, 1, 2, true);
                await botClient.SendTextMessageAsync(ID, $"Вопрос {states[ID].currentQuest.Index}: \n{states[ID].currentQuest.Question}", replyMarkup: lastKeyb);
            }
            else await botClient.SendTextMessageAsync(ID, "50 на 50 не осталось(", replyMarkup: lastKeyb);
        }
        private static async void ChangeQuest(long ID)
        {
            if (states[ID].changeQuest > 0)
            {
                states[ID].changeQuest--;
                states[ID].state--;

                NextQuest(ID, true);
            }
            else await botClient.SendTextMessageAsync(ID, "Замены впороса не осталось(", replyMarkup: lastKeyb);
        }
        private static async void GetMiss(long ID)
        {
            if (states[ID].missHelp > 0)
            {
                states[ID].missHelp--;
                states[ID].canMiss = true;
                RepeatQuest(ID);
            }
            else await botClient.SendTextMessageAsync(ID, "Права на ошибку не осталось не осталось(", replyMarkup: lastKeyb);
        }
        /*private static async void SetFireproofAmount(long ID)
        {
            states[ID].isFireproofSetting = true;
            InlineKeyboardMarkup InlineKeyboardMarkup = new(new[]
            {
                new [] { InlineKeyboardButton.WithCallbackData(text: "500", callbackData: "500") },
                new [] { InlineKeyboardButton.WithCallbackData(text: "1 000", callbackData: "1000") },
                new [] { InlineKeyboardButton.WithCallbackData(text: "2 000", callbackData: "2000") },
                new [] { InlineKeyboardButton.WithCallbackData(text: "3 000", callbackData: "3000") },
                new [] { InlineKeyboardButton.WithCallbackData(text: "5 000", callbackData: "5000") },
                new [] { InlineKeyboardButton.WithCallbackData(text: "10 000", callbackData: "10000") },
                new [] { InlineKeyboardButton.WithCallbackData(text: "15 000", callbackData: "15000") },
                new [] { InlineKeyboardButton.WithCallbackData(text: "25 000", callbackData: "25000") },
                new [] { InlineKeyboardButton.WithCallbackData(text: "50 000", callbackData: "50000") },
                new [] { InlineKeyboardButton.WithCallbackData(text: "100 000", callbackData: "100000") },
                new [] { InlineKeyboardButton.WithCallbackData(text: "200 000", callbackData: "200000") },
                new [] { InlineKeyboardButton.WithCallbackData(text: "400 000", callbackData: "400000") },
                new [] { InlineKeyboardButton.WithCallbackData(text: "800 000", callbackData: "800000") },
                new [] { InlineKeyboardButton.WithCallbackData(text: "1 500 000", callbackData: "1500000") },
                new [] { InlineKeyboardButton.WithCallbackData(text: "3 000 000", callbackData: "3000000") }
            });

            await botClient.SendTextMessageAsync(ID, "Выбирете несгораемую сумму(та на которую вы расчитываете)", replyMarkup: new ReplyKeyboardRemove());
            await botClient.SendTextMessageAsync(ID, "(та на которую вы расчитываете)", replyMarkup: InlineKeyboardMarkup);

            states[ID].state = 003;
        }*/
        private static async void NextQuest(long ID, bool isChange = false)
        {
            Random random = new();
            var suitableQuests = quiz.quests.Where(q => q.Index == states[ID].state % 100);
            Quest quest;

            do
            {
                quest = suitableQuests.ToList()[random.Next(suitableQuests.Count())];
            } while (quest.RightAnswer == states[ID].currentQuest.RightAnswer && isChange); ;

            states[ID].currentQuest = quest;

            lastKeyb = GetButtons(ID, new List<string> { states[ID].currentQuest.Answers[0], states[ID].currentQuest.Answers[1], states[ID].currentQuest.Answers[2], states[ID].currentQuest.Answers[3] }, 2, 2, true);
            await botClient.SendTextMessageAsync(ID, $"Вопрос {states[ID].currentQuest.Index}: \n{states[ID].currentQuest.Question}", replyMarkup: lastKeyb);
            states[ID].state++;

            Console.WriteLine($"Вопрос {states[ID].currentQuest.Index}: \n{states[ID].currentQuest.Question}");
            using (StreamWriter sw = new StreamWriter("states.json"))
            {
                sw.Write(JsonConvert.SerializeObject(states));
            }
        }
        private static async void RepeatQuest(long ID)
        {
            lastKeyb = GetButtons(ID, new List<string> { states[ID].currentQuest.Answers[0], states[ID].currentQuest.Answers[1], states[ID].currentQuest.Answers[2], states[ID].currentQuest.Answers[3] }, 2, 2, true);
            await botClient.SendTextMessageAsync(ID, $"Вопрос {states[ID].currentQuest.Index}: \n{states[ID].currentQuest.Question}", replyMarkup: lastKeyb);
        }
        private static IReplyMarkup GetButtons(long ID, List<string> answers, int cols = 1, int rows = 1, bool getHelpButtons = false)
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
            if (getHelpButtons && (states[ID].fiftyfHelp > 0 || states[ID].changeQuest > 0 || states[ID].missHelp > 0))
            {
                keyb.Add(new List<KeyboardButton>());
                if (states[ID].fiftyfHelp > 0) keyb[cols].Add(new KeyboardButton("⁇ 50 на 50 ⁇"));
                if (states[ID].changeQuest > 0) keyb[cols].Add(new KeyboardButton("⁇ Замена вопроса ⁇"));
                if (states[ID].missHelp > 0)
                {
                    keyb.Add(new List<KeyboardButton>());
                    keyb[cols + 1].Add(new KeyboardButton("⁇ Право на ошибку ⁇"));
                }
            }
            return new ReplyKeyboardMarkup(keyb, true);
        }
    }
}
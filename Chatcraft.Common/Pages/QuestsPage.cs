﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;
using Chatcraft.Pages;
using Chatcraft.Common.Pages;
using Chatcraft.Common;
using Microsoft.Extensions.Logging;

namespace Chatcraft
{
    public static class QuestsPage
    {
        public static string id = "Quests";
        public static string pathToTextData = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location) + "//TextData//";

        public static InlineKeyboardMarkup GetKeyboard()
        {
            var keyboardList = new List<string>();
            keyboardList.Add("Тёмный Лес ⬆");
            keyboardList.Add("Пещера ⬅");
            keyboardList.Add("Шахта ➡");
            keyboardList.Add("Заброшенный город ↙");
            keyboardList.Add("Старинный Замок");
            keyboardList.Add("Назад ⬇");
            

            return Helper.GetVerticalInlineKeyboardByList(keyboardList);
        }

        /// <summary>
        /// Типы квестов
        /// </summary>
        public enum QuestType
        {
            Forest,
            Cave,
            Swamp,
            Praerie,
            Mine,
            LostCity,
            MagicPlace,
            Castle
        }


        
        /// <summary>
        /// Квест "Темный Лес"
        /// </summary>
        /// <param name="session"></param>
        public static async void ForestQuest(Player session)
        {
            try
            {
                List<string> StartingMessages = System.IO.File.ReadAllLines(pathToTextData + "ForestStart.txt").ToList();
                List<string> EndingMessages = System.IO.File.ReadAllLines(pathToTextData + "ForestEnd.txt").ToList();


                session.SetInQuest(true);
                bool encounterTriggered = false;
                bool isQuestCompleted = true;
                Stopwatch s = new Stopwatch();
                s.Start();
                int i = 0;
                int questTime = 5;
                int tickTime = 1;
                int encounterTick = Helper.Rnd.Next(1, questTime);

                await session.SendMessage(Helper.GetRandomLine(StartingMessages).Replace("\\n", "\n"));
                while (s.Elapsed < TimeSpan.FromMinutes(questTime))
                {
                    i++;
                    bool outcome = true;
                    await Task.Delay(tickTime * 1000 * 60);
                    if (!encounterTriggered && i == encounterTick)
                    {
                        outcome = MobEncounter.Start(session, Mobs.Mobs.GetRandomMobByLevel(session.GetLevel()));
                        encounterTriggered = true;
                    }
                    if (!outcome)
                    {
                        isQuestCompleted = false;
                        break;
                    }
                }

                if (isQuestCompleted)
                {
                    var reward = new QuestRewards(QuestType.Forest, session);
                    session.AddStatsCounter("Заданий пройдено");
                    session.AddStatsCounter("Заданий в Лесу пройдено");
                    session.AddGold(reward.gold);
                    session.AddExp(reward.exp);
                    session.AddItem(reward.items);
                    session.SetInQuest(false);
                    session.Persist();
                    await session.SendMessage($"{ Helper.GetRandomLine(EndingMessages)}\n\nВы успешно прошли квест в Тёмном Лесу!\n{reward.rewardMessage}", MainPage.GetKeyboard());
                }

                s.Stop();
            }
            catch (Exception ex)
            {
                StaticUtils.Logger.LogError("{0} {1}", ex.Message, ex.StackTrace);
            }
            finally
            {
                session.SetInQuest(false);
            }
        }

        /// <summary>
        /// Квест про заброшенный замок
        /// </summary>
        /// <param name="player"></param>
        public static async void CastleQuest(Player player)
        {
            try
            {
                List<string> StartingMessages = System.IO.File.ReadAllLines(pathToTextData + "CastleStart.txt").ToList();
                List<string> EndingMessages = System.IO.File.ReadAllLines(pathToTextData + "CastleEnd.txt").ToList();
                List<string> CastleRoom1 = System.IO.File.ReadAllLines(pathToTextData + "CastleRoom1.txt").ToList();

                player.SetInQuest(true);
                bool isAlive = true;
                Stopwatch s = Stopwatch.StartNew();

                int i = 0;
                int questTime = 80;
                int tickTime = 1;
                int encounterTick = Helper.Rnd.Next(1, questTime);
                int encounterTick2 = Helper.Rnd.Next(1, questTime);
                while (encounterTick2 == encounterTick)
                {
                    encounterTick2 = Helper.Rnd.Next(1, questTime - 1);
                }

                await player.SendMessage(Helper.GetRandomLine(StartingMessages));
                while (s.Elapsed < TimeSpan.FromSeconds(questTime))
                {
                    i++;
                    bool outcome = true;
                    await Task.Delay(tickTime * 1000);
                    if (i == encounterTick || i == encounterTick2)
                    {
                        outcome = MobEncounter.Start(player, Mobs.Mobs.GetRandomMobByLevel(player.GetLevel()));
                    }
                    if (!outcome)
                    {
                        isAlive = false;
                        break;
                    }
                }

                s.Reset();

                #region Room1

                if (isAlive)
                {
                    i = 0;
                    encounterTick = Helper.Rnd.Next(1, questTime);
                    await player.SendMessage(Helper.GetRandomLine(CastleRoom1));
                    while (s.Elapsed < TimeSpan.FromSeconds(questTime))
                    {
                        i++;
                        bool outcome = true;
                        await Task.Delay(tickTime * 1000);
                        if (i == encounterTick || i == encounterTick2)
                        {
                            outcome = MobEncounter.Start(player, Mobs.Mobs.GetMobById(4));
                        }
                        if (!outcome)
                        {
                            isAlive = false;
                            break;
                        }
                    }
                }

                if (isAlive)
                {
                    await player.SendMessage("Вы вошли в темное подземелье Замка...Надо выбрать направление", CastlePage.GetKeyboardSwitchTurn());
                }

                #endregion

                if (isAlive)
                {
                    var reward = new QuestRewards(QuestType.Castle, player);
                    player.AddStatsCounter("Заданий пройдено");
                    player.AddStatsCounter("Заданий в Замке пройдено");
                    player.AddGold(reward.gold);
                    player.AddExp(reward.exp);
                    player.AddItem(reward.items);
                    player.SetInQuest(false);
                    player.Persist();
                    await player.SendMessage(Helper.GetRandomLine(EndingMessages) + "\n\n" + "Вы успешно прошли квест в Замке!\n" + reward.rewardMessage, MainPage.GetKeyboard());
                }

                s.Stop();
            }
            catch (Exception ex)
            {
                StaticUtils.Logger.LogError("{0} {1}", ex.Message, ex.StackTrace);
            }
            finally
            {
                player.SetInQuest(false);
            }
        }

        /// <summary>
        /// Квест "Пещера"
        /// </summary>
        /// <param name="session"></param>
        public static async void CaveQuest(Player session)
        {
            try
            {

                List<string> StartingMessages = System.IO.File.ReadAllLines(pathToTextData + "CaveStart.txt").ToList();
                List<string> EndingMessages = System.IO.File.ReadAllLines(pathToTextData + "CaveEnd.txt").ToList();


                session.SetInQuest(true);
                bool isQuestCompleted = true;
                Stopwatch s = Stopwatch.StartNew();

                int i = 0;
                int questTime = 10;
                int tickTime = 1;
                int encounterTick = Helper.Rnd.Next(1, questTime);
                int encounterTick2 = Helper.Rnd.Next(1, questTime);
                while (encounterTick2 == encounterTick)
                {
                    encounterTick2 = Helper.Rnd.Next(1, questTime - 1);
                }

                await session.SendMessage(Helper.GetRandomLine(StartingMessages));
                while (s.Elapsed < TimeSpan.FromMinutes(questTime))
                {
                    i++;
                    bool outcome = true;
                    await Task.Delay(tickTime * 1000 * 60);
                    if (i == encounterTick || i == encounterTick2)
                    {
                        outcome = MobEncounter.Start(session, Mobs.Mobs.GetRandomMobByLevel(session.GetLevel()));
                    }
                    if (!outcome)
                    {
                        isQuestCompleted = false;
                        break;
                    }
                }

                if (isQuestCompleted)
                {
                    var reward = new QuestRewards(QuestType.Cave, session);
                    session.AddStatsCounter("Заданий пройдено");
                    session.AddStatsCounter("Заданий в Пещере пройдено");
                    session.AddGold(reward.gold);
                    session.AddExp(reward.exp);
                    session.AddItem(reward.items);
                    session.SetInQuest(false);
                    session.Persist();
                    await session.SendMessage(Helper.GetRandomLine(EndingMessages) + "\n\n" + "Вы успешно прошли квест в Пещере!\n" + reward.rewardMessage, MainPage.GetKeyboard());
                }

                s.Stop();
            }
            catch(Exception ex)
            {
                StaticUtils.Logger.LogError("{0} {1}", ex.Message, ex.StackTrace);
            }
            finally
            {
                session.SetInQuest(false);
            }
        }

        public static async void FailCurrentQuest(Player session)
        {
            if (session.GetInQuest())
            {
                session.SetInQuest(false);
                session.AddStatsCounter("Заданий провалено");
                session.AddExp(1);
                await session.SendMessage("Задание провалено. Вы получили 1 опыта за старания");
            }
        }
    }
}

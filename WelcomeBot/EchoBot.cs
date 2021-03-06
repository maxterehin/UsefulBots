﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Bot;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Core.Extensions;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;
using WelcomeBot.Dialogs;
using WelcomeBot.Models;

namespace WelcomeBot
{
    public class EchoBot : IBot
    {
        private const string MainMenuDialogId = "mainMenu";
        private DialogSet _dialogs { get; } = ComposeMainDialog();


        public async Task OnTurn(ITurnContext context)
        {
            if (context.Activity.Type is ActivityTypes.Message)
            {
                var userInfo = UserState<UserInfo>.Get(context);
                var conversationInfo = ConversationState<ConversationInfo>.Get(context);

                // Establish dialog state from the conversation state.
                var dc = _dialogs.CreateContext(context, conversationInfo);

                // Continue any current dialog.
                await dc.Continue();

                // Every turn sends a response, so if no response was sent,
                // then there no dialog is currently active.
                if (!context.Responded)
                {
                    //Если еще нет информации о заказе, надо ее получить
                    if (string.IsNullOrWhiteSpace(userInfo?.Order?.OrderId))
                    {
                        await dc.Begin(OrderDialog.Id);
                    }
                    // Otherwise, start our bot's main dialog.
                    else
                    {
                        await dc.Begin(MainMenuDialogId);
                    }
                }
            }
        }

        private static DialogSet ComposeMainDialog()
        {
            var dialogs = new DialogSet();

            dialogs.Add(MainMenuDialogId, new WaterfallStep[]
            {
                async (dc, args, next) =>
                {
                    var menu = new List<string> { "Выбрать продукты", "Посмотреть статистику" , "Отменить заказ" };
                    await dc.Context.SendActivity(MessageFactory.SuggestedActions(menu, "How can I help you?"));
                },
                async (dc, args, next) =>
                {
                    var result = (args["Activity"] as Activity)?.Text?.Trim().ToLowerInvariant();
                    switch (result)
                    {
                        //todo: в кнстанты или nameof
                        case "выбрать продукты":
                            await dc.Begin(ProductsDialog.Id);
                            break;
                        case "посмотреть статистику":
                            //todo: добавить просмотр статистики
                            //await dc.Begin(Stats.Id);
                            break;
                        case "отменить заказ":
                            var userState = UserState<UserInfo>.Get(dc.Context);
                            userState.Order = null;
                            userState.OrderedProducts = null;
                            break;
                        default:
                            await dc.Context.SendActivity("Не понимаю.");
                            await next();
                            break;
                    }
                },
                async (dc, args, next) =>
                {
                    // Show the main menu again.
                    await dc.Replace(MainMenuDialogId);
                }
            });

            dialogs.Add(OrderDialog.Id, OrderDialog.Instance);
            dialogs.Add(ProductsDialog.Id, ProductsDialog.Instance);
            return dialogs;
        }
    }
}

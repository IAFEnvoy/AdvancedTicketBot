﻿using AdvancedTicketBot.Ticket;
using AdvancedTicketBot.Utils;
using Kook;
using Kook.WebSocket;

namespace AdvancedTicketBot {
    public class AdvancedTicketBot {
        private static BotConfig config = new(Environment.CurrentDirectory + @"\main.json");
        public static TicketInfoManager TicketInfoManager = new(Environment.CurrentDirectory + @"\ticket_info.json");
        public static TicketManager TicketManager = new(Environment.CurrentDirectory + @"\ticket.json");
        private readonly KookSocketClient client;
        private readonly bool loadInternalBot;

        private static Task Log(LogMessage msg) {
            Logger.Info($"[{msg.Source}] {msg}");
            return Task.CompletedTask;
        }

        /**
         * 创建机器人实例
         * 如果作为SDK调用需要将loadInternalBot参数设置为false
         */
        public AdvancedTicketBot(bool loadInternalBot) {
            this.loadInternalBot = loadInternalBot;
            this.client = new KookSocketClient(new() {
                AlwaysDownloadVoiceStates = true,
                AlwaysDownloadUsers = true
            });
            if (!this.loadInternalBot) return;
            Config.StartAutoSave();
        }

        /**
         * 如果作为SDK调用不需要使用此方法
         */
        public async Task MainAsync() {
            if (!this.loadInternalBot) return;

            this.client.Log += Log;
            this.client.MessageReceived += this.Client_MessageReceived;
            this.client.MessageButtonClicked += this.Client_MessageButtonClicked;

            if (config.kookBotToken == "") {
                Logger.Error("未找到token");
                Environment.Exit(0);
            }
            await this.client.LoginAsync(TokenType.Bot, config.kookBotToken);
            await this.client.StartAsync();

            await Task.Delay(Timeout.Infinite);
        }

        /**
         * 消息接收模块，一定要记得调用
         */
        public async Task Client_MessageReceived(SocketMessage message, SocketGuildUser user, SocketTextChannel channel) {
            if (message.Source != MessageSource.User) return;
            Logger.Message($"<- TEXT {message.Author.Username} ({message.Author.Id}) [{message.Channel.Name}] {message.Content}");
            if (message.Content == "/ping") await message.AddReactionAsync(new Emoji("✅"));
            TicketManager.Tickets? tickets = TicketManager.GetTicketChannel(channel.Id);
            //Card类开票
            foreach (TicketInfoManager.TicketInfo info in TicketInfoManager.TicketInfos.Where(x => x.Type == TicketInfoManager.TicketInfoType.Card))
                if (message.Content == info.Command) {
                    if (!config.botAdmins.Contains(message.Author.Id)) {
                        await channel.SendTextAsync("Permission Denied");
                        continue;
                    }
                    await channel.SendCardAsync(TicketCards.SelectTicketCard(info).Build());
                    return;
                }
            //Command类开票
            foreach (TicketInfoManager.TicketInfo info in TicketInfoManager.TicketInfos.Where(x => x.Type == TicketInfoManager.TicketInfoType.Command))
                if (message.Content.StartsWith(info.Command) && (info.TriggerChannel == 0 || info.TriggerChannel == channel.Id)) {
                    await user.UpdateAsync();
                    if (info.OpenTicketRole.Count > 0 && user.Roles.Where(x => info.OpenTicketRole.Contains(x.Id)).ToList().Count == 0) {
                        await channel.SendTextAsync("你没有开此类票的权限");
                        continue;
                    }
                    SocketCategoryChannel? category = channel.Category as SocketCategoryChannel;
                    if (info.TicketCategoryId == 1) category = null;
                    else if (info.TicketCategoryId != 0) category = this.client.GetChannel(info.TicketCategoryId) as SocketCategoryChannel;
                    ITextChannel ticketChannel = await this.client.GetGuild(channel.Guild.Id).CreateTextChannelAsync(info.FormatTitle(info.Name, user), x => x.CategoryId = category?.Id ?? null);
                    TicketManager.AddTicket(user.Id, ticketChannel.Id, info.Id);
                    await ticketChannel.AddPermissionOverwriteAsync(user);
                    await ticketChannel.ModifyPermissionOverwriteAsync(user, x => x.Modify(viewChannel: PermValue.Allow, sendMessages: PermValue.Allow, attachFiles: PermValue.Allow));
                    await ticketChannel.SendCardAsync(TicketCards.OpenTicketCard(info.Content).Build());
                    if (info.AtInTicket) await ticketChannel.SendTextAsync($"(met){user.Id}(met)");
                    if (info.AllowAt) {
                        List<ulong> kookIds = message.Content[info.Command.Length..].Split("(met)").Where(x => x != "" && ulong.TryParse(x, out ulong _)).Select(x => Convert.ToUInt64(x)).ToList();
                        foreach (ulong kookId in kookIds) {
                            try {
                                await ticketChannel.AddPermissionOverwriteAsync(channel.Guild.GetUser(kookId));
                                await ticketChannel.ModifyPermissionOverwriteAsync(channel.Guild.GetUser(kookId), y => y.Modify(viewChannel: PermValue.Allow, sendMessages: PermValue.Allow, attachFiles: PermValue.Allow));
                            } catch (Exception) {
                                // ignored
                            }
                        }
                        string content = kookIds.Aggregate("", (p, c) => $"{p}(met){c}(met)");
                        Logger.Info(content);
                        if (content != "") await ticketChannel.SendTextAsync(content);
                    }
                    await channel.SendTextAsync($"已在(chn){ticketChannel.Id}(chn)完成开票");
                    return;
                }
            //添加用户
            if (tickets != null && message.Content.StartsWith("添加")) {
                await user.UpdateAsync();
                TicketInfoManager.TicketInfo? info = TicketInfoManager.GetTicketInfo(tickets.InfoId);
                if (info != null && info.CloseTicketRole.Count != 0 && user.Roles.All(x => !info.CloseTicketRole.Contains(x.Id))) {
                    await channel.SendTextAsync("你没有权限添加用户");
                    return;
                }
                IEnumerable<ulong> addUsers = message.Content.Split(" ").Where(x => ulong.TryParse(x, out ulong _)).Select(x => Convert.ToUInt64(x));
                foreach (ulong kookId in addUsers)
                    try {
                        await channel.AddPermissionOverwriteAsync(channel.Guild.GetUser(kookId));
                        await channel.ModifyPermissionOverwriteAsync(channel.Guild.GetUser(kookId), y => y.Modify(viewChannel: PermValue.Allow, sendMessages: PermValue.Allow, attachFiles: PermValue.Allow));
                    } catch (Exception) {
                        // ignored
                    }
            }
            //关票指令
            if (tickets != null && message.Content == "关票") {
                await user.UpdateAsync();
                TicketInfoManager.TicketInfo? info = TicketInfoManager.GetTicketInfo(tickets.InfoId);
                if (info != null && info.CloseTicketRole.Count != 0 && user.Roles.All(x => !info.CloseTicketRole.Contains(x.Id))) {
                    await channel.SendTextAsync("你没有权限进行关票");
                    return;
                }
                await channel.DeleteAsync();
            }
        }

        /**
         * 按钮消息接收模块，如果使用Card模式请记得调用
         */
        public async Task Client_MessageButtonClicked(string value, Cacheable<SocketGuildUser, ulong> user, Cacheable<IMessage, Guid> message, SocketTextChannel channel) {
            if (TicketInfoManager.ReturnValMap.TryGetValue(value, out Tuple<TicketInfoManager.TicketInfo, TicketInfoManager.CardTicketInfo>? info)) {
                if (info == null) return;
                await user.Value.UpdateAsync();
                if (info.Item2.OpenTicketRole.Count > 0 && user.Value.Roles.Where(x => info.Item2.OpenTicketRole.Contains(x.Id)).ToList().Count == 0) {
                    await channel.SendTextAsync("你没有开此类票的权限", ephemeralUser: user.Value);
                    return;
                }
                SocketCategoryChannel? category = channel.Category as SocketCategoryChannel;
                if (info.Item1.TicketCategoryId == 1) category = null;
                else if (info.Item1.TicketCategoryId != 0) category = this.client.GetChannel(info.Item1.TicketCategoryId) as SocketCategoryChannel;
                ITextChannel ticketChannel = await this.client.GetGuild(channel.Guild.Id).CreateTextChannelAsync(info.Item1.FormatTitle(info.Item2.ButtonName, user.Value), x => x.CategoryId = category?.Id ?? null);
                TicketManager.AddTicket(user.Id, ticketChannel.Id, info.Item1.Id);
                await ticketChannel.AddPermissionOverwriteAsync(user.Value);
                await ticketChannel.ModifyPermissionOverwriteAsync(user.Value, x => x.Modify(viewChannel: PermValue.Allow, sendMessages: PermValue.Allow, attachFiles: PermValue.Allow));
                await ticketChannel.SendCardAsync(TicketCards.OpenTicketCard(info.Item2.Content).Build());
                if (info.Item1.AtInTicket) await ticketChannel.SendTextAsync($"(met){user.Id}(met)");
                await channel.SendTextAsync($"已在(chn){ticketChannel.Id}(chn)完成开票", ephemeralUser: user.Value);
            }
        }
    }
}

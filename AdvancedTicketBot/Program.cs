using AdvancedTicketBot.Ticket;
using AdvancedTicketBot.Utils;
using Kook;
using Kook.Rest;
using Kook.WebSocket;

namespace AdvancedTicketBot {
    internal class Program {
        public static BotConfig config = new(Environment.CurrentDirectory + @"\main.json");
        public static TicketInfoManager TicketInfoManager = new(Environment.CurrentDirectory + @"\ticket_info.json");
        public static TicketManager TicketManager = new(Environment.CurrentDirectory + @"\ticket.json");
        private readonly KookSocketClient client;

        public static Task Main() {
            return new Program().MainAsync();
        }

        private static Task Log(LogMessage msg) {
            Logger.Info($"[{msg.Source}] {msg}");
            return Task.CompletedTask;
        }

        private Program() {
            this.client = new KookSocketClient(new() {
                AlwaysDownloadVoiceStates = true,
                AlwaysDownloadUsers = true
            });
            Config.StartAutoSave();
        }

        private async Task MainAsync() {
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

        private async Task Client_MessageReceived(SocketMessage message, SocketGuildUser user, SocketTextChannel channel) {
            if (message.Source != MessageSource.User) return;
            Logger.Message($"<- TEXT {message.Author.Username} ({message.Author.Id}) [{message.Channel.Name}] {message.Content}");
            if (message.Content == "/ping") await message.AddReactionAsync(new Emoji("✅"));
            TicketManager.Tickets? tickets = TicketManager.GetTicketChannel(channel.Id);
            //关票指令
            if (tickets != null && message.Content == "/关") {
                TicketInfoManager.TicketInfo? info = TicketInfoManager.GetTicketInfo(tickets.InfoId);
                if (info != null && info.CloseTicketRole.Count!=0 && user.Roles.All(x => !info.CloseTicketRole.Contains(x.Id))) {
                    await channel.SendTextAsync("你没有权限进行关票");
                    return;
                }
                await channel.DeleteAsync();
                return;
            }
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
        }

        private async Task Client_MessageButtonClicked(string value, Cacheable<SocketGuildUser, ulong> user, Cacheable<IMessage, Guid> message, SocketTextChannel channel) {
            if (TicketInfoManager.ReturnValMap.TryGetValue(value, out Tuple<TicketInfoManager.TicketInfo, TicketInfoManager.CardTicketInfo>? info)) {
                if (info == null) return;
                SocketCategoryChannel? category = channel.Category as SocketCategoryChannel;
                if (info.Item1.TicketCategoryId == 1) category = null;
                else if (info.Item1.TicketCategoryId != 0) category = this.client.GetChannel(info.Item1.TicketCategoryId) as SocketCategoryChannel;
                ITextChannel ticketChannel = await this.client.GetGuild(channel.Guild.Id).CreateTextChannelAsync(info.Item1.FormatTitle(info.Item2.Content, user.Value), x => x.CategoryId = category?.Id ?? null);
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

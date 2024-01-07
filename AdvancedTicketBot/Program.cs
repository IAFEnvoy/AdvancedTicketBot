using AdvancedTicketBot.Ticket;
using AdvancedTicketBot.Utils;
using Kook;
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
            Logger.Info($"[{msg.Source}] {msg.Message}");
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
            try {
                if (message.Source != MessageSource.User) return;
                Logger.Message($"<- TEXT {message.Author.Username} ({message.Author.Id}) [{message.Channel.Name}] {message.Content}");
                if (message.Content == "/ping") await message.AddReactionAsync(new Emoji("✅"));
                string[] m = message.Content.Split(" ");
                foreach (TicketInfoManager.TicketInfo info in TicketInfoManager.TicketInfos.Where(x => x.Type == TicketInfoManager.TicketInfoType.Command))
                    if (info.Command == m[0] && (info.TriggerChannel == 0 || info.TriggerChannel == channel.Id)) {
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
                            List<ulong> kookIds = message.Content[m[0].Length..].Split("(met)").Where(x => x != "" && ulong.TryParse(x, out ulong _)).Select(x => Convert.ToUInt64(x)).ToList();
                            foreach (ulong kookId in kookIds) {
                                try {
                                    await ticketChannel.AddPermissionOverwriteAsync(channel.Guild.GetUser(kookId));
                                    await ticketChannel.ModifyPermissionOverwriteAsync(channel.Guild.GetUser(kookId), y => y.Modify(viewChannel: PermValue.Allow, sendMessages: PermValue.Allow, attachFiles: PermValue.Allow));
                                } catch (Exception _) {
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
                TicketManager.Tickets? tickets = TicketManager.GetTicketChannel(channel.Id);
                if (tickets != null && message.Content == "/关") {
                    TicketInfoManager.TicketInfo? info = TicketInfoManager.GetTicketInfo(tickets.InfoId);
                    if (info != null && info.CloseTicketRole != 0 && user.Roles.All(x => x.Id != info.CloseTicketRole)) {
                        await channel.SendTextAsync("你没有权限进行关票");
                        return;
                    }
                    await channel.DeleteAsync();
                }
            } catch (Exception e) {
                Logger.Error(e.ToString());
            }
        }

        private async Task Client_MessageButtonClicked(string value, Cacheable<SocketGuildUser, ulong> user, Cacheable<IMessage, Guid> message, SocketTextChannel channel) {

        }
    }
}

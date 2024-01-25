using AdvancedTicketBot.Utils;
using Kook.WebSocket;
using System.Text.Json;

namespace AdvancedTicketBot.Ticket {
    public class TicketInfoManager : Config {
        public List<TicketInfo> TicketInfos = new();
        public Dictionary<string, TicketInfo> InfoMap = new();
        public Dictionary<string, Tuple<TicketInfo, CardTicketInfo>?> ReturnValMap = new();

        public TicketInfoManager(string path) : base(path) {
        }

        public override void ForceSave() {
            throw new NotSupportedException("这玩意是只读文件呀");
        }

        public override void Load() {
            StreamReader sr = new(this.configPath);
            this.TicketInfos = JsonSerializer.Deserialize<List<TicketInfo>>(sr.ReadToEnd(), optionIncludeFields) ?? new();
            sr.Close();
            foreach (TicketInfo ticketInfo in this.TicketInfos)
                if (this.InfoMap.ContainsKey(ticketInfo.Id)) {
                    Logger.Info($"以下id出现冲突，id不能相同：{ticketInfo.Id}");
                    Environment.Exit(-1);
                } else {
                    this.InfoMap[ticketInfo.Id] = ticketInfo;
                    if (ticketInfo.Type != TicketInfoType.Card) continue;
                    foreach (CardTicketInfo cardTicketInfo in ticketInfo.TicketInfos) {
                        if (this.ReturnValMap.ContainsKey(cardTicketInfo.EventId)) {
                            Logger.Info($"以下event-id出现冲突，event-id不能相同：{cardTicketInfo.EventId} from {ticketInfo.Id}");
                            Environment.Exit(-1);
                        } else
                            this.ReturnValMap[cardTicketInfo.EventId] = new(ticketInfo, cardTicketInfo);
                    }
                }
        }

        /**
         * 根据id获取TicketInfo对象
         */
        public TicketInfo? GetTicketInfo(string id) {
            return this.InfoMap.TryGetValue(id, out TicketInfo? info) ? info : null;
        }

        // ReSharper disable UnusedAutoPropertyAccessor.Global
        // ReSharper disable CollectionNeverUpdated.Global
#pragma warning disable CS8618
        public class TicketInfo {
            //唯一Id
            public string Id { init; get; }
            //Ticket配置类型，0->Card表示卡片+按钮触发，1->Command表示指令触发
            public TicketInfoType Type { init; get; }
            //Card：发送开票卡片指令。Command：触发开票指令。
            public string Command { init; get; }
            //Card：卡片标题。Command：开票标题
            public string Name { init; get; }
            //Card：子开票信息。Command：无作用
            public List<CardTicketInfo> TicketInfos { init; get; }
            //开票频道所属分组Id，填0表示和触发频道放一起，填1表示放外面
            public ulong TicketCategoryId { init; get; }
            //Card：开票卡片备注。Command：开票成功卡片显示内容
            public string Content { init; get; }
            //Card：无作用。Command：可触发此指令的频道，填0表示不限制
            public ulong TriggerChannel { init; get; }
            //是否在Ticket中At开票人
            public bool AtInTicket { init; get; }
            //Card：无作用。Command：将at的人也拉入开票频道
            public bool AllowAt { init; get; }
            //开票频道标题格式，可用占位符：%title%, %user_name%, %user_id%, %display_name%
            public string TitleFormat { init; get; }
            //Card：无作用。Command：可执行开票的身份组，留空表示不限制
            public List<uint> OpenTicketRole { init; get; }
            //可执行关闭开票的身份组，留空表示不限制
            public List<uint> CloseTicketRole { init; get; }

            public string FormatTitle(string title, SocketGuildUser user) {
                return this.TitleFormat.Replace("%title%", title).Replace("%user_name%", user.Username).Replace("%user_id%", user.Id.ToString()).Replace("%display_name%", user.DisplayName);
            }
        }

        public class CardTicketInfo {
            //按钮内容
            public string ButtonName { init; get; }
            //按钮按下发送的返回值，不要重复
            public string EventId { init; get; }
            //开票成功卡片显示内容
            public string Content { init; get; }
            //可执行开票的身份组，留空表示不限制
            public List<uint> OpenTicketRole { init; get; }
        }

        public enum TicketInfoType {
            Card = 0, Command = 1
        }
    }
}

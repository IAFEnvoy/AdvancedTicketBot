using AdvancedTicketBot.Utils;
using Kook.WebSocket;
using System.Text.Json;

namespace AdvancedTicketBot.Ticket {
    public class TicketInfoManager : Config {
        public List<TicketInfo> TicketInfos = new();
        public Dictionary<string, TicketInfo> InfoMap = new();

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
                } else
                    this.InfoMap[ticketInfo.Id] = ticketInfo;
        }

        public TicketInfo? GetTicketInfo(string id) {
            return this.InfoMap.TryGetValue(id, out TicketInfo? info) ? info : null;
        }

        public class TicketInfo {
            //唯一Id
            public string Id { init; get; }
            //Ticket配置类型，0->Card表示卡片+按钮触发，1->Command表示指令触发
            public TicketInfoType Type { init; get; }
            //Card：发送开票卡片指令。Command：触发开票指令。
            public string Command { init; get; }
            //Card：卡片标题。Command：开票标题
            public string Name { init; get; }
            //Card：子开票标题。Command：无作用
            public List<string> Title { init; get; }
            //开票频道所属分组Id，填0表示和触发频道放一起，填1表示放外面
            public ulong TicketCategoryId { init; get; }
            //开票成功卡片显示内容
            public string Content { init; get; }
            //Card：无作用。Command：可触发此指令的频道，填0表示不限制
            public ulong TriggerChannel { init; get; }
            //是否在Ticket中At开票人
            public bool AtInTicket { init; get; }
            //Card：无作用。Command：将at的人也拉入开票频道
            public bool AllowAt { init; get; }
            //开票频道标题格式，可用占位符：%title%, %user_name%, %user_id%
            public string TitleFormat { init; get; }
            //可执行关闭开票的身份组，填0表示不限制
            public uint CloseTicketRole { init; get; }

            public string FormatTitle(string title, SocketUser user) {
                return this.TitleFormat.Replace("%title%", title).Replace("%user_name%", user.Username).Replace("%user_id%", user.Id.ToString());
            }
        }

        public enum TicketInfoType {
            Card = 0, Command = 1
        }
    }
}

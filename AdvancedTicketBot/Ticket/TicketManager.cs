using AdvancedTicketBot.Utils;
using System.Text.Json;

namespace AdvancedTicketBot.Ticket {
    public class TicketManager : Config {
        public List<Tickets> TicketsList = new();

        public TicketManager(string path) : base(path) {
        }

        public override void ForceSave() {
            StreamWriter sw = new(this.configPath, false);
            sw.Write(JsonSerializer.Serialize(this.TicketsList, optionIncludeFields));
            sw.Close();
        }

        public override void Load() {
            StreamReader sr = new(this.configPath);
            this.TicketsList = JsonSerializer.Deserialize<List<Tickets>>(sr.ReadToEnd(), optionIncludeFields) ?? new();
            sr.Close();
        }
        
        /**
         * 添加开票
         */
        public void AddTicket(ulong ownerId, ulong channelId, string infoId) {
            this.TicketsList.Add(new Tickets {
                OwnerUserId = ownerId,
                TicketChannelId = channelId,
                InfoId = infoId
            });
            this.Save();
        }

        /**
         * 根据频道id获取开票信息
         */
        public Tickets? GetTicketChannel(ulong id) {
            List<Tickets> list = this.TicketsList.Where(x => x.TicketChannelId == id).ToList();
            return list.Count != 0 ? list[0] : null;
        }

#pragma warning disable CS8618
        public class Tickets {
            public ulong OwnerUserId { init; get; }
            public ulong TicketChannelId { init; get; }
            public string InfoId { init; get; }
        }
    }
}

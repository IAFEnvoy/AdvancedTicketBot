using AdvancedTicketBot.Utils;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AdvancedTicketBot {
    internal class BotConfig : Config {
        [JsonPropertyName("token")]
        public string kookBotToken = "";
        [JsonPropertyName("admins")]
        public List<ulong> botAdmins = new();

        public BotConfig() : base("") { }

        public BotConfig(string path) : base(path) { }

        public override void ForceSave() {
            StreamWriter sw = new(this.configPath, false);
            sw.Write(JsonSerializer.Serialize(this, optionIncludeFields));
            sw.Close();
        }

        public override void Load() {
            StreamReader sr = new(this.configPath);
            BotConfig another = JsonSerializer.Deserialize<BotConfig>(sr.ReadToEnd(), optionIncludeFields) ?? new();
            this.CopyFrom(another);
            sr.Close();
        }

        private void CopyFrom(BotConfig another) {
            this.kookBotToken = another.kookBotToken;
            this.botAdmins = another.botAdmins;
        }
    }
}

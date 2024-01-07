using System.Text.Json;
using System.Timers;
using Timer = System.Timers.Timer;

namespace AdvancedTicketBot.Utils {
    public abstract class Config {
        private static readonly List<Config> configs = new();
        private const int maxUnSaveCount = 10;
        public static readonly JsonSerializerOptions optionIncludeFields = new() { IncludeFields = true };
        protected readonly string configPath;
        private bool shouldSave;
        private int saveCount;

        public static void StartAutoSave() {
            Timer timer = new(60 * 1000);
            timer.Elapsed += SaveAllConfig;
            timer.AutoReset = true;
            timer.Start();
        }

        private static void SaveAllConfig(object? sender, ElapsedEventArgs e) {
            SaveAllConfig();
        }

        public static void SaveAllConfig() {
            lock (configs) {
                foreach (Config config in configs.Where(config => config.shouldSave)) {
                    Logger.Info($"正在保存{config.configPath}");
                    try {
                        config.ForceSave();
                        Logger.Info($"保存{config.configPath}成功");
                    } catch (Exception ex) {
                        Logger.Info($"保存{config.configPath}失败：{ex.Message}");
                    }
                    config.saveCount = 0;
                    config.shouldSave = false;
                }
            }
        }

        protected Config(string path) {
            this.configPath = path;
            configs.Add(this);
            if (path == "") return;
            if (File.Exists(this.configPath)) {
                Logger.Info($"正在加载 {this.configPath}");
                this.Load();
            } else
                Logger.Warn($"未找到文件 {this.configPath}");
        }

        public void Save() {
            this.shouldSave = true;
            this.saveCount++;
            if (this.saveCount < maxUnSaveCount) return;
            Logger.Info($"正在保存 {this.configPath}");
            this.ForceSave();
            this.saveCount = 0;
            this.shouldSave = false;
        }

        public abstract void ForceSave();

        public abstract void Load();
    }
}

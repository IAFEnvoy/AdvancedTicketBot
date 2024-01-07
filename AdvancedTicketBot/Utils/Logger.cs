namespace AdvancedTicketBot.Utils {
    static internal class Logger {
        public static void Message(string message) {
            Console.ForegroundColor = ConsoleColor.Blue;
            Console.Write(DateTime.Now.ToString("[HH:mm:ss] "));
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine(message);
        }
        public static void Info(string message) {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write(DateTime.Now.ToString("[HH:mm:ss] "));
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine(message);
        }
        public static void Warn(string message) {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write(DateTime.Now.ToString("[HH:mm:ss] "));
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine(message);
        }
        public static void Error(string message) {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Write(DateTime.Now.ToString("[HH:mm:ss] "));
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine(message);
        }
    }
}

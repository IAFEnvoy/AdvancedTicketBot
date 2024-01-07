using Kook;

namespace AdvancedTicketBot.Ticket {
    public class TicketCards {
        public static CardBuilder OpenTicketCard(string content) {
            return new CardBuilder().WithTheme(CardTheme.Primary)
                .AddModule(new HeaderModuleBuilder {
                    Text = "服务器开票系统"
                })
                .AddModule(new SectionModuleBuilder().WithText(content, true))
                .AddModule(new SectionModuleBuilder().WithText("输入`/关`以关票", true));
        }
    }
}

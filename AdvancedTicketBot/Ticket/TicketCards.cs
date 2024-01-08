using Kook;

namespace AdvancedTicketBot.Ticket {
    public static class TicketCards {
        public static CardBuilder OpenTicketCard(string content) {
            return new CardBuilder().WithTheme(CardTheme.Primary)
                .AddModule(new HeaderModuleBuilder {
                    Text = "服务器开票系统"
                })
                .AddModule(new SectionModuleBuilder().WithText(content, true))
                .AddModule(new SectionModuleBuilder().WithText("输入`/关`以关票", true));
        }
        public static CardBuilder SelectTicketCard(TicketInfoManager.TicketInfo info) {
            CardBuilder cb = new CardBuilder().WithTheme(CardTheme.Primary).WithSize(CardSize.Large)
                .AddModule(new HeaderModuleBuilder {
                    Text = info.Name
                })
                .AddModule(new SectionModuleBuilder().WithText(info.Content, true));
            ActionGroupModuleBuilder actions = new();
            foreach (TicketInfoManager.CardTicketInfo i in info.TicketInfos)
                actions.AddElement(new ButtonElementBuilder(i.ButtonName).WithValue(i.EventId).WithClick(ButtonClickEventType.ReturnValue));
            return cb.AddModule(actions);
        }
    }
}

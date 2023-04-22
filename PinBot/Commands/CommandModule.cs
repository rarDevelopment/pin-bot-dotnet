using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PinBot.Modules
{
    public class CommandModule : ModuleBase<CommandModule>
    {
        [SlashCommand("test", "Just a test command")]
        public async Task TestCommand()
            => await RespondAsync("Hello There");

    }
}
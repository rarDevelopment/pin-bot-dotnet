using Discord;
using Discord.Interactions;
using System.Threading.Tasks;

namespace PinBot.Modules
{
    public abstract class ModuleBase<T> : InteractionModuleBase<SocketInteractionContext>
    {
        public InteractionService Commands { get; set; }

        private ILogger<T> _logger { get; set; }

    }
}
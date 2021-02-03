using Rocket.API;
using Rocket.Core.Utils;
using Rocket.Unturned.Chat;
using Rocket.Unturned.Player;
using System.Collections.Generic;

namespace BowieD.Unturned.BadApple.Plugin.Commands
{
    public sealed class CommandBadApple : IRocketCommand
    {
        public AllowedCaller AllowedCaller => AllowedCaller.Player;
        public string Name => "badapple";
        public string Help => "Bad Apple!!";
        public string Syntax => "<init/play>";
        public List<string> Aliases => new List<string>();
        public List<string> Permissions => new List<string>();
        public void Execute(IRocketPlayer caller, string[] command)
        {
            if (caller is UnturnedPlayer up)
            {
                if (command.Length > 0)
                {
                    switch (command[0].ToLowerInvariant())
                    {
                        case "init":
                            {
                                if (Plugin.isReady)
                                    return;

                                Plugin.InitScreenAt(up.Position);
                                UnturnedChat.Say(caller, "screen init");
                                Plugin.PrepareBadApple();
                                UnturnedChat.Say(caller, "prepared");
                            }
                            break;
                        case "play":
                            {
                                if (Plugin.isPlaying)
                                    return;

                                TaskDispatcher.QueueOnMainThread(async () =>
                                {
                                    await Plugin.PlayBadApple();
                                });
                            }
                            break;
                        case "stop":
                            {
                                if (!Plugin.isPlaying)
                                    return;

                                Plugin.demandStop = true;
                            }
                            break;
                    }
                }
            }
        }
    }
}

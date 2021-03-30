using System;

namespace Oxide.Plugins
{
    [Info("PerformanceTester", "TeamDMA", "0.0.1")]
    [Description("Some performance tests")]
    class PerformanceTester : RustPlugin
    {
        void OnPlayerCommand(BasePlayer player, string command, string[] args)
        {
            if(args.Length > 0)
            {
                Puts(player.displayName + " used the command '/" + command + " " + String.Join(" ", args) + "'");
            }
            else
            {
                Puts(player.displayName + " used the command '/" + command + "'");
            }
        }
    }
}

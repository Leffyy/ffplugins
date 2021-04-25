# DalamudPluginProjectTemplate
An opinionated Visual Studio project template for Dalamud plugins, with attributes for more maintainable command setup and teardown.

## Attributes
This template also includes an attribute framework reminiscent of [Discord.Net](https://github.com/discord-net/Discord.Net).

```csharp
[Command("/example1")]
[HelpMessage("Example help message.")]
public void ExampleCommand1(string command, string args)
{
    var chat = this.pluginInterface.Framework.Gui.Chat;
    var world = this.pluginInterface.ClientState.LocalPlayer.CurrentWorld.GameData;
    chat.Print($"Hello {world.Name}!");
    PluginLog.Log("Message sent successfully.");
}
```

This automatically register and unregister the methods that they're attached to on setup and dispose.

## GitHub Actions
Running the shell script `DownloadGithubActions.sh` will download some useful GitHub actions for you. You can also delete this file if you have no need for it.
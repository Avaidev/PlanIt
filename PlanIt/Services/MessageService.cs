using System.Threading.Tasks;
using MsBox.Avalonia;
using MsBox.Avalonia.Enums;

namespace PlanIt.Services;

public class MessageService
{
    public MessageService() {}

    public static async Task ErrorMessage(string message)
    {
        var messageBox = MessageBoxManager.GetMessageBoxStandard("Error",
            message + " ", ButtonEnum.Ok, Icon.Error);
        await messageBox.ShowAsync();
    }

    public static async Task WarningMessage(string message)
    {
        var messageBox = MessageBoxManager.GetMessageBoxStandard("Warning", message, ButtonEnum.Ok, Icon.Warning);
        await messageBox.ShowAsync();
    }

    public static async Task<bool?> AskYesNoCancelMessage(string message)
    {
        var messageBox =
            MessageBoxManager.GetMessageBoxStandard("Choose variant", message, ButtonEnum.YesNoCancel, Icon.Question);
        var buttonResult = await messageBox.ShowAsync();
        return buttonResult switch
        {
            ButtonResult.Yes => true,
            ButtonResult.No => false,
            _ => null
        };
    }

    public static async Task<bool> AskYesNoMessage(string message)
    {
        var messageBox =
            MessageBoxManager.GetMessageBoxStandard("Choose variant", message, ButtonEnum.YesNo, Icon.Question);
        var buttonResult = await messageBox.ShowAsync();
        return buttonResult switch
        {
            ButtonResult.Yes => true,
            _ => false
        };
    }
}
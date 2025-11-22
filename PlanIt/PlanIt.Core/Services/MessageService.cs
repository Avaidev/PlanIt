using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using MsBox.Avalonia;
using MsBox.Avalonia.Dto;
using MsBox.Avalonia.Enums;

namespace PlanIt.Core.Services;

public static class MessageService
{
    private static Window GetActiveWindow()
    {
        var lifetime = (IClassicDesktopStyleApplicationLifetime)Application.Current.ApplicationLifetime;
        return lifetime.Windows.FirstOrDefault(x => x.IsActive) ?? lifetime.MainWindow;
    }
    
    public static async Task ErrorMessage(string message, Window? owner = null)
    {
        owner ??= GetActiveWindow();
        var messageBox = MessageBoxManager.GetMessageBoxStandard(new MessageBoxStandardParams
        {
            ButtonDefinitions = ButtonEnum.Ok,
            ContentTitle = "Error",
            ContentMessage = message,
            Icon = Icon.Error,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            CanResize = false,
            ShowInCenter = true,
            Topmost = true,
            SystemDecorations = SystemDecorations.Full
        });
        await messageBox.ShowWindowDialogAsync(owner);
    }

    public static async Task WarningMessage(string message, Window? owner = null)
    {
        owner ??= GetActiveWindow();
        var messageBox = MessageBoxManager.GetMessageBoxStandard(new MessageBoxStandardParams
        {
            ButtonDefinitions = ButtonEnum.Ok,
            ContentTitle = "Warning",
            ContentMessage = message,
            Icon = Icon.Warning,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            CanResize = false,
            ShowInCenter = true,
            Topmost = true,
            SystemDecorations = SystemDecorations.Full
        });
        await messageBox.ShowWindowDialogAsync(owner);
    }

    public static async Task<bool?> AskYesNoCancelMessage(string message, Window? owner = null)
    {
        owner ??= GetActiveWindow();
        var messageBox =
            MessageBoxManager.GetMessageBoxStandard(new MessageBoxStandardParams
            {
                ButtonDefinitions = ButtonEnum.YesNoCancel,
                ContentTitle = "Choose",
                ContentMessage = message,
                Icon = Icon.Question,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                CanResize = false,
                ShowInCenter = true,
                Topmost = true,
                SystemDecorations = SystemDecorations.Full
            });
        var buttonResult = await messageBox.ShowWindowDialogAsync(owner);
        return buttonResult switch
        {
            ButtonResult.Yes => true,
            ButtonResult.No => false,
            _ => null
        };
    }

    public static async Task<bool> AskYesNoMessage(string message, Window? owner = null)
    {
        owner ??= GetActiveWindow();
        var messageBox =
            MessageBoxManager.GetMessageBoxStandard(new MessageBoxStandardParams
            {
                ButtonDefinitions = ButtonEnum.YesNo,
                ContentTitle = "Choose",
                ContentMessage = message,
                Icon = Icon.Question,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                CanResize = false,
                ShowInCenter = true,
                Topmost = true,
                SystemDecorations = SystemDecorations.Full
            });
        var buttonResult = await messageBox.ShowWindowDialogAsync(owner);
        return buttonResult switch
        {
            ButtonResult.Yes => true,
            _ => false
        };
    }
}
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using Snap.Hutao.Remastered.Core.Annotation;
using Snap.Hutao.Remastered.Core.Threading;
using Snap.Hutao.Remastered.Factory.ContentDialog;
using Snap.Hutao.Remastered.Service.Plugin;
using System.Windows.Input;
using Snap.Hutao.Remastered.Service.Notification;

namespace TestPluginSDK.ViewModel;

[BindableCustomPropertyProvider]
public class TestPluginPageViewModel : Snap.Hutao.Remastered.ViewModel.Abstraction.ViewModel
{
    private readonly IPluginSettingService pluginSettingService;
    private readonly IContentDialogFactory contentDialogFactory;
    private readonly ITaskContext taskContext;
    private readonly IMessenger messenger;

    public TestPluginPageViewModel(IServiceProvider serviceProvider)
    {
        this.pluginSettingService = serviceProvider.GetRequiredService<global::Snap.Hutao.Remastered.Service.Plugin.IPluginSettingService>();
        this.contentDialogFactory = serviceProvider.GetRequiredService<global::Snap.Hutao.Remastered.Factory.ContentDialog.IContentDialogFactory>();
        this.taskContext = serviceProvider.GetRequiredService<global::Snap.Hutao.Remastered.Core.Threading.ITaskContext>();
        this.messenger = serviceProvider.GetRequiredService<global::CommunityToolkit.Mvvm.Messaging.IMessenger>();

        ShowCounterCommand = new RelayCommand(ShowCounter);
        IncrementCounterCommand = new RelayCommand(IncrementCounter);
        UpdateMessageCommand = new RelayCommand<string>(UpdateMessage);
    }

    public int Counter
    {
        get => TestPlugin.Counter.Value;
        set
        {
            int counter = TestPlugin.Counter.Value;
            if (SetProperty(ref counter, value))
            {
                TestPlugin.Counter.Value = value;
                OnPropertyChanged(nameof(Counter));
            }
        }
    }

    public string Message
    {
        get => TestPlugin.Message.Value!;
        set
        {
            string message = TestPlugin.Message.Value!;
            if (SetProperty(ref message, value))
            {
                TestPlugin.Message.Value = value;
                OnPropertyChanged(nameof(Message));
            }
        }
    }

    public ICommand ShowCounterCommand { get; }
    public ICommand IncrementCounterCommand { get; }
    public ICommand UpdateMessageCommand { get; }


    private void ShowCounter()
    {
        messenger.Send(InfoBarMessage.Information($"Counter is {Counter}"));
    }

    private void IncrementCounter()
    {
        Counter++;
        messenger.Send(InfoBarMessage.Success($"Counter incremented to {Counter}"));
    }

    private void UpdateMessage(string? newMessage)
    {
        if (!string.IsNullOrWhiteSpace(newMessage))
        {
            Message = newMessage;
            messenger.Send(InfoBarMessage.Success($"Message updated to: {Message}"));
        }
        else
        {
            messenger.Send(InfoBarMessage.Warning("Please enter a valid message"));
        }
    }
}

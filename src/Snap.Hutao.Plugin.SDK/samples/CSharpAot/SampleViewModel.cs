using System.ComponentModel;
using System.Windows.Input;
using Snap.Hutao.Plugin.Contracts;
using Snap.Hutao.Plugin.SDK;

namespace TestPlugin.Aot;

[WinRT.GeneratedBindableCustomProperty]
public sealed partial class SampleViewModel : INotifyPropertyChanged, IDisposable
{
    private readonly PluginContext context;
    private readonly PluginSetting<int> counter;
    private readonly PluginSetting<string> message;
    private readonly IDisposable counterSubscription;
    private readonly IDisposable messageSubscription;
    private string draft;
    private bool closed;

    public SampleViewModel(PluginContext context, PluginSetting<int> counter, PluginSetting<string> message)
    {
        this.context = context;
        this.counter = counter;
        this.message = message;
        draft = message.Value;
        IncrementCommand = new SampleCommand(context, async () => await counter.SetValueAsync(counter.Value + 1));
        SaveCommand = new SampleCommand(context, async () =>
        {
            if (string.IsNullOrWhiteSpace(Message))
            {
                context.Notify(NotificationKind.Warning, "Message cannot be empty.");
                return;
            }
            await message.SetValueAsync(Message);
            context.Notify(NotificationKind.Success, "Message saved.");
        });
        counterSubscription = counter.Subscribe(() => PropertyChanged?.Invoke(this, new(nameof(Counter))));
        messageSubscription = message.Subscribe(() => Message = message.Value);
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    public int Counter => counter.Value;
    public string Message
    {
        get => draft;
        set
        {
            if (draft == value) return;
            draft = value;
            PropertyChanged?.Invoke(this, new(nameof(Message)));
        }
    }
    public ICommand IncrementCommand { get; }
    public ICommand SaveCommand { get; }

    public void Dispose()
    {
        if (closed) return;
        closed = true;
        ((SampleCommand)IncrementCommand).Close();
        ((SampleCommand)SaveCommand).Close();
        counterSubscription.Dispose();
        messageSubscription.Dispose();
    }
}

public sealed partial class SampleCommand(PluginContext context, Func<Task> execute) : ICommand
{
    private bool busy;
    private bool closed;
    public event EventHandler? CanExecuteChanged;
    public bool CanExecute(object? parameter) => !busy && !closed;

    public async void Execute(object? parameter)
    {
        if (!CanExecute(parameter)) return;
        busy = true;
        CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        try
        {
            await context.RunOnUiAsync(async () =>
            {
                try { await execute(); }
                catch (Exception error) { if (!closed) context.Log(NotificationKind.Error, error.ToString()); }
                finally
                {
                    busy = false;
                    if (!closed) CanExecuteChanged?.Invoke(this, EventArgs.Empty);
                }
            });
        }
        catch (Exception error) { System.Diagnostics.Debug.WriteLine(error); busy = false; }
    }

    internal void Close() { closed = true; CanExecuteChanged?.Invoke(this, EventArgs.Empty); }
}

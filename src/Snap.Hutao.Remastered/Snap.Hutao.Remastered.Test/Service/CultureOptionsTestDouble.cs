using Snap.Hutao.Remastered.Core.Property;

namespace Snap.Hutao.Remastered.Service;

// Replace only the database property factory. CurrentCulture is linked from the application source.
public sealed partial class CultureOptions
{
    private readonly IReadOnlyDictionary<string, string> settings;

    public CultureOptions(IReadOnlyDictionary<string, string> settings)
    {
        this.settings = settings;
    }

    private IObservableProperty<T> CreatePropertyForClassUsingCustom<T>(string key, T defaultValue,
        Func<string, T> from, Func<T, string> to)
        where T : class
    {
        T value = settings.TryGetValue(key, out string? saved) && !string.IsNullOrEmpty(saved)
            ? from(saved)
            : defaultValue;
        return new TestProperty<T> { Value = value };
    }

    private sealed class TestProperty<T> : IObservableProperty<T>
    {
        public T Value { get; set; } = default!;

        public event PropertyChangedEventHandler? PropertyChanged
        {
            add { }
            remove { }
        }

        public INotifyPropertyChangedDeferral GetDeferral()
        {
            throw new NotSupportedException();
        }
    }
}

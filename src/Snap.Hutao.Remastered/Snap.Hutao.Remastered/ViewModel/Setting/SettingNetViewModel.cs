using CommunityToolkit.Mvvm.Input;
using Snap.Hutao.Remastered.Factory.ContentDialog;
using Snap.Hutao.Remastered.Model;
using Snap.Hutao.Remastered.Service;
using Snap.Hutao.Remastered.Service.Git;
using Snap.Hutao.Remastered.Web.Hutao;
using Snap.Hutao.Remastered.UI.Xaml.View.Dialog;
using System.Collections.Immutable;

namespace Snap.Hutao.Remastered.ViewModel.Setting;

[BindableCustomPropertyProvider]
[Service(ServiceLifetime.Scoped)]
public sealed partial class SettingNetViewModel : Abstraction.ViewModel
{
    private static readonly NameValue<string> AutoDomainOption = new("自动", GitRepositoryDomainSetting.Auto);

    private readonly HutaoInfrastructureClient hutaoInfrastructureClient;
    private readonly IServiceProvider serviceProvider;
    private readonly IContentDialogFactory contentDialogFactory;

    public AppOptions AppOptions { get; }

    public SettingNetViewModel(IServiceProvider serviceProvider)
    {
        this.serviceProvider = serviceProvider;
        hutaoInfrastructureClient = serviceProvider.GetRequiredService<HutaoInfrastructureClient>();
        contentDialogFactory = serviceProvider.GetRequiredService<IContentDialogFactory>();
        AppOptions = serviceProvider.GetRequiredService<AppOptions>();
    }

    public ImmutableArray<NameValue<string>> GitRepositoryDomainOptions
    {
        get => field == default ? [AutoDomainOption] : field;
        private set => SetProperty(ref field, value);
    }

    // TODO: Replace with IObservableProperty
    public NameValue<string>? SelectedGitRepositoryDomainOption
    {
        get
        {
            string selectedDomain = AppOptions.GitRepositoryDomainOverride.Value;
            return field ??= FindDomainOption(GitRepositoryDomainOptions, selectedDomain) ?? AutoDomainOption;
        }
        set
        {
            if (SetProperty(ref field, value) && value is not null)
            {
                AppOptions.GitRepositoryDomainOverride.Value = value.Value;
            }
        }
    }

    public async ValueTask InitializeGitRepositoryDomainOptionsAsync(CancellationToken token)
    {
        ImmutableArray<NameValue<string>> options = [AutoDomainOption, .. await GetDomainOptionsFromApiAsync(token).ConfigureAwait(true)];
        string configuredDomain = AppOptions.GitRepositoryDomainOverride.Value;

        NameValue<string>? selected = FindDomainOption(options, configuredDomain);
        if (selected is null)
        {
            configuredDomain = GitRepositoryDomainSetting.Auto;
            AppOptions.GitRepositoryDomainOverride.Value = configuredDomain;
            selected = AutoDomainOption;
        }

        GitRepositoryDomainOptions = options;
        SelectedGitRepositoryDomainOption = selected;
    }

    [Command("OpenGitSpeedTestDialogCommand")]
    private async Task OpenGitSpeedTestDialogAsync()
    {
        GitSourcesSpeedTestDialog dialog = await contentDialogFactory.CreateInstanceAsync<GitSourcesSpeedTestDialog>(serviceProvider);
        await dialog.ShowAsync();
    }

    private async ValueTask<ImmutableArray<NameValue<string>>> GetDomainOptionsFromApiAsync(CancellationToken token)
    {
        ImmutableHashSet<string>.Builder hosts = ImmutableHashSet.CreateBuilder<string>(StringComparer.OrdinalIgnoreCase);
        await AddDomainFromRepositoryNameAsync("Snap.Metadata", hosts, token).ConfigureAwait(false);
        await AddDomainFromRepositoryNameAsync("Snap.ContentDelivery", hosts, token).ConfigureAwait(false);

        return [.. hosts
            .OrderBy(static host => host, StringComparer.OrdinalIgnoreCase)
            .Select(static host => new NameValue<string>(host, host))];
    }

    private async ValueTask AddDomainFromRepositoryNameAsync(string repositoryName, ImmutableHashSet<string>.Builder hosts, CancellationToken token)
    {
        Web.Hutao.Response.HutaoResponse<ImmutableArray<GitRepository>> response = await hutaoInfrastructureClient.GetGitRepositoryAsync(repositoryName, token).ConfigureAwait(false);
        ImmutableArray<GitRepository> repositories = response.Data;
        if (response.ReturnCode != 0 || repositories.IsDefaultOrEmpty)
        {
            return;
        }

        foreach (GitRepository repository in repositories)
        {
            if (!string.IsNullOrWhiteSpace(repository.HttpsUrl.Host))
            {
                hosts.Add(repository.HttpsUrl.Host);
            }
        }
    }

    private static NameValue<string>? FindDomainOption(ImmutableArray<NameValue<string>> options, string domain)
    {
        foreach (NameValue<string> option in options)
        {
            if (string.Equals(option.Value, domain, StringComparison.OrdinalIgnoreCase))
            {
                return option;
            }
        }

        return default;
    }
}

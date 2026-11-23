// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media.Imaging;
using Snap.Hutao.Remastered.Core;
using Snap.Hutao.Remastered.Core.Caching;
using Snap.Hutao.Remastered.Core.DataTransfer;
using Snap.Hutao.Remastered.Core.Logging;
using Snap.Hutao.Remastered.UI.Content;
using Snap.Hutao.Remastered.UI.Xaml.Control.Theme;
using System.IO;
using Windows.Graphics.Imaging;
using Windows.Storage.Streams;

namespace Snap.Hutao.Remastered.UI.Xaml.Control.Image;

[SuppressMessage("", "CA1001")]
[SuppressMessage("", "SH003")]
[TemplatePart(Name = PartImage, Type = typeof(Microsoft.UI.Xaml.Controls.Image))]
[DependencyProperty<string>("SourceName", DefaultValue = "UnknownSourceName")]
[DependencyProperty<string>("CachedName", DefaultValue = "UnknownCachedName")]
[DependencyProperty<Thickness>("NineGrid", NotNull = true)]
[DependencyProperty<object>("PlaceholderSource")]
[DependencyProperty<object>("Source", PropertyChangedCallbackName = nameof(OnSourceChanged))]
[DependencyProperty<bool>("ShowAsMonoChrome", DefaultValue = false, NotNull = true)]
public sealed partial class CachedImage : Microsoft.UI.Xaml.Controls.Control
{
    private const string PartImage = "Image";

    private SourceRequest? sourceRequest;
    private BitmapImage? displayedBitmap;
    private Uri? displayedUri;
    private CachedImageResource? displayedResource;
    private bool isUnloaded;
    private bool sourceUpdateQueued;

    public CachedImage()
    {
        DefaultStyleKey = typeof(CachedImage);
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
        ActualThemeChanged += OnActualThemeChanged;
    }

    private Microsoft.UI.Xaml.Controls.Image? Image { get; set; }

    [WinRT.DynamicWindowsRuntimeCast(typeof(global::Microsoft.UI.Xaml.Controls.Image))]
    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();
        Microsoft.UI.Xaml.Controls.Image? previous = Image;
        Image = GetTemplateChild(PartImage) as Microsoft.UI.Xaml.Controls.Image;
        if (!Equals(previous, Image))
        {
            CancelSourceRequest();
            ApplyDisplayedImage();
        }

        QueueSourceUpdate();
    }

    private static string? GetSourceText(object? source)
    {
        return source switch
        {
            null => null,
            Uri uri => uri.OriginalString,
            _ => source.ToString() ?? string.Empty,
        };
    }

    private static Uri? GetSourceUri(string? source)
    {
        if (source is null)
        {
            return null;
        }

        Uri uri = new(source, UriKind.RelativeOrAbsolute);
        return uri.IsAbsoluteUri ? uri : new("ms-appx:///" + uri.OriginalString.TrimStart('/'));
    }

    private static void OnSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is CachedImage control)
        {
            control.QueueSourceUpdate();
        }
    }

    private void OnActualThemeChanged(FrameworkElement sender, object args)
    {
        if (!isUnloaded && !XamlApplicationLifetime.Exiting && ShowAsMonoChrome)
        {
            QueueSourceUpdate();
        }
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        isUnloaded = false;
        QueueSourceUpdate();
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        isUnloaded = true;
        CancelSourceRequest();
    }

    private void CancelSourceRequest()
    {
        SourceRequest? previous = sourceRequest;
        sourceRequest = null;
        previous?.Dispose();
    }

    private ElementTheme GetSourceTheme()
    {
        return ShowAsMonoChrome ? ThemeHelper.ApplicationToElement(ThemeHelper.ElementToApplication(ActualTheme)) : ElementTheme.Default;
    }

    private void QueueSourceUpdate()
    {
        if (isUnloaded || XamlApplicationLifetime.Exiting)
        {
            return;
        }

        if (sourceRequest is { } current && (current.Source != GetSourceText(Source) || current.Theme != GetSourceTheme()))
        {
            CancelSourceRequest();
        }

        if (sourceUpdateQueued)
        {
            return;
        }

        // Template, Loaded and binding updates can arrive together. Only load their final value.
        sourceUpdateQueued = DispatcherQueue.TryEnqueue(() =>
        {
            sourceUpdateQueued = false;
            UpdateSource();
        });
    }

    private void UpdateSource()
    {
        if (Image is null || isUnloaded || XamlApplicationLifetime.Exiting || !IsLoaded)
        {
            return;
        }

        string? source = GetSourceText(Source);
        ElementTheme theme = GetSourceTheme();
        if (sourceRequest is { } current && current.Source == source && current.Theme == theme)
        {
            return;
        }

        CancelSourceRequest();
        SourceRequest request = new(source, theme);
        sourceRequest = request;
        LoadSourceAsync(request).SafeForget();
    }

    private bool IsCurrent(SourceRequest request)
    {
        return !request.Token.IsCancellationRequested && !XamlApplicationLifetime.Exiting;
    }

    private async Task LoadSourceAsync(SourceRequest request)
    {
        ITaskContext taskContext = TaskContext.GetForDependencyObject(this);
        try
        {
            await ApplySourceAsync(request, request.Source, taskContext);
            return;
        }
        catch (Exception ex) when (!IsCurrent(request))
        {
            // A recycled item or a closing window has discarded the request.
            System.Diagnostics.Trace.WriteLine($"Discarded CachedImage request: {ex.Message}");
            return;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Trace.WriteLine($"CachedImage source load failed: {ex}");
        }

        await taskContext.SwitchToMainThreadAsync();
        if (!IsCurrent(request))
        {
            return;
        }

        string? placeholder = GetSourceText(PlaceholderSource);
        if (placeholder is not null && placeholder != request.Source)
        {
            try
            {
                await ApplySourceAsync(request, placeholder, taskContext);
                return;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.WriteLine($"CachedImage placeholder load failed: {ex}");
            }
        }

        await taskContext.SwitchToMainThreadAsync();
        if (IsCurrent(request))
        {
            ClearImage();
        }
    }

    private async Task ApplySourceAsync(SourceRequest request, string? source, ITaskContext taskContext, CachedImageResource? failed = null)
    {
        Uri? uri = GetSourceUri(source);
        Uri? targetUri = uri;
        CachedImageResource? resource = null;
        if (uri is { Scheme: "http" or "https" })
        {
            // Cache lookups may contend with shared downloads. Keep them off the XAML thread.
            await taskContext.SwitchToBackgroundAsync();
            request.Token.ThrowIfCancellationRequested();
            IImageCache imageCache = Ioc.Default.GetRequiredService<IImageCache>();
            resource = await imageCache.GetImageAsync(uri, request.Theme, failed).ConfigureAwait(false);
            targetUri = new Uri(resource.Value.File.ToString());
        }
        else if (uri is { Scheme: "ms-appx" } && !Core.RuntimeEnvironment.IsPackaged)
        {
            targetUri = InstalledLocation.ToAbsoluteUri(uri);
        }

        request.Token.ThrowIfCancellationRequested();
        if (XamlApplicationLifetime.Exiting)
        {
            return;
        }

        await taskContext.SwitchToMainThreadAsync();
        if (!IsCurrent(request) || Image is not { } target)
        {
            return;
        }

        if (targetUri is null)
        {
            ClearImage();
            return;
        }

        if (Equals(targetUri, displayedUri) && resource == displayedResource)
        {
            ApplyDisplayedImage();
            return;
        }

        BitmapImage bitmap;
        try
        {
            // The normal URI path lets WinUI decode to the live layout and reuse
            // its URI-backed images when a page creates new image controls.
            bool refresh = failed is not null || (Equals(targetUri, displayedUri) && resource != displayedResource);
            bitmap = await LoadUriBitmapAsync(target, targetUri, request, refresh);
        }
        catch (Exception ex) when (targetUri.IsFile && IsCurrent(request)
            && ex is IOException or ArgumentException or System.Runtime.InteropServices.COMException)
        {
            await taskContext.SwitchToMainThreadAsync();
            request.Token.ThrowIfCancellationRequested();
            try
            {
                // The URI loader can reject a readable local file. Check it using
                // the stream decoder before asking the cache to remove it.
                bitmap = await LoadStreamBitmapAsync(target, targetUri, request);
            }
            catch (Exception streamError) when (failed is null && resource is not null && IsCurrent(request))
            {
                System.Diagnostics.Trace.WriteLine($"CachedImage decode failed for '{targetUri.LocalPath}': {streamError.Message}");
                await ApplySourceAsync(request, source, taskContext, resource);
                return;
            }
        }

        await taskContext.SwitchToMainThreadAsync();
        if (IsCurrent(request))
        {
            if (resource is { } cached && uri is not null)
            {
                CachedName = Path.GetFileName(cached.File);
                SourceName = Path.GetFileName(uri.AbsolutePath);
            }

            displayedUri = targetUri;
            displayedResource = resource;
            displayedBitmap = bitmap;
        }
    }

    private async Task<BitmapImage> LoadUriBitmapAsync(Microsoft.UI.Xaml.Controls.Image target, Uri uri, SourceRequest request, bool refresh)
    {
        BitmapImage bitmap = new()
        {
            DecodePixelType = DecodePixelType.Logical,
            CreateOptions = refresh ? BitmapCreateOptions.IgnoreImageCache : BitmapCreateOptions.None,
        };
        TaskCompletionSource ready = new(TaskCreationOptions.RunContinuationsAsynchronously);
        void OnOpened(object sender, RoutedEventArgs args) => ready.TrySetResult();
        void OnFailed(object sender, ExceptionRoutedEventArgs args) => ready.TrySetException(new IOException(args.ErrorMessage));

        bitmap.ImageOpened += OnOpened;
        bitmap.ImageFailed += OnFailed;
        try
        {
            await AttachBitmapAsync(target, bitmap, request);
            bitmap.UriSource = uri;
            await ready.Task.WaitAsync(request.Token);
            return bitmap;
        }
        finally
        {
            bitmap.ImageOpened -= OnOpened;
            bitmap.ImageFailed -= OnFailed;
        }
    }

    private async Task<BitmapImage> LoadStreamBitmapAsync(Microsoft.UI.Xaml.Controls.Image target, Uri uri, SourceRequest request)
    {
        BitmapImage bitmap = new() { DecodePixelType = DecodePixelType.Logical };
        await AttachBitmapAsync(target, bitmap, request);
        using FileStream file = new(uri.LocalPath, FileMode.Open, FileAccess.Read, FileShare.Read | FileShare.Delete);
        using IRandomAccessStream stream = file.AsRandomAccessStream();
        await bitmap.SetSourceAsync(stream).AsTask(request.Token);
        return bitmap;
    }

    private static async Task AttachBitmapAsync(Microsoft.UI.Xaml.Controls.Image target, BitmapImage bitmap, SourceRequest request)
    {
        target.Source = bitmap;
        target.Visibility = Visibility.Visible;

        if (!target.IsLoaded)
        {
            TaskCompletionSource loaded = new(TaskCreationOptions.RunContinuationsAsynchronously);
            void OnImageLoaded(object sender, RoutedEventArgs args) => loaded.TrySetResult();
            target.Loaded += OnImageLoaded;
            try
            {
                await loaded.Task.WaitAsync(request.Token);
            }
            finally
            {
                target.Loaded -= OnImageLoaded;
            }
        }

        request.Token.ThrowIfCancellationRequested();
    }

    private void ClearImage()
    {
        displayedUri = null;
        displayedResource = null;
        displayedBitmap = null;
        ApplyDisplayedImage();
    }

    private void ApplyDisplayedImage()
    {
        if (Image is { } target)
        {
            if (!ReferenceEquals(target.Source, displayedBitmap))
            {
                target.Source = displayedBitmap;
            }

            Visibility visibility = displayedBitmap is null ? Visibility.Collapsed : Visibility.Visible;
            if (target.Visibility != visibility)
            {
                target.Visibility = visibility;
            }
        }
    }

    private sealed class SourceRequest : IDisposable
    {
        private readonly CancellationTokenSource cancellation = new();

        public SourceRequest(string? source, ElementTheme theme)
        {
            Source = source;
            Theme = theme;
            Token = cancellation.Token;
        }

        public string? Source { get; }

        public ElementTheme Theme { get; }

        public CancellationToken Token { get; }

        public void Dispose()
        {
            cancellation.Cancel();
            cancellation.Dispose();
        }
    }

    [Command("CopyToClipboardCommand")]
    private async Task CopyToClipboard()
    {
        SentrySdk.AddBreadcrumb(BreadcrumbFactory2.CreateUI("Copy image to Clipboard", "CachedImage.Command", [("source_name", SourceName ?? "<null>")]));

        if (XamlRoot.XamlContext()?.ServiceProvider.GetRequiredService<IClipboardProvider>() is not { } clipboardProvider)
        {
            return;
        }

        if (displayedUri is { IsFile: true } uri)
        {
            using (FileStream netStream = File.OpenRead(uri.LocalPath))
            {
                using (IRandomAccessStream fxStream = netStream.AsRandomAccessStream())
                {
                    BitmapDecoder decoder = await BitmapDecoder.CreateAsync(fxStream);
                    SoftwareBitmap softwareBitmap = await decoder.GetSoftwareBitmapAsync(BitmapPixelFormat.Bgra8, BitmapAlphaMode.Premultiplied);
                    using (InMemoryRandomAccessStream memory = new())
                    {
                        BitmapEncoder encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.BmpEncoderId, memory);
                        encoder.SetSoftwareBitmap(softwareBitmap);
                        await encoder.FlushAsync();
                        await clipboardProvider.SetBitmapAsync(memory).ConfigureAwait(false);
                    }
                }
            }
        }
    }
}


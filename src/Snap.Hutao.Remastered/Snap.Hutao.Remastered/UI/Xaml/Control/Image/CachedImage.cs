// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Snap.Hutao.Remastered.Core;
using Snap.Hutao.Remastered.Core.Caching;
using Snap.Hutao.Remastered.Core.DataTransfer;
using Snap.Hutao.Remastered.Core.ExceptionService;
using Snap.Hutao.Remastered.Core.Logging;
using Snap.Hutao.Remastered.UI.Content;
using Snap.Hutao.Remastered.UI.Xaml.Control.Theme;
using System.IO;
using System.Runtime.InteropServices;
using Windows.Graphics.Imaging;
using Windows.Storage.Streams;

namespace Snap.Hutao.Remastered.UI.Xaml.Control.Image;

[SuppressMessage("", "CA1001")]
[SuppressMessage("", "SH003")]
[TemplatePart(Name = PartImage, Type = typeof(Microsoft.UI.Xaml.Controls.Image))]
[DependencyProperty<string>("SourceName", DefaultValue = "UnknownSourceName")]
[DependencyProperty<string>("CachedName", DefaultValue = "UnknownCachedName")]
[DependencyProperty<Thickness>("NineGrid", NotNull = true)]
[DependencyProperty<Stretch>("Stretch", DefaultValue = Stretch.Uniform, NotNull = true)]
[DependencyProperty<object>("PlaceholderSource")]
[DependencyProperty<object>("Source", PropertyChangedCallbackName = nameof(OnSourceChanged))]
[DependencyProperty<bool>("ShowAsMonoChrome", DefaultValue = false, NotNull = true)]
public sealed partial class CachedImage : Microsoft.UI.Xaml.Controls.Control
{
    private const string PartImage = "Image";

    private readonly AsyncLock sourceLock = new();

    private CancellationTokenSource? sourceCts;
    private bool isUnloaded;
    private BitmapImage? pendingImageSource;
    private Uri? pendingImageUri;

    public CachedImage()
    {
        DefaultStyleKey = typeof(CachedImage);
        Loaded += OnLoaded;
    }

    private Microsoft.UI.Xaml.Controls.Image? Image { get; set; }

    private bool IsInitialized { get; set; }

    protected override void OnApplyTemplate()
    {
        if (Image is { } oldImage)
        {
            oldImage.ImageOpened -= OnImageOpened;
            oldImage.ImageFailed -= OnImageFailed;
            oldImage.Loaded -= OnImageElementLoaded;
        }

        pendingImageSource = null;
        pendingImageUri = null;
        Image = GetTemplateChild(PartImage) as Microsoft.UI.Xaml.Controls.Image;

        if (Image is { } newImage)
        {
            newImage.ImageOpened += OnImageOpened;
            newImage.ImageFailed += OnImageFailed;
            newImage.Loaded += OnImageElementLoaded;
        }

        IsInitialized = true;
        SetSourceAsync(Source, false).SafeForget();

        base.OnApplyTemplate();
    }

    private static bool IsHttpUri(Uri uri)
    {
        return uri is { IsAbsoluteUri: true, Scheme: "http" or "https" };
    }

    private static Uri ResolveAppxUriForUnpackaged(Uri uri)
    {
        if (Core.RuntimeEnvironment.IsPackaged)
        {
            return uri;
        }

        return InstalledLocation.ToAbsoluteUri(uri);
    }

    private static void OnSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not CachedImage control)
        {
            return;
        }

        if (Equals(e.OldValue, e.NewValue))
        {
            return;
        }

        control.SetSourceAsync(e.NewValue, false).SafeForget();
    }

    private static void OnActualThemeChanged(FrameworkElement sender, object args)
    {
        if (sender is not CachedImage { ShowAsMonoChrome: true, IsLoaded: true } control || XamlApplicationLifetime.Exiting)
        {
            return;
        }

        control.SetSourceAsync(control.Source, false).SafeForget();
    }

    private static void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (sender is not CachedImage control)
        {
            return;
        }

        control.ActualThemeChanged -= OnActualThemeChanged;
        control.Unloaded -= OnUnloaded;
        control.ActualThemeChanged += OnActualThemeChanged;
        control.Unloaded += OnUnloaded;
        control.isUnloaded = false;
        // Source may have changed while this control was detached. Resolve the
        // latest value; the disk cache and URI check avoid downloading/decoding it again.
        control.SetSourceAsync(control.Source, false).SafeForget();
    }

    private static void OnUnloaded(object sender, RoutedEventArgs e)
    {
        if (sender is not CachedImage control)
        {
            return;
        }

        control.ActualThemeChanged -= OnActualThemeChanged;
        control.Unloaded -= OnUnloaded;
        control.isUnloaded = true;
        control.pendingImageSource = null;
        control.pendingImageUri = null;
        CancellationTokenSource? previous = control.sourceCts;
        control.sourceCts = null;
        previous?.Cancel();
        previous?.Dispose();
    }

    private static void OnImageOpened(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement element || XamlApplicationLifetime.Exiting)
        {
            return;
        }

        // Use one opacity owner. Mixing a zero XAML opacity with compositor
        // animation and direct XAML writes makes visibility depend on event order.
        element.Opacity = 1D;
    }

    private static void OnImageFailed(object sender, ExceptionRoutedEventArgs e)
    {
        if (sender is not Microsoft.UI.Xaml.Controls.Image image || XamlApplicationLifetime.Exiting)
        {
            return;
        }

        System.Diagnostics.Trace.WriteLine($"CachedImage decode failed: {e.ErrorMessage}");
        image.Opacity = 0D;
    }

    private void OnImageElementLoaded(object sender, RoutedEventArgs e)
    {
        StartPendingImageDecode();
    }

    private void StartPendingImageDecode()
    {
        if (isUnloaded || XamlApplicationLifetime.Exiting || Image is not { IsLoaded: true } target
            || pendingImageUri is not { } uri || pendingImageSource is not { } bitmap
            || !ReferenceEquals(target.Source, bitmap)) return;

        pendingImageUri = null;
        pendingImageSource = null;
        // URI assignment is the decode trigger. Unlike the old timed fade-out,
        // this event proves the actual Image is connected to its live visual tree.
        bitmap.UriSource = uri;
    }

    private void AttachUriSource(Microsoft.UI.Xaml.Controls.Image? target, Uri? uri)
    {
        // As in the original implementation, the template may receive a source
        // before Loaded. A later unload is guarded by the request's lifetime token.
        if (target is null || XamlApplicationLifetime.Exiting)
        {
            return;
        }

        if (uri is null)
        {
            pendingImageSource = null;
            pendingImageUri = null;
            target.Visibility = Visibility.Collapsed;
            target.Source = null;
        }
        else
        {
            // https://learn.microsoft.com/en-us/windows/uwp/debug-test-perf/optimize-animations-and-media#optimize-image-resources
            // Connecting the BitmapImage to the tree before setting its UriSource
            target.Visibility = Visibility.Visible;
            target.Opacity = 1D;
            if (target.Source is BitmapImage { PixelWidth: > 0 } current && Equals(current.UriSource, uri))
            {
                target.Opacity = 1D;
                return;
            }

            if (ReferenceEquals(target.Source, pendingImageSource) && Equals(pendingImageUri, uri))
            {
                StartPendingImageDecode();
                return;
            }

            BitmapImage source = new();

            // https://learn.microsoft.com/en-us/windows/uwp/debug-test-perf/optimize-animations-and-media#right-sized-decoding
            // UriSource should be set after the Image is connected to live XAML tree
            target.Source = source;
            source.DecodePixelType = DecodePixelType.Logical;
            pendingImageSource = source;
            pendingImageUri = uri;
            StartPendingImageDecode();
        }

    }

    private async Task<Uri?> ProvideCachedResourceAsync(Uri imageUri, ElementTheme theme, CancellationToken token)
    {
        // IImageCache is process-wide, not window-scoped. The ContentIsland's
        // AppData is only assigned during root Loading; cache hits must not turn
        // into a null image merely because template creation ran before that event.
        IImageCache imageCache = Ioc.Default.GetRequiredService<IImageCache>();

        try
        {
            HutaoException.ThrowIf(string.IsNullOrEmpty(imageUri.Host), SH.ControlImageCachedImageInvalidResourceUri);
            string file = await imageCache.GetFileFromCacheAsync(imageUri, theme).ConfigureAwait(false);
            token.ThrowIfCancellationRequested();
            return file.ToUri();
        }
        catch (OperationCanceledException)
        {
            // Ignored
            throw;
        }
        catch (publicImageCacheException)
        {
            // Ignored
            throw;
        }
        catch (Exception ex)
        {
            // A UI lifetime error is not evidence of a corrupt cached file.
            // Never delete a healthy image and start another download for it.
            System.Diagnostics.Trace.WriteLine($"CachedImage resource load failed: {ex}");
            SentrySdk.CaptureException(ex);
            return default;
        }
    }

    private async Task SetSourceAsync(object? source, bool placeholder)
    {
        ITaskContext taskContext = TaskContext.GetForDependencyObject(this);
        try
        {
            if (!IsInitialized || isUnloaded || XamlApplicationLifetime.Exiting)
            {
                return;
            }

            CancellationTokenSource? previous = sourceCts;
            sourceCts = new();
            CancellationToken token = sourceCts.Token;
            previous?.Cancel();
            previous?.Dispose();
            ElementTheme theme = ShowAsMonoChrome ? ThemeHelper.ApplicationToElement(ThemeHelper.ElementToApplication(ActualTheme)) : ElementTheme.Default;

            try
            {
                // Resolve a fallback outside the lock; recursively acquiring sourceLock
                // from inside it deadlocks when a source is invalid.
                if (source is not null && source is not Uri && !Uri.TryCreate(source.ToString(), UriKind.RelativeOrAbsolute, out _))
                {
                    if (!placeholder) await SetSourceAsync(PlaceholderSource, true).ConfigureAwait(false);
                    return;
                }

                using (await sourceLock.LockAsync().ConfigureAwait(false))
                {
                    token.ThrowIfCancellationRequested();

                    if (source is null)
                    {
                        if (XamlApplicationLifetime.Exiting) return;
                        await taskContext.SwitchToMainThreadAsync();
                        token.ThrowIfCancellationRequested();
                        AttachUriSource(Image, default);
                        return;
                    }

                    if (source as Uri is not { } uri)
                    {
                        if (!Uri.TryCreate(source as string ?? source.ToString(), UriKind.RelativeOrAbsolute, out uri))
                        {
                            return;
                        }
                    }

                    if (!IsHttpUri(uri) && !uri.IsAbsoluteUri)
                    {
                        uri = new("ms-appx:///" + uri.OriginalString.TrimStart('/'));
                    }

                    Uri? targetUri = uri.Scheme is "ms-appx" ? ResolveAppxUriForUnpackaged(uri) : await ProvideCachedResourceAsync(uri, theme, token).ConfigureAwait(false);
                    // Do not post a completed download back into a closing XAML
                    // dispatcher. Only live requests are allowed to touch UI DPs.
                    token.ThrowIfCancellationRequested();
                    if (XamlApplicationLifetime.Exiting) return;
                    await taskContext.SwitchToMainThreadAsync();
                    if (!token.IsCancellationRequested && !isUnloaded && !XamlApplicationLifetime.Exiting)
                    {
                        if (targetUri is { IsFile: true } && IsHttpUri(uri))
                        {
                            CachedName = Path.GetFileName(targetUri.LocalPath);
                            SourceName = Path.GetFileName(uri.AbsolutePath);
                        }
                        // Only attach our image if we still have a valid request.
                        AttachUriSource(Image, targetUri);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // Ignored
            }
            catch (Exception exception)
            {
                if (isUnloaded || XamlApplicationLifetime.Exiting)
                {
                    return;
                }

                if (!placeholder)
                {
                    System.Diagnostics.Trace.WriteLine($"CachedImage could not resolve source: {exception}");
                    await taskContext.SwitchToMainThreadAsync();
                    if (!isUnloaded && !XamlApplicationLifetime.Exiting)
                        await SetSourceAsync(PlaceholderSource, true).ConfigureAwait(false);
                }
            }
        }
        catch (COMException ex)
        {
            if (ex.HResult != unchecked((int)0x8000FFFF))
            {
                throw;
            }
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

        if (Image is { Source: BitmapImage bitmap })
        {
            using (FileStream netStream = File.OpenRead(bitmap.UriSource.LocalPath))
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

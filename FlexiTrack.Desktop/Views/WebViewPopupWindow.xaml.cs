using System.IO;
using System.Windows;
using FlexiTrack.Desktop.Services;
using Microsoft.Web.WebView2.Core;
using Microsoft.Win32;

namespace FlexiTrack.Desktop.Views;

public partial class WebViewPopupWindow : Window
{
    private readonly string _userDataFolder;
    private readonly string _apiUrl;
    private bool _isInitialized;

    public WebViewPopupWindow(string apiUrl)
    {
        InitializeComponent();

        _apiUrl = apiUrl;

        var appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        _userDataFolder = Path.Combine(appData, "FlexiTrack", "WebView2");
        Directory.CreateDirectory(_userDataFolder);

        Loaded += OnLoaded;
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (!_isInitialized)
        {
            await InitializeWebViewAsync();
            _isInitialized = true;
        }
    }

    private async Task InitializeWebViewAsync()
    {
        try
        {
            var env = await CoreWebView2Environment.CreateAsync(
                userDataFolder: _userDataFolder);

            await WebView.EnsureCoreWebView2Async(env);

            WebView.CoreWebView2.Settings.IsStatusBarEnabled = false;
            WebView.CoreWebView2.Settings.AreDefaultContextMenusEnabled = false;

            WebView.CoreWebView2.NavigationCompleted += (s, args) =>
            {
                if (!args.IsSuccess && args.WebErrorStatus != CoreWebView2WebErrorStatus.OperationCanceled)
                {
                    ShowConnectionError();
                }
            };

            WebView.CoreWebView2.DownloadStarting += OnDownloadStarting;

            WebView.CoreWebView2.Navigate(_apiUrl);
        }
        catch (Exception ex)
        {
            LogService.LogError("WebView initialization failed", ex);
            MessageBox.Show(
                $"Failed to initialize WebView2: {ex.Message}\n\nMake sure the WebView2 runtime is installed.",
                "FlexiTrack Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            Close();
        }
    }

    private void ShowConnectionError()
    {
        WebView.CoreWebView2.NavigateToString($@"
            <!DOCTYPE html>
            <html>
            <head>
                <style>
                    body {{
                        font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', sans-serif;
                        display: flex;
                        justify-content: center;
                        align-items: center;
                        height: 100vh;
                        margin: 0;
                        background: #f8f9fa;
                        text-align: center;
                    }}
                    .error {{
                        padding: 2rem;
                    }}
                    h1 {{
                        color: #dc3545;
                        margin-bottom: 1rem;
                        font-size: 1.5rem;
                    }}
                    p {{
                        color: #6c757d;
                        margin: 0.5rem 0;
                    }}
                    button {{
                        margin-top: 1.5rem;
                        padding: 0.75rem 1.5rem;
                        background: #0066cc;
                        color: white;
                        border: none;
                        border-radius: 4px;
                        cursor: pointer;
                        font-size: 1rem;
                    }}
                    button:hover {{
                        background: #0055aa;
                    }}
                </style>
            </head>
            <body>
                <div class='error'>
                    <h1>Connection Error</h1>
                    <p>Cannot connect to FlexiTrack API server.</p>
                    <p>Make sure the server is running at:</p>
                    <p><strong>{_apiUrl}</strong></p>
                    <button onclick=""window.location.href='{_apiUrl}'"">Retry</button>
                </div>
            </body>
            </html>
        ");
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Hide();
    }

    public async Task ClearBrowsingDataAsync()
    {
        if (WebView.CoreWebView2 != null)
        {
            await WebView.CoreWebView2.Profile.ClearBrowsingDataAsync();
        }
    }

    public void NavigateToApp()
    {
        if (WebView.CoreWebView2 != null)
        {
            WebView.CoreWebView2.Navigate(_apiUrl);
        }
    }

    private void OnDownloadStarting(object? sender, CoreWebView2DownloadStartingEventArgs e)
    {
        var deferral = e.GetDeferral();

        try
        {
            var suggestedFileName = Path.GetFileName(e.ResultFilePath);
            var extension = Path.GetExtension(suggestedFileName).ToLowerInvariant();

            var saveDialog = new SaveFileDialog
            {
                FileName = suggestedFileName,
                Title = "Save File"
            };

            saveDialog.Filter = extension switch
            {
                ".csv" => "CSV Files (*.csv)|*.csv|All Files (*.*)|*.*",
                ".zip" => "ZIP Files (*.zip)|*.zip|All Files (*.*)|*.*",
                _ => "All Files (*.*)|*.*"
            };

            if (saveDialog.ShowDialog() == true)
            {
                e.ResultFilePath = saveDialog.FileName;
                e.Handled = true;
            }
            else
            {
                e.Cancel = true;
                e.Handled = true;
            }
        }
        finally
        {
            deferral.Complete();
        }
    }
}

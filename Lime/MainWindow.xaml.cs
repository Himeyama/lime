using System;
using Common;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using System.Text.Json.Serialization;
using System.Diagnostics;
using System.Text.Json;
using System.Collections.Generic;
using System.IO;
using Microsoft.UI.Windowing;
using Microsoft.UI;
using WinRT.Interop;
using Windows.Graphics;
using System.Threading.Tasks;
using System.Net.Http;
using System.Text.RegularExpressions;


namespace Lime;


public class LinkItem
{
    [JsonPropertyName("href")]
    public string Href { get; set; } = "";
    [JsonPropertyName("name")]
    public string Name { get; set; } = "";
    [JsonPropertyName("index")]
    public int Index { get; set; } = 0;
}

public sealed partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        ExtendsContentIntoTitleBar = true;
        SetTitleBar(AppTitleBar);

        Title = AppTitle.Text;

        SetWindowSize(800, 1200);

        LoadConfigFile();
    }

    void LoadConfigFile()
    {
        // ドキュメントのパスを取得
        string documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        string configFilePath = Path.Combine(documentsPath, ".lime_config.json");

        // .lime_config.json が存在するか確認
        if (!File.Exists(configFilePath))
        {
            // ファイルが存在しない場合、新規作成
            File.WriteAllText(configFilePath, "[]");
            // 隠しファイルとして属性を設定
            File.SetAttributes(configFilePath, FileAttributes.Hidden);
        }
        else
        {
            // ファイルが存在する場合、読み込み
            string jsonContent = File.ReadAllText(configFilePath);
            LinkItems.Items.Clear();
            List<LinkItem> items = JsonSerializer.Deserialize<List<LinkItem>>(jsonContent);
            foreach (var item in items)
            {
                LinkItems.Items.Add(item);
            }
        }
    }

    async Task SaveConfigFile()
    {
        string documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        string configFilePath = Path.Combine(documentsPath, ".lime_config.json");

        // JSON にシリアライズしてファイルに書き込む
        try{
            string jsonContent = JsonSerializer.Serialize(LinkItems.Items, new JsonSerializerOptions()
            {
                WriteIndented = true
            });
            File.SetAttributes(configFilePath, FileAttributes.Normal);
            File.WriteAllText(configFilePath, jsonContent);
            File.SetAttributes(configFilePath, FileAttributes.Hidden);
        }catch(Exception ex){
            await Dialog("Error", ex.Message);

        }
    }

    void AddLink(LinkItem linkItem)
    {
        LinkItems.Items.Add(linkItem);
    }

    async void AddModifyItem(bool modify = false)
    {
        TextBox name = new()
        {
            Header = new TextBlock
            {
                Text = HeaderName.Text
            }
        };

        TextBox url = new()
        {
            Header = new TextBlock
            {
                Text = Url.Text
            },
            Margin = new Thickness(0, 16, 0, 0)
        };

        if(modify)
        {
            if (LinkItems.SelectedItem is LinkItem selectedItem)
            {
                name.Text = selectedItem.Name;
                url.Text = selectedItem.Href;
            }
        }

        ContentDialog dialog = new()
        {
            XamlRoot = Content.XamlRoot,
            Title = modify ? ModifyDialogTitle.Text : AddDialogTitle.Text,
            PrimaryButtonText = modify ? Modify.Text : Add.Text,
            CloseButtonText = Cancel.Text,
            DefaultButton = ContentDialogButton.Primary,
            Content = new StackPanel{
                Children = {
                    name,
                    url
                }
            }
        };

        ContentDialogResult result = await dialog.ShowAsync();

        bool flag = true;
        while(flag){
            if(result == ContentDialogResult.Primary){
                if(url.Text == string.Empty){
                    await Dialog(Error.Text, UrlCannotBeEmpty.Text);
                    result = await dialog.ShowAsync();
                    continue;
                }

                if(name.Text == string.Empty){
                    name.Text = await GetPageTitle(url.Text);
                    result = await dialog.ShowAsync();
                }else{
                    // 新しいリンクアイテムを作成
                    LinkItem newItem = new()
                    {
                        Name = name.Text,
                        Href = url.Text,
                        Index = LinkItems.Items.Count
                    };
                    if(modify){
                        int idx = LinkItems.SelectedIndex;
                        if (LinkItems.SelectedItem is LinkItem selectedItem)
                            LinkItems.Items.Remove(selectedItem);
                        LinkItems.Items.Insert(idx, newItem);
                    }else{
                        AddLink(newItem);
                    }
                    await SaveConfigFile();
                    flag = false;
                }
            }else{
                flag = false;
            }
        }
    }

    void AddItem(object sender, RoutedEventArgs e)
    {
        AddModifyItem();
    }

    void ModifyItem(object sender, RoutedEventArgs e)
    {
        AddModifyItem(true);
    }

    async Task<string> GetPageTitle(string url)
    {
        try
        {
            if (!Uri.IsWellFormedUriString(url, UriKind.Absolute))
            {
                await Dialog(Error.Text, InvalidRequestUri.Text + ": " + url);
                return null;
            }

            using HttpClient client = new();
            string html = await client.GetStringAsync(url);

            // 正規表現を使用してタイトルを抽出
            Match match = Regex.Match(html, @"<title>\s*(.+?)\s*</title>", RegexOptions.IgnoreCase);
            if (match.Success)
            {
                return match.Groups[1].Value;
            }
            else
            {
                return null;
            }
        }
        catch (Exception ex)
        {
            await Dialog(Error.Text, ex.Message);
            return null;
        }
    }

    async void DeleteItem(object sender, RoutedEventArgs e)
    {
        if (LinkItems.SelectedItem is LinkItem selectedItem)
        {
            ContentDialog dialog = new()
            {
                XamlRoot = Content.XamlRoot,
                Title = DeleteConfirmation.Text,
                PrimaryButtonText = Delete.Text,
                CloseButtonText = Cancel.Text,
                DefaultButton = ContentDialogButton.Primary,
                Content = string.Format(DeleteConfirmationMessage.Text, selectedItem.Name)
            };

            ContentDialogResult result = await dialog.ShowAsync();

            if (result == ContentDialogResult.Primary)
            {
                LinkItems.Items.Remove(selectedItem);
                int i = 0;
                foreach(LinkItem linkItem in LinkItems.Items){
                    linkItem.Index = i++;
                }
                await SaveConfigFile();
            }
        }
    }

    async Task Dialog(string title, string content)
    {
        ContentDialog dialog = new()
        {
            XamlRoot = Content.XamlRoot,
            Title = title,
            PrimaryButtonText = "OK",
            DefaultButton = ContentDialogButton.Primary,
            Content = content
        };
        await dialog.ShowAsync();
    }

    void LinkItems_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        ModifyButton.IsEnabled = DeleteButton.IsEnabled = true;
    }

    static void OpenLinkInBrowser(string url)
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = url,
                UseShellExecute = true // 必要に応じてシェルを使用して実行
            });
        }
        catch (Exception ex)
        {
            Status.AddMessage($"エラーが発生しました: {ex.Message}");
        }
    }

    void LinkItems_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
    {
        ListView listView = sender as ListView;
        if (listView.SelectedItem is LinkItem selectedItem)
        {
            // リンクを開く
            OpenLinkInBrowser(selectedItem.Href);
        }
    }

    void SetWindowSize(int width, int height)
    {
        IntPtr hWnd = WindowNative.GetWindowHandle(this);
        WindowId myWndId = Win32Interop.GetWindowIdFromWindow(hWnd);
        AppWindow appWindow = AppWindow.GetFromWindowId(myWndId);
        appWindow.Resize(new SizeInt32(width, height));
    }
}

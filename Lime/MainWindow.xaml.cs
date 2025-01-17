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

        // ZoomIn.KeyboardAcceleratorTextOverride = ZoomInText.Text;
        // ZoomOut.KeyboardAcceleratorTextOverride = ZoomOutText.Text;
        // AddLink("Example", "http://example.com");

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

    async void AddItem(object sender, RoutedEventArgs e)
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

        ContentDialog dialog = new()
        {
            XamlRoot = Content.XamlRoot,
            Title = AddDialogTitle.Text,
            PrimaryButtonText = Add.Text,
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

        if(result == ContentDialogResult.Primary){
            // 新しいリンクアイテムを作成
            LinkItem newItem = new()
            {
                Name = name.Text,
                Href = url.Text,
                Index = LinkItems.Items.Count
            };
            AddLink(newItem);
            await SaveConfigFile();
        }
    }

    async void ModifyItem(object sender, RoutedEventArgs e)
    {
        if (LinkItems.SelectedItem is LinkItem selectedItem)
        {
            TextBox name = new()
            {
                Header = new TextBlock
                {
                    Text = HeaderName.Text
                },
                Text = selectedItem.Name
            };

            TextBox url = new()
            {
                Header = new TextBlock
                {
                    Text = Url.Text
                },
                Text = selectedItem.Href,
                Margin = new Thickness(0, 16, 0, 0)
            };

            ContentDialog dialog = new()
            {
                XamlRoot = Content.XamlRoot,
                Title = ModifyDialogTitle.Text,
                PrimaryButtonText = Modify.Text,
                CloseButtonText = Cancel.Text,
                DefaultButton = ContentDialogButton.Primary,
                Content = new StackPanel
                {
                    Children = {
                        name,
                        url
                    }
                }
            };

            ContentDialogResult result = await dialog.ShowAsync();

            if (result == ContentDialogResult.Primary)
            {
                LinkItem newItem = new()
                {
                    Name = name.Text,
                    Href = url.Text,
                    Index = selectedItem.Index
                };
                int idx = LinkItems.SelectedIndex;
                LinkItems.Items.Remove(selectedItem);
                LinkItems.Items.Insert(idx, newItem);

                await SaveConfigFile();
            }
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
        // if (sender is ListView chatItems)
        // {
        //     // 選択されたアイテムを取得
        //     if (chatItems.SelectedItem is HistoryItem historyItem)
        //     {
        //         TabViewItem? tabViewItem = Tabs.SelectedItem as TabViewItem;
        //         if (tabViewItem == null)
        //         {
        //             return;
        //         }
        //         tabViewItem.Header = historyItem.HeadUser;
        //         if(tabViewItem.Content is Client client)
        //         {
        //             client.Print(historyItem);
        //         }
        //     }
        // }
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

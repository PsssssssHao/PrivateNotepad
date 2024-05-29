using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using PrivateNotepad.Core;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;
using Windows.Storage.Pickers;
using WinRT.Interop;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace PrivateNotepad
{
    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainWindow : Window
    {
        private ObservableCollection<HptFile> hptFiles = new();

        public MainWindow()
        {
            this.InitializeComponent();

            // 自定义标题
            this.ExtendsContentIntoTitleBar = true;
            this.SetTitleBar(AppTitleBar);

            // 关闭事件
            var hWnd = WindowNative.GetWindowHandle(this);
            WindowId windowId = Win32Interop.GetWindowIdFromWindow(hWnd);
            AppWindow appWindow = AppWindow.GetFromWindowId(windowId);
            appWindow.Closing += MainWindow_Closing;

            // 创建新标签
            CreateNewTab();
        }

        /// <summary>
        /// 新文件菜单
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Menu_NewFileClick(object sender, RoutedEventArgs e)
        {
            CreateNewTab();
        }

        /// <summary>
        /// 打开菜单
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void Menu_OpenFileClick(object sender, RoutedEventArgs e)
        {
            FileOpenPicker openPicker = new FileOpenPicker
            {
                //SuggestedStartLocation = PickerLocationId.DocumentsLibrary,
                FileTypeFilter = { ".hpt" }
            };

            // Retrieve the window handle (HWND) of the current WinUI 3 window.
            var hWnd = WindowNative.GetWindowHandle(this);

            // Initialize the file picker with the window handle (HWND).
            InitializeWithWindow.Initialize(openPicker, hWnd);

            StorageFile? file = await openPicker.PickSingleFileAsync();

            if (file != null)
            {
                var hptFile = HptFile.OpenFile(file.Path);
                if (hptFile is null)
                {
                    ContentDialog dialog = new ContentDialog
                    {
                        // XamlRoot must be set in the case of a ContentDialog running in a Desktop app
                        XamlRoot = this.Content.XamlRoot,
                        Style = Application.Current.Resources["DefaultContentDialogStyle"] as Style,
                        Title = "错误",
                        PrimaryButtonText = "确定",
                        DefaultButton = ContentDialogButton.Primary,
                        Content = "无法打开该文件！"
                    };
                    await dialog.ShowAsync();
                    return;
                }
                CreateNewTab(hptFile);
            }
        }

        /// <summary>
        /// 保存菜单
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void Menu_SaveFileClick(object sender, RoutedEventArgs e)
        {
            HptFile? hptFile = TabView.SelectedItem as HptFile;
            if (hptFile is null)
            {
                ContentDialog dialog = new ContentDialog
                {
                    // XamlRoot must be set in the case of a ContentDialog running in a Desktop app
                    XamlRoot = this.Content.XamlRoot,
                    Style = Application.Current.Resources["DefaultContentDialogStyle"] as Style,
                    Title = "错误",
                    PrimaryButtonText = "确定",
                    DefaultButton = ContentDialogButton.Primary,
                    Content = "保存文件时发生未知错误！"
                };
                await dialog.ShowAsync();
                return;
            }
            await SaveFile(hptFile);
        }

        /// <summary>
        /// 关于菜单
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void Menu_AboutClick(object sender, RoutedEventArgs e)
        {
            ContentDialog dialog = new ContentDialog
            {
                // XamlRoot must be set in the case of a ContentDialog running in a Desktop app
                XamlRoot = Content.XamlRoot,
                Style = Application.Current.Resources["DefaultContentDialogStyle"] as Style,
                Title = "关于",
                PrimaryButtonText = "确定",
                DefaultButton = ContentDialogButton.Primary,
                Content = "一个基于.NET 8和WinUI 3的简易加密记事本"
            };
            await dialog.ShowAsync();
        }

        /// <summary>
        /// 新标签菜单
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private void TabView_AddButtonClick(TabView sender, object args)
        {
            CreateNewTab();
        }

        /// <summary>
        /// 关闭标签
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private async void TabView_TabCloseRequested(TabView sender, TabViewTabCloseRequestedEventArgs args)
        {
            HptFile? hptFile = args.Item as HptFile;
            if (hptFile is null)
            {
                return;
            }

            if (hptFile.NotSaved)
            {
                ContentDialog dialog = new ContentDialog
                {
                    // XamlRoot must be set in the case of a ContentDialog running in a Desktop app
                    XamlRoot = Content.XamlRoot,
                    Style = Application.Current.Resources["DefaultContentDialogStyle"] as Style,
                    Title = "提示",
                    PrimaryButtonText = "是",
                    CloseButtonText = "否",
                    DefaultButton = ContentDialogButton.Primary,
                    Content = "该文件尚未保存，是否确认关闭？"
                };
                var result = await dialog.ShowAsync();
                if (result == ContentDialogResult.None)
                {
                    return;
                }
            }
            hptFiles.Remove(hptFile);

            if (hptFiles.Count < 1)
            {
                Close();
            }
        }

        /// <summary>
        /// 拖动结束
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Grid_DragOver(object sender, DragEventArgs e)
        {
            e.AcceptedOperation = DataPackageOperation.Copy;

            if (e.DragUIOverride != null)
            {
                e.DragUIOverride.Caption = "打开文件";
                e.DragUIOverride.IsContentVisible = true;
            }
        }

        /// <summary>
        /// 拖动放下
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void Grid_Drop(object sender, DragEventArgs e)
        {
            if (e.DataView.Contains(StandardDataFormats.StorageItems))
            {
                var items = await e.DataView.GetStorageItemsAsync();

                foreach (var item in items)
                {
                    if (item is StorageFile file)
                    {
                        var ex = Path.GetExtension(file.Path);
                        if (ex != ".hpt")
                        {
                            continue;
                        }
                        var hptFile = HptFile.OpenFile(file.Path);
                        if (hptFile is null)
                        {
                            continue;
                        }
                        CreateNewTab(hptFile);
                    }
                }
            }

        }

        /// <summary>
        /// 主窗口关闭
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private async void MainWindow_Closing(AppWindow sender, AppWindowClosingEventArgs args)
        {
            var TabViewItems = TabView.TabItems;
            if (hptFiles.Any(o=> o.NotSaved))
            {
                args.Cancel = true;
                ContentDialog dialog = new ContentDialog
                {
                    // XamlRoot must be set in the case of a ContentDialog running in a Desktop app
                    XamlRoot = Content.XamlRoot,
                    Style = Application.Current.Resources["DefaultContentDialogStyle"] as Style,
                    Title = "提示",
                    PrimaryButtonText = "是",
                    CloseButtonText = "否",
                    DefaultButton = ContentDialogButton.Primary,
                    Content = "有文件尚未保存，是否确认关闭？"
                };
                var result = await dialog.ShowAsync();
                if (result == ContentDialogResult.Primary)
                {
                    Close();
                }
            }
        }

        /// <summary>
        /// 保存文件
        /// </summary>
        /// <param name="hptFile"></param>
        /// <returns></returns>
        private async Task<bool> SaveFile(HptFile hptFile)
        {
            if (hptFile.NewFile)
            {
                FileSavePicker savePicker = new FileSavePicker
                {
                    SuggestedStartLocation = PickerLocationId.DocumentsLibrary,
                };
                savePicker.FileTypeChoices.Add("Hao Private Text", new List<string>() { ".hpt" });

                // Retrieve the window handle (HWND) of the current WinUI 3 window.
                var hWnd = WindowNative.GetWindowHandle(this);

                // Initialize the file picker with the window handle (HWND).
                InitializeWithWindow.Initialize(savePicker, hWnd);

                StorageFile? saveFile = await savePicker.PickSaveFileAsync();
                if (saveFile != null)
                {
                    var fullPath = saveFile.Path;
                    hptFile.Path = System.IO.Path.GetDirectoryName(fullPath) + "\\";
                    hptFile.FileName = System.IO.Path.GetFileName(fullPath);
                    hptFile.SaveContent();
                    return true;
                }
                return false;
            }
            hptFile.SaveContent();
            return true;
        }

        /// <summary>
        /// 创建新标签
        /// </summary>
        /// <param name="file"></param>
        private void CreateNewTab(HptFile? hptFile = null)
        {
            if (hptFile is null)
            {
                hptFile = new();
                hptFile.FileName = "未命名文件";
            }
            hptFiles.Add(hptFile);
            TabView.SelectedItem = hptFile;
        }
    }
}

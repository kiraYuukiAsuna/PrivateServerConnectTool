// Copyright (c) Microsoft Corporation and Contributors.
// Licensed under the MIT License.

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Titanium.Web.Proxy.Models;
using Titanium.Web.Proxy;
using Windows.Foundation;
using Windows.Foundation.Collections;
using System.Threading.Tasks;
using Titanium.Web.Proxy.EventArguments;
using System.Net;
using System.Net.Security;
using System.Runtime.InteropServices;
using System.Net.Http;
using System.Threading;
using System.Globalization;
using System.Diagnostics;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml.Documents;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace PrivateServerConnectTool
{
    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainWindow : Window
    {
        Config currentConfig;

        ConfigManager configManager;

        Proxy proxy;

        Patch patch;

        public static string CurrentPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "");
        public static string DataPath = Path.Combine(CurrentPath, "Data");
        public static string PatchPath = Path.Combine(CurrentPath, "Patch");
        public static string ConfigFilePath = Path.Combine(DataPath, "config.json");

        [DllImport("kernel32.dll")]
        public static extern bool AllocConsole();//显示控制台
        [DllImport("kernel32.dll")]
        public static extern bool FreeConsole(); //释放控制台、关闭控制台

        public MainWindow()
        {
#if DEBUG
            // show console
            AllocConsole();
#endif
            // set language
            Thread.CurrentThread.CurrentUICulture =
                CultureInfo.CreateSpecificCulture("zh-CN");

            // init
            this.InitializeComponent();

            // load config from file

            Directory.CreateDirectory(DataPath);

            currentConfig = new Config();
            configManager = new ConfigManager();
            configManager.LoadConfigFromFile(ref currentConfig, ConfigFilePath);

            if (currentConfig.gameExeFilePath==null || currentConfig.gameExeFilePath=="")
            {
                currentConfig.gameExeFilePath = "C:/Program Files/Genshin Impact/Genshin Impact Game/YuanShen.exe";
            }
            if(currentConfig.serverIPAddress ==null || currentConfig.serverIPAddress =="")
            {
                currentConfig.serverIPAddress = "124.222.243.43";
            }
            configManager.SaveConfigToFile(ref currentConfig, ConfigFilePath);

            patch = new Patch();
            proxy = new Proxy();

            serverIPTextBox.Text = currentConfig.serverIPAddress;
            proxyPortTextBox.Text = currentConfig.proxyPort.ToString();
            gameExeFilePathTextBox.Text = currentConfig.gameExeFilePath;
            httpsEnableCheckBox.IsChecked = currentConfig.useHTTPS;

            updatePatchStatus();

            if (proxy.IsExternalProxyEnabled())
            {
                ProxyStatus.Text = "Enabled(External)";
            }
            else
            {
                ProxyStatus.Text = "Disabled";
            }

            //PrivateServerConnectTool.Properties.Resources.ResourceManager.GetString("IPAddress");
            //Properties.Resources.IPAddress
        }

        private void updatePatchStatus()
        {
            var mhypbase_path = Path.Combine(Directory.GetParent(currentConfig.gameExeFilePath).ToString(), "mhypbase.dll");
            var res_patchStatus = patch.GetPatchStatus(mhypbase_path);
            if (res_patchStatus == Patch.PatchOpeartionStatus.PATCHED)
            {
                patchStatus.Text = "Patched";
            }
            else if (res_patchStatus == Patch.PatchOpeartionStatus.NOT_PATCHED)
            {
                patchStatus.Text = "Unpatched";
            }
            else
            {
                patchStatus.Text = "Unknow";
            }
        }

        private void handleEnableHTTPSChecked(object sender, RoutedEventArgs e)
        {
            currentConfig.useHTTPS = true;
            configManager.SaveConfigToFile(ref currentConfig, ConfigFilePath);
        }

        private void handleEnableHTTPSUnchecked(object sender, RoutedEventArgs e)
        {
            currentConfig.useHTTPS = false;
            configManager.SaveConfigToFile(ref currentConfig, ConfigFilePath);
        }

        private async void handleEnableProxyBtnClicked(object sender, RoutedEventArgs e)
        {
            if (proxy.IsInternalProxyEnabled())
            {
                ContentDialog dialog = new ContentDialog();

                // XamlRoot must be set in the case of a ContentDialog running in a Desktop app
                dialog.XamlRoot = this.Content.XamlRoot;
                dialog.Style = null;
                dialog.Title = "Info:";
                dialog.PrimaryButtonText = "OK";
                dialog.SecondaryButtonText = null;
                dialog.CloseButtonText = "Cancel";
                dialog.DefaultButton = ContentDialogButton.Primary;
                dialog.Content = "Proxy already Enabled!";

                var result = await dialog.ShowAsync();

                return;
            }

            if (!proxy.Start(currentConfig))
            {
                ContentDialog dialog = new ContentDialog();

                // XamlRoot must be set in the case of a ContentDialog running in a Desktop app
                dialog.XamlRoot = this.Content.XamlRoot;
                dialog.Style = null;
                dialog.Title = "Error:";
                dialog.PrimaryButtonText = "OK";
                dialog.SecondaryButtonText = null;
                dialog.CloseButtonText = "Cancel";
                dialog.DefaultButton = ContentDialogButton.Primary;
                dialog.Content = "Proxy Start Failed!";

                var result = await dialog.ShowAsync();

            }
            else
            {
                ProxyStatus.Text = "Enabled(Internal)";
            }

        }

        private async void handleDisableProxyBtnClicked(object sender, RoutedEventArgs e)
        {
            if (!proxy.IsInternalProxyEnabled())
            {
                ContentDialog dialog = new ContentDialog();

                // XamlRoot must be set in the case of a ContentDialog running in a Desktop app
                dialog.XamlRoot = this.Content.XamlRoot;
                dialog.Style = null;
                dialog.Title = "Info:";
                dialog.PrimaryButtonText = "OK";
                dialog.SecondaryButtonText = null;
                dialog.CloseButtonText = "Cancel";
                dialog.DefaultButton = ContentDialogButton.Primary;
                dialog.Content = "Proxy not being Enabled!";

                var result = await dialog.ShowAsync();

                return;
            }

            proxy.Stop();
            ProxyStatus.Text = "Disabled";
        }

        private async void handlePatchBtnClicked(object sender, RoutedEventArgs e)
        {
            if (!File.Exists(currentConfig.gameExeFilePath))
            {
                ContentDialog dialog = new ContentDialog();

                // XamlRoot must be set in the case of a ContentDialog running in a Desktop app
                dialog.XamlRoot = this.Content.XamlRoot;
                dialog.Style = null;
                dialog.Title = "Info:";
                dialog.PrimaryButtonText = "OK";
                dialog.SecondaryButtonText = null;
                dialog.CloseButtonText = "Cancel";
                dialog.DefaultButton = ContentDialogButton.Primary;
                dialog.Content = "Game exe file not exist! Check your game exe path!";

                var result = await dialog.ShowAsync();
                if (result == ContentDialogResult.Primary)
                {

                }
                else
                {

                }
            }

            var mhypbaseFullpath = Path.Combine(Directory.GetParent(currentConfig.gameExeFilePath).ToString(), "mhypbase.dll");

            var patchResult = patch.DoPatch(PatchPath, mhypbaseFullpath);

            if(patchResult == Patch.PatchOpeartionStatus.SUCCESS)
            {
                updatePatchStatus();

                ContentDialog dialog = new ContentDialog();

                // XamlRoot must be set in the case of a ContentDialog running in a Desktop app
                dialog.XamlRoot = this.Content.XamlRoot;
                dialog.Style = null;
                dialog.Title = "Info:";
                dialog.PrimaryButtonText = "OK";
                dialog.SecondaryButtonText = null;
                dialog.CloseButtonText = "Cancel";
                dialog.DefaultButton = ContentDialogButton.Primary;
                dialog.Content = "Patch successfully!";

                var result = await dialog.ShowAsync();
                if (result == ContentDialogResult.Primary)
                {

                }
                else
                {

                }

            }
            else
            {
                ContentDialog dialog = new ContentDialog();

                // XamlRoot must be set in the case of a ContentDialog running in a Desktop app
                dialog.XamlRoot = Content.XamlRoot;
                dialog.Style = null;
                dialog.Title = "Info:";
                dialog.PrimaryButtonText = "OK";
                dialog.SecondaryButtonText = null;
                dialog.CloseButtonText = "Cancel";
                dialog.DefaultButton = ContentDialogButton.Primary;
                dialog.Content = "Patch failed! Error: " + patchResult.ToString();

                var result = await dialog.ShowAsync();
                if (result == ContentDialogResult.Primary)
                {

                }
                else
                {

                }
            }
        }

        private async void handleUnpatchBtnClicked(object sender, RoutedEventArgs e)
        {
            if (!File.Exists(currentConfig.gameExeFilePath))
            {
                ContentDialog dialog = new ContentDialog();

                // XamlRoot must be set in the case of a ContentDialog running in a Desktop app
                dialog.XamlRoot = this.Content.XamlRoot;
                dialog.Style = null;
                dialog.Title = "Info:";
                dialog.PrimaryButtonText = "OK";
                dialog.SecondaryButtonText = null;
                dialog.CloseButtonText = "Cancel";
                dialog.DefaultButton = ContentDialogButton.Primary;
                dialog.Content = "Game exe file not exist! Check your game exe path!";

                var result = await dialog.ShowAsync();
                if (result == ContentDialogResult.Primary)
                {

                }
                else
                {

                }
            }
            var RSAPatchPath = Path.Combine(Directory.GetParent(currentConfig.gameExeFilePath).ToString(), "mhypbase.dll");
            var mhypbase_path = Path.Combine(Directory.GetParent(currentConfig.gameExeFilePath).ToString(), "mhypbase.dll.backup");

            var patchResult = patch.DoUnpatch(RSAPatchPath, mhypbase_path);

            if (patchResult == Patch.PatchOpeartionStatus.SUCCESS)
            {
                updatePatchStatus();

                ContentDialog dialog = new ContentDialog();

                // XamlRoot must be set in the case of a ContentDialog running in a Desktop app
                dialog.XamlRoot = this.Content.XamlRoot;
                dialog.Style = null;
                dialog.Title = "Info:";
                dialog.PrimaryButtonText = "OK";
                dialog.SecondaryButtonText = null;
                dialog.CloseButtonText = "Cancel";
                dialog.DefaultButton = ContentDialogButton.Primary;
                dialog.Content = "Unpatch successfully!";

                var result = await dialog.ShowAsync();
                if (result == ContentDialogResult.Primary)
                {

                }
                else
                {

                }

            }
            else
            {
                ContentDialog dialog = new ContentDialog();

                // XamlRoot must be set in the case of a ContentDialog running in a Desktop app
                dialog.XamlRoot = this.Content.XamlRoot;
                dialog.Style = null;
                dialog.Title = "Info:";
                dialog.PrimaryButtonText = "OK";
                dialog.SecondaryButtonText = null;
                dialog.CloseButtonText = "Cancel";
                dialog.DefaultButton = ContentDialogButton.Primary;
                dialog.Content = "Unpatch failed! Error: " + patchResult.ToString();

                var result = await dialog.ShowAsync();
                if (result == ContentDialogResult.Primary)
                {

                }
                else
                {

                }
            }
        }

        private async void handleLanuchGameBtnClicked(object sender, RoutedEventArgs e)
        {
            var isrun = Process.GetProcesses().Where(pr => pr.ProcessName == "YuanShen" || pr.ProcessName == "GenshinImpact");
            if (isrun.Any())
            {
                ContentDialog dialog = new ContentDialog();

                // XamlRoot must be set in the case of a ContentDialog running in a Desktop app
                dialog.XamlRoot = this.Content.XamlRoot;
                dialog.Style = null;
                dialog.Title = "Info:";
                dialog.PrimaryButtonText = "OK";
                dialog.SecondaryButtonText = null;
                dialog.CloseButtonText = "Cancel";
                dialog.DefaultButton = ContentDialogButton.Primary;
                dialog.Content = "Game is running. Do you want to kill it before start a new one?";

                var result = await dialog.ShowAsync();
                if (result == ContentDialogResult.Primary)
                {
                    foreach (var process in isrun)
                    {
                        process.Kill();
                    }
                }
            }

            // game exe path check
            if (!File.Exists(currentConfig.gameExeFilePath))
            {
                ContentDialog dialog = new ContentDialog();

                // XamlRoot must be set in the case of a ContentDialog running in a Desktop app
                dialog.XamlRoot = this.Content.XamlRoot;
                dialog.Style = null;
                dialog.Title = "Info:";
                dialog.PrimaryButtonText = "OK";
                dialog.SecondaryButtonText = null;
                dialog.CloseButtonText = "Cancel";
                dialog.DefaultButton = ContentDialogButton.Primary;
                dialog.Content = "Game exe file not exist! Check your game exe path!";

                var result = await dialog.ShowAsync();
                if (result == ContentDialogResult.Primary)
                {
                    return;
                }
                else
                {
                    return;
                }
            }

            // patch status check
            var mhypbase_path = Path.Combine(Directory.GetParent(currentConfig.gameExeFilePath).ToString(), "mhypbase.dll");

            if (patch.GetPatchStatus(mhypbase_path) == Patch.PatchOpeartionStatus.NOT_PATCHED)
            {
                ContentDialog dialog = new ContentDialog();

                // XamlRoot must be set in the case of a ContentDialog running in a Desktop app
                dialog.XamlRoot = this.Content.XamlRoot;
                dialog.Style = null;
                dialog.Title = "Info:";
                dialog.PrimaryButtonText = "OK";
                dialog.SecondaryButtonText = null;
                dialog.CloseButtonText = "Cancel";
                dialog.DefaultButton = ContentDialogButton.Primary;
                dialog.Content = "Game is not being patched! Do you want to run the official version?";

                var result = await dialog.ShowAsync();
                if (result == ContentDialogResult.Primary)
                {
                    
                }
                else
                {
                    return;
                }
            }
            else
            {
                ContentDialog dialog = new ContentDialog();

                // XamlRoot must be set in the case of a ContentDialog running in a Desktop app
                dialog.XamlRoot = this.Content.XamlRoot;
                dialog.Style = null;
                dialog.Title = "Info:";
                dialog.PrimaryButtonText = "OK";
                dialog.SecondaryButtonText = null;
                dialog.CloseButtonText = "Cancel";
                dialog.DefaultButton = ContentDialogButton.Primary;
                dialog.Content = "Game is being patched! Do you want to run the private server version?";

                var result = await dialog.ShowAsync();
                if (result == ContentDialogResult.Primary)
                {

                }
                else
                {
                    return;
                }
            }

            // proxy status check
            if (!proxy.IsInternalProxyEnabled())
            {
                ContentDialog dialog = new ContentDialog();

                // XamlRoot must be set in the case of a ContentDialog running in a Desktop app
                dialog.XamlRoot = this.Content.XamlRoot;
                dialog.Style = null;
                dialog.Title = "Info:";
                dialog.PrimaryButtonText = "OK";
                dialog.SecondaryButtonText = null;
                dialog.CloseButtonText = "Cancel";
                dialog.DefaultButton = ContentDialogButton.Primary;
                dialog.Content = "Proxy is not enabled! Which game version you want to play?  Make sure you have unpatched the game if you want to play official version! Click [OK] to continue, Click [Cancel] to stop.";

                var result = await dialog.ShowAsync();
                if (result == ContentDialogResult.Primary)
                {
                    
                }
                else
                {
                    return;
                }
            }


            var progress = new Process();
            progress.StartInfo = new ProcessStartInfo
            {
                FileName = currentConfig.gameExeFilePath
            };
            try
            {
                progress.Start();
            }
            catch (Exception ex)
            {
                ContentDialog dialog = new ContentDialog();

                // XamlRoot must be set in the case of a ContentDialog running in a Desktop app
                dialog.XamlRoot = this.Content.XamlRoot;
                dialog.Style = null;
                dialog.Title = "Info:";
                dialog.PrimaryButtonText = "OK";
                dialog.SecondaryButtonText = null;
                dialog.CloseButtonText = "Cancel";
                dialog.DefaultButton = ContentDialogButton.Primary;
                dialog.Content = "Error!";

                var result = await dialog.ShowAsync();
                if (result == ContentDialogResult.Primary)
                {
                    return;
                }
                else
                {
                    return;
                }
            }
        }
        private void handleLoadConfigBtnClicked(object sender, RoutedEventArgs e)
        {

        }

        private void handleSaveConfigBtnClicked(object sender, RoutedEventArgs e)
        {
            currentConfig.serverIPAddress = serverIPTextBox.Text;
            currentConfig.proxyPort = Convert.ToInt32(proxyPortTextBox.Text);
            currentConfig.gameExeFilePath = gameExeFilePathTextBox.Text;
            currentConfig.useHTTPS = httpsEnableCheckBox.IsChecked??true;

            configManager.SaveConfigToFile(ref currentConfig, ConfigFilePath);
        }

    }
}

namespace ProcessAffinityManager

open Avalonia
open Avalonia.Controls
open Avalonia.Controls.ApplicationLifetimes
open Avalonia.Data.Core.Plugins
open Avalonia.Markup.Xaml
open Avalonia.Media
open Avalonia.Platform
open CommunityToolkit.Mvvm.Input
open ProcessAffinityManager.Services
open ProcessAffinityManager.ViewModels
open ProcessAffinityManager.Views

type App() =
    inherit Application()

    let systemCpuSetsInfo =
        match NativeMethods.getSystemCpuSets () with
        | Ok info -> info
        | Error error ->
            eprintfn $"Failed to get system CPU sets information: {error}"
            exit 2

    let config = ConfigurationService.load systemCpuSetsInfo

    let showMainWindow (desktop: IClassicDesktopStyleApplicationLifetime) =
        if desktop.MainWindow = null || not desktop.MainWindow.IsLoaded then
            ()
        else
            desktop.MainWindow.Show()
            desktop.MainWindow.Activate()

            if desktop.MainWindow.WindowState = WindowState.Minimized then
                desktop.MainWindow.WindowState <- WindowState.Normal

    override this.Initialize() = AvaloniaXamlLoader.Load(this)

    override this.OnFrameworkInitializationCompleted() =
        // Line below is needed to remove Avalonia data validation.
        // Without this line you will get duplicate validations from both Avalonia and CT
        BindingPlugins.DataValidators.RemoveAt(0)

        // Avalonia does not support variant font, which is wrongly set by FluentAvalonia
        this.Resources["ContentControlThemeFontFamily"] <- FontFamily("Segoe UI")
        this.Resources["ContentDialogMaxWidth"] <- 650.0

        let systemCpuSetsInfo =
            match NativeMethods.getSystemCpuSets () with
            | Ok info -> info
            | Error error ->
                eprintfn $"Failed to get system CPU sets information: {error}"
                exit 2

        match this.ApplicationLifetime with
        | :? IClassicDesktopStyleApplicationLifetime as desktop ->
            desktop.ShutdownRequested.Add(fun _ ->
                (desktop.MainWindow.DataContext :?> MainWindowViewModel).SaveConfiguration())

            let trayIcon = new TrayIcon()

            trayIcon.Icon <- WindowIcon(AssetLoader.Open(System.Uri "avares://ProcessAffinityManager/Assets/logo.ico"))

            trayIcon.ToolTipText <- "Process Affinity Manager"

            let contextMenu = NativeMenu()
            let openItem = NativeMenuItem(Header = "Open")
            let exitItem = NativeMenuItem(Header = "Exit")

            openItem.Click.Add(fun _ -> showMainWindow desktop)
            trayIcon.Clicked.Add(fun _ -> showMainWindow desktop)

            exitItem.Click.Add(fun _ ->
                (desktop.MainWindow.DataContext :?> MainWindowViewModel).SaveConfiguration()
                desktop.Shutdown())

            contextMenu.Add(openItem)
            contextMenu.Add(exitItem)
            trayIcon.Menu <- contextMenu

            trayIcon.IsVisible <- true

            desktop.MainWindow <-
                MainWindow(config.Settings.StartMinimized, DataContext = MainWindowViewModel(config, systemCpuSetsInfo))

            (desktop.MainWindow.DataContext :?> MainWindowViewModel).ApplyTheme()
        | _ -> ()

        base.OnFrameworkInitializationCompleted()

namespace ProcessAffinityManager.ViewModels

open Avalonia.Media
open Avalonia.Media.Immutable
open FluentAvalonia.UI.Media
open ProcessAffinityManager.Services

type SettingsPageViewModel(settings: AppSettings) =
    inherit ViewModelBase()

    let mutable _currentSettings = settings
    let mutable _startOnBoot = StartupService.isStartupTaskScheduled ()

    static member AppThemeTypes = [| System; Dark; Light |]
    
    member this.StartOnSystemBoot
        with get () = _startOnBoot
        and set value =
            if _startOnBoot <> value then
                let success =
                    if value then
                        StartupService.createStartupTask ()
                    else
                        StartupService.removeStartupTask ()

                if success then
                    _startOnBoot <- value

                    _currentSettings <-
                        { _currentSettings with
                            StartOnSystemBoot = value }

                    this.OnPropertyChanged()

    member this.StartMinimized
        with get () = _currentSettings.StartMinimized
        and set value =
            if _currentSettings.StartMinimized <> value then
                _currentSettings <-
                    { _currentSettings with
                        StartMinimized = value }

                this.OnPropertyChanged()

    member this.Theme
        with get () = _currentSettings.Theme
        and set value =
            if _currentSettings.Theme <> value then
                _currentSettings <- { _currentSettings with Theme = value }
                this.OnPropertyChanged()

                this.ApplyTheme()

    member this.ApplyTheme() =
        let theme =
            match this.Theme with
            | AppTheme.Light -> Avalonia.Styling.ThemeVariant.Light
            | AppTheme.Dark -> Avalonia.Styling.ThemeVariant.Dark
            | _ -> Avalonia.Styling.ThemeVariant.Default // System

        match Avalonia.Application.Current.ApplicationLifetime with
        | :? Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime as app ->
            app.MainWindow.RequestedThemeVariant <- theme
            
            // let background =
            //     if app.MainWindow.ActualThemeVariant = Avalonia.Styling.ThemeVariant.Light then
            //         ImmutableSolidColorBrush(Color2(243uy, 243uy, 243uy).LightenPercent 0.5f, 0.9)
            //     else
            //         ImmutableSolidColorBrush(Color2(32uy, 32uy, 32uy).LightenPercent -0.8f, 0.78)
        | _ -> ()

    member this.GetSettings() = _currentSettings

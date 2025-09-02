namespace ProcessAffinityManager.ViewModels

open System.Collections.ObjectModel
open Avalonia
open Avalonia.Controls
open Avalonia.Controls.ApplicationLifetimes
open CommunityToolkit.Mvvm.ComponentModel
open CommunityToolkit.Mvvm.Input
open FluentAvalonia.UI.Controls
open ProcessAffinityManager.Models
open ProcessAffinityManager.Services

type MainWindowViewModel(config: AppConfiguration, systemCpuSetsInfo) =
    inherit ViewModelBase()

    let monitoringService: IProcessMonitoringService = ProcessMonitoringService()

    let masterRules = ObservableCollection<Rule>()
    let masterProfiles = ObservableCollection<Profile>()
    let mutable masterSettings = config.Settings

    let homePageVM = HomePageViewModel(monitoringService, masterProfiles)
    let rulesPageVM = RulesPageViewModel(masterRules, masterProfiles)
    let profilesPageVM = ProfilesPageViewModel(masterProfiles)
    let settingsPageVM = SettingsPageViewModel(masterSettings)
    let logViewerVM = LogViewerViewModel()
    let aboutPageVM = AboutPageViewModel()

    let mutable _currentPage: ObservableObject = homePageVM
    let mutable _isMonitoring = monitoringService.IsRunning

    let getMainWindow () =
        Application.Current.ApplicationLifetime :?> IClassicDesktopStyleApplicationLifetime
        |> _.MainWindow

    do
        masterSettings <- config.Settings
        config.Rules |> List.iter masterRules.Add
        config.Profiles |> List.iter masterProfiles.Add

        rulesPageVM.ResyncViewModels()

        if masterRules.Count <> 0 then
            monitoringService.Start (CpuSetsHelper.cpuSetIdByCoreId systemCpuSetsInfo) masterRules masterProfiles
            homePageVM.RefreshProcessViews()
            _isMonitoring <- monitoringService.IsRunning

    member this.CurrentPage
        with get () = _currentPage
        and set value = base.SetProperty(&_currentPage, value, "CurrentPage") |> ignore

    member this.IsMonitoring
        with get () = _isMonitoring
        and set value = base.SetProperty(&_isMonitoring, value, "IsMonitoring") |> ignore

    member this.OnSelectionChanged (_sender: obj) (e: NavigationViewSelectionChangedEventArgs) =
        let tag = (e.SelectedItem :?> NavigationViewItem).Tag :?> string

        match tag with
        | "Home" -> this.CurrentPage <- homePageVM
        | "Rules" -> this.CurrentPage <- rulesPageVM
        | "Profiles" -> this.CurrentPage <- profilesPageVM
        | "Log" -> this.CurrentPage <- logViewerVM
        | "ToggleMonitoring" -> ()
        | "Settings" -> this.CurrentPage <- settingsPageVM
        | "About" -> this.CurrentPage <- aboutPageVM
        | _ -> invalidArg "NavigationViewSelectionChangedEventArgs" tag

    member this.ToggleMonitoringCommand =
        RelayCommand(fun () ->
            // This property is toggled by Avalonia after user interaction
            if this.IsMonitoring then
                monitoringService.Start (CpuSetsHelper.cpuSetIdByCoreId systemCpuSetsInfo) masterRules masterProfiles
                homePageVM.RefreshProcessViews()
            else
                monitoringService.Stop()

            this.IsMonitoring <- monitoringService.IsRunning)

    member this.SaveConfiguration() =
        let config =
            { Rules = rulesPageVM.Rules |> Seq.toList
              Profiles = profilesPageVM.Profiles |> Seq.toList
              Settings = settingsPageVM.GetSettings() }

        ConfigurationService.save config

    member this.TransparencyLevelHint =
        [ WindowTransparencyLevel.Mica; WindowTransparencyLevel.AcrylicBlur ]

    member this.ApplyTheme = settingsPageVM.ApplyTheme

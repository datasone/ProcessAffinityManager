namespace ProcessAffinityManager.ViewModels

open System.Collections.ObjectModel
open Avalonia.Rendering
open CommunityToolkit.Mvvm.Input
open ProcessAffinityManager.Models
open ProcessAffinityManager.Services

type ProcessViewModel(name, pid, profile) =
    inherit ViewModelBase()

    let mutable _appliedProfileName = profile

    member _.Name = name
    member _.PID = pid

    member this.AppliedProfileName
        with get () = _appliedProfileName
        and set value = base.SetProperty(&_appliedProfileName, value, "AppliedProfileName") |> ignore

type HomePageViewModel(monitoringService: IProcessMonitoringService, allProfiles: ObservableCollection<Profile>) =
    inherit ViewModelBase()

    let processes = ObservableCollection<ProcessViewModel>()

    do
        monitoringService.ProcessStarted.Add(fun procInfo ->
            Avalonia.Threading.Dispatcher.UIThread.Post(fun () ->
                let profileName =
                    procInfo.AppliedProfile
                    |> Option.map _.Name
                    |> Option.defaultValue "No profile applied"

                let newProcVM =
                    ProcessViewModel(procInfo.ProcessName, procInfo.ProcessId, profileName)

                processes.Add newProcVM))

        monitoringService.ProcessChanged.Add(fun procInfo ->
            Avalonia.Threading.Dispatcher.UIThread.Post(fun () ->
                processes
                |> Seq.tryFind (fun (pVM: ProcessViewModel) -> pVM.PID = procInfo.ProcessId)
                |> Option.iter (fun pVM ->
                    let profileName =
                        procInfo.AppliedProfile
                        |> Option.map _.Name
                        |> Option.defaultValue "No profile applied"

                    pVM.AppliedProfileName <- profileName)))

        monitoringService.ProcessEnded.Add(fun processId ->
            Avalonia.Threading.Dispatcher.UIThread.Post(fun () ->
                processes
                |> Seq.tryFind (fun p -> p.PID = processId)
                |> Option.iter (fun p -> processes.Remove p |> ignore)))

    member val AllProfiles = allProfiles with get
    member val Processes = processes with get

    member this.RefreshProcessViews() =
        this.Processes.Clear()

        let currentProcesses = monitoringService.GetCurrentProcesses()

        for procInfo in currentProcesses do
            let profileName =
                procInfo.AppliedProfile
                |> Option.map _.Name
                |> Option.defaultValue "No profile applied"

            let procVM = ProcessViewModel(procInfo.ProcessName, procInfo.ProcessId, profileName)

            this.Processes.Add procVM

        this.OnPropertyChanged()

    member this.RefreshCommand = RelayCommand(fun () -> this.RefreshProcessViews())

    member this.OverrideProfileCommand =
        RelayCommand<obj>(fun param ->
            let procVM, profile = param :?> obj * obj
            let procVM = procVM :?> ProcessViewModel
            let profile = profile :?> Profile

            LoggingService.Info $"Overriding PID {procVM.PID} with profile '{profile.Name}'"
            monitoringService.ApplyProfileToProcess procVM.PID profile)

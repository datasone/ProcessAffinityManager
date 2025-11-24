namespace ProcessAffinityManager.ViewModels

open System.Collections.ObjectModel
open Avalonia
open Avalonia.Controls.ApplicationLifetimes
open CommunityToolkit.Mvvm.Input
open FluentAvalonia.UI.Controls
open ProcessAffinityManager.Models
open ProcessAffinityManager.Views

type RuleViewModel(rule: Rule, profiles: ObservableCollection<Profile>) =
    inherit ViewModelBase()

    let mutable _canMoveUp = true
    let mutable _canMoveDown = true

    member _.Rule = rule

    member _.ProfileName =
        profiles
        |> Seq.tryFind (fun p ->
            match rule.Target with
            | MainProfile pid -> p.Id = pid
            | SubProfile spid -> p.SubProfiles |> Seq.exists (fun sp -> sp.Id = spid))
        |> Option.map _.Name
        |> Option.defaultValue "Profile not found"

    member _.RuleType = rule.RuleType
    member _.Criteria = rule.Criteria

    member _.CanMoveUp
        with get () = _canMoveUp
        and set value = base.SetProperty(&_canMoveUp, value, "CanMoveUp") |> ignore

    member _.CanMoveDown
        with get () = _canMoveDown
        and set value = base.SetProperty(&_canMoveDown, value, "CanMoveDown") |> ignore

type RulesPageViewModel(rules: ObservableCollection<Rule>, profiles: ObservableCollection<Profile>) =
    inherit ViewModelBase()

    let getMainWindow () =
        Application.Current.ApplicationLifetime :?> IClassicDesktopStyleApplicationLifetime
        |> _.MainWindow

    let ruleVMs = ObservableCollection<RuleViewModel>()

    let updateButtonStates () =
        let count = ruleVMs.Count

        ruleVMs
        |> Seq.iteri (fun i vm ->
            vm.CanMoveUp <- i > 0
            vm.CanMoveDown <- i < count - 1)

    let resyncViewModels () =
        ruleVMs.Clear()

        rules
        |> Seq.sortBy _.Priority
        |> Seq.iter (fun rule ->
            let vm = RuleViewModel(rule, profiles)
            ruleVMs.Add vm)

        updateButtonStates ()

    do resyncViewModels ()

    member val RuleVMs = ruleVMs with get

    member val Rules = rules with get
    member val Profiles = profiles with get

    member this.ResyncViewModels = resyncViewModels

    member this.MoveUpCommand =
        RelayCommand<RuleViewModel>(fun ruleVM ->
            let oldIndex = this.RuleVMs.IndexOf ruleVM

            if oldIndex > 0 then
                let ruleToMove = this.Rules[oldIndex]
                let ruleToSwap = this.Rules[oldIndex - 1]
                this.Rules[oldIndex] <- { ruleToSwap with Priority = oldIndex }

                this.Rules[oldIndex - 1] <-
                    { ruleToMove with
                        Priority = oldIndex - 1 }

                resyncViewModels ())

    member this.MoveDownCommand =
        RelayCommand<RuleViewModel>(fun ruleVM ->
            let oldIndex = this.RuleVMs.IndexOf ruleVM

            if oldIndex < this.Rules.Count - 1 then
                let ruleToMove = this.Rules[oldIndex]
                let ruleToSwap = this.Rules[oldIndex + 1]
                this.Rules[oldIndex] <- { ruleToSwap with Priority = oldIndex }

                this.Rules[oldIndex + 1] <-
                    { ruleToMove with
                        Priority = oldIndex + 1 }

                resyncViewModels ())

    member this.AddRuleCommand =
        RelayCommand(fun () ->
            let newRule =
                { Id = System.Guid.NewGuid()
                  RuleType = ProcessName
                  Criteria = ""
                  Priority = 0
                  Target =
                    MainProfile(
                        if profiles.Count > 0 then
                            profiles[0].Id
                        else
                            System.Guid.Empty
                    ) }

            let editorVM = RuleEditorViewModel(newRule, this.Profiles)
            let dialog = RuleEditorDialog(DataContext = editorVM)

            async {
                let! result = dialog.ShowAsync(getMainWindow ()) |> Async.AwaitTask

                if result = ContentDialogResult.Primary then
                    this.Rules.Insert(0, dialog.GetRule())
                    resyncViewModels ()
            }
            |> Async.StartImmediate)

    member this.EditRuleCommand =
        RelayCommand<RuleViewModel>(fun ruleVM ->
            let idx = this.RuleVMs.IndexOf ruleVM

            let editorVM = RuleEditorViewModel(ruleVM.Rule, this.Profiles)
            let dialog = RuleEditorDialog(DataContext = editorVM)

            async {
                let! result = dialog.ShowAsync(getMainWindow ()) |> Async.AwaitTask

                if result = ContentDialogResult.Primary then
                    this.Rules[idx] <- dialog.GetRule()
                    resyncViewModels ()
            }
            |> Async.StartImmediate)

    member this.RemoveRuleCommand =
        RelayCommand<RuleViewModel>(fun ruleVM ->
            async {
                let dialog =
                    BasicDialogs.questionDialog
                        (getMainWindow ())
                        "Confirm Deletion"
                        "Are you sure you want to delete this rule?"

                let! result = dialog.ShowAsync(true) |> Async.AwaitTask

                if result = TaskDialogStandardResult.Yes then
                    this.Rules.Remove(ruleVM.Rule) |> ignore
                    this.RuleVMs.Remove(ruleVM) |> ignore

                resyncViewModels ()
            }
            |> Async.StartImmediate)

namespace ProcessAffinityManager.ViewModels

open System.Collections.ObjectModel
open Avalonia.Controls
open ProcessAffinityManager.Models

type ProfileTargetView(name: string, target: ProfileTarget) =
    member _.Name = name
    member _.Target = target

type RuleEditorViewModel(rule: Rule, allProfiles: ObservableCollection<Profile>) =
    inherit ViewModelBase()

    let mutable _ruleType = rule.RuleType
    let mutable _criteria = rule.Criteria

    let createTargetList () =
        allProfiles
        |> Seq.collect (fun p ->
            let mainTarget = ProfileTargetView(p.Name, MainProfile p.Id)

            let subTargets =
                p.SubProfiles
                |> List.map (fun sp ->
                    let name = $"{p.Name} ({sp.DelaySeconds}s delay)"
                    ProfileTargetView(name, SubProfile sp.Id))

            mainTarget :: subTargets)
        |> ObservableCollection

    let _allTargets = createTargetList ()

    let mutable _selectedTargetView: ProfileTargetView =
        _allTargets
        |> Seq.tryFind (fun tv -> tv.Target = rule.Target)
        |> Option.defaultValue _allTargets[0]

    static member RuleTypes = [| ProcessName; ExecutableFilePath; ExecutableDirectory; FallbackDefault |]

    member val AllTargets = _allTargets with get

    member _.RuleType
        with get () = _ruleType
        and set value = base.SetProperty(&_ruleType, value, "RuleType") |> ignore

    member _.Criteria
        with get () = _criteria
        and set value = base.SetProperty(&_criteria, value, "Criteria") |> ignore

    member _.SelectedTargetView
        with get () = _selectedTargetView
        and set value = base.SetProperty(&_selectedTargetView, value, "SelectedTargetView") |> ignore

    member this.GetRule() =
        { rule with
            RuleType = this.RuleType
            Criteria = this.Criteria
            Target = this.SelectedTargetView.Target }

    member this.TransparencyLevelHint =
        [ WindowTransparencyLevel.Mica; WindowTransparencyLevel.AcrylicBlur ]
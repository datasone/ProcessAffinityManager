namespace ProcessAffinityManager.ViewModels

open System.Collections.ObjectModel
open Avalonia.Controls
open CommunityToolkit.Mvvm.Input
open ProcessAffinityManager.Models

type CoreViewModel() =
    inherit ViewModelBase()

    let mutable _isSelected = false

    member val CoreNumber = 0 with get, set

    member this.IsSelected
        with get () = _isSelected
        and set value = base.SetProperty(&_isSelected, value, "IsSelected") |> ignore

type SubProfileViewModel(subProfile) =
    inherit ViewModelBase()

    let _subProfile = subProfile
    let mutable _delaySeconds = subProfile.DelaySeconds

    member this.DelaySeconds
        with get () = _delaySeconds
        and set value = base.SetProperty(&_delaySeconds, value, "DelaySeconds") |> ignore

    member this.SubProfile =
        { _subProfile with
            DelaySeconds = _delaySeconds }

type ProfileEditorViewModel(profile: Profile, allProfiles: ObservableCollection<Profile>) =
    inherit ViewModelBase()

    let mutable _name = profile.Name
    let mutable _profileType = profile.ProfileType
    let mutable _isExclusive = profile.ExclusiveMode <> NotExclusive

    let mutable _selectedFallback =
        match profile.ExclusiveMode with
        | IsExclusive(Some profileId) ->
            Seq.tryFind (fun (prof: Profile) -> prof.Id = profileId) allProfiles
            |> Option.defaultValue allProfiles[0]
        | _ -> allProfiles[0]

    let coreCount = System.Environment.ProcessorCount

    let cores =
        seq { 0 .. coreCount - 1 }
        |> Seq.map (fun i ->
            let isSet = (profile.CpuMask >>> i) &&& 1L = 1L
            let coreVM = CoreViewModel()
            coreVM.CoreNumber <- i
            coreVM.IsSelected <- isSet
            coreVM)
        |> ObservableCollection
    
    member val Cores = cores with get

    member this.Name
        with get () = _name
        and set value = base.SetProperty(&_name, value, "Name") |> ignore

    member this.ProfileType
        with get () = _profileType
        and set value = base.SetProperty(&_profileType, value, "ProfileType") |> ignore

    member this.GetProfile() =
        let finalMask =
            this.Cores
            |> Seq.fold
                (fun mask core ->
                    if core.IsSelected then
                        mask ||| (1L <<< core.CoreNumber) // Set the bit
                    else
                        mask)
                0L

        let finalExclusiveMode =
            if not this.IsExclusive then
                NotExclusive
            else
                IsExclusive(Some this.SelectedFallbackProfile.Id)

        { profile with
            Name = this.Name
            ProfileType = _profileType
            CpuMask = finalMask
            ExclusiveMode = finalExclusiveMode
            SubProfiles =
                this.SubProfileViewModels
                |> Seq.map (fun (spVM: SubProfileViewModel) -> spVM.SubProfile)
                |> Seq.toList }

    member val AvailableFallbacks = allProfiles |> Seq.where (fun p -> p.Id <> profile.Id) |> ObservableCollection

    member this.IsExclusiveAvailable
        with get () =
            allProfiles
            |> Seq.filter (fun p -> p <> profile)
            |> Seq.forall (fun p -> p.ExclusiveMode = NotExclusive)
    
    member this.IsExclusive
        with get () = _isExclusive
        and set value = base.SetProperty(&_isExclusive, value, "IsExclusive") |> ignore

    member this.SelectedFallbackProfile
        with get (): Profile = _selectedFallback
        and set value = base.SetProperty(&_selectedFallback, value, "SelectedFallbackProfile") |> ignore

    member val SubProfileViewModels =
        profile.SubProfiles |> Seq.map SubProfileViewModel |> ObservableCollection with get

    member this.AddSubProfileCommand =
        RelayCommand(fun () ->
            let newSubProfile =
                { Id = System.Guid.NewGuid()
                  ParentProfileId = profile.Id
                  DelaySeconds = 5 }

            this.SubProfileViewModels.Add(SubProfileViewModel newSubProfile))

    member this.RemoveSubProfileCommand =
        RelayCommand<SubProfileViewModel>(fun subProfileVM -> this.SubProfileViewModels.Remove subProfileVM |> ignore)
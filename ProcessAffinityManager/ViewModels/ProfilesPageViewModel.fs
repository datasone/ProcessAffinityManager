namespace ProcessAffinityManager.ViewModels

open System.Collections.ObjectModel
open Avalonia
open Avalonia.Controls.ApplicationLifetimes
open CommunityToolkit.Mvvm.Input
open FluentAvalonia.UI.Controls
open ProcessAffinityManager.Models
open ProcessAffinityManager.Views

type ProfilesPageViewModel(profiles: ObservableCollection<Profile>) =
    inherit ViewModelBase()

    let getMainWindow () =
        Application.Current.ApplicationLifetime :?> IClassicDesktopStyleApplicationLifetime
        |> _.MainWindow

    member val Profiles = profiles with get

    member this.AddProfileCommand =
        RelayCommand(fun () ->
            let newProfile =
                { Id = System.Guid.NewGuid()
                  Name = "New Profile"
                  ProfileType = ProfileType.CPUAffinity
                  CpuMask = 0L
                  ExclusiveMode = NotExclusive
                  SubProfiles = [] }

            let editorVM = ProfileEditorViewModel(newProfile, profiles)
            let dialog = ProfileEditorDialog(DataContext = editorVM)

            async {
                let! result = dialog.ShowAsync(getMainWindow ()) |> Async.AwaitTask

                if result = ContentDialogResult.Primary then
                    let newProfile = dialog.GetProfile()
                    this.Profiles.Add(newProfile)
            }
            |> Async.StartImmediate)

    member this.EditProfileCommand =
        RelayCommand<Profile>(fun profile ->
            let editorVM = ProfileEditorViewModel(profile, profiles)
            let dialog = ProfileEditorDialog(DataContext = editorVM)

            async {
                let! result = dialog.ShowAsync(getMainWindow ()) |> Async.AwaitTask

                if result = ContentDialogResult.Primary then
                    let newProfile = dialog.GetProfile()
                    let index = this.Profiles |> Seq.findIndex (fun prof -> prof.Id = profile.Id)
                    this.Profiles[index] <- newProfile
            }
            |> Async.StartImmediate)

    member this.RemoveProfileCommand =
        RelayCommand<Profile>(fun profile ->
            async {
                let dialog =
                    BasicDialogs.questionDialog
                        (getMainWindow ())
                        "Confirm Deletion"
                        $"Are you sure you want to delete the profile '{profile.Name}'?"

                let! result = dialog.ShowAsync(true) |> Async.AwaitTask

                if result = TaskDialogStandardResult.Yes then
                    this.Profiles.Remove(profile) |> ignore
            }
            |> Async.StartImmediate)

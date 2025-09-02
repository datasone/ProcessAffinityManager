namespace ProcessAffinityManager.Services

open System
open Avalonia.Data.Converters

type SizeAdjuster() =
    interface IValueConverter with
        member this.Convert(value, _targetType, parameter, _culture) =
            match value, parameter with
            | :? double as parentHeight, (:? string as paramString) ->
                let subtractionValue = Convert.ToDouble paramString

                (parentHeight - subtractionValue) |> max 0.0 |> box
            | _ -> value

        member this.ConvertBack(_value, _targetType, _parameter, _culture) = failwith "unreachable"

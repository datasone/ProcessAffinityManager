module ProcessAffinityManager.Services.NativeMethods

open System
open System.Diagnostics
open System.Runtime.InteropServices

[<CLIMutable>]
[<StructLayout(LayoutKind.Explicit)>]
type CPUSET_ALLFLAGS =
    { [<FieldOffset(0)>]
      AllFlags: byte

      [<FieldOffset(0)>]
      AllFlagsStruct: byte }

[<CLIMutable>]
[<StructLayout(LayoutKind.Explicit)>]
type CPUSET_SCHEDULINGCLASS =
    { [<FieldOffset(0)>]
      Reserved: int

      [<FieldOffset(0)>]
      SchedulingClass: byte }

[<CLIMutable>]
[<StructLayout(LayoutKind.Sequential)>]
type SYSTEM_CPU_SET_INFORMATION_CPUSET =
    { Id: uint32
      Group: uint16
      LogicalProcessorIndex: byte
      CoreIndex: byte
      LastLevelCacheIndex: byte
      NumaNodeIndex: byte
      EfficiencyClass: byte
      AllFlagsStruct: CPUSET_ALLFLAGS
      CpuSetSchedulingClass: CPUSET_SCHEDULINGCLASS
      AllocationTag: uint64 }

[<CLIMutable>]
[<StructLayout(LayoutKind.Explicit)>]
type SYSTEM_CPU_SET_INFORMATION_CPUSET_UNION =
    { [<FieldOffset(0)>]
      CpuSet: SYSTEM_CPU_SET_INFORMATION_CPUSET }

type CPUSET_TYPE =
    | CpuSetInformation = 0

[<CLIMutable>]
[<StructLayout(LayoutKind.Sequential)>]
type SYSTEM_CPU_SET_INFORMATION =
    { Size: uint32
      Type: CPUSET_TYPE
      CpuSetUnion: SYSTEM_CPU_SET_INFORMATION_CPUSET_UNION }

[<DllImport("kernel32.dll", SetLastError = true)>]
extern bool GetSystemCpuSetInformation(
    nativeint Buffer,
    uint32 BufferLength,
    uint32& ReturnLength,
    nativeint Process,
    uint32 Flags
)

let ERROR_INSUFFICIENT_BUFFER = 122

let getSystemCpuSets () =
    let mutable bufferLength = 0u

    GetSystemCpuSetInformation(IntPtr.Zero, 0u, &bufferLength, IntPtr.Zero, 0u)
    |> ignore // It will always fail

    let error = Marshal.GetLastWin32Error()

    if error <> ERROR_INSUFFICIENT_BUFFER then
        Error error
    else
        let buffer = Marshal.AllocHGlobal(int bufferLength)

        try
            let success =
                GetSystemCpuSetInformation(buffer, bufferLength, &bufferLength, IntPtr.Zero, 0u)

            if not success then
                Error(Marshal.GetLastWin32Error())
            else
                let struct_size = Marshal.SizeOf(typeof<SYSTEM_CPU_SET_INFORMATION>)
                let len = int bufferLength / struct_size

                Ok(
                    List.map
                        (fun i ->
                            Marshal.PtrToStructure<SYSTEM_CPU_SET_INFORMATION>(buffer + nativeint (i * struct_size)))
                        [ 0 .. len - 1 ]
                )
        finally
            Marshal.FreeHGlobal buffer

[<DllImport("kernel32.dll", SetLastError = true)>]
extern bool SetProcessDefaultCpuSets(nativeint Process, [<InAttribute>] uint32[] CpuSetIds, uint32 CpuSetIdCount)

let setProcessDefaultCpuSets (processObj: Process) (cpuSetIds: uint32 seq) =
    let cpuSetIds = Array.ofSeq cpuSetIds
    let processHandle = processObj.Handle

    if SetProcessDefaultCpuSets(processHandle, cpuSetIds, uint32 cpuSetIds.Length) then
        Ok()
    else
        Error(Marshal.GetLastWin32Error())

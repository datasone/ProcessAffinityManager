# Process Affnity Manager
A GUI Program inspired by [LassoProcessManager](https://github.com/kenshinakh1/LassoProcessManager/).

## About this tool
This tool was written for improving CPU core scheduling on AMD's 7950X3D.
When I want to put the foreground gaming workload into CCD0 (cache CCD), and put all other programs into CCD1 (frequency CCD).

I originally modified LassoProcessManager to achieve this, but a CLI program is hard to manage and troubleshooting,
so I wrote this GUI tool.

Apart from having a GUI, this tool adds the following features:
- CPU sets, it's a more gentle limiation compared to CPU affinity. It allows program to execute on more cores
if the workload needs more, and some game DRMs don't like CPU affinity being set.
- Exclusive mode, as mentioned earlier, allow some programs to have exclusive access to cores. When activated,
all other programs will be kicked to another profile (CPU core group).
# SilverProject: A New .NET Based Post Exploitation Tool

SilverProject includes two sub-projects: **SilverSmoke**, the payload written in C#, and **SilverFlame** the Command and Control server, written in Python 3.

The payload, **SilverSmoke**, uses the Microsoft's ClearScript C# extension for embedding scripting in .NET applications. Using this package allows the payload size to be kept much smaller than using an alternative embedded language, such as IronPython or Boo. This is because the ClearScript package has the ability to use the already built-in JScript interpreter for interpreting JavaScript, while also giving it access to the full .NET Framework. 

The main distinguishing features of the SilverSmoke payload are:

 - Modularity & Flexibility - Post exploitation modules can be written in C#, Loaded as compiled IL Assemblies, or as ClearScript scripts. C# and ClearScript modules can be dynamically modified according to the options specified in their file, while IL Assemblies can have string parameters passed to functions.
 - Dynamic evaluation/compilation of .NET languages - This  is what allows modules to be written as C# Source, Compiled IL Assemblies, and ClearScript scripts. ClearScript and IL modules are prefered, because C# modules are compiled on the target computer, resulting in calls to csc.exe. Because modules are dynamically evaluated, modules can be edited in real time without needing to be recompiled.
 - Interoperability with native code - The SilverSmoke payload contains a library containing all the necesarry proxy code to allow ClearScript modules to call almost all commonly used native functions.
 - Fileless - The SilverSmoke payload loads and interprets modules in-memory (with the exception of compiling C# source code). This means no additional files need to be dropped to disk to extend the functionality of the payload.
 - Real-time communication - All modules have the ability to send updates to the Command & Control server at any point in their execution.
 - TLS Encryption - All communications between the client and server are secured using TLS/SSL
 - Interoperability with CMD and Powershell - Optionally, the operator can spawn a cmd.exe or powershell.exe process and send commands to it.
 - Minimal dependencies - The only dependency of the payload is the C++ Microsoft Redistributable
 - Smaller size compared to some other .NET based payloads



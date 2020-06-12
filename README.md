# SilverProject: A New .NET DLR Based Post Exploitation Tool

[![Codacy Badge](https://app.codacy.com/project/badge/Grade/3027449b9f8f42b189fb417a62cfed9e)](https://www.codacy.com?utm_source=github.com&amp;utm_medium=referral&amp;utm_content=RT-KT/SilverProject&amp;utm_campaign=Badge_Grade)

SilverProject includes two sub-projects: **SilverSmoke**, the payload written in C#, and **SilverFlame** the Command and Control server, written in Python 3.

The payload, **SilverSmoke**, uses the Microsoft's ClearScript C# extension for embedding scripting in .NET applications. Using this package allows the payload size to be kept much smaller than using an alternative embedded language, such as IronPython or Boo. This is because the ClearScript package has the ability to use the already built-in JScript interpreter for interpreting JavaScript, while also giving it access to the full .NET Framework. 

The main distinguishing features of the SilverSmoke payload are:

 - Modularity & Flexibility - Post exploitation modules can be written in C#, Loaded as compiled .NET Assemblies, or as ClearScript scripts. C# and ClearScript modules can be dynamically modified according to the options specified in their file, while IL Assemblies can have string parameters passed to functions.
 - Dynamic evaluation/compilation of .NET languages - This  is what allows modules to be written as C# Source, Compiled .NET Assemblies, and ClearScript scripts. ClearScript and IL modules are prefered, because C# modules are compiled on the target computer, resulting in calls to csc.exe. Because modules are dynamically evaluated, modules can be edited in real time without needing to be recompiled.
 - Interoperability with native code - The SilverSmoke payload contains a library containing all the necesarry proxy code to allow ClearScript modules to call almost all commonly used native functions.
 - Fileless - The SilverSmoke payload loads and interprets modules in-memory (with the exception of compiling C# source code). This means no additional files need to be dropped to disk to extend the functionality of the payload.
 - Real-time communication - All modules have the ability to send updates to the Command & Control server at any point in their execution.
 - TLS Encryption - All communications between the client and server are secured using TLS/SSL
 - Interoperability with CMD and Powershell - Optionally, the operator can spawn a cmd.exe or powershell.exe process and send commands to it.
 - Minimal dependencies - The only dependencies of the payload are the C++ Microsoft Redistributable, and the .NET Framework
 - Smaller size compared to some other .NET based payloads
- Minimalistic - The payload and C2 server are designed to be as minimal as possible, with code that is easy to follow and understand.


## SilverFlame: The Command & Control Server

The SilverFlame listener allows for the loading of modules, handling many seperate sessions simultaneously, and interacting with cmd or powershell on a target.

## What situations to use SilverSmoke, rather than some other payload (Meterpreter, Empire)

Since both Meterpreter and Empire have been around for a fairly long time, they are usually easily detected. PowerShell now has significantly increased logging capabilities, making PowerShell based attacks less desirable for attackers prioritizing stealth. The SilverSmoke payload allows for scripting and modularity in the same way Empire does, without the drawbacks of having extensive logging and security tools made to detect it (yet?).

Meterpreter and Empire are both *far* more complete than this project is currently. This project is just a demonstration of the .NET DLR's offensive capabilities, and provides an alternative tool for security researchers.

I would not recommend using this in actual engagements, as it has not been thoroughly tested and reviewed yet.

If you're in an engagement, stick with tried-and-true tools. If you want to experiment with .NET DLR capabilities, feel free to try this tool out.

## Installing SilverProject, Building the Payload & Executing it

 - Clone the repository to your local machine: `git clone https://github.com/RT-KT/SilverProject.git`
 - Navigate to the SilverSmoke folder and open the SilverSmoke.sln file in Visual Studio.
 - Modify the `host` and `port` variables at the beginning of the C# file to the IP and port of the attacker machine.
 - On the attacker machine, navigate into the SilverFlame directory with a terminal
 - Optional, but highly recommended: Create new SSL public and private keys in the SSL directory
 - Run `python3 SilverFlame.py`
 - Execute the payload on the target machine
 
 ## Interacting with a client in SilverFlame
 The SilverFlame server commands are modeled after the Metasploit Framework's command structure:
 
 - The `use` command is used to load a module.
 - The `show options` command is used to show modifiable options in a module, and their current values.
 - The `set` command is used to set options. This command respects quotes in it's parsing.
 - The `run` command sends a module to be executed. More than one module can not be executed on a host concurrently.
 - The `list` command lists directories and modules within the modules folder. This command can be given a directory inside the modules folder.
 - The `interact` command allows the operator to switch between different sessions,
 - The `sessions` command shows different sessions and their respective IP addresses
 - The `exec` command can be used to execute Python code on the **attackers** machine, for debugging.
 - Appending a `!` to input will pass the input after it to a cmd.exe process
 - The `exit` command closes all sessions and closes the server.

 ## Related Projects & ## Acknowledgments
 

 - [SILENTTRINITY by byt3bl33d3r](https://github.com/byt3bl33d3r/SILENTTRINITY) - The inspiration for this project
 - [C# SSL Reverse Shell by 0xvm](https://github.com/0xvm/csharp_reverse_shell) - Some base code for instantiating SSL connections in C#, as well as handling shell I/O
 - [GhostPack by harmj0y](https://github.com/GhostPack) - Lots of great offensive C# tools
## TODO/Upcoming Features

 - HTTP/HTTPS based Beacon-style payload
 - Alternative server using Asyncio rather than threading
 - Better encryption
 - Team server
 - Lotsa modules
 - Better logging



# MactypeLiteInjector

The official [MacType](https://github.com/snowie2000/mactype) tray program has the feature to (auto) inject MacType.dll into processes, but it has a bug that hangs on my machines when running for days which the developer don't feel like to fix, and since the MacTypeTray isn't opensourced, there's no way for me to fix it.

So I write my own injector, which can inject MacType.dll into running process or start a process then inject to it.

I have no knowledege on how to write a dll injector, the injector part of code is copied directly from: http://www.codingvision.net/miscellaneous/c-inject-a-dll-into-a-process-w-createremotethread

## Usage
Build this project for both x86 and x64 platforms, rename x64 build filename to MactypeLiteInjector64.exe and put the two executables in the same directory as MacType.dll.

Or you can download the pre-built exetutables from github release page (**warning: most AV might report this program as a virus**)

Then you can use either of the two executables (they would detect whether the given process is a 32-bit or 64-bit one)

```MactypeLiteInjector.exe [PID]``` to inject MacType.dll into a running process  
  
```MactypeLiteInjector.exe [Program To Run]```  or
```MactypeLiteInjector.exe [Program To Run] [Custom Working Directory]``` to start a program then inject to it

## Extra info
Minimal files requierd for MacType to run:
>EasyHK32.dll  
>EasyHK64.dll  
>MacType.Core.dll  
>MacType.dll  
>MacType64.Core.dll  
>MacType64.dll  
>MacType.ini  
>RN_K56CM.ini (my mactype profile)  
>MactypeLiteInjector.exe (this program)  
>MactypeLiteInjector64.exe (this program)  

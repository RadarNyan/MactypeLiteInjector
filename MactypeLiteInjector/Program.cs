using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;

public class BasicInject
{
	[DllImport("kernel32.dll")]
	public static extern IntPtr OpenProcess(int dwDesiredAccess, bool bInheritHandle, int dwProcessId);

	[DllImport("kernel32.dll", CharSet = CharSet.Auto)]
	public static extern IntPtr GetModuleHandle(string lpModuleName);

	[DllImport("kernel32", CharSet = CharSet.Ansi, ExactSpelling = true, SetLastError = true)]
	static extern IntPtr GetProcAddress(IntPtr hModule, string procName);

	[DllImport("kernel32.dll", SetLastError = true, ExactSpelling = true)]
	static extern IntPtr VirtualAllocEx(IntPtr hProcess, IntPtr lpAddress,
		uint dwSize, uint flAllocationType, uint flProtect);

	[DllImport("kernel32.dll", SetLastError = true)]
	static extern bool WriteProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, byte[] lpBuffer, uint nSize, out UIntPtr lpNumberOfBytesWritten);

	[DllImport("kernel32.dll")]
	static extern IntPtr CreateRemoteThread(IntPtr hProcess,
		IntPtr lpThreadAttributes, uint dwStackSize, IntPtr lpStartAddress, IntPtr lpParameter, uint dwCreationFlags, IntPtr lpThreadId);

	[DllImport("kernel32.dll", SetLastError = true, CallingConvention = CallingConvention.Winapi)]
	[return: MarshalAs(UnmanagedType.Bool)]
	public static extern bool IsWow64Process([In] IntPtr hProcess, [Out] out bool lpSystemInfo);

	// privileges
	const int PROCESS_CREATE_THREAD = 0x0002;
	const int PROCESS_QUERY_INFORMATION = 0x0400;
	const int PROCESS_VM_OPERATION = 0x0008;
	const int PROCESS_VM_WRITE = 0x0020;
	const int PROCESS_VM_READ = 0x0010;

	// used for memory allocation
	const uint MEM_COMMIT = 0x00001000;
	const uint MEM_RESERVE = 0x00002000;
	const uint PAGE_READWRITE = 4;

	public static void Main(string[] args)
	{
		string InjectorPath = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);

		bool is64bitMode;
		if (IntPtr.Size == 4) {
			// 32-bit
			is64bitMode = false;
		} else if (IntPtr.Size == 8) {
			// 64-bit
			is64bitMode = true;
		} else {
			// The future is now!
			MessageBox.Show("This program only support running on 32-bit or 64-bit systems.");
			return;
		}

		if (args.Length == 0) {
			string executeName = is64bitMode ? "MactypeLiteInjector64" : "MactypeLiteInjector";
			MessageBox.Show(
				$"Inject running process\r\n" +
				$"{executeName}.exe [PID]\r\n\r\n" +
				$"Start a program and inject\r\n" +
				$"{executeName}.exe [Program To Run]\r\n" +
				$"{executeName}.exe [Program To Run] [Custom Working Directory]", "Usage");
			return;
		}

		// the target process - I'm using a dummy process for this
		// if you don't have one, open Task Manager and choose wisely
		// Process targetProcess = Process.GetProcessesByName("notepad2")[0];
		int processID;
		if (int.TryParse(args[0], out int arg0)) {
			processID = arg0;
		} else {
			ProcessStartInfo targetProcessInfo = new ProcessStartInfo() {
				FileName = Path.GetFileName(args[0]),
				WorkingDirectory = (args.Length == 1) ? Path.GetDirectoryName(args[0]) : args[1]
			};
			Process targetProcess = Process.Start(targetProcessInfo);
			// Wait for Target Process to Start, the dirty way
			while (true) {
				try {
					var time = targetProcess.StartTime;
					break;
				}
				catch (Exception) {
				}
			}
			processID = targetProcess.Id;
		}

		IsWow64Process(Process.GetProcessById(processID).Handle, out bool isWow64);

		if (is64bitMode && isWow64) {
			// 32-bit process on 64-bit injector, call 32-bit injector
			ProcessStartInfo Injector32 = new ProcessStartInfo() {
				FileName = Path.Combine(InjectorPath, "MactypeLiteInjector.exe"),
				Arguments = $"{processID}"
			};
			Process.Start(Injector32);
			return;
		} else if (!is64bitMode && !isWow64) {
			// 64-bit process on 32-bit injector, call 64-bit injector
			ProcessStartInfo Injector64 = new ProcessStartInfo() {
				FileName = Path.Combine(InjectorPath, "MactypeLiteInjector64.exe"),
				Arguments = $"{processID}"
			};
			Process.Start(Injector64);
			return;
		}

		// name of the dll we want to inject
		string dllName = Path.Combine(InjectorPath, is64bitMode ? "MacType64.dll" : "MacType.dll");

		// geting the handle of the process - with required privileges
		IntPtr procHandle = OpenProcess(PROCESS_CREATE_THREAD | PROCESS_QUERY_INFORMATION | PROCESS_VM_OPERATION | PROCESS_VM_WRITE | PROCESS_VM_READ, false, processID);

		// searching for the address of LoadLibraryA and storing it in a pointer
		IntPtr loadLibraryAddr = GetProcAddress(GetModuleHandle("kernel32.dll"), "LoadLibraryA");

		// alocating some memory on the target process - enough to store the name of the dll
		// and storing its address in a pointer
		IntPtr allocMemAddress = VirtualAllocEx(procHandle, IntPtr.Zero, (uint)((dllName.Length + 1) * Marshal.SizeOf(typeof(char))), MEM_COMMIT | MEM_RESERVE, PAGE_READWRITE);

		// writing the name of the dll there
		UIntPtr bytesWritten;
		WriteProcessMemory(procHandle, allocMemAddress, Encoding.Default.GetBytes(dllName), (uint)((dllName.Length + 1) * Marshal.SizeOf(typeof(char))), out bytesWritten);

		// creating a thread that will call LoadLibraryA with allocMemAddress as argument
		CreateRemoteThread(procHandle, IntPtr.Zero, 0, loadLibraryAddr, allocMemAddress, 0, IntPtr.Zero);
	}
}
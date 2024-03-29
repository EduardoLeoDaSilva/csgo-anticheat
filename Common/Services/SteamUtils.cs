﻿using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using Microsoft.Win32;

namespace Common.Utils
{

    /// <summary>
    /// This class allows basic communication with Steam.
    /// </summary>
    public class SteamBridge : IDisposable
    {
        /// <summary>
        /// Handle to the steamclient.dll.
        /// </summary>
        private readonly IntPtr _handle;

        /// <summary>
        /// Address to the vtable of the steam client.
        /// </summary>
        private readonly IntPtr _steamClientVirtualTable;

        /// <summary>
        /// Variable to check if our instance is disposed.
        /// </summary>
        private bool _isDisposed;

        /// <summary>
        /// <see cref="SteamBridge"/>
        /// </summary>
        public SteamBridge()
        {
            // Get the steam path from the registry.
            var steamPath = Registry.GetValue(@"HKEY_CURRENT_USER\Software\Valve\Steam", "SteamPath", "") as string;

            // We need the path to the steam folder.
            if (String.IsNullOrEmpty(steamPath))
                throw new InvalidOperationException("Unable to locate the steam folder");

            // Set the module directory to steam.
            SetDllDirectory(steamPath);

            // Load the steam client library.
            _handle = LoadLibraryEx(Environment.Is64BitProcess ?
                "steamclient64.dll" : "steamclient.dll", IntPtr.Zero, 8);

            // Restore default path.
            SetDllDirectory(null);

            // We have to be able to load the library.
            if (_handle == IntPtr.Zero)
                throw new InvalidOperationException("Unable to load steamclient.dll");

            // Get the virtual table address of the steam client interface.
            _steamClientVirtualTable = GetSteamClientVirtualTableAddress();

            // Make sure we have the virtual table address.
            if (_steamClientVirtualTable == IntPtr.Zero)
                throw new InvalidOperationException("Unable to get the address of ISteamClient012");
        }

        /// <summary>
        /// Destructor to make sure resources are cleaned up.
        /// </summary>
        ~SteamBridge()
        {
            Dispose();
        }

        /// <summary>
        /// Call the CreateInterface method in the Steam Client.
        /// </summary>
        private IntPtr CreateInterface(string version)
        {
            // Get the address of CreateInterface.
            var address = GetProcAddress(_handle, "CreateInterface");

            // We require the address of CreateInterface.
            if (address == IntPtr.Zero)
                throw new InvalidOperationException("CreateInterface not found in steamclient.dll");

            // Marshal the function so we can call it.
            var createInterface = (CreateInterfaceFn)Marshal.GetDelegateForFunctionPointer(
                address, typeof(CreateInterfaceFn));

            // Call the function.
            return createInterface(version, IntPtr.Zero);
        }

        /// <summary>
        /// Get the virtual table address of the ISteamClient012 interface.
        /// </summary>
        private IntPtr GetSteamClientVirtualTableAddress()
        {
            // Use CreateInterface to create the interface and return a pointer.
            var address = CreateInterface("SteamClient012");

            // Read the pointer to the vtable.
            return Marshal.ReadIntPtr(address);
        }

        /// <summary>
        /// Create a steam pipe and return the handle.
        /// </summary>
        private int CreateSteamPipe()
        {
            // The CreateSteamPipe function is index 0 in the vtable.
            var createSteamPipe = (CreateSteamPipeFn)Marshal.GetDelegateForFunctionPointer(
                Marshal.ReadIntPtr(_steamClientVirtualTable, 0 * IntPtr.Size), typeof(CreateSteamPipeFn));

            // Call the method and return the handle.
            return createSteamPipe(_steamClientVirtualTable);
        }

        /// <summary>
        /// Create a steam pipe and return the handle.
        /// </summary>
        private bool ReleaseSteamPipe(int hSteamPipe)
        {
            // The BReleaseSteamPipe function is index 1 in the vtable.
            var releaseSteamPipe = (ReleaseSteamPipeFn)Marshal.GetDelegateForFunctionPointer(
                Marshal.ReadIntPtr(_steamClientVirtualTable, 1 * IntPtr.Size), typeof(ReleaseSteamPipeFn));

            // Call the method and return the status.
            return releaseSteamPipe(_steamClientVirtualTable, hSteamPipe);
        }

        /// <summary>
        /// Get a handle to the global user.
        /// </summary>
        private int ConnectToGlobalUser(int hSteamPipe)
        {
            // The ConnectToGlobalUser function is index 2 in the vtable.
            var connectToGlobalUser = (ConnectToGlobalUserFn)Marshal.GetDelegateForFunctionPointer(
                Marshal.ReadIntPtr(_steamClientVirtualTable, 2 * IntPtr.Size), typeof(ConnectToGlobalUserFn));

            // Call the method and return the handle.
            return connectToGlobalUser(_steamClientVirtualTable, hSteamPipe);
        }

        /// <summary>
        /// Release the handle to the steam user.
        /// </summary>
        private void ReleaseUser(int hSteamPipe, int hSteamUser)
        {
            // The ReleaseUser function is index 4 in the vtable.
            var releaseUser = (ReleaseUserFn)Marshal.GetDelegateForFunctionPointer(
                Marshal.ReadIntPtr(_steamClientVirtualTable, 4 * IntPtr.Size), typeof(ReleaseUserFn));

            // Call the method to release the user handle.
            releaseUser(_steamClientVirtualTable, hSteamPipe, hSteamUser);
        }

        /// <summary>
        /// Get the steam id of the current user.
        /// </summary>
        public UInt64 GetSteamId()
        {
            try
            {


            // Create a steam pipe and connect to the global user.
            var hSteamPipe = CreateSteamPipe();
            var hSteamUser = ConnectToGlobalUser(hSteamPipe);

            // The GetSteamUser function is index 5 in the vtable.
            var getSteamUser = (GetSteamUserFn)Marshal.GetDelegateForFunctionPointer(
                Marshal.ReadIntPtr(_steamClientVirtualTable, 5 * IntPtr.Size), typeof(GetSteamUserFn));

            // Get the address where we can find the vtable address.
            var steamUserAddress = getSteamUser(_steamClientVirtualTable, hSteamUser, hSteamPipe, "SteamUser012");

            // Get the virtual table of the user.
            var steamUserVirtualTableAddress = Marshal.ReadIntPtr(steamUserAddress);

            // The GetSteamId function is index 2 in the vtable.
            var getSteamId = (GetSteamIdFn)Marshal.GetDelegateForFunctionPointer(
                Marshal.ReadIntPtr(steamUserVirtualTableAddress, 2 * IntPtr.Size), typeof(GetSteamIdFn));

            // Create a variable to hold the steam id.
            UInt64 id = 0;

            // Call the method to read the steam id into the variable.
            getSteamId(steamUserAddress, ref id);

            // Release the global user and steam pipe.
            ReleaseUser(hSteamPipe, hSteamUser);
            ReleaseSteamPipe(hSteamPipe);

            // Return the steam id.
            return id;

            }
            catch (Exception e)
            {
                Console.WriteLine("Abra a steam toba profundo");
                return 0;
            }
        }

        /// <summary>
        ///  <see cref="IDisposable.Dispose"/>
        /// </summary>
        public void Dispose()
        {
            // Don't do anything if we are already disposed.
            if (_isDisposed)
                return;

            // Free the library.
            if (_handle != IntPtr.Zero)
                FreeLibrary(_handle);

            // Everthing has been disposed.
            _isDisposed = true;
        }

        #region Delegates

        [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        internal delegate IntPtr CreateInterfaceFn(string version, IntPtr returnCode);

        [UnmanagedFunctionPointer(CallingConvention.ThisCall, CharSet = CharSet.Ansi)]
        internal delegate int CreateSteamPipeFn(IntPtr thisA);

        [UnmanagedFunctionPointer(CallingConvention.ThisCall, CharSet = CharSet.Ansi)]
        internal delegate bool ReleaseSteamPipeFn(IntPtr thisA, int hSteamPipe);

        [UnmanagedFunctionPointer(CallingConvention.ThisCall, CharSet = CharSet.Ansi)]
        internal delegate int ConnectToGlobalUserFn(IntPtr thisA, int hSteamPipe);

        [UnmanagedFunctionPointer(CallingConvention.ThisCall, CharSet = CharSet.Ansi)]
        internal delegate void ReleaseUserFn(IntPtr thisA, int hSteamPipe, int hUser);

        [UnmanagedFunctionPointer(CallingConvention.ThisCall, CharSet = CharSet.Ansi)]
        internal delegate IntPtr GetSteamUserFn(IntPtr thisA, int hUser, int hSteamPipe, string pchVersion);

        [UnmanagedFunctionPointer(CallingConvention.ThisCall, CharSet = CharSet.Ansi)]
        internal delegate void GetSteamIdFn(IntPtr thisA, ref UInt64 steamId);

        #endregion

        #region Native

        [DllImport("kernel32.dll", CharSet = CharSet.Ansi, SetLastError = true)]
        internal static extern IntPtr GetProcAddress(IntPtr hModule, string procName);

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        internal static extern IntPtr LoadLibraryEx(string lpFileName, IntPtr hFile, UInt32 dwFlags);

        [DllImport("kernel32.dll", SetLastError = true)]
        internal static extern bool FreeLibrary(IntPtr hModule);

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        internal static extern bool SetDllDirectory(string lpPathName);

        #endregion
    }

    public class SteamUtils
    {

        public static string GetCurrentSteamUser()
        {
            Process steam;

            Process[] temp;

            string fileName,
                contents;

            string result;

            FileStream fs;


            steam = null;
            result = string.Empty;

            try
            {
                temp = Process.GetProcessesByName("steam");

                if (temp.Length == 1)
                    steam = temp[0];
                else if (temp.Length > 1)
                    Console.WriteLine("You have more than one steam running.");
                else
                {
                    temp = Process.GetProcessesByName("steam.exe");

                    if (temp.Length == 1)
                        steam = temp[0];
                    else if (temp.Length > 1)
                        Console.WriteLine("You have more than one steam running.");
                }


                if (steam != null)
                {
                    fileName = steam.MainModule.FileName.ToLower();

                    fileName = fileName.Replace("steam.exe.exe", "logs\\connection_log.txt").Replace("steam.exe", "logs\\connection_log.txt");

                    if (File.Exists(fileName) == true)
                    {
                        fs = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);

                        using (StreamReader sr = new StreamReader(fs))
                            contents = sr.ReadToEnd();

                        contents = contents;
                        result = contents;
                        var tt = result.Split(' ');
                        var steamUsers =  Regex.Matches(result, "U:[0-9]{1}:[0-9]+");

                        var steamTreateUser = steamUsers.Select(x => x.Value);
                        return steamTreateUser.Last();

                    }
                }
            }
            catch { }
            return result;

        }

        public static Int64 TranslateSteamID(string steamID)
        {
            Int64 result = 0;

            var template = new Regex(@"STEAM_(\d):([0-1]):(\d+)");
            var matches = template.Matches(steamID);
            if (matches.Count <= 0) return 0;
            var parts = matches[0].Groups;
            if (parts.Count != 4) return 0;

            Int64 x = Int64.Parse(parts[1].Value) << 24;
            Int64 y = Int64.Parse(parts[2].Value);
            Int64 z = Int64.Parse(parts[3].Value) << 1;

            result = ((1 + (1 << 20) + x) << 32) | (y + z);
            return result;
        }
    }
}

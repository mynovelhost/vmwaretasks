using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using VixCOM;

namespace VMWareCrash
{
    class Program
    {
        private static VixCOM.VixLib vix = new VixLib();

        private static void ThreadProc(object param)
        {
            ConnectionInfo connectionInfo = (ConnectionInfo)param;

            while (true)
            {
                try
                {
                    // connect to a VI host
                    ConsoleOutput.WriteLine("Connecting to {0}", string.IsNullOrEmpty(connectionInfo.Uri)
                        ? "VMWare Workstation"
                        : connectionInfo.Uri);
                    IJob connectJob = vix.Connect(Constants.VIX_API_VERSION, connectionInfo.HostType,
                        connectionInfo.Uri, 0, connectionInfo.Username, connectionInfo.Password, 0, null, null);
                    object[] connectProperties = { Constants.VIX_PROPERTY_JOB_RESULT_HANDLE };
                    object hosts = null;
                    ulong rc = connectJob.Wait(connectProperties, ref hosts);
                    if (vix.ErrorIndicatesFailure(rc))
                    {
                        ((IVixHandle2) connectJob).Close();
                        throw new Exception(vix.GetErrorText(rc, "en-US"));
                    }

                    IHost host = (IHost)((object[])hosts)[0];

                    {
                        // open a vm
                        ConsoleOutput.WriteLine("Opening {0}", connectionInfo.Vmx);
                        IJob openJob = host.OpenVM(connectionInfo.Vmx, null);
                        object[] openProperties = { Constants.VIX_PROPERTY_JOB_RESULT_HANDLE };
                        object openResults = null;
                        rc = openJob.Wait(openProperties, ref openResults);
                        if (vix.ErrorIndicatesFailure(rc))
                        {
                            ((IVixHandle2) openJob).Close();
                            throw new Exception(vix.GetErrorText(rc, "en-US"));
                        }
                        ConsoleOutput.WriteLine("Opened {0}", connectionInfo.Vmx);
                        IVM2 vm = (IVM2)((object[])openResults)[0];
                        ((IVixHandle2)openJob).Close();
                        // get root snapshot
                        ConsoleOutput.WriteLine("Fetching root snapshot");
                        ISnapshot snapshot = null;
                        rc = vm.GetRootSnapshot(0, out snapshot);
                        if (vix.ErrorIndicatesFailure(rc)) throw new Exception(vix.GetErrorText(rc, "en-US"));
                        ConsoleOutput.WriteLine("Reverting to snapshot");
                        // revert to the snapshot
                        IJob revertJob = vm.RevertToSnapshot(snapshot, Constants.VIX_VMPOWEROP_NORMAL, null, null);
                        rc = revertJob.WaitWithoutResults();
                        ((IVixHandle2)snapshot).Close();
                        ((IVixHandle2)revertJob).Close();
                        if (vix.ErrorIndicatesFailure(rc)) throw new Exception(vix.GetErrorText(rc, "en-US"));
                        // power on
                        ConsoleOutput.WriteLine("Powering on");
                        IJob powerOnJob = vm.PowerOn(VixCOM.Constants.VIX_VMPOWEROP_NORMAL, null, null);
                        rc = powerOnJob.WaitWithoutResults();
                        ((IVixHandle2)powerOnJob).Close();
                        if (vix.ErrorIndicatesFailure(rc)) throw new Exception(vix.GetErrorText(rc, "en-US"));
                        // wait for tools in guest
                        ConsoleOutput.WriteLine("Waiting for tools");
                        IJob waitForToolsJob = vm.WaitForToolsInGuest(240, null);
                        rc = waitForToolsJob.WaitWithoutResults();
                        ((IVixHandle2)waitForToolsJob).Close();
                        if (vix.ErrorIndicatesFailure(rc)) throw new Exception(vix.GetErrorText(rc, "en-US"));
                        // power off
                        ConsoleOutput.WriteLine("Powering off");
                        IJob powerOffJob = vm.PowerOff(VixCOM.Constants.VIX_VMPOWEROP_NORMAL, null);
                        rc = powerOffJob.WaitWithoutResults();
                        ((IVixHandle2)powerOffJob).Close();
                        if (vix.ErrorIndicatesFailure(rc)) throw new Exception(vix.GetErrorText(rc, "en-US"));
                        ((IVixHandle2)vm).Close();
                    }

                    // disconnect
                    ConsoleOutput.WriteLine("Disconnecting");
                    host.Disconnect();
                }
                catch (Exception ex)
                {
                    ConsoleOutput.WriteLine("ERROR: {0}", ex.Message);
                    ConsoleOutput.WriteLine("{0}", ex.StackTrace);
                }
            }
        }

        /// <summary>
        /// AV demo, see http://communities.vmware.com/thread/196429.
        /// </summary>
        /// <param name="args"></param>
        static void Main(string[] args)
        {
            try
            {
                ConnectionInfo[] connections = 
                {
                    //new ConnectionInfo(@"c:\Users\dblock\Virtual Machines\Windows XP Pro SP2\winXPPro.vmx"),
                    //new ConnectionInfo(@"c:\Users\dblock\Documents\Virtual Machines\WinXP Pro SP3\WinXP Pro SP3.vmx"),
                    new ConnectionInfo("https://crockett.nycapt35k.com/sdk", "consoleuser", "admin123", "[console-doogie] nkrasnov2k8/nkrasnov2k8.vmx"),
                    // new ConnectionInfo("https://linc.nycapt35k.com/sdk", "vmuser", "admin123", "[dbprotect-1] ddoub-mauve/ddoub-mauve.vmx"),
                    // new ConnectionInfo("https://linc.nycapt35k.com/sdk", "vmuser", "admin123", "[dbprotect-1] ddoub-purple/ddoub-purple.vmx"),
                    // new ConnectionInfo("https://tubbs.nycapt35k.com/sdk", "vmuser", "admin123", "[adpro-1] snowtest-w2k8/snowtest-w2k8.vmx"),
                };

                // spawn threads
                List<Thread> threads = new List<Thread>(connections.Length);
                foreach (ConnectionInfo connectionInfo in connections)
                {
                    ConsoleOutput.WriteLine("Starting thread {0}.", threads.Count + 1);
                    ParameterizedThreadStart threadStart = new ParameterizedThreadStart(ThreadProc);
                    Thread thread = new Thread(threadStart);
                    thread.Start(connectionInfo);
                    threads.Add(thread);
                }

                // wait for the threads to complete
                foreach (Thread thread in threads)
                {
                    thread.Join();
                }
            }
            catch (Exception ex)
            {
                ConsoleOutput.WriteLine("ERROR: {0}", ex.Message);
            }
        }
    }
}

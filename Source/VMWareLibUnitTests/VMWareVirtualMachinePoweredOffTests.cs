﻿using System;
using System.Collections.Generic;
using NUnit.Framework;
using Vestris.VMWareLib;
using System.Configuration;
using System.IO;
using System.Drawing;
using System.Threading;
using System.Text;
using Interop.VixCOM;

namespace Vestris.VMWareLibUnitTests
{
    [TestFixture]
    public class VMWareVirtualMachinePoweredOffTests : VMWareUnitTest
    {
        public override void SetUp()
        {
            if (! _test.Config.RunPoweredOffTests)
                Assert.Ignore("Skipping, powered off tests disabled.");

            foreach (VMWareVirtualMachine virtualMachine in _test.VirtualMachines)
            {
                if (virtualMachine.IsRunning)
                {
                    virtualMachine.PowerOff();
                }
            }
        }

        [Test]
        protected void TestUpgradeVirtualHardware()
        {
            foreach (VMWareVirtualMachine virtualMachine in _test.VirtualMachines)
            {
                // upgrading virtual hardware should always succeed
                ConsoleOutput.WriteLine("Upgrading virtual hardware ...");
                virtualMachine.UpgradeVirtualHardware();
            }
        }

        [Test]
        public void TestCloneVirtualMachine()
        {           
            if (!_test.Config.RunWorkstationTests)
                Assert.Ignore("Skipping test, Workstation tests disabled.");

            foreach (VMWareVirtualMachine virtualMachine in _test.VirtualMachines)
            {
                string vmxPathName = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
                ConsoleOutput.WriteLine("Creating linked clone: {0}", vmxPathName);
                Directory.CreateDirectory(vmxPathName);
                string vmxFileName = Path.Combine(vmxPathName, "Clone.vmx");
                virtualMachine.Clone(VMWareVirtualMachineCloneType.Linked, vmxFileName);
                Assert.IsTrue(File.Exists(vmxFileName));
                Directory.Delete(vmxPathName, true);
            }
        }

        [Test]
        public void TestDeleteVirtualMachine()
        {
            if (!_test.Config.RunWorkstationTests)
                Assert.Ignore("Skipping, test requires server admin privileges for ESX, Workstation tests disabled.");

            foreach (IVMWareTestProvider testProvider in _test.Providers)
            {
                VMWareVirtualMachine virtualMachine = testProvider.VirtualMachine;
                string vmxPathName = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
                ConsoleOutput.WriteLine("Creating linked clone: {0}", vmxPathName);
                Directory.CreateDirectory(vmxPathName);
                string vmxFileName = Path.Combine(vmxPathName, "Clone.vmx");
                virtualMachine.Clone(VMWareVirtualMachineCloneType.Linked, vmxFileName);
                Assert.IsTrue(File.Exists(vmxFileName));

                VMWareVirtualHost virtualHost = testProvider.ConnectedVirtualHost;
                VMWareVirtualMachine virtualMachineClone = virtualHost.Open(vmxFileName);
                virtualMachineClone.Delete(Constants.VIX_VMDELETE_DISK_FILES);
                Assert.IsFalse(File.Exists(vmxFileName));
                Assert.IsFalse(Directory.Exists(vmxPathName));
            }
        }

        [Test]
        public void TestCloneVirtualMachineSnapshot()
        {
            if (!_test.Config.RunWorkstationTests)
                Assert.Ignore("Skipping, test requires server admin privileges for ESX, Workstation tests disabled.");

            foreach (VMWareVirtualMachine virtualMachine in _test.VirtualMachines)
            {
                string vmxPathName = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
                ConsoleOutput.WriteLine("Creating linked clone of root snapshot: {0}", vmxPathName);
                Directory.CreateDirectory(vmxPathName);
                string vmxFileName = Path.Combine(vmxPathName, "Clone.vmx");
                virtualMachine.Snapshots.GetCurrentSnapshot().Clone(VMWareVirtualMachineCloneType.Linked, vmxFileName);
                Assert.IsTrue(File.Exists(vmxFileName));
                Directory.Delete(vmxPathName, true);
            }
        }
    }
}

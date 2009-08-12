using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using Interop.VixCOM;
using System.IO;

namespace Vestris.VMWareLib
{
    /// <summary>
    /// A collection of root snapshots.
    /// </summary>
    /// <remarks>
    /// Shared snapshots will only be accessible inside the guest operating system if snapshots are 
    /// enabled for the virtual machine.
    /// </remarks>
    public class VMWareRootSnapshotCollection : VMWareSnapshotCollection
    {
        /// <summary>
        ///  A collection of snapshots that belong to a virtual machine.
        /// </summary>
        /// <param name="vm">A virtual machine instance.</param>
        public VMWareRootSnapshotCollection(IVM2 vm)
            : base(vm, null)
        {

        }

        /// <summary>
        /// A list of root snapshots on the current virtual machine.
        /// </summary>
        /// <remarks>
        /// The list is populated on first access, this may time some time.
        /// </remarks>
        /// <returns>A list of snapshots.</returns>
        protected override List<VMWareSnapshot> Snapshots
        {
            get
            {
                if (_snapshots == null)
                {
                    List<VMWareSnapshot> snapshots = new List<VMWareSnapshot>();
                    int nSnapshots = 0;
                    VMWareInterop.Check(_vm.GetNumRootSnapshots(out nSnapshots));
                    for (int i = 0; i < nSnapshots; i++)
                    {
                        ISnapshot snapshot = null;
                        VMWareInterop.Check(_vm.GetRootSnapshot(i, out snapshot));
                        snapshots.Add(new VMWareSnapshot(_vm, snapshot, null));
                    }
                    _snapshots = snapshots;
                }

                return _snapshots;
            }
        }

        /// <summary>
        /// Get a snapshot by its exact name. 
        /// </summary>
        /// <param name="name">Snapshot name.</param>
        /// <returns>A snapshot.</returns>
        /// <remarks>This function will throw an exception if more than one snapshot with the same exists or if the snapshot doesn't exist.</remarks>
        public VMWareSnapshot GetNamedSnapshot(string name)
        {
            ISnapshot snapshot = null;
            ulong rc = _vm.GetNamedSnapshot(name, out snapshot);
            switch (rc)
            {
                case Constants.VIX_OK:
                    return new VMWareSnapshot(_vm, snapshot, null);
                default:
                    VMWareInterop.Check(rc);
                    break;
            }
            return null;
        }

        /// <summary>
        /// Current snapshot.
        /// </summary>
        /// <returns>Current snapshot.</returns>
        public VMWareSnapshot GetCurrentSnapshot()
        {
            ISnapshot snapshot = null;
            VMWareInterop.Check(_vm.GetCurrentSnapshot(out snapshot));
            return new VMWareSnapshot(_vm, snapshot, null);
        }

        /// <summary>
        /// Delete/remove a snapshot.
        /// </summary>
        /// <param name="item">Snapshot to delete.</param>
        /// <returns>True if the snapshot was deleted.</returns>
        public void RemoveSnapshot(VMWareSnapshot item)
        {
            item.RemoveSnapshot();
            _snapshots = null;
        }

        /// <summary>
        /// Delete a snapshot.
        /// </summary>
        /// <param name="name">Name of the snapshot to delete.</param>
        public void RemoveSnapshot(string name)
        {
            RemoveSnapshot(GetNamedSnapshot(name));
        }

        /// <summary>
        /// Create a new snapshot, child of the current snapshot.
        /// </summary>
        /// <param name="name">Snapshot name.</param>
        /// <param name="description">Snapshot description.</param>
        public void CreateSnapshot(string name, string description)
        {
            CreateSnapshot(name, description, 0, VMWareInterop.Timeouts.CreateSnapshotTimeout);
        }

        /// <summary>
        /// Create a new snapshot, child of the current snapshot.
        /// </summary>
        /// <param name="name">Snapshot name.</param>
        /// <param name="description">Snapshot description.</param>
        /// <param name="flags">Flags, one of 
        /// <list type="bullet">
        ///  <item>VIX_SNAPSHOT_INCLUDE_MEMORY: Captures the full state of a running virtual machine, including the memory</item>
        /// </list>
        /// </param>
        /// <param name="timeoutInSeconds">Timeout in seconds.</param>
        public void CreateSnapshot(string name, string description, int flags, int timeoutInSeconds)
        {
            VMWareJobCallback callback = new VMWareJobCallback();
            using (VMWareJob job = new VMWareJob(_vm.CreateSnapshot(name, description, flags, null, callback), callback))
            {
                job.Wait(timeoutInSeconds);
            }
            _snapshots = null;
        }
    }
}

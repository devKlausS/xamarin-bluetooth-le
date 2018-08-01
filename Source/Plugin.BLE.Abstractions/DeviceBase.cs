﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Plugin.BLE.Abstractions.Contracts;

namespace Plugin.BLE.Abstractions
{
    public interface ICancellationMaster
    {
        CancellationTokenSource TokenSource { get; set; }
    }

    public static class ICancellationMasterExtensions
    {
        public static CancellationTokenSource GetCombinedSource(this ICancellationMaster cancellationMaster, CancellationToken token)
        {
            return CancellationTokenSource.CreateLinkedTokenSource(cancellationMaster.TokenSource.Token, token);
        }

        public static void CancelEverything(this ICancellationMaster cancellationMaster)
        {
            cancellationMaster.TokenSource?.Cancel();
            cancellationMaster.TokenSource?.Dispose();
            cancellationMaster.TokenSource = null;
        }

        public static void CancelEverythingAndReInitialize(this ICancellationMaster cancellationMaster)
        {
            cancellationMaster.CancelEverything();
            cancellationMaster.TokenSource = new CancellationTokenSource();
        }
    }

    public abstract class DeviceBase : IDevice, ICancellationMaster
    {
        protected readonly IAdapter Adapter;
        protected readonly List<IService> KnownServices = new List<IService>();
        public Guid Id { get; protected set; }
        public string Name { get; protected set; }
        public int Rssi { get; protected set; }
        public DeviceState State => GetState();
        public IList<AdvertisementRecord> AdvertisementRecords { get; protected set; }
        public abstract object NativeDevice { get; }

        CancellationTokenSource ICancellationMaster.TokenSource { get; set; } = new CancellationTokenSource();

        protected DeviceBase(IAdapter adapter)
        {
            Adapter = adapter;
        }

        public async Task<IList<IService>> GetServicesAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            if (!KnownServices.Any())
            {
                using (var source = this.GetCombinedSource(cancellationToken))
                {
                    KnownServices.AddRange(await GetServicesNativeAsync());
                }
            }

            return KnownServices;
        }

        public async Task<IService> GetServiceAsync(Guid id, CancellationToken cancellationToken = default(CancellationToken))
        {
            var services = await GetServicesAsync(cancellationToken);
            return services.FirstOrDefault(x => x.Id == id);
        }

        public async Task<int> RequestMtuAsync(int requestValue)
        {
            return await RequestMtuNativeAsync(requestValue);
        }

        public bool UpdateConnectionInterval(ConnectionInterval interval)
        {
            return UpdateConnectionIntervalNative(interval);
        }

        public abstract Task<bool> UpdateRssiAsync();
        protected abstract DeviceState GetState();
        protected abstract Task<IEnumerable<IService>> GetServicesNativeAsync();
        protected abstract Task<int> RequestMtuNativeAsync(int requestValue);
        protected abstract bool UpdateConnectionIntervalNative(ConnectionInterval interval);

        public override string ToString()
        {
            return Name;
        }

        public virtual void Dispose()
        {
            Adapter.DisconnectDeviceAsync(this);
        }

        public void ClearServices()
        {
            this.CancelEverythingAndReInitialize();

            foreach (var service in KnownServices)
            {
                try
                {
                    service.Dispose();
                }
                catch (Exception ex)
                {
                    Trace.Message("Exception while cleanup of service: {0}", ex.Message);
                }
            }

            KnownServices.Clear();
        }

        public override bool Equals(object other)
        {
            if (other == null)
            {
                return false;
            }

            if (other.GetType() != GetType())
            {
                return false;
            }

            var otherDeviceBase = (DeviceBase) other;
            return Id == otherDeviceBase.Id;
        }

        public override int GetHashCode()
        {
            return Id.GetHashCode();
        }
    }
}
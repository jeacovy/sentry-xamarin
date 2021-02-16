﻿using Sentry.Xamarin.Extensions;
using Sentry.Extensibility;
using System;
using Xamarin.Essentials;
using Sentry.Reflection;

namespace Sentry.Xamarin.Internals
{
    /// <summary>
    /// An event processor that populates the Event with xamarin specific Tags.
    /// </summary>
    public partial class XamarinEventProcessor : ISentryEventProcessor
    {
        private Lazy<FormsContext> _formsContext = new Lazy<FormsContext>(() => new FormsContext());
        private SentryXamarinOptions _options;
        private volatile bool _formsContextLoaded = true;
        private volatile bool _ConnectivityStatusAllowed = true;

        internal static readonly SdkVersion NameAndVersion
            = typeof(XamarinEventProcessor).Assembly.GetNameAndVersion();

        private class FormsContext
        {
            internal string Manufacturer { get; }
            internal string Model { get; }
            internal string Platform { get; }
            internal string PlatformVersion { get; }
            internal bool IsEmulator { get; }
            internal string Type { get; }
            internal float? ScreenDensity { get; }
            internal string ScreenResolution { get; }

            internal FormsContext()
            {
                Manufacturer = DeviceInfo.Manufacturer.FilterUnknownOrEmpty();
                Model = DeviceInfo.Model.FilterUnknownOrEmpty();
                Platform = DeviceInfo.Platform.ToString();
                PlatformVersion = DeviceInfo.VersionString;
                IsEmulator = DeviceInfo.DeviceType != DeviceType.Physical;
                Type = DeviceInfo.Idiom.ToString();
                try
                {
                    ScreenResolution = $"{DeviceDisplay.MainDisplayInfo.Height}x{DeviceDisplay.MainDisplayInfo.Width}";
                    ScreenDensity = (float)DeviceDisplay.MainDisplayInfo.Density;
                }
                //xUnit Throws Exception because no Screen is present.
                catch { }
            }
        }

        /// <summary>
        /// The NativeEventProcessor contructor.
        /// </summary>
        /// <param name="options">The Sentry options.</param>
        public XamarinEventProcessor(SentryXamarinOptions options) => _options = options;

        /// <summary>
        /// Applies the Xamarin Tags and Context.
        /// </summary>
        /// <param name="event">The event to be applied.</param>
        /// <returns>The Sentry event.</returns>
        public SentryEvent Process(SentryEvent @event)
        {
            if (_formsContextLoaded)
            {
                try
                {
                    var formsContext = _formsContext.Value;


                    if (NameAndVersion.Version != null)
                    {
                        @event.Sdk.Name = _options.ProtocolPackageName;
                        @event.Sdk.Version = NameAndVersion.Version;

                        @event.Sdk.AddPackage(_options.ProtocolPackageName, NameAndVersion.Version);
                    }

                    @event.Contexts.Device.Simulator = formsContext.IsEmulator;
                    @event.Contexts.Device.Manufacturer = formsContext.Manufacturer;
                    @event.Contexts.Device.Model = formsContext.Model;
                    @event.Contexts.OperatingSystem.Name = formsContext.Platform;
                    @event.Contexts.OperatingSystem.Version = formsContext.PlatformVersion;
                    @event.Contexts.Device.ScreenResolution = formsContext.ScreenResolution;
                    @event.Contexts.Device.ScreenDensity = formsContext.ScreenDensity;
                    if (_ConnectivityStatusAllowed)
                    {
                        @event.Contexts.Device.IsOnline = Connectivity.NetworkAccess == NetworkAccess.Internet;
                    }
                }
                catch(PermissionException pe)
                {
                    _options.DiagnosticLogger?.Log(SentryLevel.Error, "Failed to capture ConnectivityStatus FormsEventProcessor into event.", pe);
                    _ConnectivityStatusAllowed = false;
                }
                catch (Exception ex)
                {
                    _options.DiagnosticLogger?.Log(SentryLevel.Error, "Failed to add FormsEventProcessor into event.", ex);
                    //In case of any failure, this process function will be disabled to avoid throwing exceptions for future events.
                    _formsContextLoaded = false;
                }
            }
            else
            {
                _options.DiagnosticLogger?.Log(SentryLevel.Debug, "FormsEventProcessor disabled due to previous error.");
            }
            return @event;
        }
    }
}
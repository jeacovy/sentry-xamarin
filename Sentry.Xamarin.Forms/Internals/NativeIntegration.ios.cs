﻿using Foundation;
using Sentry.Integrations;
using Sentry.Protocol;
using System;
using System.Collections.Generic;
using UIKit;

namespace Sentry.Xamarin.Forms.Internals
{
    internal partial class NativeIntegration : ISdkIntegration
    {
        internal bool Implemented => true;
        private List<NSObject> _observerTokens;
        private SentryXamarinOptions _xamarinOptions;
        private IHub _hub;

        internal NativeIntegration(SentryXamarinOptions options) => _xamarinOptions = options;

        /// <summary>
        /// Initialize the iOS specific code.
        /// </summary>
        /// <param name="hub">The hub.</param>
        /// <param name="options">The Sentry options.</param>
        public void Register(IHub hub, SentryOptions options)
        {
            _hub = hub;
            _observerTokens = new List<NSObject>();
            _observerTokens.Add(NSNotificationCenter.DefaultCenter.AddObserver(UIApplication.DidEnterBackgroundNotification, AppEnteredBackground));
            _observerTokens.Add(NSNotificationCenter.DefaultCenter.AddObserver(UIApplication.WillEnterForegroundNotification, AppEnteredForeground));
            _observerTokens.Add(NSNotificationCenter.DefaultCenter.AddObserver(UIApplication.DidReceiveMemoryWarningNotification, MemoryWarning));
        }
        internal void Unregister()
        {
            NSNotificationCenter.DefaultCenter.RemoveObservers(_observerTokens);
            _xamarinOptions.NativeIntegrationEnabled = false;
        }

        internal Action<NSNotification> AppEnteredBackground => (_) =>
        {
            _hub.AddBreadcrumb(null,
                "ui.lifecycle",
                "navigation", data: new Dictionary<string, string>
                {
                    ["screen"] = SentryXamarinFormsIntegration.CurrentPage,
                    ["state"] = "background"
                }, level: BreadcrumbLevel.Info);
        };

        internal Action<NSNotification> AppEnteredForeground => (_) =>
        {
            _hub.AddBreadcrumb(null,
                "ui.lifecycle",
                "navigation", data: new Dictionary<string, string>
                {
                    ["screen"] = SentryXamarinFormsIntegration.CurrentPage,
                    ["state"] = "foreground"
                }, level: BreadcrumbLevel.Info);
        };

        internal Action<NSNotification> MemoryWarning => (_) =>
        {
            _hub.AddBreadcrumb("low memory",
                "xamarin",
                "info",
                level: BreadcrumbLevel.Warning);
        };
    }
}
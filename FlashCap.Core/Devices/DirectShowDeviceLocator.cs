////////////////////////////////////////////////////////////////////////////
//
// FlashCap - Independent camera capture library.
// Copyright (c) Kouji Matsui (@kekyo@mi.kekyo.net)
//
// Licensed under Apache-v2: https://opensource.org/licenses/Apache-2.0
//
////////////////////////////////////////////////////////////////////////////

using FlashCap.Internal;
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace FlashCap.Devices;

internal enum DirectShowDeviceLocatorKind
{
    DevicePath,
    MonikerDisplayName,
}

internal readonly struct DirectShowDeviceLocator
{
    public readonly string Value;
    public readonly DirectShowDeviceLocatorKind Kind;

    internal bool RequiresClocklessGraph =>
        this.Kind == DirectShowDeviceLocatorKind.MonikerDisplayName;

    private DirectShowDeviceLocator(
        string value, DirectShowDeviceLocatorKind kind)
    {
        this.Value = value;
        this.Kind = kind;
    }

    internal static DirectShowDeviceLocator? Create(
        NativeMethods_DirectShow.IMoniker moniker,
        NativeMethods_DirectShow.IPropertyBag propertyBag)
    {
        var devicePath = propertyBag.GetValue(
            "DevicePath", default(string))?.Trim();
        if (!string.IsNullOrEmpty(devicePath))
        {
            return new DirectShowDeviceLocator(
                devicePath!, DirectShowDeviceLocatorKind.DevicePath);
        }

        var monikerDisplayName = GetDisplayName(moniker);
        return !string.IsNullOrEmpty(monikerDisplayName) ?
            new DirectShowDeviceLocator(
                monikerDisplayName!,
                DirectShowDeviceLocatorKind.MonikerDisplayName) :
            null;
    }

    private bool Matches(NativeMethods_DirectShow.IMoniker moniker)
    {
        if (this.Kind == DirectShowDeviceLocatorKind.DevicePath)
        {
            var value = this.Value;
            return moniker.GetPropertyBag() is { } propertyBag &&
                propertyBag.SafeReleaseBlock(propertyBag =>
                    string.Equals(
                        propertyBag.GetValue(
                            "DevicePath", default(string))?.Trim(),
                        value,
                        StringComparison.Ordinal));
        }

        return string.Equals(
            GetDisplayName(moniker),
            this.Value,
            StringComparison.Ordinal);
    }

    private static string? GetDisplayName(
        NativeMethods_DirectShow.IMoniker moniker)
    {
        ((System.Runtime.InteropServices.ComTypes.IMoniker)moniker).
            GetDisplayName(null!, null!, out var displayName);
        return displayName;
    }

    internal static NativeMethods_DirectShow.IBaseFilter? BindCaptureSource(
        NativeMethods_DirectShow.IMoniker moniker)
    {
        object? boundObject = null;
        try
        {
            var captureSource = moniker.BindToObject(
                null, null,
                in NativeMethods_DirectShow.IID_IBaseFilter,
                out boundObject) == 0 ?
                boundObject as NativeMethods_DirectShow.IBaseFilter :
                null;
            if (captureSource is { })
            {
                boundObject = null;
            }
            return captureSource;
        }
        finally
        {
            if (boundObject is { } &&
                Marshal.IsComObject(boundObject))
            {
                Marshal.ReleaseComObject(boundObject);
            }
        }
    }

    internal NativeMethods_DirectShow.IBaseFilter? FindCaptureSource()
    {
        foreach (var moniker in NativeMethods_DirectShow.EnumerateDeviceMoniker(
            NativeMethods_DirectShow.CLSID_VideoInputDeviceCategory))
        {
            try
            {
                if (this.Matches(moniker))
                {
                    return BindCaptureSource(moniker);
                }
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex);
            }
        }

        return null;
    }

    public override string ToString() =>
        $"{this.Kind}={this.Value}";
}

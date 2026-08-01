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
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace FlashCap.Devices;

public sealed class DirectShowDevices : CaptureDevices
{
    public DirectShowDevices() :
        this(new DefaultBufferPool())
    {
    }

    public DirectShowDevices(BufferPool defaultBufferPool) :
        base(defaultBufferPool)
    {
    }

    private CaptureDeviceDescriptor? CreateDescriptor(
        NativeMethods_DirectShow.IMoniker moniker)
    {
        try
        {
            return moniker.GetPropertyBag() is { } propertyBag ?
                propertyBag.SafeReleaseBlock(propertyBag =>
                {
                    if (propertyBag.GetValue(
                        "FriendlyName", default(string))?.Trim() is not { } friendlyName ||
                        DirectShowDeviceLocator.Create(
                            moniker, propertyBag) is not { } locator ||
                        DirectShowDeviceLocator.BindCaptureSource(moniker) is not { } captureSource)
                    {
                        return null;
                    }

                    var name = string.IsNullOrEmpty(friendlyName) ?
                        "Unknown" : friendlyName;

                    return captureSource.SafeReleaseBlock(
                        captureSource =>
                            (CaptureDeviceDescriptor)new DirectShowDeviceDescriptor(
                                locator, name,
                                propertyBag.GetValue(
                                    "Description", default(string))?.Trim() ??
                                    $"{name} (DirectShow)",
                                captureSource.EnumeratePins().
                                Collect(pin =>
                                    pin.GetPinInfo() is { } pinInfo &&
                                    pinInfo.dir == NativeMethods_DirectShow.PIN_DIRECTION.Output ?
                                        pin : null).
                                SelectMany(pin =>
                                    pin.EnumerateFormats().
                                    Collect(format => format.CreateVideoCharacteristics())).
                                Distinct().
                                OrderByDescending(vc => vc).
                                ToArray(),
                                this.DefaultBufferPool));
                }) :
                null;
        }
        catch (Exception ex)
        {
            Trace.WriteLine(ex);
            return null;
        }
    }

    protected override IEnumerable<CaptureDeviceDescriptor> OnEnumerateDescriptors() =>
        NativeMethods_DirectShow.EnumerateDeviceMoniker(
            NativeMethods_DirectShow.CLSID_VideoInputDeviceCategory).
        Collect(this.CreateDescriptor);
}

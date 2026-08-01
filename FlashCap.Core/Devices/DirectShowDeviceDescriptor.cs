////////////////////////////////////////////////////////////////////////////
//
// FlashCap - Independent camera capture library.
// Copyright (c) Kouji Matsui (@kekyo@mi.kekyo.net)
//
// Licensed under Apache-v2: https://opensource.org/licenses/Apache-2.0
//
////////////////////////////////////////////////////////////////////////////

using System.Threading;
using System.Threading.Tasks;

namespace FlashCap.Devices;

public sealed class DirectShowDeviceDescriptor : CaptureDeviceDescriptor
{
    private readonly DirectShowDeviceLocator locator;

    internal DirectShowDeviceDescriptor(
        DirectShowDeviceLocator locator, string name, string description,
        VideoCharacteristics[] characteristics,
        BufferPool defaultBufferPool) :
        base(name, description, characteristics, defaultBufferPool) =>
        this.locator = locator;

    public override object Identity =>
        this.locator.Value;

    public override DeviceTypes DeviceType =>
        DeviceTypes.DirectShow;

    protected override Task<CaptureDevice> OnOpenWithFrameProcessorAsync(
        VideoCharacteristics characteristics,
        TranscodeFormats transcodeFormat,
        FrameProcessor frameProcessor,
        CancellationToken ct) =>
        this.InternalOnOpenWithFrameProcessorAsync(
            new DirectShowDevice(this.locator, this.Name),
            characteristics, transcodeFormat, frameProcessor, ct);
}

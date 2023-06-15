﻿namespace FFmpeg.Wrapper;

public unsafe class AudioEncoder : MediaEncoder
{
    public AVSampleFormat SampleFormat {
        get => _ctx->sample_fmt;
        set => SetOrThrowIfOpen(ref _ctx->sample_fmt, value);
    }
    public int SampleRate {
        get => _ctx->sample_rate;
        set => SetOrThrowIfOpen(ref _ctx->sample_rate, value);
    }
    public int NumChannels => _ctx->ch_layout.nb_channels;
    public AVChannelLayout ChannelLayout {
        get => _ctx->ch_layout;
        set => SetOrThrowIfOpen(ref _ctx->ch_layout, value);
    }

    public AudioFormat Format {
        get => new(_ctx);
        set {
            _ctx->sample_rate = value.SampleRate;
            _ctx->sample_fmt = value.SampleFormat;
            _ctx->ch_layout = value.Layout;
        }
    }

    /// <summary> Number of samples per channel in an audio frame (set after the encoder is opened). </summary>
    /// <remarks>
    /// Each submitted frame except the last must contain exactly frame_size samples per channel.
    /// May be null when the codec has AV_CODEC_CAP_VARIABLE_FRAME_SIZE set, then the frame size is not restricted.
    /// </remarks>
    public int? FrameSize => _ctx->frame_size == 0 ? null : _ctx->frame_size;

    public AudioEncoder(AVCodecID codecId, in AudioFormat format, int bitrate = 0)
        : this(MediaCodec.GetEncoder(codecId), format, bitrate) { }

    public AudioEncoder(MediaCodec codec, in AudioFormat format, int bitrate = 0)
        : this(AllocContext(codec), takeOwnership: true)
    {
        Format = format;
        BitRate = bitrate;
        TimeBase = new AVRational() { den = format.SampleRate, num = 1 };
    }

    public AudioEncoder(AVCodecContext* ctx, bool takeOwnership)
        : base(ctx, MediaTypes.Audio, takeOwnership) { }
}
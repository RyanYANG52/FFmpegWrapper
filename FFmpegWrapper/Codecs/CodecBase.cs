﻿namespace FFmpeg.Wrapper;

public unsafe abstract class CodecBase : FFObject
{
    protected AVCodecContext* _ctx;
    protected bool _ownsContext;
    private bool _hasUserExtraData = false;

    public AVCodecContext* Handle {
        get {
            ThrowIfDisposed();
            return _ctx;
        }
    }
    public bool IsOpen { get; private set; } = false;

    public string CodecName => new string((sbyte*)_ctx->codec->long_name);

    public AVRational TimeBase {
        get => _ctx->time_base;
        set => SetOrThrowIfOpen(ref _ctx->time_base, value);
    }
    public AVRational FrameRate {
        get => _ctx->framerate;
        set => SetOrThrowIfOpen(ref _ctx->framerate, value);
    }
    /// <summary> Timestamp scale, in seconds. </summary>
    public double TimeScale => ffmpeg.av_q2d(_ctx->time_base);

    public Span<byte> ExtraData {
        get => GetExtraData();
        set => SetExtraData(value);
    }

    /// <summary> Indicates if the codec requires flushing with NULL input at the end in order to give the complete and correct output. </summary>
    public bool IsDelayed => (_ctx->codec->capabilities & ffmpeg.AV_CODEC_CAP_DELAY) != 0;

    public AVMediaType CodecType => _ctx->codec_type;

    internal CodecBase(AVCodec* codec)
    {
        _ctx = ffmpeg.avcodec_alloc_context3(codec);
        _ownsContext = true;

        if (_ctx == null) {
            throw new OutOfMemoryException("Failed to allocate codec context.");
        }
    }
    internal CodecBase(AVCodecContext* ctx)
    {
        _ctx = ctx;
        _ownsContext = false;
    }

    protected static AVCodec* FindCoder(AVCodecID codecId, AVMediaType type, bool isEncoder)
    {
        AVCodec* codec = isEncoder 
            ? ffmpeg.avcodec_find_encoder(codecId)
            : ffmpeg.avcodec_find_decoder(codecId);
        
        if (codec == null) {
            throw new NotSupportedException($"Could not find {(isEncoder ? "decoder" : "encoder")} for codec {codecId.ToString().Substring("AV_CODEC_ID_".Length)}.");
        }
        if (codec->type != type) {
            throw new ArgumentException("Specified codec is not valid for the current media type.");
        }
        return codec;
    }

    /// <summary> Initializes the codec. </summary>
    public virtual void Open()
    {
        if (!IsOpen) {
            ffmpeg.avcodec_open2(Handle, null, null).CheckError("Could not open codec");
            IsOpen = true;
        }
    }

    /// <summary> Reset the decoder state / flush internal buffers. </summary>
    public virtual void Flush()
    {
        if (!IsOpen) {
            throw new InvalidOperationException("Cannot flush closed codec");
        }
        ffmpeg.avcodec_flush_buffers(Handle);
    }

    private Span<byte> GetExtraData()
    {
        return new Span<byte>(_ctx->extradata, _ctx->extradata_size);
    }
    private void SetExtraData(Span<byte> buf)
    {
        ThrowIfOpen();

        ffmpeg.av_freep(&_ctx->extradata);

        if (buf.IsEmpty) {
            _ctx->extradata = null;
            _ctx->extradata_size = 0;
        } else {
            _ctx->extradata = (byte*)ffmpeg.av_mallocz((ulong)buf.Length + ffmpeg.AV_INPUT_BUFFER_PADDING_SIZE);
            _ctx->extradata_size = buf.Length;
            buf.CopyTo(new Span<byte>(_ctx->extradata, buf.Length));
            _hasUserExtraData = true;
        }
    }

    protected void SetOrThrowIfOpen<T>(ref T loc, T value)
    {
        ThrowIfOpen();
        loc = value;
    }

    protected void ThrowIfOpen()
    {
        ThrowIfDisposed();

        if (IsOpen) {
            throw new InvalidOperationException("Value must be set before the codec is open.");
        }
    }

    protected override void Free()
    {
        if (_ctx != null) {
            if (_hasUserExtraData) {
                ffmpeg.av_freep(&_ctx->extradata);
            }
            if (_ownsContext) {
                fixed (AVCodecContext** c = &_ctx) {
                    ffmpeg.avcodec_free_context(c);
                }
            } else {
                _ctx = null;
            }
        }
    }
    protected void ThrowIfDisposed()
    {
        if (_ctx == null) {
            throw new ObjectDisposedException(nameof(CodecBase));
        }
    }
}
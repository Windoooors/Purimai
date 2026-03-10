using System;
using ManagedBass;
using ManagedBass.Mix;
using UI;

namespace Game
{
    public static class GlobalAudioMixer
    {
        public static int MixerHandle { get; private set; }

        public static void Init()
        {
            if (MixerHandle != 0) return;

            Bass.Configure(Configuration.UpdatePeriod, 5);
            Bass.Configure(Configuration.DeviceBufferLength, 8);

            if (!Bass.Init(-1, 44100, DeviceInitFlags.Latency))
                if (Bass.LastError != Errors.Already)
                    throw new Exception("BASS Init Failed");

            MixerHandle = BassMix.CreateMixerStream(44100, 2, BassFlags.Default);
            Bass.ChannelSetAttribute(MixerHandle, ChannelAttribute.Buffer, 0);

            Bass.ChannelPlay(MixerHandle);

            UIManager.OnApplicationLoseFocus += () =>
            {
                Bass.ChannelPause(MixerHandle);
            };
            
            UIManager.OnApplicationHasFocus += () =>
            {
                Bass.ChannelPlay(MixerHandle);
            };
        }

        public static void Free()
        {
            if (MixerHandle != 0)
            {
                Bass.StreamFree(MixerHandle);
                MixerHandle = 0;
                Bass.Free();
            }
        }
    }

    public class BassHandler : IDisposable
    {
        private const BassFlags MixerPauseFlag = BassFlags.MixerChanPause;
        private bool _disposed;
        private int _sourceStream;

        public BassHandler(string filePath)
        {
            GlobalAudioMixer.Init();

            _sourceStream = Bass.CreateStream(filePath, 0, 0, BassFlags.Decode | BassFlags.Prescan);

            if (_sourceStream == 0)
                throw new Exception($"Loading Failed: {Bass.LastError}");

            BassMix.MixerAddChannel(GlobalAudioMixer.MixerHandle, _sourceStream,
                BassFlags.Default | BassFlags.MixerChanNoRampin);
            Pause();
        }

        public bool IsPlaying
        {
            get
            {
                if (_disposed || _sourceStream == 0) return false;
                var inMixer = BassMix.ChannelGetMixer(_sourceStream) != 0;
                var isPaused = BassMix.ChannelHasFlag(_sourceStream, MixerPauseFlag);
                return inMixer && !isPaused && Bass.ChannelIsActive(_sourceStream) == PlaybackState.Playing;
            }
        }

        public float Volume
        {
            get
            {
                CheckDisposed();
                Bass.ChannelGetAttribute(_sourceStream, ChannelAttribute.Volume, out var vol);
                return vol;
            }
            set
            {
                CheckDisposed();
                Bass.ChannelSetAttribute(_sourceStream, ChannelAttribute.Volume, Math.Clamp(value, 0f, 1f));
            }
        }

        public double Duration => Bass.ChannelBytes2Seconds(_sourceStream, Bass.ChannelGetLength(_sourceStream));

        public void Dispose()
        {
            if (!_disposed)
            {
                if (_sourceStream != 0)
                {
                    BassMix.MixerRemoveChannel(_sourceStream);
                    Bass.StreamFree(_sourceStream);
                    _sourceStream = 0;
                }

                _disposed = true;
            }

            GC.SuppressFinalize(this);
        }

        public void Play()
        {
            CheckDisposed();

            var state = BassMix.ChannelHasFlag(_sourceStream, MixerPauseFlag);

            if (state)
                BassMix.ChannelRemoveFlag(_sourceStream, MixerPauseFlag);
        }

        public void PlayOneShot()
        {
            CheckDisposed();

            BassMix.ChannelSetPosition(_sourceStream, 0);
            Play();
        }

        public void Pause()
        {
            CheckDisposed();
            BassMix.ChannelAddFlag(_sourceStream, MixerPauseFlag);
        }

        public void Stop()
        {
            CheckDisposed();
            Pause();
            Bass.ChannelSetPosition(_sourceStream, 0);
        }

        public double GetPosition()
        {
            return Bass.ChannelBytes2Seconds(_sourceStream, Bass.ChannelGetPosition(_sourceStream));
        }

        public void SetPosition(double seconds)
        {
            Bass.ChannelSetPosition(_sourceStream, Bass.ChannelSeconds2Bytes(_sourceStream, seconds));
        }

        private void CheckDisposed()
        {
            if (_disposed) throw new ObjectDisposedException(nameof(BassHandler));
        }

        ~BassHandler()
        {
            Dispose();
        }
    }
}
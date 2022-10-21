
// can't use FadeInOutSampleProvider to fade out in advanced:
// https://github.com/naudio/NAudio/blob/master/Docs/FadeInOutSampleProvider.md
// "For example, to automatically fade out at the end of the source, you'd actually need to read ahead by the duration of the fade (or know in advance where you want the fade to begin)"

// so use DelayFadeOutSampleProvider:
// [errors] https://gist.github.com/markheath/8fb396a5fe4bf117f361
// [fixed? nope] https://gist.github.com/Ahmed-Abdelhameed/b867a8cfe739cd7a128b73a33d317402
// via: https://www.markheath.net/post/naudio-delay-fade-out
// rewritten (this): https://gist.github.com/pengowray/621ec0566199a0a22d51a51e1be77784

using System;
using NAudio.Wave;

namespace upSidetone.Sound;

/// <summary>
/// Sample Provider to allow fading in and out
/// </summary>
public class FadeOutSampleProvider : ISampleProvider {

    private readonly object lockObject = new object();
    private readonly ISampleProvider source;

    private int fadeInStart;
    private int fadeInSamples;
    private int fadeOutStart;
    private int fadeOutSamples;

    private int position;

    /// <summary>
    /// Creates a new FadeInOutSampleProvider
    /// </summary>
    /// <param name="source">The source stream with the audio to be faded in or out</param>
    /// <param name="initiallySilent">If true, we start faded out</param>
    public FadeOutSampleProvider(ISampleProvider source) {
        this.source = source;
    }

    public void SetFadeIn(TimeSpan FadeInLength, TimeSpan FadeInStartPosition) {
        lock (lockObject) {
            fadeInStart = (int)(FadeInStartPosition.TotalSeconds * source.WaveFormat.SampleRate); ;
            fadeInSamples = (int)(FadeInLength.TotalSeconds * source.WaveFormat.SampleRate);
        }
    }

    public void SetFadeIn(TimeSpan FadeInLength) {
        lock (lockObject) {
            fadeInStart = 0;
            fadeInSamples = (int)(FadeInLength.TotalSeconds * source.WaveFormat.SampleRate);
        }
    }

    public void SetFadeInSeconds(double fadeDurationInSeconds) {
        lock (lockObject) {
            fadeInStart = 0;
            fadeInSamples = (int)(fadeDurationInSeconds * source.WaveFormat.SampleRate);
        }
    }


    public void SetFadeInSamples(int fadeDurationInSamples) {
        lock (lockObject) {
            fadeInStart = 0;
            fadeInSamples = fadeDurationInSamples;
        }
    }

    /// <summary>
    /// Requests that a fade-out begins (will start on the next call to Read)
    /// </summary>
    /// <param name="fadeDurationInSeconds">Duration of fade in seconds</param>
    public void SetFadeOutSeconds(double fadeAfterSeconds, double fadeDurationInSeconds) {
        lock (lockObject) {
            fadeOutStart = (int)(fadeAfterSeconds * source.WaveFormat.SampleRate);
            fadeOutSamples = (int)(fadeDurationInSeconds * source.WaveFormat.SampleRate);
        }
    }

    public void SetFadeOutSamples(int fadeAfterSamples, int fadeDurationInSamples) {
        lock (lockObject) {
            fadeOutStart = fadeAfterSamples;
            fadeOutSamples = fadeDurationInSamples;
        }
    }


    public void SetFadeOut(TimeSpan FadeOutStartPosition, TimeSpan FadeOutLength) {
        lock (lockObject) {
            fadeOutStart = (int)(FadeOutStartPosition.TotalSeconds * source.WaveFormat.SampleRate); ;
            fadeOutSamples = (int)(FadeOutLength.TotalSeconds * source.WaveFormat.SampleRate);
        }
    }

    public void FadeOut() {
        lock (lockObject) {
            if (position >= fadeOutStart) return;

            fadeOutStart = position;
        }
    }

    public void FadeEnding(TimeSpan FadeOutLength, TimeSpan SourceLength) {
        lock (lockObject) {
            var wave = source as IWaveProvider;
            fadeOutStart = (int)((SourceLength - FadeOutLength).TotalSeconds * source.WaveFormat.SampleRate);
            fadeOutSamples = (int)(FadeOutLength.TotalSeconds * source.WaveFormat.SampleRate);
        }
    }

    /// <summary>
    /// Reads samples from this sample provider
    /// </summary>
    /// <param name="buffer">Buffer to read into</param>
    /// <param name="offset">Offset within buffer to write to</param>
    /// <param name="count">Number of samples desired</param>
    /// <returns>Number of samples read</returns>
    public int Read(float[] buffer, int offset, int count) {
        int sourceSamplesRead = source.Read(buffer, offset, count);

        lock (lockObject) {
            for (int i = 0; i < sourceSamplesRead; i++) {
                int samplePos = position + i;

                if (fadeInSamples > 0) {
                    if (samplePos < fadeInStart) {
                        buffer[i + offset] = 0;
                    } else if (samplePos >= fadeInStart && samplePos < (fadeInStart + fadeInSamples)) {
                        buffer[i + offset] *= (samplePos - fadeInStart) / (float)fadeInSamples;
                    }
                }

                if (fadeOutSamples > 0) {
                    if (samplePos >= fadeOutStart && samplePos < (fadeOutStart + fadeOutSamples)) {
                        buffer[i + offset] *= (1 - ((samplePos - fadeOutStart) / (float)fadeOutSamples));
                    } else if (samplePos >= fadeOutStart + fadeOutSamples) {
                        buffer[i + offset] = 0;
                    }
                }
            }
            position += sourceSamplesRead;
        }
        return sourceSamplesRead;
    }

    /// <summary>
    /// WaveFormat of this SampleProvider
    /// </summary>
    public WaveFormat WaveFormat {
        get { return source.WaveFormat; }
    }
}
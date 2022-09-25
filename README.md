# UpSidetone 

by Pengo Wray [AA9GO]

Low latency morse code sounder / oscillator / "keyer" / straight key beeper.

![image](https://user-images.githubusercontent.com/800133/192145002-a4ffde78-d4ef-469f-8a21-27c6ac3b8d66.png)

# Uses

* Practice morse code keying with a straight key
* Send Morse code over Zoom or Discord to your CW Club [either: 1. Share the app, or 2. Configure VoiceMeeter Banana to send the audio with your mic; TODO: guide]
* Proof-of-concept to show a low latency PC-generated sidetone is possible in 2022.
* Test latency of your audio devices (e.g. use a recording device to measure the time from mouse click to beep)
* Start of a general purpose utility, CW hardware and software.

#Motivation

UpSidetone was created so I could practice keying morse code with a straight key connected up to my Windows PC (via a cheap modified USB mouse), without buying specialized hardware (which has high shipping times and costs to my home in Australia). It's basic, but it's now at a point where people other than me might also find a use for it.

Default PC audio typically has too much latency to comfortably listen to your own Morse code as you send it, but this app gives you ways to reduce the latency: choose a better driver, choosing a low sample rate, and reducing the latency setting (aka buffer size). For my hardware, this is plenty to eleminate the need for an expensive radio or ugly sounding practice ossilator. I've only tested with my own hardware (an old Roland Duo Capture, and my motherboard's Realtek ASIO), so your mileage may vary. If you do a bunch of testing on various hardware, please share your results.

# Future plans

The longer term vision for UpSidetone is (apart from making beeps) to bridge physical morse keys/paddles and various Morse software, for example allowing you to use a Morse key with old ms-dos Morse training software (by emulating a serial port for DOSBox), or letting you use your Morse key as a MIDI instrument (or recording to a MIDI file), or giving a low latency sidetone when using a web-based CW chatrooms. So far we don't do any of these things.

In the short term, I'd like to add support for various keying modes (Iambic A + B, Ultimatic, etc). I haven't got a paddle yet so it's just straight key (or cootie) style for now. Also would like to add some QoL features like saving your settings, volume slider, etc.

# Features:

* Treat various inputs like they're a straight key. Beeps when you:

  * Mouse: Click the button
  * Keyboard: Press the `ctrl` key or `[`
  * MIDI instrument: Press any button on a selected MIDI device
  * Other inputs: please ask.

# Audio devices:

* ASIO support (recommended if available). Supports the various Windows audio output APIs, including ASIO.
* Choose latency for audio drivers (WaveOut / DirectSound / WASAPI)
* Automatically chooses the lowest sample rate for the device (Minimum: 8,000 samples/second). (TODO: preferred sample rate option)

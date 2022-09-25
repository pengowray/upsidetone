# UpSidetone 

by Pengo Wray [AA9GO]

Low latency morse code sounder / oscillator / "keyer" / straight key beeper.

UpSidetone was created so I could practice keying morse code with a straight key connected up to my Windows PC (via a cheap modified USB mouse). The morse key clicks the mouse button. The mouse clicks the button. UpSidetone beeps when the button is pressed. It all happens instantly.

Default audio settings typically have too much latency, but this app gives you ways to reduce the latency by finding a better driver, choosing a low sample rate, and reducing the latency setting (aka buffer size). For me, this is enough to eleminate the need for an expensive radio or ugly sounding practice ossilator. It works with my hardware but your mileage may vary.

It's very basic, but it works for me, and it's now at a point where even people other than me can use it.

The vision for UpSidetone is (apart from making beeps) to bridge physical morse keys/paddles and various Morse software, for example allowing you to use a Morse key with old ms-dos Morse training software (by emulating a serial port for DOSBox), or letting you use your Morse key as a MIDI instrument or save to a MIDI file, or giving a low latency sidetone when using a web-based CW chatrooms. So far we don't do any of these things.

In the short term, I'd like to add support for various keying modes (Iambic A + B, Ultimatic, etc). I haven't got a paddle yet so it's just straight key (or cootie) style for now. Also would like to add some QoL features like saving your settings. (Right now it resets each run).

Features:

* Treat various inputs like they're a straight key. Beeps when you:

  * Mouse: Click the button
  * Keyboard: Press the `ctrl` key or `[`
  * MIDI instrument: Press any button on a selected MIDI device
  * Other inputs: please ask.

Audio devices:

* ASIO support (recommended if available). Supports the various Windows audio output APIs, including ASIO.
* Choose latency for WaveOut/WASAPI/DirectSound.
* Automatically chooses the lowest sample rate for the device. (TODO: preferred sample rate option)


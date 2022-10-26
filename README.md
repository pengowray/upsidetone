# upSidetone 

by Pengo Wray [AA9GO]

Low latency morse code keyer app for PC

![image](https://user-images.githubusercontent.com/800133/197386299-15112019-1427-43cb-86a3-157cfb72e086.png)

# Features:

* Treat various inputs like they're a straight key. Beeps when you:

  * Mouse: Click the button, or choose a mouse to use in the background
  * Keyboard (foreground only): left and right ctrl keys; \[ and \]; Z and X; left and right arrows
  * MIDI instrument (works in background): Press any midi key on a selected MIDI device (every second key is a dash)
  * Serial port: Only tested with a straight key (let me know if it works with your paddle)
  * Other inputs: feel free to ask
  * Check "Flip left/right" if your double paddles are wired backwards etc

* Keyer modes:

  * Straight key (cootie / bug / oscillator)
  * Iambic A
  * Iambic B
  * Hybrid — machine-spaced dits and human controlled dashes. Mix and match them with squeeze insertion; a two-paddle semi-automatic bug-alike mode.
  * Ultimatic
  * No Repeats

# Audio devices:

* ASIO or WASAPI recommended. Reduce WASAPI latency with the options button.
* ASIO support. Supports the various Windows audio output APIs, including ASIO.
* Choose latency / audio buffer size (WASAPI / DirectSound / MME supported)
* Automatically chooses the lowest sample rate for the device (Minimum: 8,000 samples/second) to reduce xruns.

# Uses

* Practice morse code keying with a straight key
* Send Morse code over Zoom or Discord to your CW Club [either: 1. Share the app, or 2. Configure VoiceMeeter Banana to mix upSidetone's audio with your mic]
* Test latency of your audio devices (e.g. use a recording device to measure the time from mouse click to beep)
* Start of a general purpose utility, CW hardware and software.

# Motivation(s)

There were a number of reasons to create upSidetone. The most basic was to practice keying morse code, which seemed like something that ought to be possible with minimal audio latency, and without purchasing any obsolete hardware (Why hunt for some specific legacy serial port adapter and pay shipping to Australia when you can modify a $5 USB mouse by gluing a headphone jack to it?). Apart from serving as a proof-of-concept (to show a low latency PC-generated sidetone is possible in 2022), it also let me try creating my ideal CW keying mode ("cyborg"), get my head around how Iambic A and B keying modes work more precisely than I ever cared to think about, and learn the names of all the pins on those legacy serial ports. I also did not want to make this another project that goes silent with its author, so I've open sourced it with a permissive license. 

It's basic, but it's now at a point where people other than me might also find a use for it.

Default PC audio typically has too much latency to comfortably listen to your own Morse code as you send it, but this app gives you ways to reduce the latency: choose a better driver (ASIO or WASAPI recommended), choosing a low sample rate (automatic), and reducing the latency setting aka audio buffer (click the Options or ASIO button). This is plenty to eleminate the need for an expensive radio or ugly sounding practice oscillator. I've only tested with my own hardware, so your mileage may vary. If you do a bunch of testing on various hardware, please share your results.

# Future plans

The longer term vision for UpSidetone is as a general purpose utility to bridge physical morse keys/paddles and various Morse software, for example allowing you to use a Morse key with old ms-dos Morse training software (by emulating a serial port for DOSBox), make recordings, do analysis, or giving a low latency sidetone when using a web-based CW chatrooms.

TODO:

* Redo UI
* Preferred sample rate option
* Save settings
* Much more in my notes

Releases:

* https://github.com/pengowray/upsidetone/releases

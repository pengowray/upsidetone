# upSidetone 

by Pengo Wray [AA9GO]

Low latency morse code keyer app for PC

![image](https://user-images.githubusercontent.com/800133/198856516-db0efeaa-28b7-457c-86a5-469725bc0c24.png)

# Features:

* Treat various inputs like they're a straight key. Beeps when you:

  * Mouse: Click the button, or choose a mouse to use in the background
  * Keyboard (foreground only): left and right ctrl keys; \[ and \]; Z and X; left and right arrows
  * MIDI instrument (works in background): Press any midi key on a selected MIDI device (every second key is a dash)
  * Serial port
  * Other inputs: feel free to ask
  * Check "Flip left/right" if your double paddles are wired backwards etc

* Keyer modes:

  * Straight key (cootie / bug / oscillator)
  * Iambic A
  * Iambic B
  * Hybrid — machine-spaced dits and human controlled dashes. Mix and match them with squeeze insertion; a two-paddle semi-automatic bug-alike mode.
  * Ultimatic
  * No Repeats

"Virtual paddles pass-thru":

* Pass your paddle inputs on to another app. If another program uses a serial port for paddles, you can pass input on from upSidetone. To create a pair of loopback serial ports, use [com0com](https://sourceforge.net/projects/com0com/) or similar software.

# Audio devices:

* ASIO or WASAPI recommended. Reduce WASAPI latency with the options button.
* ASIO support. Supports the various Windows audio output APIs, including ASIO.
* Choose latency / audio buffer size (WASAPI / DirectSound / MME supported)
* Automatically chooses the lowest sample rate for the device (Minimum: 8,000 samples/second) to reduce xruns.

# Uses

* Practice morse code keying with a straight key
* Send Morse code over Zoom or Discord to your CW Club [either: 1. Share the app, or 2. Configure VoiceMeeter Banana to mix upSidetone's audio with your mic]
* Test latency of your audio devices (e.g. use a recording device to measure the time from mouse click to beep)
* Adapter: Convert straight key or paddle input from your choice of input device to a virtual serial port for use by other software
* Proof of concept and start of a general purpose utility. This software is still in early stages and may change significantly in future versions.

# Motivation(s)

There were a number of reasons to create upSidetone. The most basic was to practice keying morse code, which seemed like something that ought to be possible with a straight key (or paddles) and a PC. There's a couple of problems: 1. Most software seems to create a small but noticable delay between pressing the key and hearing a sound. Minimal audio latency is important for Morse code. 2. I didn't want to purchase any specialized or obsolete hardware. A cheap USB mouse has three buttons, why pay 10x more for a special serial-paddle adapter? Not to mention shipping to Australia from the US is slow and expensive. So I soldered a headphone jack to a mouse, got the wires the wrong way around, and went about making this software with a "Flip inputs" checkbox so that I wouldn't have to resolder the connections. 

Apart from serving as a proof-of-concept (to show a low latency PC-generated sidetone is, yes, possible in 2022), it also let me try creating my ideal CW keying mode ("hybrid"), get my head around how Iambic A and B keying modes work more precisely than I ever cared to think about, and learn the names of all the pins on those legacy serial ports that I decided to support anyway. I also did not want to make this yet another project that goes silent with its author, so I've open sourced it with a permissive license. 

It's basic, it's not finished, but it's now at a point where people other than me might also find a use for it.

Default PC audio typically has too much latency to comfortably listen to your own Morse code as you send it, but this app gives you multiple ways to reduce the latency: choose tghe best driver available (ASIO or WASAPI recommended), choosing a low sample rate (currerntly automatic), and reducing the latency setting aka audio buffer (click the Options or ASIO button and see how long you can go). This is enough to let you practice Morse code without the need for an expensive radio's sidetone or an ugly sounding practice oscillator. I've only tested with my own hardware, so your mileage may vary. If you do a bunch of testing on various hardware, please share your results.

# Future ideas

The longer term vision for UpSidetone is as a general purpose utility. Some possiblities:

1. (part done) bridge physical morse keys/paddles and various Morse software, for example allowing you to use a Morse key with old ms-dos Morse training software (by emulating a serial port for DOSBox) — 
2. (todo) allow presets, recordings + playback, analysis, visual feedback
3. (todo) give a low latency sidetone when using a web-based CW chatrooms.
4. (todo) Legacy modes: Attempt to emulate exactly a Curtis 8044 series keyer chip and an Accu-keyer circuit (and variations)
5. (todo) Make cross platform
7. (todo) Rework the UI to be more intuitive. e.g: let the user separately add and configure: inputs (paddles); outputs (audio, ports, keyer); and keyer/oscillator presets (tone, weights, envelope, etc)
8. (todo) Extensive help

More immediate TODO:

* Save settings
* Move audio handling in its own thread 
* Preferred sample rate setting
* Exclsuive mode audio
* Much more in my notes

Download:

* https://github.com/pengowray/upsidetone/releases

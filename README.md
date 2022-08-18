# WindowsAudioFade

[日本語はこちら](./README_jp.md)

A program that crossfades the background music I used at an osu! tournaments with the audio from the osu! client.

## Supplement

This program is very confusing to use because I am making it for own use.

Only osu! and vlc are supported as fade destinations. If there is a high demand and you want to use it with other software, etc. (flexibility is needed), I will create it.

## Installation

.Net Framework 4.7.2(Windows 10 April 2018 Update (version 1803) installed as standard) is required to run.

### Latest build: [Windows(x86)](https://github.com/Fairy-Phy/WindowsAudioFade/releases/download/1.0.0/WindowsAudioFadev1.0.0.zip)

Unzip the zip file and open the exe file in it to run it.

## Usage

The basic is CLI text input format. (It was not originaly made for public use...)

### 1, Specify the sound device to fading

The up and down arrow keys can be used to select the device, and press Enter to confirm the selection. If the device configuration has changed, press "Device Update" to update the device list.

### 1.5, After checking whether the process exists on the target device, run to a volume change test

### 2, Enter mode

Basically, they exist from 0 to 3 and can be used by executing them in sequence.

|Mode|Volume|
|-|-|
|0|vlc: 34→11|
|1|vlc: 11→0, osu: 0->100|
|2|vlc: 0→11, osu: 100->0|
|3|vlc: 11→34|
|exit|Close program|

## License

This source code and software are licensed under the MIT License.

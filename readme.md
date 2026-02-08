# Pixelator
![icon](/img/pixelator_large.png)

Pixelator is a Paint.NET plugin enhancement to reduce colour depth and pixelate images, you can use to create artwork and game assets. It enhances the built-in pixelate effect in Paint.NET with several extra features as shown in the examples below.

The output's colour palette be auto-generated, or it can use a fixed retro palette like MSDOS or Gameboy. 

The plugin was written by DATB (not using AI!) and compiled in CodeLab.

https://github.com/user-attachments/assets/955648c6-5659-462f-b058-fa2df281d82a

## How to install and use Pixelator

1. Download and unzip **pixelator.zip**
1. Close Paint.NET
1. Right-click **Install_Pixelator.bat**, and choose **Run as administrator**.
1. Open an image in Paint.NET
1. Choose **Effects > Distort > Pixelator**

## Options

- **Flatten** Applies a bilateral surface blur to reduce noise and produce cleaner result.

- **Pixelate** Reduce the pixel count by a percentage, default 25%.

- **Antialias** Use bicubic resampling to soften colour differences.

-  **Resample** Nudge the pixelation image sampling, as it's sensitive to the sampling reference point.

- **Colour palette** Choice of wplace, 8 colour, 27 colour, MSDOS, Commodore 64, RISC OS, Auto (smart/balanced).

- **Number of colours** For auto-generated palettes

- **Brightness / Contrast**

- **Dither style** None, Normal (retro style) and Photo (Floyd-Steinberg 4x4 grid)

![Image](/img/pixelator_interface.png)

# Examples

|Palettes            ||||
|---|---|---|---|
|Original |![img](/img/hibiscus.png)|![img](/img/rabbits.png)|![img](/img/laroux.png)|
|Wplace|![img](/img/hibiscus1.png)|![img](/img/rabbits1.png)|![img](/img/laroux1.png)|
|Gameboy |![img](/img/hibiscus2.png)|![img](/img/rabbits2.png)|![img](/img/laroux2.png)|
|8 colours |![img](/img/hibiscus3.png)|![img](/img/rabbits3.png)|![img](/img/laroux3.png)|
|27 colours |![img](/img/hibiscus4.png)|![img](/img/rabbits4.png)|![img](/img/laroux4.png)|
|MSDOS |![img](/img/hibiscus5.png)|![img](/img/rabbits5.png)|![img](/img/laroux5.png)|
|Commodore 64|![img](/img/hibiscus6.png)|![img](/img/rabbits6.png)|![img](/img/laroux6.png)|
|RISC OS |![img](/img/hibiscus7.png)|![img](/img/rabbits7.png)|![img](/img/laroux7.png)|
|Auto (smart), 6 colours |![img](/img/hibiscus8.png)|![img](/img/rabbits8.png)|![img](/img/laroux8.png)|
|Auto (balanced), 6 colours |![img](/img/hibiscus9.png)|![img](/img/rabbits9.png)|![img](/img/laroux9.png)|
|**Effects**|||
|Flatten, wplace palette, normal dither |![img](/img/hibiscus10.png)|![img](/img/rabbits10.png)|![img](/img/laroux10.png)|
|Flatten, 8 colours, normal dither |![img](/img/hibiscus11.png)|![img](/img/rabbits11.png)|![img](/img/laroux11.png)|
|No dither |![img](/img/hibiscus12.png)|![img](/img/rabbits12.png)|![img](/img/laroux12.png)|
|Photo dither |![img](/img/hibiscus13.png)|![img](/img/rabbits13.png)|![img](/img/laroux13.png)|
|Pixelate |![img](/img/hibiscus14.png)|![img](/img/rabbits14.png)|![img](/img/laroux14.png)|

## How it works

All Paint.NET plugins implement `Prerender` and `Render` functions which process a source image into a destination image. `Prerender` is used when combining multiple images, which is not needed here. The effect plugin has a UI which is defined using special comments in the code. 

Pixelator defines each pixel using BRGA channels: blue, green, red, alpha; where the transparent alpha channel is duplicated from the source unchanged.

Colours are simplified using a euclidean mean comparison of each pixel with the target palette. Dithering is then achieved by finding the remainder between the source pixel's RGB values and the selected palette colour's RGB values, and spreading that difference across nearby pixels in a regular pattern.

The colours for auto-generated palettes are selected using either a _k-means_ (smart) or _median cut_ (balanced) algorithm:

- "K-means" is an iterative method that refines groupings of similar pixels - this is better at capturing extreme values and usually produces a better result, although it's more computationally intensive and sensitive to the initial conditions.

- "Median cut" sorts all pixels by their overall brightness (luminosity) and evenly selects the required number of colours from the list. It's less intensive, but the results are usually blander.

### Pls get in touch with any questions or requests 😊🌈

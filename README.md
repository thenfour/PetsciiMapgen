things to try:

DCT:
  all this would work better if we could do DCT and map in a similar way to JPEG.
  but afaik, DCT is not practical in a fragshader. BUT, maybe it's not. OR, maybe
  the DCT can be performed outside of the shader (done in C#, cells in parallel?)

  the idea then is that instead of colorants, we look at cosine frequencies. i
  imagine 5 cosine freqs would be more visually useful. so currently we default to
  Nv5+2 (7 dimensions). 5 cosine variants.

  needs a POC to do DCT in a shader, and to see how this looks otherwise.

COLOR is still fucked; just looks pretty bad (albeit it seems to "work"). needs investigation.

bitmap size is limited by memory usage
it's also limited by tooll



what it's meant to do
- post-processing effect

what does it do
- generates a map
- converts images to charset images

what it doesn't do
- perfection. it's desigened for lightweight shading, not perfect conversion

how it could be improved

how it works
- mapping via pixel format

features
- lab, yuv, hsl, yuv5
- chroma subsampling
- outputting text
- font colorkeying

topics
- exisitng works
- what works and what sucks
- characteristics of parameters
  - partitioning
    - you'll know when you partitioned too far when your blacks & whites don't look right.
    - breadth? depth?
  - pixelformat
    - 2x2 is great for small fonts.
    - 5 is good when you need the extra detail. otherwise it can look noisy.
  - value quantization
    - gradients want high valuespercomponent
    - map size
  - subsampling
  - color vs grayscale
    - grayscale has 2 fewer "dimensions", which means you have a LOT more values per component. so much more accurate.
  - colorspace
    - performance
  - src palette

@echo off
pushd %~dp0..


REM mz700 example @ YUV5
bin\Release\PetsciiMapgen.exe ^
  -outdir C:\temp ^
  -processImagesInDir "C:\root\git\thenfour\PetsciiMapgen\img\testImages" ^
  -testpalette ThreeBit ^
  -testpalette C64Color ^

  -partitions 4x8 ^
  -pf yuv5 ^
  -pfargs 40v5+0 ^

  -fonttype mono ^
  -fontImage img\fonts\mz700.png ^
  -charsize 8x8 ^
  -palette BlackAndWhite
REM  -calcn 80000000


REM mz700 example @ 2x2
REM yuv5    = 40v5+0
REM yuv2x2  = 92v2x2+0
bin\Release\PetsciiMapgen.exe ^
  -outdir C:\temp ^
  -processImagesInDir "C:\root\git\thenfour\PetsciiMapgen\img\testImages" ^

  -partitions 4x8 ^
  -pf yuv ^
  -pfargs 92v2x2+0 ^

  -fonttype mono ^
  -fontImage img\fonts\mz700.png ^
  -charsize 8x8 ^
  -palette BlackAndWhite
REM  -calcn 80000000


REM mz700 example @ 1x1
bin\Release\PetsciiMapgen.exe ^
  -outdir C:\temp ^
  -processImagesInDir "C:\root\git\thenfour\PetsciiMapgen\img\testImages" ^

  -partitions 4x8 ^
  -pf yuv ^
  -pfargs 2048v1x1+0 ^

  -fonttype mono ^
  -fontImage img\fonts\mz700.png ^
  -charsize 8x8 ^
  -palette BlackAndWhite
REM  -calcn 80000000






REM C64 colored grayscale YUV5
REM versus YUV2x2 versus YUV1x1
bin\Release\PetsciiMapgen.exe ^
  -outdir C:\temp ^
  -processImagesInDir "C:\root\git\thenfour\PetsciiMapgen\img\testImages" ^
  -testpalette ThreeBit ^
  -testpalette C64Color ^

  -partitions 4x8 ^
  -pf yuv5 ^
  -pfargs 24v5+0 ^

  -fonttype mono ^
  -fontImage img\fonts\c64opt160.png ^
  -charsize 8x8 ^
  -palette C64Color
REM  -calcn 80000000

REM C64 colored grayscale YUV2x2
REM versus YUV2x2 versus YUV1x1
bin\Release\PetsciiMapgen.exe ^
  -outdir C:\temp ^
  -processImagesInDir "C:\root\git\thenfour\PetsciiMapgen\img\testImages" ^
  -testpalette ThreeBit ^
  -testpalette C64Color ^

  -partitions 4x8 ^
  -pf yuv ^
  -pfargs 52v2x2+0 ^

  -fonttype mono ^
  -fontImage img\fonts\c64opt160.png ^
  -charsize 8x8 ^
  -palette C64Color
REM  -calcn 80000000

REM C64 colored grayscale YUV1x1
bin\Release\PetsciiMapgen.exe ^
  -outdir C:\temp ^
  -processImagesInDir "C:\root\git\thenfour\PetsciiMapgen\img\testImages" ^
  -testpalette ThreeBit ^
  -testpalette C64Color ^

  -partitions 4x8 ^
  -pf yuv ^
  -pfargs 2048v1x1+0 ^

  -fonttype mono ^
  -fontImage img\fonts\c64opt160.png ^
  -charsize 8x8 ^
  -palette C64Color
REM  -calcn 80000000








REM C64 full color, yuv 1x1
bin\Release\PetsciiMapgen.exe ^
  -outdir C:\temp ^
  -processImagesInDir "C:\root\git\thenfour\PetsciiMapgen\img\testImages" ^
  -testpalette ThreeBit ^
  -testpalette C64Color ^

  -partitions 8x7 ^
  -pf yuv ^
  -pfargs 96v1x1+2 ^

  -fonttype mono ^
  -fontImage img\fonts\c64opt160.png ^
  -charsize 8x8 ^
  -palette C64Color
REM  -calcn 80000000

REM C64 full color, yuv 2x2
bin\Release\PetsciiMapgen.exe ^
  -outdir C:\temp ^
  -processImagesInDir "C:\root\git\thenfour\PetsciiMapgen\img\testImages" ^
  -testpalette ThreeBit ^
  -testpalette C64Color ^

  -partitions 3x8 ^
  -pf yuv ^
  -pfargs 12v2x2+2 ^

  -fonttype mono ^
  -fontImage img\fonts\c64opt160.png ^
  -charsize 8x8 ^
  -palette C64Color
REM  -calcn 80000000


REM C64 full color, yuv 5
bin\Release\PetsciiMapgen.exe ^
  -outdir C:\temp ^
  -processImagesInDir "C:\root\git\thenfour\PetsciiMapgen\img\testImages" ^
  -testpalette ThreeBit ^
  -testpalette C64Color ^

  -partitions 3x8 ^
  -pf yuv5 ^
  -pfargs 9v5+2 ^

  -fonttype mono ^
  -fontImage img\fonts\c64opt160.png ^
  -charsize 8x8 ^
  -palette C64Color
REM  -calcn 80000000








REM C64
REM   colorgray
REM   graygray
REM   color
REM   bw
REM mz700
REM   bw
REM topaz
REM   wb31
REM   wb13
REM vga
REM   bw
REM   gray
REM   rgbprim
REM   rgbhalftone
REM emoji
REM   12
REM   16
REM   24
REM   32
REM   64
REM mariotiles

popd


@echo off
pushd %~dp0..

REM C64 grayscale ColorGray8B
REM bin\Release\PetsciiMapgen.exe ^
REM   -outdir C:\temp ^
REM   -testpalette ThreeBit ^
REM   -testpalette C64Color ^
REM   -partitions 3x8 ^
REM   -pfargs 47v2x2+0 ^
REM   -fonttype mono ^
REM   -fontImage img\fonts\c64opt160.png ^
REM   -charsize 8x8 ^
REM   -palette C64ColorGray8B
REM -calcn 75000000

REM C64 grayscale ColorGray8B
::bin\Release\PetsciiMapgen.exe ^
::  -outdir C:\temp ^
::  -processImagesInDir "C:\root\git\thenfour\PetsciiMapgen\img\testImages" ^
::  -testpalette ThreeBit ^
::  -testpalette C64Color ^

::  -partitions 4x8 ^
::  -pfargs 72v2x2+0 ^

::  -fonttype mono ^
::  -fontImage img\fonts\c64opt160.png ^
::  -charsize 8x8 ^
::  -palette C64ColorGray8B ^
REM -calcn 75000000

REM 



bin\Release\PetsciiMapgen.exe ^
  -outdir C:\temp ^
  -processImagesInDir "C:\root\git\thenfour\PetsciiMapgen\img\testImages" ^
  -testpalette ThreeBit ^
  -testpalette C64Color ^

  -partitions 4x8 ^
  -pf yuv5 ^
  -pfargs 16v5+0 ^

  -fonttype mono ^
  -fontImage img\fonts\c64opt160.png ^
  -charsize 8x8 ^
  -palette C64ColorGray8B ^



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


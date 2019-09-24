@echo off
pushd %~dp0..




REM C64 colored grayscale YUV1x1
REM versus YUV2x2 versus YUV1x1
REM this can be increased certanly. maybe even 30v
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



popd


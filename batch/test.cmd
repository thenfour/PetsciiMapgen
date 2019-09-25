@echo off
pushd %~dp0..




REM
bin\Release\PetsciiMapgen.exe ^
  -outdir C:\temp ^
  -processImagesInDir "C:\root\git\thenfour\PetsciiMapgen\img\testImages" ^
  -testpalette ThreeBit ^
  -testpalette C64Color ^

  -partitions 3x8 ^
  -pf yuv5 ^
  -pfargs 6v3x3+0 ^

  -fonttype mono ^
  -fontImage img\fonts\c64opt160.png ^
  -charsize 8x16 ^
  -palette C64ColorGray8B ^
REM  -calcn 80000000


popd


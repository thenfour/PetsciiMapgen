@echo off
pushd %~dp0..

REM C64 grayscale ColorGray8B
bin\Release\PetsciiMapgen.exe ^
  -outdir C:\temp ^
  -processImagesInDir "C:\root\git\thenfour\PetsciiMapgen\img\testImages" ^
  -testpalette ThreeBit ^
  -testpalette C64Color ^

  -partitions 4x8 ^
  -pfargs 8v3x3+0 ^

  -fonttype mono ^
  -fontImage img\fonts\c64opt160.png ^
  -charsize 8x8 ^
  -palette C64ColorGray8B
REM  -calcn 80000000



popd


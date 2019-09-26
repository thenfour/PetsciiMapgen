@echo off
pushd %~dp0..




REM Emoji 12, color
REM palettes: BlackAndWhite, Gray3, Gray4, Gray5, ThreeBit, RGBPrimariesHalftone16
bin\Release\PetsciiMapgen.exe ^
  -outdir C:\temp ^
  -processImagesInDir "C:\root\git\thenfour\PetsciiMapgen\img\testImages" ^
  -testpalette ThreeBit ^

  -partitions 1x1 ^
  -pf yuv5 ^
  -pfargs 6v5+2 ^

  -fonttype normal ^
  -fontImage "C:\root\git\thenfour\PetsciiMapgen\img\fonts\emojidark12.png" ^
  -charsize 12x12 
REM  -calcn 80000000



popd


@echo off
pushd %~dp0..


REM mz700 black and white YUV5
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


REM mz700 black and white 2x2
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



REM C64 grayscale via C64gray8A, B, grays i forgot to add here.






REM TOPAZ WB1.3 grayscale
bin\Release\PetsciiMapgen.exe ^
  -outdir C:\temp ^
  -processImagesInDir "C:\root\git\thenfour\PetsciiMapgen\img\testImages" ^
  -testpalette ThreeBit ^

  -partitions 4x8 ^
  -pf yuv5 ^
  -pfargs 32v5+0 ^

  -fonttype mono ^
  -fontImage img\fonts\topaz96.gif ^
  -charsize 8x16 ^
  -palette Workbench134 
REM  -calcn 80000000


REM TOPAZ WB3.1 grayscale
bin\Release\PetsciiMapgen.exe ^
  -outdir C:\temp ^
  -processImagesInDir "C:\root\git\thenfour\PetsciiMapgen\img\testImages" ^
  -testpalette ThreeBit ^

  -partitions 4x8 ^
  -pf yuv5 ^
  -pfargs 32v5+0 ^

  -fonttype mono ^
  -fontImage img\fonts\topaz96.gif ^
  -charsize 8x16 ^
  -palette Workbench314 
REM  -calcn 80000000






REM VGA GRAYSCALES: (between N=36 and 24 or so)

REM palettes: BlackAndWhite, Gray3, Gray4, Gray5, ThreeBit, RGBPrimariesHalftone16
bin\Release\PetsciiMapgen.exe ^
  -outdir C:\temp ^
  -processImagesInDir "C:\root\git\thenfour\PetsciiMapgen\img\testImages" ^
  -testpalette ThreeBit ^

  -partitions 4x8 ^
  -pf yuv5 ^
  -pfargs 24v5+0 ^

  -fonttype mono ^
  -fontImage img\fonts\VGA240.png ^
  -charsize 8x16 ^
  -palette ThreeBit
REM  -calcn 80000000


REM VGA palettes: BlackAndWhite, Gray3, Gray4, Gray5, ThreeBit, RGBPrimariesHalftone16
bin\Release\PetsciiMapgen.exe ^
  -outdir C:\temp ^
  -processImagesInDir "C:\root\git\thenfour\PetsciiMapgen\img\testImages" ^
  -testpalette ThreeBit ^

  -partitions 4x8 ^
  -pf yuv5 ^
  -pfargs 20v5+0 ^

  -fonttype mono ^
  -fontImage img\fonts\VGA240.png ^
  -charsize 8x16 ^
  -palette RGBPrimariesHalftone16 
REM  -calcn 80000000



REM VGA palettes: BlackAndWhite, Gray3, Gray4, Gray5, ThreeBit, RGBPrimariesHalftone16
bin\Release\PetsciiMapgen.exe ^
  -outdir C:\temp ^
  -processImagesInDir "C:\root\git\thenfour\PetsciiMapgen\img\testImages" ^
  -testpalette ThreeBit ^

  -partitions 4x8 ^
  -pf yuv ^
  -pfargs 40v2x2+0 ^

  -fonttype mono ^
  -fontImage img\fonts\VGA240.png ^
  -charsize 8x16 ^
  -palette RGBPrimariesHalftone16 
REM  -calcn 80000000





REM VGA color!

REM VGA palettes: BlackAndWhite, Gray3, Gray4, Gray5, ThreeBit, RGBPrimariesHalftone16
bin\Release\PetsciiMapgen.exe ^
  -outdir C:\temp ^
  -processImagesInDir "C:\root\git\thenfour\PetsciiMapgen\img\testImages" ^
  -testpalette ThreeBit ^

  -partitions 3x8 ^
  -pf yuv5 ^
  -pfargs 9v5+2 ^

  -fonttype mono ^
  -fontImage img\fonts\VGA240.png ^
  -charsize 8x16 ^
  -palette RGBPrimariesHalftone16 
REM  -calcn 80000000

REM VGA palettes: BlackAndWhite, Gray3, Gray4, Gray5, ThreeBit, RGBPrimariesHalftone16
bin\Release\PetsciiMapgen.exe ^
  -outdir C:\temp ^
  -processImagesInDir "C:\root\git\thenfour\PetsciiMapgen\img\testImages" ^
  -testpalette ThreeBit ^

  -partitions 3x8 ^
  -pf yuv ^
  -pfargs 12v2x2+2 ^

  -fonttype mono ^
  -fontImage img\fonts\VGA240.png ^
  -charsize 8x16 ^
  -palette RGBPrimariesHalftone16 
REM  -calcn 80000000







REM VGA boxdrawing grayscale
REM palettes: BlackAndWhite, Gray3, Gray4, Gray5, ThreeBit, RGBPrimariesHalftone16
bin\Release\PetsciiMapgen.exe ^
  -outdir C:\temp ^
  -processImagesInDir "C:\root\git\thenfour\PetsciiMapgen\img\testImages" ^
  -testpalette ThreeBit ^

  -partitions 3x8 ^
  -pf yuv5 ^
  -pfargs 33v5+0 ^

  -fonttype mono ^
  -fontImage img\fonts\VGAboxonly45.png ^
  -charsize 8x16 ^
  -palette Gray8 
REM  -calcn 80000000

REM VGA boxdrawing grayscale
REM palettes: BlackAndWhite, Gray3, Gray4, Gray5, ThreeBit, RGBPrimariesHalftone16
bin\Release\PetsciiMapgen.exe ^
  -outdir C:\temp ^
  -processImagesInDir "C:\root\git\thenfour\PetsciiMapgen\img\testImages" ^
  -testpalette ThreeBit ^

  -partitions 3x8 ^
  -pf yuv ^
  -pfargs 66v2x2+0 ^

  -fonttype mono ^
  -fontImage img\fonts\VGAboxonly45.png ^
  -charsize 8x16 ^
  -palette Gray8 
REM  -calcn 80000000




REM VGA boxdrawing color
REM palettes: BlackAndWhite, Gray3, Gray4, Gray5, ThreeBit, RGBPrimariesHalftone16
bin\Release\PetsciiMapgen.exe ^
  -outdir C:\temp ^
  -processImagesInDir "C:\root\git\thenfour\PetsciiMapgen\img\testImages" ^
  -testpalette ThreeBit ^

  -partitions 3x8 ^
  -pf yuv5 ^
  -pfargs 9v5+2 ^

  -fonttype mono ^
  -fontImage img\fonts\VGAboxonly45.png ^
  -charsize 8x16 ^
  -palette RGBPrimariesHalftone16 
REM  -calcn 80000000



REM VGA boxdrawing color
REM palettes: BlackAndWhite, Gray3, Gray4, Gray5, ThreeBit, RGBPrimariesHalftone16
bin\Release\PetsciiMapgen.exe ^
  -outdir C:\temp ^
  -processImagesInDir "C:\root\git\thenfour\PetsciiMapgen\img\testImages" ^
  -testpalette ThreeBit ^

  -partitions 3x8 ^
  -pf yuv ^
  -pfargs 12v2x2+2 ^

  -fonttype mono ^
  -fontImage img\fonts\VGAboxonly45.png ^
  -charsize 8x16 ^
  -palette RGBPrimariesHalftone16 
REM  -calcn 80000000

REM VGA boxdrawing color
REM palettes: BlackAndWhite, Gray3, Gray4, Gray5, ThreeBit, RGBPrimariesHalftone16
bin\Release\PetsciiMapgen.exe ^
  -outdir C:\temp ^
  -processImagesInDir "C:\root\git\thenfour\PetsciiMapgen\img\testImages" ^
  -testpalette ThreeBit ^

  -partitions 3x8 ^
  -pf yuv ^
  -pfargs 128v1x1+2 ^

  -fonttype mono ^
  -fontImage img\fonts\VGAboxonly45.png ^
  -charsize 8x16 ^
  -palette RGBPrimariesHalftone16 
REM  -calcn 80000000







REM SMB3 tileset, color, 1x1
REM palettes: BlackAndWhite, Gray3, Gray4, Gray5, ThreeBit, RGBPrimariesHalftone16
bin\Release\PetsciiMapgen.exe ^
  -outdir C:\temp ^
  -processImagesInDir "C:\root\git\thenfour\PetsciiMapgen\img\testImages" ^
  -testpalette ThreeBit ^

  -partitions 3x8 ^
  -pf yuv ^
  -pfargs 128v1x1+2 ^

  -fonttype colorkey ^
  -fontImage img\fonts\mariotiles4.png ^
  -colorkey #04c1aa ^
  -lefttoppadding 1 ^
  -charsize 16x16 ^
  -palette MarioBg 
REM  -calcn 80000000

REM SMB3 tileset, color, 2x2
REM palettes: BlackAndWhite, Gray3, Gray4, Gray5, ThreeBit, RGBPrimariesHalftone16
bin\Release\PetsciiMapgen.exe ^
  -outdir C:\temp ^
  -processImagesInDir "C:\root\git\thenfour\PetsciiMapgen\img\testImages" ^
  -testpalette ThreeBit ^

  -partitions 3x8 ^
  -pf yuv ^
  -pfargs 18v2x2+2 ^

  -fonttype colorkey ^
  -fontImage img\fonts\mariotiles4.png ^
  -colorkey #04c1aa ^
  -lefttoppadding 1 ^
  -charsize 16x16 ^
  -palette MarioBg 
REM  -calcn 80000000

REM SMB3 tileset, color, yuv5
REM palettes: BlackAndWhite, Gray3, Gray4, Gray5, ThreeBit, RGBPrimariesHalftone16
bin\Release\PetsciiMapgen.exe ^
  -outdir C:\temp ^
  -processImagesInDir "C:\root\git\thenfour\PetsciiMapgen\img\testImages" ^
  -testpalette ThreeBit ^

  -partitions 3x8 ^
  -pf yuv5 ^
  -pfargs 9v5+2 ^

  -fonttype colorkey ^
  -fontImage img\fonts\mariotiles4.png ^
  -colorkey #04c1aa ^
  -lefttoppadding 1 ^
  -charsize 16x16 ^
  -palette MarioBg 
REM  -calcn 80000000

REM SMB3 tileset, grayscale
REM palettes: BlackAndWhite, Gray3, Gray4, Gray5, ThreeBit, RGBPrimariesHalftone16
bin\Release\PetsciiMapgen.exe ^
  -outdir C:\temp ^
  -processImagesInDir "C:\root\git\thenfour\PetsciiMapgen\img\testImages" ^
  -testpalette ThreeBit ^

  -partitions 3x8 ^
  -pf yuv5 ^
  -pfargs 73v5+0 ^

  -fonttype colorkey ^
  -fontImage img\fonts\mariotiles4.png ^
  -colorkey #04c1aa ^
  -lefttoppadding 1 ^
  -charsize 16x16 ^
  -palette MarioBg 
REM  -calcn 80000000

REM SMB3 tileset, grayscale
REM palettes: BlackAndWhite, Gray3, Gray4, Gray5, ThreeBit, RGBPrimariesHalftone16
bin\Release\PetsciiMapgen.exe ^
  -outdir C:\temp ^
  -processImagesInDir "C:\root\git\thenfour\PetsciiMapgen\img\testImages" ^
  -testpalette ThreeBit ^

  -partitions 3x8 ^
  -pf yuv ^
  -pfargs 72v2x2+0 ^

  -fonttype colorkey ^
  -fontImage img\fonts\mariotiles4.png ^
  -colorkey #04c1aa ^
  -lefttoppadding 1 ^
  -charsize 16x16 ^
  -palette MarioBg 
REM  -calcn 80000000

REM SMB3 tileset, grayscale
REM palettes: BlackAndWhite, Gray3, Gray4, Gray5, ThreeBit, RGBPrimariesHalftone16
bin\Release\PetsciiMapgen.exe ^
  -outdir C:\temp ^
  -processImagesInDir "C:\root\git\thenfour\PetsciiMapgen\img\testImages" ^
  -testpalette ThreeBit ^

  -partitions 4x8 ^
  -pf yuv ^
  -pfargs 16384v1x1+0 ^

  -fonttype colorkey ^
  -fontImage img\fonts\mariotiles4.png ^
  -colorkey #04c1aa ^
  -lefttoppadding 1 ^
  -charsize 16x16 ^
  -palette MarioBg 
REM  -calcn 80000000





REM Emoji 12, grayscale
REM palettes: BlackAndWhite, Gray3, Gray4, Gray5, ThreeBit, RGBPrimariesHalftone16
bin\Release\PetsciiMapgen.exe ^
  -outdir C:\temp ^
  -processImagesInDir "C:\root\git\thenfour\PetsciiMapgen\img\testImages" ^
  -testpalette ThreeBit ^

  -partitions 4x8 ^
  -pf yuv ^
  -pfargs 16384v1x1+0 ^

  -fonttype normal ^
  -fontImage img\fonts\emojidark12.png ^
  -charsize 12x12 
REM  -calcn 80000000

REM Emoji 12, grayscale
REM palettes: BlackAndWhite, Gray3, Gray4, Gray5, ThreeBit, RGBPrimariesHalftone16
bin\Release\PetsciiMapgen.exe ^
  -outdir C:\temp ^
  -processImagesInDir "C:\root\git\thenfour\PetsciiMapgen\img\testImages" ^
  -testpalette ThreeBit ^

  -partitions 4x8 ^
  -pf yuv ^
  -pfargs 72v2x2+0 ^

  -fonttype normal ^
  -fontImage img\fonts\emojidark12.png ^
  -charsize 12x12 
REM  -calcn 80000000


REM Emoji 12, grayscale
REM palettes: BlackAndWhite, Gray3, Gray4, Gray5, ThreeBit, RGBPrimariesHalftone16
bin\Release\PetsciiMapgen.exe ^
  -outdir C:\temp ^
  -processImagesInDir "C:\root\git\thenfour\PetsciiMapgen\img\testImages" ^
  -testpalette ThreeBit ^

  -partitions 4x8 ^
  -pf yuv5 ^
  -pfargs 28v5+0 ^

  -fonttype normal ^
  -fontImage img\fonts\emojidark12.png ^
  -charsize 12x12 
REM  -calcn 80000000




REM Emoji 12, color
REM palettes: BlackAndWhite, Gray3, Gray4, Gray5, ThreeBit, RGBPrimariesHalftone16
bin\Release\PetsciiMapgen.exe ^
  -outdir C:\temp ^
  -processImagesInDir "C:\root\git\thenfour\PetsciiMapgen\img\testImages" ^
  -testpalette ThreeBit ^

  -partitions 3x8 ^
  -pf yuv5 ^
  -pfargs 12v5+2 ^

  -fonttype normal ^
  -fontImage img\fonts\emojidark12.png ^
  -charsize 12x12 
REM  -calcn 80000000

REM Emoji 12, color
REM palettes: BlackAndWhite, Gray3, Gray4, Gray5, ThreeBit, RGBPrimariesHalftone16
bin\Release\PetsciiMapgen.exe ^
  -outdir C:\temp ^
  -processImagesInDir "C:\root\git\thenfour\PetsciiMapgen\img\testImages" ^
  -testpalette ThreeBit ^

  -partitions 3x8 ^
  -pf yuv ^
  -pfargs 15v2x2+2 ^

  -fonttype normal ^
  -fontImage img\fonts\emojidark12.png ^
  -charsize 12x12 
REM  -calcn 80000000

REM Emoji 12, color
REM palettes: BlackAndWhite, Gray3, Gray4, Gray5, ThreeBit, RGBPrimariesHalftone16
bin\Release\PetsciiMapgen.exe ^
  -outdir C:\temp ^
  -processImagesInDir "C:\root\git\thenfour\PetsciiMapgen\img\testImages" ^
  -testpalette ThreeBit ^

  -partitions 2x8 ^
  -pf yuv ^
  -pfargs 180v1x1+2 ^

  -fonttype normal ^
  -fontImage img\fonts\emojidark12.png ^
  -charsize 12x12 
REM  -calcn 80000000









REM emoji
REM   12
REM   16
REM   24
REM   32
REM   64
REM comicsans
REM unicode dingbats
REM unicode box drawing
REM unicode math symbols

popd

